//! 网络伺服模块
//!
//! 学习 SiYuan 的设计：在 Tauri 原生应用之外启动一个 HTTP 服务器，
//! 让局域网内其他设备可通过浏览器访问。支持：
//! - 静态文件服务（前端 dist 目录）
//! - User-Agent 检测自动切换桌面/移动端视图
//! - CORS 跨域支持
//! - 可配置端口（默认 6806）
//!
//! 安全最佳实践：
//! - 网络伺服通过 Bearer PIN 鉴权（PIN 在 admin_settings 中以 Argon2 散列存储）
//! - CORS 不使用 `Any` 而是限制到局域网回环/同源
//! - 请求体大小限制 1 MiB，避免 LAN 攻击者耗尽内存

use axum::{
    extract::{Request, State as AxumState},
    http::{header, StatusCode},
    middleware::{self, Next},
    response::{IntoResponse, Redirect, Response},
    routing::get,
    Router,
};
use parking_lot::RwLock;
use std::net::SocketAddr;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use tokio::net::TcpListener;
use tokio::sync::Mutex;
use tower_http::cors::CorsLayer;
use tower_http::limit::RequestBodyLimitLayer;
use tower_http::services::ServeDir;

use crate::services::crypto::verify_password;

/// 服务器运行状态
#[derive(Clone)]
pub struct ServerState {
    pub running: Arc<AtomicBool>,
    /// 当前端口。使用 AtomicU32 而非 Mutex<u16> 避免跨 await 的 Send 问题
    pub port: Arc<std::sync::atomic::AtomicU32>,
    pub shutdown_tx: Arc<Mutex<Option<tokio::sync::oneshot::Sender<()>>>>,
    /// Argon2 散列后的网络伺服 PIN（如果未设置则强制要求首次启动时配置）
    pub pin_hash: Arc<RwLock<Option<String>>>,
}

impl ServerState {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
            port: Arc::new(std::sync::atomic::AtomicU32::new(0)),
            shutdown_tx: Arc::new(Mutex::new(None)),
            pin_hash: Arc::new(RwLock::new(None)),
        }
    }

    pub fn is_running(&self) -> bool {
        self.running.load(Ordering::SeqCst)
    }

    pub fn get_port(&self) -> u16 {
        self.port.load(Ordering::SeqCst) as u16
    }

    /// 设置网络伺服 PIN（Argon2 散列格式）。设为 None 关闭网络伺服鉴权。
    /// 安全最佳实践：即使 LAN 内部，鉴权也防止未授权设备访问业务页面。
    pub fn set_pin_hash(&self, hash: Option<String>) {
        *self.pin_hash.write() = hash;
    }
}

