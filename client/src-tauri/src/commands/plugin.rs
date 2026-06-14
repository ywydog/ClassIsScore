use crate::state::AppState;
use parking_lot::RwLock;
use serde::Serialize;
use std::sync::Arc;
use tauri::State;

#[derive(Serialize, Clone)]
pub struct PluginInfo {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub is_enabled: bool,
    pub installed_at: String,
}

#[tauri::command]
pub async fn plugin_list(
    _state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<PluginInfo>, String> {
    // 暂无内置插件
    Ok(vec![])
}

#[tauri::command]
pub async fn plugin_get(
    _state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<PluginInfo, String> {
    Err(format!("插件 {} 不存在", id))
}

#[tauri::command]
pub async fn plugin_install(
    _state: State<'_, Arc<RwLock<AppState>>>,
    path: String,
) -> Result<PluginInfo, String> {
    Err(format!("暂不支持从外部安装插件: {}", path))
}

#[tauri::command]
pub async fn plugin_delete(
    _state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<(), String> {
    Err(format!("插件 {} 不存在", id))
}

#[tauri::command]
pub async fn plugin_toggle(
    _state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    _enabled: bool,
) -> Result<(), String> {
    Err(format!("插件 {} 不存在", id))
}
