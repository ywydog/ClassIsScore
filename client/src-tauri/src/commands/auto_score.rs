use crate::db::entities::auto_evaluation_config;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, EntityTrait, QueryOrder, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

#[derive(Debug, Serialize, Deserialize)]
pub struct AutoScoreRuleInput {
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
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[tauri::command]
pub async fn auto_score_get_rules(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<auto_evaluation_config::Model>, String> {
    let db = get_db(&state)?;

    let rules = auto_evaluation_config::Entity::find()
        .order_by_asc(auto_evaluation_config::Column::Id)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(rules)
}

#[tauri::command]
pub async fn auto_score_add_rule(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: AutoScoreRuleInput,
) -> Result<auto_evaluation_config::Model, String> {
    let db = get_db(&state)?;

    let rule = auto_evaluation_config::ActiveModel {
        name: Set(input.name),
        trigger_type: Set(input.trigger_type),
        trigger_time: Set(input.trigger_time),
        day_of_week: Set(input.day_of_week),
        day_of_month: Set(input.day_of_month),
        evaluation_item_id: Set(input.evaluation_item_id),
        score_change: Set(input.score_change),
        reason: Set(input.reason),
        target_type: Set(input.target_type),
        target_group_id: Set(input.target_group_id),
        target_student_id: Set(input.target_student_id),
        is_enabled: Set(true),
        ..Default::default()
    };

    let result = rule.insert(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn auto_score_update_rule(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
    input: AutoScoreRuleInput,
) -> Result<auto_evaluation_config::Model, String> {
    let db = get_db(&state)?;

    let existing: auto_evaluation_config::ActiveModel =
        auto_evaluation_config::Entity::find_by_id(id)
            .one(&db)
            .await
            .map_err(|e| e.to_string())?
            .ok_or_else(|| "规则不存在".to_string())?
            .into();

    let updated = auto_evaluation_config::ActiveModel {
        name: Set(input.name),
        trigger_type: Set(input.trigger_type),
        trigger_time: Set(input.trigger_time),
        day_of_week: Set(input.day_of_week),
        day_of_month: Set(input.day_of_month),
        evaluation_item_id: Set(input.evaluation_item_id),
        score_change: Set(input.score_change),
        reason: Set(input.reason),
        target_type: Set(input.target_type),
        target_group_id: Set(input.target_group_id),
        target_student_id: Set(input.target_student_id),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn auto_score_delete_rule(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    auto_evaluation_config::Entity::delete_by_id(id)
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(())
}

#[tauri::command]
pub async fn auto_score_toggle_rule(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
    is_enabled: bool,
) -> Result<auto_evaluation_config::Model, String> {
    let db = get_db(&state)?;

    let existing: auto_evaluation_config::ActiveModel =
        auto_evaluation_config::Entity::find_by_id(id)
            .one(&db)
            .await
            .map_err(|e| e.to_string())?
            .ok_or_else(|| "规则不存在".to_string())?
            .into();

    let updated = auto_evaluation_config::ActiveModel {
        is_enabled: Set(is_enabled),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}
