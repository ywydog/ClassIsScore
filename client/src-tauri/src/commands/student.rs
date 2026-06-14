use crate::db::entities::student;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, QueryOrder, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

#[derive(Debug, Serialize, Deserialize)]
pub struct StudentCreateInput {
    pub name: String,
    pub student_number: Option<String>,
    pub group_id: Option<i64>,
    pub avatar: Option<String>,
    pub pet_type: Option<String>,
    pub pet_name: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct StudentUpdateInput {
    pub id: i64,
    pub name: Option<String>,
    pub student_number: Option<String>,
    pub group_id: Option<i64>,
    pub avatar: Option<String>,
    pub pet_type: Option<String>,
    pub pet_name: Option<String>,
    pub pet_exp: Option<i32>,
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[tauri::command]
pub async fn student_list(
    state: State<'_, Arc<RwLock<AppState>>>,
    group_id: Option<i64>,
) -> Result<Vec<student::Model>, String> {
    let db = get_db(&state)?;

    let query = student::Entity::find();
    let query = if let Some(gid) = group_id {
        query.filter(student::Column::GroupId.eq(gid))
    } else {
        query
    };

    let students = query
        .order_by_asc(student::Column::Id)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(students)
}

#[tauri::command]
pub async fn student_get(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<student::Model, String> {
    let db = get_db(&state)?;

    student::Entity::find_by_id(id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "学生不存在".to_string())
}

#[tauri::command]
pub async fn student_create(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: StudentCreateInput,
) -> Result<student::Model, String> {
    let db = get_db(&state)?;

    let student = student::ActiveModel {
        name: Set(input.name),
        student_number: Set(input.student_number),
        group_id: Set(input.group_id),
        avatar: Set(input.avatar),
        pet_type: Set(input.pet_type),
        pet_name: Set(input.pet_name),
        ..Default::default()
    };

    let result = student.insert(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn student_update(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: StudentUpdateInput,
) -> Result<student::Model, String> {
    let db = get_db(&state)?;

    let existing: student::ActiveModel = student::Entity::find_by_id(input.id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "学生不存在".to_string())?
        .into();

    let updated = student::ActiveModel {
        name: input.name.map(Set).unwrap_or(existing.name),
        student_number: input.student_number.map(|v| Set(Some(v))).unwrap_or(existing.student_number),
        group_id: input.group_id.map(|v| Set(Some(v))).unwrap_or(existing.group_id),
        avatar: input.avatar.map(|v| Set(Some(v))).unwrap_or(existing.avatar),
        pet_type: input.pet_type.map(|v| Set(Some(v))).unwrap_or(existing.pet_type),
        pet_name: input.pet_name.map(|v| Set(Some(v))).unwrap_or(existing.pet_name),
        pet_exp: input.pet_exp.map(Set).unwrap_or(existing.pet_exp),
        updated_at: Set(chrono::Local::now().naive_utc()),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn student_delete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    student::Entity::delete_by_id(id)
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(())
}

#[tauri::command]
pub async fn student_batch_create(
    state: State<'_, Arc<RwLock<AppState>>>,
    students: Vec<StudentCreateInput>,
) -> Result<Vec<student::Model>, String> {
    let db = get_db(&state)?;

    let mut results = Vec::new();
    for input in students {
        let student = student::ActiveModel {
            name: Set(input.name),
            student_number: Set(input.student_number),
            group_id: Set(input.group_id),
            avatar: Set(input.avatar),
            pet_type: Set(input.pet_type),
            pet_name: Set(input.pet_name),
            ..Default::default()
        };
        let result = student.insert(&db).await.map_err(|e| e.to_string())?;
        results.push(result);
    }

    Ok(results)
}

#[tauri::command]
pub async fn student_reset_scores(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<(), String> {
    let db = get_db(&state)?;

    let students = student::Entity::find().all(&db).await.map_err(|e| e.to_string())?;

    for s in students {
        let mut active: student::ActiveModel = s.into();
        active.total_score = Set(0);
        active.pet_exp = Set(0);
        active.updated_at = Set(chrono::Local::now().naive_utc());
        active.update(&db).await.map_err(|e| e.to_string())?;
    }

    Ok(())
}
