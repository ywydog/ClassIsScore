use crate::db::entities::evaluation_item;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, EntityTrait, QueryOrder, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

#[derive(Debug, Serialize, Deserialize)]
pub struct EvaluationCreateInput {
    pub name: String,
    pub score_change: i32,
    pub category: Option<String>,
    pub is_quick_access: Option<bool>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct EvaluationUpdateInput {
    pub id: i64,
    pub name: Option<String>,
    pub score_change: Option<i32>,
    #[serde(default)]
    pub category: Option<Option<String>>,
    pub is_quick_access: Option<bool>,
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[tauri::command]
pub async fn evaluation_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<evaluation_item::Model>, String> {
    let db = get_db(&state)?;

    let items = evaluation_item::Entity::find()
        .order_by_asc(evaluation_item::Column::Id)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(items)
}

#[tauri::command]
pub async fn evaluation_create(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: EvaluationCreateInput,
) -> Result<evaluation_item::Model, String> {
    let db = get_db(&state)?;

    let item = evaluation_item::ActiveModel {
        name: Set(input.name),
        score_change: Set(input.score_change),
        category: Set(input.category),
        is_quick_access: Set(input.is_quick_access.unwrap_or(false)),
        ..Default::default()
    };

    let result = item.insert(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn evaluation_update(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: EvaluationUpdateInput,
) -> Result<evaluation_item::Model, String> {
    let db = get_db(&state)?;

    let existing: evaluation_item::ActiveModel = evaluation_item::Entity::find_by_id(input.id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "评估项不存在".to_string())?
        .into();

    let updated = evaluation_item::ActiveModel {
        name: input.name.map(Set).unwrap_or(existing.name),
        score_change: input.score_change.map(Set).unwrap_or(existing.score_change),
        category: match input.category {
            None => existing.category,
            Some(None) => Set(None),
            Some(Some(v)) => Set(Some(v)),
        },
        is_quick_access: input.is_quick_access.map(Set).unwrap_or(existing.is_quick_access),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn evaluation_delete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    evaluation_item::Entity::delete_by_id(id)
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(())
}
