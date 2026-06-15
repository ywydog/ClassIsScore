use crate::db::entities::settlement_record;
use crate::db::entities::student;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, EntityTrait, QueryOrder, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::{Emitter, State};

#[derive(Debug, Serialize, Deserialize)]
pub struct SettlementCreateInput {
    pub name: String,
    pub period: Option<String>,
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}

#[tauri::command]
pub async fn settlement_list(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<settlement_record::Model>, String> {
    let db = get_db(&state)?;

    let records = settlement_record::Entity::find()
        .order_by_desc(settlement_record::Column::CreatedAt)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(records)
}

#[tauri::command]
pub async fn settlement_create(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: SettlementCreateInput,
) -> Result<settlement_record::Model, String> {
    let db = get_db(&state)?;

    // 快照当前所有学生数据
    let students = student::Entity::find()
        .order_by_asc(student::Column::Id)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let snapshot_data = serde_json::to_string(&students).map_err(|e| e.to_string())?;

    let record = settlement_record::ActiveModel {
        name: Set(input.name),
        period: Set(input.period),
        snapshot_data: Set(Some(snapshot_data)),
        status: Set(0),
        ..Default::default()
    };

    let result = record.insert(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn settlement_complete(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<settlement_record::Model, String> {
    let db = get_db(&state)?;

    let existing: settlement_record::ActiveModel = settlement_record::Entity::find_by_id(id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "结算记录不存在".to_string())?
        .into();

    let updated = settlement_record::ActiveModel {
        status: Set(1),
        updated_at: Set(chrono::Local::now().naive_utc()),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;
    Ok(result)
}

#[tauri::command]
pub async fn settlement_rollback(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<settlement_record::Model, String> {
    let db = get_db(&state)?;

    let record = settlement_record::Entity::find_by_id(id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "结算记录不存在".to_string())?;

    let snapshot_str = record
        .snapshot_data
        .clone()
        .ok_or_else(|| "快照数据不存在".to_string())?;

    let snapshot_students: Vec<student::Model> =
        serde_json::from_str(&snapshot_str).map_err(|e| e.to_string())?;

    // 从快照恢复学生分数
    for snap in snapshot_students {
        if let Some(current) = student::Entity::find_by_id(snap.id)
            .one(&db)
            .await
            .map_err(|e| e.to_string())?
        {
            let mut active: student::ActiveModel = current.into();
            active.total_score = Set(snap.total_score);
            active.pet_exp = Set(snap.pet_exp);
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
        }
    }

    // 更新结算状态为已回滚(2)
    let existing: settlement_record::ActiveModel = record.into();
    let updated = settlement_record::ActiveModel {
        status: Set(2),
        updated_at: Set(chrono::Local::now().naive_utc()),
        ..existing
    };

    let result = updated.update(&db).await.map_err(|e| e.to_string())?;

    // 发出积分更新事件
    let guard = state.read();
    if let Some(handle) = guard.app_handle.get() {
        let _ = handle.emit("score-update", serde_json::json!({
            "studentId": "all",
            "studentName": "",
            "scoreChange": 0,
            "newScore": 0,
            "reason": "结算回滚",
        }));
    }

    Ok(result)
}
