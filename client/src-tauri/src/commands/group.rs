use crate::db::entities::student_group;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, EntityTrait, QueryOrder, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

#[derive(Debug, Serialize, Deserialize)]
pub struct GroupCreateInput {
    pub name: String,
    pub description: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct GroupUpdateInput {
    pub id: i64,
    pub name: Option<String>,
    pub description: Option<String>,
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[tauri::command]
pub async fn group_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<student_group::Model>, String> {
    let db = get_db(&state)?;

    let groups = student_group::Entity::find()
        .order_by_asc(student_group::Column::Id)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(groups)
}

#[tauri::command]
pub async fn group_create(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: GroupCreateInput,
) -> Result<student_group::Model, String> {
    let db = get_db(&state)?;

    let group = student_group::ActiveModel {
        name: Set(input.name),
        description: Set(input.description),
        ..Default::default()
    };

    let result = group.insert(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn group_update(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: GroupUpdateInput,
) -> Result<student_group::Model, String> {
    let db = get_db(&state)?;

    let existing: student_group::ActiveModel = student_group::Entity::find_by_id(input.id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "小组不存在".to_string())?
        .into();

    let updated = student_group::ActiveModel {
        name: input.name.map(Set).unwrap_or(existing.name),
        description: input.description.map(|v| Set(Some(v))).unwrap_or(existing.description),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn group_delete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    student_group::Entity::delete_by_id(id)
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(())
}