/// 启动 HTTP 伺服
///
/// # Arguments
/// * `frontend_dir` - 前端 dist 目录路径
/// * `port` - 监听端口，0 表示自动分配
/// * `network_serve` - true 绑定 0.0.0.0，false 绑定 127.0.0.1
/// * `pin_hash` - 网络伺服访问 PIN（Argon2 散列）；None 表示无 PIN
pub async fn serve(
    state: ServerState,
    frontend_dir: PathBuf,
    port: u16,
    network_serve: bool,
    pin_hash: Option<String>,
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
    state.set_pin_hash(pin_hash);

    // 安全最佳实践：
    // - CORS 限制到同源/同网域扩展（不开放 `Any` 防止第三方网站跨域读取响应）
    // - Body 上限 1 MiB 防 DoS
    // - 顶层鉴权中间件（无 PIN 时拒绝所有访问；带 PIN 时检查 Authorization 头）
    let app = Router::new()
        .route("/api/health", get(|| async { "ok" }))
        .fallback_service(ServeDir::new(serve_dir.clone()))
        .layer(RequestBodyLimitLayer::new(1024 * 1024))
        .layer(
            CorsLayer::new()
                .allow_origin([
                    "http://localhost".parse().unwrap(),
                    "http://127.0.0.1".parse().unwrap(),
                    "tauri://localhost".parse().unwrap(),
                ])
                .allow_methods([
                    axum::http::Method::GET,
                    axum::http::Method::POST,
                    axum::http::Method::HEAD,
                ])
                .allow_headers([header::AUTHORIZATION, header::CONTENT_TYPE]),
        )
        .layer(middleware::from_fn_with_state(
            state.clone(),
            pin_auth_middleware,
        ))
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

/// PIN 鉴权中间件
///
/// 安全最佳实践：网络伺服必须携带 Authorization: Bearer <PIN> 才能访问。
/// PIN 通过 Argon2id 散列存储（与管理员密码同等强度）。
/// `ping` 端点（`/api/health`）豁免，用于局域网存活探测。
async fn pin_auth_middleware(
    AxumState(state): AxumState<ServerState>,
    req: Request,
    next: Next,
) -> Response {
    let path = req.uri().path().to_string();

    // 健康检查端点豁免（仅返回 "ok"，无业务信息泄露）
    if path == "/api/health" {
        return next.run(req).await;
    }

    let stored_hash = state.pin_hash.read().clone();
    let stored_hash = match stored_hash {
        Some(h) => h,
        None => {
            // 安全最佳实践：未配置 PIN 时拒绝所有非健康检查访问
            return (StatusCode::UNAUTHORIZED, "network serve PIN not configured").into_response();
        }
    };

    let auth_header = req
        .headers()
        .get(header::AUTHORIZATION)
        .and_then(|v| v.to_str().ok())
        .unwrap_or("");

    let token = auth_header.strip_prefix("Bearer ").unwrap_or("").trim();

    if token.is_empty() {
        return (StatusCode::UNAUTHORIZED, "missing bearer token").into_response();
    }

    if !verify_password(&stored_hash, token) {
        return (StatusCode::UNAUTHORIZED, "invalid token").into_response();
    }

    next.run(req).await
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

#[cfg(test)]
mod tests {
    use super::*;
    use crate::services::crypto::hash_password;
    use axum::body::Body;
    use axum::http::{Request as HttpRequest, StatusCode as AxumStatus};
    use axum::middleware::from_fn_with_state;
    use axum::routing::get;
    use http_body_util::BodyExt;
    use tower::ServiceExt;

    /// 用 middleware 包裹一个返回 200 的 dummy handler，
    /// 通过 axum 真实管道发送请求，验证鉴权逻辑。
    async fn build_test_router(state: ServerState) -> axum::Router {
        axum::Router::new()
            .route(
                "/api/protected",
                get(|| async { axum::Json(serde_json::json!({"ok": true})) }),
            )
            .route(
                "/api/health",
                get(|| async { "ok" }),
            )
            .route(
                "/",
                get(|| async { axum::Json(serde_json::json!({"root": true})) }),
            )
            .layer(from_fn_with_state(state.clone(), pin_auth_middleware))
            .with_state(state)
    }

    #[tokio::test]
    async fn health_endpoint_does_not_require_token() {
        let state = ServerState::new(); // no PIN configured
        let app = build_test_router(state).await;

        let req = HttpRequest::builder()
            .uri("/api/health")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::OK);
    }

    #[tokio::test]
    async fn protected_endpoint_without_pin_returns_401() {
        let state = ServerState::new(); // no PIN
        let app = build_test_router(state).await;

        let req = HttpRequest::builder()
            .uri("/api/protected")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::UNAUTHORIZED);
    }

    #[tokio::test]
    async fn protected_endpoint_with_wrong_bearer_returns_401() {
        let state = ServerState::new();
        let hash = hash_password("correct-pin");
        state.set_pin_hash(Some(hash));
        let app = build_test_router(state).await;

        let req = HttpRequest::builder()
            .uri("/api/protected")
            .header(header::AUTHORIZATION, "Bearer wrong-pin")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::UNAUTHORIZED);
    }

    #[tokio::test]
    async fn protected_endpoint_with_correct_bearer_returns_200() {
        let state = ServerState::new();
        let hash = hash_password("correct-pin");
        state.set_pin_hash(Some(hash));
        let app = build_test_router(state).await;

        let req = HttpRequest::builder()
            .uri("/api/protected")
            .header(header::AUTHORIZATION, "Bearer correct-pin")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::OK);

        let body_bytes = resp.into_body().collect().await.unwrap().to_bytes();
        let body: serde_json::Value = serde_json::from_slice(&body_bytes).unwrap();
        assert_eq!(body, serde_json::json!({"ok": true}));
    }

    #[tokio::test]
    async fn protected_endpoint_without_auth_header_returns_401() {
        let state = ServerState::new();
        let hash = hash_password("some-pin");
        state.set_pin_hash(Some(hash));
        let app = build_test_router(state).await;

        let req = HttpRequest::builder()
            .uri("/api/protected")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::UNAUTHORIZED);
    }

    #[tokio::test]
    async fn non_bearer_auth_scheme_returns_401() {
        let state = ServerState::new();
        let hash = hash_password("some-pin");
        state.set_pin_hash(Some(hash));
        let app = build_test_router(state).await;

        // "Basic xxx" 不应被识别为 Bearer
        let req = HttpRequest::builder()
            .uri("/api/protected")
            .header(header::AUTHORIZATION, "Basic dXNlcjpwYXNz")
            .body(Body::empty())
            .unwrap();
        let resp = app.oneshot(req).await.unwrap();
        assert_eq!(resp.status(), AxumStatus::UNAUTHORIZED);
    }

    #[test]
    fn ua_mobile_detection_basic() {
        // iPhone / iPod / 显式含 "Mobile" → 移动端
        assert!(detect_mobile(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)"
        ));
        assert!(detect_mobile(
            "Mozilla/5.0 (Linux; Android 10; Pixel 3 Build/QP1A.191005.007.A3; Mobile)"
        ));
        // Android 但不含 Mobile 标记 → 视为桌面端（与 SiYuan 的 serve.go 行为一致）
        assert!(!detect_mobile(
            "Mozilla/5.0 (Linux; Android 9; SM-T720)"
        ));
        assert!(!detect_mobile("Mozilla/5.0 (Windows NT 10.0)"));
    }
}

