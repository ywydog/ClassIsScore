use crate::db::entities::score_record;
use crate::db::entities::student;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ColumnTrait, EntityTrait, QueryFilter, QueryOrder};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

use super::get_db;

#[derive(Debug, Serialize, Deserialize)]
pub struct LeaderboardEntry {
    pub student: student::Model,
    pub rank: i64,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct IndividualStats {
    pub student: student::Model,
    pub total_positive: i64,
    pub total_negative: i64,
    pub record_count: i64,
}

#[tauri::command]
pub async fn leaderboard_query(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<LeaderboardEntry>, String> {
    let db = get_db(&state)?;

    let students = student::Entity::find()
        .order_by_desc(student::Column::TotalScore)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let entries = students
        .into_iter()
        .enumerate()
        .map(|(i, s)| LeaderboardEntry {
            student: s,
            rank: (i + 1) as i64,
        })
        .collect();

    Ok(entries)
}

#[tauri::command]
pub async fn leaderboard_by_group(
    state: State<'_, Arc<RwLock<AppState>>>,
    group_id: i64,
) -> Result<Vec<LeaderboardEntry>, String> {
    let db = get_db(&state)?;

    let students = student::Entity::find()
        .filter(student::Column::GroupId.eq(group_id))
        .order_by_desc(student::Column::TotalScore)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let entries = students
        .into_iter()
        .enumerate()
        .map(|(i, s)| LeaderboardEntry {
            student: s,
            rank: (i + 1) as i64,
        })
        .collect();

    Ok(entries)
}

#[tauri::command]
pub async fn leaderboard_individual(
    state: State<'_, Arc<RwLock<AppState>>>,
    student_id: i64,
) -> Result<IndividualStats, String> {
    let db = get_db(&state)?;

    let student_model = student::Entity::find_by_id(student_id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "学生不存在".to_string())?;

    let records = score_record::Entity::find()
        .filter(score_record::Column::StudentId.eq(student_id))
        .filter(score_record::Column::Reverted.eq(false))
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let total_positive: i64 = records.iter().map(|r| r.score_change.max(0) as i64).sum();
    let total_negative: i64 = records.iter().map(|r| r.score_change.min(0).abs() as i64).sum();

    Ok(IndividualStats {
        student: student_model,
        total_positive,
        total_negative,
        record_count: records.len() as i64,
    })
}
