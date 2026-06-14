use crate::db::entities::admin_settings;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use std::sync::Arc;
use tauri::State;

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
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
