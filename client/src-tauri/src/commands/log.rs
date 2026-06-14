use crate::services::logger::LoggerService;
use crate::services::logger::LogLevel;
use crate::state::AppState;
use parking_lot::RwLock;
use std::sync::Arc;
use tauri::State;

fn get_logger(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<LoggerService, String> {
    let guard = state.read();
    guard.logger.get().cloned().ok_or_else(|| "日志服务未初始化".to_string())
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
pub struct LogQueryResult {
    pub lines: Vec<String>,
}

#[tauri::command]
pub async fn log_query(
    state: State<'_, Arc<RwLock<AppState>>>,
    lines: Option<usize>,
) -> Result<LogQueryResult, String> {
    let logger = get_logger(&state)?;

    let line_count = lines.unwrap_or(100);
    let result = logger.read_logs(line_count);

    Ok(LogQueryResult { lines: result })
}

#[tauri::command]
pub async fn log_clear(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<(), String> {
    let logger = get_logger(&state)?;

    logger.clear_logs()
}

#[tauri::command]
pub async fn log_set_level(
    state: State<'_, Arc<RwLock<AppState>>>,
    level: String,
) -> Result<(), String> {
    let logger = get_logger(&state)?;

    let log_level = LogLevel::from_str(&level)
        .ok_or_else(|| format!("无效的日志级别: {}", level))?;

    logger.set_level(log_level);
    Ok(())
}

#[tauri::command]
pub async fn log_write(
    state: State<'_, Arc<RwLock<AppState>>>,
    level: String,
    message: String,
) -> Result<(), String> {
    let logger = get_logger(&state)?;

    let log_level = LogLevel::from_str(&level)
        .ok_or_else(|| format!("无效的日志级别: {}", level))?;

    logger.write_log(log_level, &message, Some("frontend"));
    Ok(())
}
