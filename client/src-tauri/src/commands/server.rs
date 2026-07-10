//! 网络伺服 Tauri 命令
//!
//! 提供前端调用的命令：启动/停止/查询 HTTP 伺服状态。
//! 学习 SiYuan 的设计，在原生应用内启动 HTTP 服务器，
//! 让局域网内其他设备可通过浏览器访问。

use crate::server::{self, ServerState};
use parking_lot::RwLock;
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::{Manager, State};

use crate::state::AppState;

#[derive(Debug, Serialize, Deserialize)]
pub struct ServerStatus {
    pub running: bool,
    pub port: u16,
    pub url: Option<String>,
}

/// 启动网络伺服
///
/// 启动后，局域网内其他设备可通过浏览器访问应用的 Web 界面。
/// 绑定 0.0.0.0，允许外部访问。
#[tauri::command]
pub async fn server_start(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<ServerStatus, String> {
    let app_handle = {
        let guard = state.read();
        guard
            .app_handle
            .get()
            .ok_or("应用未初始化")?
            .clone()
    };

    // 前端 dist 目录：Tauri 开发模式用 frontendDist，生产模式用资源目录
    let frontend_dir = app_handle
        .path()
        .resource_dir()
        .map_err(|e| format!("资源目录获取失败: {}", e))?
        .join("frontend");

    // 如果资源目录不存在，尝试开发模式路径
    let frontend_dir = if frontend_dir.exists() {
        frontend_dir
    } else {
        // Tauri 2 dev 模式：frontendDist 在项目根目录
        let dev_dir = std::env::current_dir()
            .unwrap_or_default()
            .parent()
            .map(|p| p.join("dist"))
            .unwrap_or_default();
        if dev_dir.exists() {
            dev_dir
        } else {
            return Err("前端构建产物目录不存在，请先执行 pnpm build".to_string());
        }
    };

    // 从设置中读取端口和网络伺服配置
    let port = 6806u16; // 默认端口
    let network_serve = true; // 网络伺服模式始终绑定 0.0.0.0

    // 获取或创建 ServerState
    let global_state = SERVER_STATE.get_or_init(|| Arc::new(RwLock::new(ServerState::new())));
    let server_state = {
        let guard = global_state.read();
        guard.clone()
    };

    let actual_port = server::serve(server_state.clone(), frontend_dir, port, network_serve).await?;

    // 获取本机 IP 用于显示
    let local_ip = get_local_ip().unwrap_or_else(|| "127.0.0.1".to_string());

    Ok(ServerStatus {
        running: true,
        port: actual_port,
        url: Some(format!("http://{}:{}", local_ip, actual_port)),
    })
}

/// 停止网络伺服
#[tauri::command]
pub async fn server_stop() -> Result<ServerStatus, String> {
    let global_state = match SERVER_STATE.get() {
        Some(s) => s,
        None => {
            return Ok(ServerStatus {
                running: false,
                port: 0,
                url: None,
            });
        }
    };

    let server_state = {
        let guard = global_state.read();
        guard.clone()
    };

    server::shutdown(&server_state).await?;

    Ok(ServerStatus {
        running: false,
        port: 0,
        url: None,
    })
}

/// 查询网络伺服状态
#[tauri::command]
pub async fn server_status() -> Result<ServerStatus, String> {
    let global_state = match SERVER_STATE.get() {
        Some(s) => s,
        None => {
            return Ok(ServerStatus {
                running: false,
                port: 0,
                url: None,
            });
        }
    };

    let (running, port) = {
        let guard = global_state.read();
        let running = guard.is_running();
        let port = guard.get_port();
        (running, port)
    };

    let url = if running {
        let local_ip = get_local_ip().unwrap_or_else(|| "127.0.0.1".to_string());
        Some(format!("http://{}:{}", local_ip, port))
    } else {
        None
    };

    Ok(ServerStatus {
        running,
        port: if running { port } else { 0 },
        url,
    })
}

/// 全局 ServerState（懒加载单例）
static SERVER_STATE: std::sync::OnceLock<Arc<RwLock<ServerState>>> = std::sync::OnceLock::new();

/// 获取本机局域网 IP 地址
fn get_local_ip() -> Option<String> {
    use std::net::UdpSocket;
    // 通过连接一个外部地址来获取本机 IP（不会真正发送数据）
    let socket = UdpSocket::bind("0.0.0.0:0").ok()?;
    socket.connect("8.8.8.8:80").ok()?;
    let addr = socket.local_addr().ok()?;
    Some(addr.ip().to_string())
}