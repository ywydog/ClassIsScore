use crate::db::entities::admin_settings;
use crate::db::entities::auto_evaluation_config;
use crate::db::entities::evaluation_item;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

/// 导出的设置数据结构
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
        version: "1.0".to_string(),
        exported_at: chrono::Local::now().to_rfc3339(),
        settings,
        evaluation_items: eval_items,
        auto_evaluation_configs: auto_configs,
    })
}

/// 导入设置时的输入结构（去掉 id 和时间戳，让数据库自动生成）
#[derive(Debug, Serialize, Deserialize)]
pub struct ImportSettingEntry {
    pub setting_key: String,
    pub setting_value: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImportEvalItemEntry {
    pub name: String,
    pub score_change: i32,
    pub category: Option<String>,
    pub is_quick_access: bool,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ImportAutoEvalEntry {
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

#[derive(Debug, Serialize, Deserialize)]
pub struct SettingsImportData {
    pub settings: Vec<ImportSettingEntry>,
    pub evaluation_items: Vec<ImportEvalItemEntry>,
    pub auto_evaluation_configs: Vec<ImportAutoEvalEntry>,
}

#[tauri::command]
pub async fn settings_import(
    state: State<'_, Arc<RwLock<AppState>>>,
    data: SettingsImportData,
) -> Result<String, String> {
    let db = get_db(&state)?;
    let mut imported = 0usize;

    // 导入 admin_settings：upsert 逻辑
    for entry in data.settings {
        let existing = admin_settings::Entity::find()
            .filter(admin_settings::Column::SettingKey.eq(&entry.setting_key))
            .one(&db)
            .await
            .map_err(|e| e.to_string())?;

        if let Some(model) = existing {
            let mut active: admin_settings::ActiveModel = model.into();
            active.setting_value = Set(entry.setting_value);
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
        } else {
            let new_setting = admin_settings::ActiveModel {
                setting_key: Set(entry.setting_key),
                setting_value: Set(entry.setting_value),
                ..Default::default()
            };
            new_setting.insert(&db).await.map_err(|e| e.to_string())?;
        }
        imported += 1;
    }

    // 导入 evaluation_items：追加
    for entry in data.evaluation_items {
        let item = evaluation_item::ActiveModel {
            name: Set(entry.name),
            score_change: Set(entry.score_change),
            category: Set(entry.category),
            is_quick_access: Set(entry.is_quick_access),
            ..Default::default()
        };
        item.insert(&db).await.map_err(|e| e.to_string())?;
        imported += 1;
    }

    // 导入 auto_evaluation_configs：追加
    for entry in data.auto_evaluation_configs {
        let config = auto_evaluation_config::ActiveModel {
            name: Set(entry.name),
            trigger_type: Set(entry.trigger_type),
            trigger_time: Set(entry.trigger_time),
            day_of_week: Set(entry.day_of_week),
            day_of_month: Set(entry.day_of_month),
            evaluation_item_id: Set(entry.evaluation_item_id),
            score_change: Set(entry.score_change),
            reason: Set(entry.reason),
            target_type: Set(entry.target_type),
            target_group_id: Set(entry.target_group_id),
            target_student_id: Set(entry.target_student_id),
            is_enabled: Set(entry.is_enabled),
            ..Default::default()
        };
        config.insert(&db).await.map_err(|e| e.to_string())?;
        imported += 1;
    }

    Ok(format!("成功导入 {} 条设置记录", imported))
}

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
