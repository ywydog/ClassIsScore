//! 插件管理
//!
//! 存储结构与 theme.rs 一致：所有字段都存进 settings 表，
//! 键名模式 `plugin.<id>.<field>`。

use crate::db::entities::admin_settings;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

use super::get_db;

const PREFIX: &str = "plugin.";

/// 插件在 settings 表里登记的所有字段名。
const PLUGIN_FIELDS: &[&str] = &["name", "description", "version", "author", "enabled"];

fn setting_key(id: &str, field: &str) -> String {
    format!("{}.{}.{}", PREFIX, id, field)
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct PluginInfo {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub is_enabled: bool,
    pub installed_at: String,
}

async fn read_setting(db: &sea_orm::DatabaseConnection, key: &str) -> Result<Option<String>, String> {
    let model = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(key))
        .one(db)
        .await
        .map_err(|e| e.to_string())?;
    Ok(model.and_then(|m| m.setting_value))
}

async fn write_setting(
    db: &sea_orm::DatabaseConnection,
    key: &str,
    value: String,
) -> Result<(), String> {
    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(key))
        .one(db)
        .await
        .map_err(|e| e.to_string())?;

    if let Some(model) = existing {
        let mut active: admin_settings::ActiveModel = model.into();
        active.setting_value = Set(Some(value));
        active.updated_at = Set(chrono::Local::now().naive_utc());
        active.update(db).await.map_err(|e| e.to_string())?;
    } else {
        let new_setting = admin_settings::ActiveModel {
            setting_key: Set(key.to_string()),
            setting_value: Set(Some(value)),
            ..Default::default()
        };
        new_setting.insert(db).await.map_err(|e| e.to_string())?;
    }
    Ok(())
}

async fn collect_ids(db: &sea_orm::DatabaseConnection) -> Result<Vec<String>, String> {
    let all = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.like(format!("{}%.name", PREFIX)))
        .all(db)
        .await
        .map_err(|e| e.to_string())?;

    let mut ids = Vec::new();
    for row in all {
        if let Some(rest) = row.setting_key.strip_prefix(PREFIX) {
            if let Some(id) = rest.strip_suffix(".name") {
                ids.push(id.to_string());
            }
        }
    }
    Ok(ids)
}

#[tauri::command]
pub async fn plugin_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<PluginInfo>, String> {
    let db = get_db(&state)?;
    let ids = collect_ids(&db).await?;

    let mut out = Vec::with_capacity(ids.len());
    for id in ids {
        let name = read_setting(&db, &setting_key(&id, "name")).await?.unwrap_or_default();
        let description = read_setting(&db, &setting_key(&id, "description")).await?.unwrap_or_default();
        let version = read_setting(&db, &setting_key(&id, "version")).await?.unwrap_or_default();
        let author = read_setting(&db, &setting_key(&id, "author")).await?.unwrap_or_default();
        let enabled_str = read_setting(&db, &setting_key(&id, "enabled")).await?.unwrap_or_default();
        let is_enabled = enabled_str == "true";

        out.push(PluginInfo {
            id,
            name,
            description,
            version,
            author,
            is_enabled,
            installed_at: chrono::Local::now().to_rfc3339(),
        });
    }
    Ok(out)
}

#[tauri::command]
pub async fn plugin_get(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<PluginInfo, String> {
    let db = get_db(&state)?;
    let name = read_setting(&db, &setting_key(&id, "name")).await?
        .ok_or_else(|| "插件不存在".to_string())?;
    let description = read_setting(&db, &setting_key(&id, "description")).await?.unwrap_or_default();
    let version = read_setting(&db, &setting_key(&id, "version")).await?.unwrap_or_default();
    let author = read_setting(&db, &setting_key(&id, "author")).await?.unwrap_or_default();
    let enabled_str = read_setting(&db, &setting_key(&id, "enabled")).await?.unwrap_or_default();

    Ok(PluginInfo {
        id,
        name,
        description,
        version,
        author,
        is_enabled: enabled_str == "true",
        installed_at: chrono::Local::now().to_rfc3339(),
    })
}

#[tauri::command]
pub async fn plugin_install(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    name: String,
    description: Option<String>,
    version: Option<String>,
    author: Option<String>,
) -> Result<PluginInfo, String> {
    let db = get_db(&state)?;
    write_setting(&db, &setting_key(&id, "name"), name.clone()).await?;
    write_setting(&db, &setting_key(&id, "description"), description.clone().unwrap_or_default()).await?;
    write_setting(&db, &setting_key(&id, "version"), version.clone().unwrap_or_else(|| "1.0.0".to_string())).await?;
    write_setting(&db, &setting_key(&id, "author"), author.clone().unwrap_or_default()).await?;
    write_setting(&db, &setting_key(&id, "enabled"), "false".to_string()).await?;

    Ok(PluginInfo {
        id,
        name,
        description: description.unwrap_or_default(),
        version: version.unwrap_or_else(|| "1.0.0".to_string()),
        author: author.unwrap_or_default(),
        is_enabled: false,
        installed_at: chrono::Local::now().to_rfc3339(),
    })
}

#[tauri::command]
pub async fn plugin_toggle(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    enabled: bool,
) -> Result<(), String> {
    let db = get_db(&state)?;
    write_setting(
        &db,
        &setting_key(&id, "enabled"),
        if enabled { "true" } else { "false" }.to_string(),
    )
    .await
}

#[tauri::command]
pub async fn plugin_delete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<(), String> {
    let db = get_db(&state)?;
    for field in PLUGIN_FIELDS {
        let key = setting_key(&id, field);
        let existing = admin_settings::Entity::find()
            .filter(admin_settings::Column::SettingKey.eq(&key))
            .one(&db)
            .await
            .map_err(|e| e.to_string())?;
        if let Some(model) = existing {
            let active: admin_settings::ActiveModel = model.into();
            active.delete(&db).await.map_err(|e| e.to_string())?;
        }
    }
    Ok(())
}
