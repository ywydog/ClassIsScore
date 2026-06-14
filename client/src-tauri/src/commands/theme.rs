use crate::db::entities::admin_settings;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use serde::Serialize;
use std::sync::Arc;
use tauri::State;

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[derive(Serialize, Clone)]
pub struct ThemeInfo {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub css_path: String,
    pub is_enabled: bool,
    pub installed_at: String,
}

/// 内置主题列表
fn builtin_themes() -> Vec<ThemeInfo> {
    vec![ThemeInfo {
        id: "xianxia".to_string(),
        name: "修仙主题".to_string(),
        description: "诸天修仙练气境界体系，道友切磋、仙宠渡劫、灵力排行".to_string(),
        version: "1.0.0".to_string(),
        author: "ClassIsScore".to_string(),
        css_path: "xianxia/styles.css".to_string(),
        is_enabled: false,
        installed_at: "2025-01-01T00:00:00".to_string(),
    }]
}

/// 从 admin_settings 读取当前 themeMode
async fn get_theme_mode(db: &sea_orm::DatabaseConnection) -> String {
    let setting = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("themeMode"))
        .one(db)
        .await
        .ok()
        .flatten();

    setting
        .and_then(|s| s.setting_value)
        .unwrap_or_else(|| "default".to_string())
}

/// 写入 themeMode 到 admin_settings
async fn set_theme_mode(db: &sea_orm::DatabaseConnection, mode: &str) -> Result<(), String> {
    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("themeMode"))
        .one(db)
        .await
        .map_err(|e| e.to_string())?;

    if let Some(model) = existing {
        let mut active: admin_settings::ActiveModel = model.into();
        active.setting_value = Set(Some(mode.to_string()));
        active.updated_at = Set(chrono::Local::now().naive_utc());
        active.update(db).await.map_err(|e| e.to_string())?;
    } else {
        let new_setting = admin_settings::ActiveModel {
            setting_key: Set("themeMode".to_string()),
            setting_value: Set(Some(mode.to_string())),
            ..Default::default()
        };
        new_setting.insert(db).await.map_err(|e| e.to_string())?;
    }
    Ok(())
}

#[tauri::command]
pub async fn theme_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<ThemeInfo>, String> {
    let db = get_db(&state)?;
    let current_mode = get_theme_mode(&db).await;

    let mut themes = builtin_themes();
    for theme in &mut themes {
        theme.is_enabled = current_mode == theme.id;
    }

    Ok(themes)
}

#[tauri::command]
pub async fn theme_get(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<ThemeInfo, String> {
    let db = get_db(&state)?;
    let current_mode = get_theme_mode(&db).await;

    let themes = builtin_themes();
    themes
        .into_iter()
        .find(|t| t.id == id)
        .map(|mut t| {
            t.is_enabled = current_mode == t.id;
            t
        })
        .ok_or_else(|| format!("主题 {} 不存在", id))
}

#[tauri::command]
pub async fn theme_toggle(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    enabled: bool,
) -> Result<(), String> {
    let db = get_db(&state)?;

    // 验证主题存在
    let themes = builtin_themes();
    if !themes.iter().any(|t| t.id == id) {
        return Err(format!("主题 {} 不存在", id));
    }

    let mode = if enabled { id.as_str() } else { "default" };
    set_theme_mode(&db, mode).await?;

    Ok(())
}

#[tauri::command]
pub async fn theme_install(
    _state: State<'_, Arc<RwLock<AppState>>>,
    path: String,
) -> Result<ThemeInfo, String> {
    Err(format!("暂不支持从外部安装主题: {}", path))
}

#[tauri::command]
pub async fn theme_delete(
    _state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<(), String> {
    // 内置主题不允许删除
    let themes = builtin_themes();
    if themes.iter().any(|t| t.id == id) {
        return Err("内置主题不可删除".to_string());
    }
    Err(format!("主题 {} 不存在", id))
}
