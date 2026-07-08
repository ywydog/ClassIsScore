//! 主题管理
//!
//! 主题元信息以 settings 表为存储：
//! - `theme.<id>.name`         主题显示名
//! - `theme.<id>.description`  描述
//! - `theme.<id>.version`      版本
//! - `theme.<id>.author`       作者
//! - `theme.<id>.enabled`      "true" / "false"
//!
//! 这种"用 settings 当 KV 表"的做法和项目里其他 settings 用法一致，
//! 避免为两个零碎功能再开表 / 写迁移。

use crate::db::entities::admin_settings;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

use super::get_db;

const PREFIX: &str = "theme.";

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ThemeInfo {
    pub id: String,
    pub name: String,
    pub description: String,
    pub version: String,
    pub author: String,
    pub is_enabled: bool,
    pub installed_at: String,
}

fn setting_key(id: &str, field: &str) -> String {
    format!("{}.{}.{}", PREFIX, id, field)
}

/// 主题在 settings 表里登记的所有字段名。
/// 用静态切片，避免运行时分配。
const THEME_FIELDS: &[&str] = &["name", "description", "version", "author", "enabled"];

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
        // "theme.<id>.name" → "<id>"
        if let Some(rest) = row.setting_key.strip_prefix(PREFIX) {
            if let Some(id) = rest.strip_suffix(".name") {
                ids.push(id.to_string());
            }
        }
    }
    Ok(ids)
}

#[tauri::command]
pub async fn theme_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<ThemeInfo>, String> {
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

        // installed_at 暂用最近一次更新的任意字段近似；不存在就空串
        let installed_at = read_setting(&db, &setting_key(&id, "name")).await?
            .map(|_| chrono::Local::now().to_rfc3339())
            .unwrap_or_default();

        out.push(ThemeInfo {
            id,
            name,
            description,
            version,
            author,
            is_enabled,
            installed_at,
        });
    }
    Ok(out)
}

#[tauri::command]
pub async fn theme_get(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<ThemeInfo, String> {
    let db = get_db(&state)?;
    let name = read_setting(&db, &setting_key(&id, "name")).await?
        .ok_or_else(|| "主题不存在".to_string())?;
    let description = read_setting(&db, &setting_key(&id, "description")).await?.unwrap_or_default();
    let version = read_setting(&db, &setting_key(&id, "version")).await?.unwrap_or_default();
    let author = read_setting(&db, &setting_key(&id, "author")).await?.unwrap_or_default();
    let enabled_str = read_setting(&db, &setting_key(&id, "enabled")).await?.unwrap_or_default();

    Ok(ThemeInfo {
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
pub async fn theme_install(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    name: String,
    description: Option<String>,
    version: Option<String>,
    author: Option<String>,
) -> Result<ThemeInfo, String> {
    let db = get_db(&state)?;
    let resolved_name = name;
    let resolved_description = description.unwrap_or_default();
    let resolved_version = version.unwrap_or_else(|| "1.0.0".to_string());
    let resolved_author = author.unwrap_or_default();

    write_setting(&db, &setting_key(&id, "name"), resolved_name.clone()).await?;
    write_setting(&db, &setting_key(&id, "description"), resolved_description.clone()).await?;
    write_setting(&db, &setting_key(&id, "version"), resolved_version.clone()).await?;
    write_setting(&db, &setting_key(&id, "author"), resolved_author.clone()).await?;
    // 新装默认禁用
    write_setting(&db, &setting_key(&id, "enabled"), "false".to_string()).await?;

    Ok(ThemeInfo {
        id,
        name: resolved_name,
        description: resolved_description,
        version: resolved_version,
        author: resolved_author,
        is_enabled: false,
        installed_at: chrono::Local::now().to_rfc3339(),
    })
}

#[tauri::command]
pub async fn theme_toggle(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
    enabled: bool,
) -> Result<(), String> {
    let db = get_db(&state)?;
    write_setting(&db, &setting_key(&id, "enabled"), if enabled { "true" } else { "false" }.to_string()).await
}

#[tauri::command]
pub async fn theme_delete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: String,
) -> Result<(), String> {
    let db = get_db(&state)?;
    // 删除该 id 下所有字段
    for field in THEME_FIELDS {
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
