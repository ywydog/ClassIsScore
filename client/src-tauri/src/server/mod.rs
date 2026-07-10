//! 网络伺服模块
//!
//! 学习 SiYuan 的设计：在 Tauri 原生应用之外启动一个 HTTP 服务器，
//! 让局域网内其他设备可通过浏览器访问。支持：
//! - 静态文件服务（前端 dist 目录）
//! - User-Agent 检测自动切换桌面/移动端视图
//! - CORS 跨域支持
//! - 可配置端口（默认 6806）

use axum::{
    extract::Request,
    http::header,
    middleware::{self, Next},
    response::{IntoResponse, Redirect, Response},
    routing::get,
    Router,
};
use std::net::SocketAddr;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use tokio::net::TcpListener;
use tokio::sync::Mutex;
use tower_http::cors::{Any, CorsLayer};
use tower_http::services::ServeDir;

/// 服务器运行状态
#[derive(Clone)]
pub struct ServerState {
    pub running: Arc<AtomicBool>,
    /// 当前端口。使用 AtomicU32 而非 Mutex<u16> 避免跨 await 的 Send 问题
    pub port: Arc<std::sync::atomic::AtomicU32>,
    pub shutdown_tx: Arc<Mutex<Option<tokio::sync::oneshot::Sender<()>>>>,
}

impl ServerState {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
            port: Arc::new(std::sync::atomic::AtomicU32::new(0)),
            shutdown_tx: Arc::new(Mutex::new(None)),
        }
    }

    pub fn is_running(&self) -> bool {
        self.running.load(Ordering::SeqCst)
    }

    pub fn get_port(&self) -> u16 {
        self.port.load(Ordering::SeqCst) as u16
    }
}

/// 启动 HTTP 伺服
///
/// # Arguments
/// * `frontend_dir` - 前端 dist 目录路径
/// * `port` - 监听端口，0 表示自动分配
/// * `network_serve` - true 绑定 0.0.0.0，false 绑定 127.0.0.1
pub async fn serve(
    state: ServerState,
    frontend_dir: PathBuf,
    port: u16,
    network_serve: bool,
) -> Result<u16, String> {
    if state.is_running() {
        return Err("服务器已在运行".to_string());
    }

    let host = if network_serve { "0.0.0.0" } else { "127.0.0.1" };
    let addr: SocketAddr = format!("{}:{}", host, port)
        .parse()
        .map_err(|e| format!("无效的地址: {}", e))?;

    let listener = TcpListener::bind(&addr)
        .await
        .map_err(|e| format!("端口绑定失败: {}", e))?;

    let actual_port = listener
        .local_addr()
        .map_err(|e| format!("获取端口失败: {}", e))?
        .port();

    let serve_dir = frontend_dir.clone();

    let app = Router::new()
        .route("/api/health", get(|| async { "ok" }))
        .fallback_service(ServeDir::new(serve_dir.clone()))
        .layer(
            CorsLayer::new()
                .allow_origin(Any)
                .allow_methods(Any)
                .allow_headers(Any),
        )
        .layer(middleware::from_fn(move |req, next| {
            let dir = serve_dir.clone();
            ua_detect_middleware(req, next, dir)
        }));

    let (shutdown_tx, shutdown_rx) = tokio::sync::oneshot::channel::<()>();
    state.running.store(true, Ordering::SeqCst);
    state.port.store(actual_port as u32, Ordering::SeqCst);
    *state.shutdown_tx.lock().await = Some(shutdown_tx);

    let running_flag = state.running.clone();
    tokio::spawn(async move {
        axum::serve(listener, app)
            .with_graceful_shutdown(async {
                let _ = shutdown_rx.await;
            })
            .await
            .ok();
        running_flag.store(false, Ordering::SeqCst);
    });

    Ok(actual_port)
}

/// 停止 HTTP 伺服
pub async fn shutdown(state: &ServerState) -> Result<(), String> {
    if !state.is_running() {
        return Err("服务器未运行".to_string());
    }
    let tx = state.shutdown_tx.lock().await.take();
    if let Some(tx) = tx {
        let _ = tx.send(());
    }
    state.running.store(false, Ordering::SeqCst);
    Ok(())
}

/// User-Agent 检测中间件
///
/// 在用户访问根路径时，根据 User-Agent 自动切换桌面/移动端视图。
/// 逻辑与 SiYuan 的 serve.go 中的 UA 检测一致：
/// - Android 平板（有 Android 但无 Mobile）→ 桌面端
/// - 移动端 UA → 移动端
/// - 其他 → 桌面端
async fn ua_detect_middleware(
    req: Request,
    next: Next,
    _frontend_dir: PathBuf,
) -> Response {
    // 只对根路径或 hash 锚点路径做 UA 检测
    let path = req.uri().path();
    let is_root = path == "/" || path == "/index.html" || path == "";

    if !is_root {
        return next.run(req).await;
    }

    let ua = req
        .headers()
        .get(header::USER_AGENT)
        .and_then(|v| v.to_str().ok())
        .unwrap_or("");

    let is_mobile = detect_mobile(ua);

    // 重定向到对应视图的 hash 路由
    if is_mobile {
        Redirect::to("/#/m/dashboard").into_response()
    } else {
        Redirect::to("/#/admin/dashboard").into_response()
    }
}

/// 检测是否为移动端
fn detect_mobile(ua: &str) -> bool {
    // 平板设备（Android 但不含 Mobile 标记）→ 视为桌面端
    // 与 SiYuan 的 serve.go 行为一致
    if ua.contains("Android") && !ua.contains("Mobile") {
        return false;
    }

    // 移动端标识
    let mobile_keywords = [
        "Mobile", "iPhone", "iPod", "Android", "BlackBerry",
        "Opera Mini", "IEMobile", "webOS",
    ];
    for kw in &mobile_keywords {
        if ua.contains(kw) {
            return true;
        }
    }

    false
}

