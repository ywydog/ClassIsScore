use crate::db::entities::admin_settings;
use crate::db::entities::auto_evaluation_config;
use crate::db::entities::evaluation_item;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

use super::get_db;

#[tauri::command]
pub async fn settings_get_all(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<admin_settings::Model>, String> {
    let db = get_db(&state)?;

    let settings = admin_settings::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(settings)
}

#[tauri::command]
pub async fn settings_get(
    state: State<'_, Arc<RwLock<AppState>>>,
    key: String,
) -> Result<Option<admin_settings::Model>, String> {
    let db = get_db(&state)?;

    let setting = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(&key))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(setting)
}

#[tauri::command]
pub async fn settings_set(
    state: State<'_, Arc<RwLock<AppState>>>,
    key: String,
    value: String,
) -> Result<admin_settings::Model, String> {
    let db = get_db(&state)?;

    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(&key))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    if let Some(model) = existing {
        let mut active: admin_settings::ActiveModel = model.into();
        active.setting_value = Set(Some(value));
        active.updated_at = Set(chrono::Local::now().naive_utc());
        let result = active.update(&db).await.map_err(|e| e.to_string())?;
        Ok(result)
    } else {
        let new_setting = admin_settings::ActiveModel {
            setting_key: Set(key),
            setting_value: Set(Some(value)),
            ..Default::default()
        };
        let result = new_setting.insert(&db).await.map_err(|e| e.to_string())?;
        Ok(result)
    }
}

#[derive(Debug, Serialize, Deserialize)]
pub struct SettingsExportData {
    pub version: String,
    pub exported_at: String,
    pub settings: Vec<admin_settings::Model>,
    pub evaluation_items: Vec<evaluation_item::Model>,
    pub auto_evaluation_configs: Vec<auto_evaluation_config::Model>,
}

#[tauri::command]
pub async fn settings_export(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<SettingsExportData, String> {
    let db = get_db(&state)?;

    let settings = admin_settings::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let eval_items = evaluation_item::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let auto_configs = auto_evaluation_config::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(SettingsExportData {
        version: "1.0.0".to_string(),
        exported_at: chrono::Local::now().to_rfc3339(),
        settings,
        evaluation_items: eval_items,
        auto_evaluation_configs: auto_configs,
    })
}

#[derive(Debug, Serialize, Deserialize)]
pub struct SettingsImportData {
    pub settings: Vec<ImportSettingItem>,
    pub evaluation_items: Vec<ImportEvalItem>,
    pub auto_evaluation_configs: Vec<ImportAutoEvalConfig>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImportSettingItem {
    pub setting_key: String,
    pub setting_value: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImportEvalItem {
    pub name: String,
    pub score_change: i32,
    pub category: Option<String>,
    pub is_quick_access: bool,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImportAutoEvalConfig {
    pub name: String,
    pub trigger_type: String,
    pub trigger_time: Option<String>,
    pub day_of_week: Option<i32>,
    pub day_of_month: Option<i32>,
    pub evaluation_item_id: Option<i64>,
    pub score_change: Option<f64>,
    pub reason: Option<String>,
    pub target_type: Option<String>,
    pub target_group_id: Option<i64>,
    pub target_student_id: Option<i64>,
    pub is_enabled: bool,
}

#[tauri::command]
pub async fn settings_import(
    state: State<'_, Arc<RwLock<AppState>>>,
    data: SettingsImportData,
) -> Result<String, String> {
    let db = get_db(&state)?;

    // 导入设置（upsert：已有 key 覆盖，新 key 插入）
    for item in data.settings {
        let existing = admin_settings::Entity::find()
            .filter(admin_settings::Column::SettingKey.eq(&item.setting_key))
            .one(&db)
            .await
            .map_err(|e| e.to_string())?;

        if let Some(model) = existing {
            let mut active: admin_settings::ActiveModel = model.into();
            active.setting_value = Set(item.setting_value);
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
        } else {
            let new_setting = admin_settings::ActiveModel {
                setting_key: Set(item.setting_key),
                setting_value: Set(item.setting_value),
                ..Default::default()
            };
            new_setting.insert(&db).await.map_err(|e| e.to_string())?;
        }
    }

    // 导入评估项（追加插入）
    for item in data.evaluation_items {
        let new_item = evaluation_item::ActiveModel {
            name: Set(item.name),
            score_change: Set(item.score_change),
            category: Set(item.category),
            is_quick_access: Set(item.is_quick_access),
            ..Default::default()
        };
        new_item.insert(&db).await.map_err(|e| e.to_string())?;
    }

    // 导入自动评估配置（追加插入）
    for item in data.auto_evaluation_configs {
        let new_config = auto_evaluation_config::ActiveModel {
            name: Set(item.name),
            trigger_type: Set(item.trigger_type),
            trigger_time: Set(item.trigger_time),
            day_of_week: Set(item.day_of_week),
            day_of_month: Set(item.day_of_month),
            evaluation_item_id: Set(item.evaluation_item_id),
            score_change: Set(item.score_change),
            reason: Set(item.reason),
            target_type: Set(item.target_type),
            target_group_id: Set(item.target_group_id),
            target_student_id: Set(item.target_student_id),
            is_enabled: Set(item.is_enabled),
            ..Default::default()
        };
        new_config.insert(&db).await.map_err(|e| e.to_string())?;
    }

    Ok("导入成功".to_string())
}

/// 读取后端数据目录路径（用于前端展示"数据存在哪里"）。
#[tauri::command]
pub async fn settings_data_path(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<String, String> {
    let guard = state.read();
    let data_dir = guard.get_data_dir();
    Ok(data_dir.to_string_lossy().to_string())
}
