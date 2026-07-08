//! 跨平台路径解析服务
//!
//! 桌面端：app_data_dir() 指向用户配置目录
//! Android：app_data_dir() 指向应用私有目录（卸载即清空）

use std::path::PathBuf;
use tauri::{AppHandle, Manager};

/// 获取应用数据目录
pub fn data_dir(app: &AppHandle) -> Result<PathBuf, String> {
    let dir = app
        .path()
        .app_data_dir()
        .map_err(|e| format!("解析 app_data_dir 失败: {}", e))?;
    std::fs::create_dir_all(&dir).map_err(|e| format!("创建数据目录失败: {}", e))?;
    Ok(dir)
}

/// 获取数据库文件路径
pub fn db_path(app: &AppHandle) -> Result<PathBuf, String> {
    Ok(data_dir(app)?.join("classisscore.db"))
}

/// 获取本地缓存目录（可被系统清理）
pub fn cache_dir(app: &AppHandle) -> Result<PathBuf, String> {
    let dir = app
        .path()
        .app_local_data_dir()
        .map_err(|e| format!("解析 app_local_data_dir 失败: {}", e))?;
    std::fs::create_dir_all(&dir).map_err(|e| format!("创建缓存目录失败: {}", e))?;
    Ok(dir)
}

/// 获取日志目录
pub fn log_dir(app: &AppHandle) -> Result<PathBuf, String> {
    let dir = cache_dir(app)?.join("logs");
    std::fs::create_dir_all(&dir).map_err(|e| format!("创建日志目录失败: {}", e))?;
    Ok(dir)
}
