use crate::db::entities::score_record;
use crate::db::entities::student;
use crate::db::entities::student_group;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ColumnTrait, EntityTrait, QueryFilter, QueryOrder, QuerySelect};
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

#[derive(Debug, Serialize, Deserialize)]
pub struct GroupLeaderboardEntry {
    pub group_id: i64,
    pub group_name: String,
    pub total_score: i64,
    pub student_count: i64,
}

/// 按小组聚合：每个小组的总积分 + 学生数，从高到低排序。
#[tauri::command]
pub async fn leaderboard_all_groups(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<GroupLeaderboardEntry>, String> {
    use std::collections::HashMap;
    let db = get_db(&state)?;

    let groups = student_group::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let students = student::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let mut buckets: HashMap<i64, (String, i64, i64)> = HashMap::new();
    for g in groups {
        buckets.insert(g.id, (g.name, 0, 0));
    }
    for s in students {
        if let Some(gid) = s.group_id {
            if let Some(entry) = buckets.get_mut(&gid) {
                entry.1 += s.total_score as i64;
                entry.2 += 1;
            }
        }
    }

    let mut result: Vec<GroupLeaderboardEntry> = buckets
        .into_iter()
        .map(|(group_id, (name, score, count))| GroupLeaderboardEntry {
            group_id,
            group_name: name,
            total_score: score,
            student_count: count,
        })
        .collect();
    result.sort_by(|a, b| b.total_score.cmp(&a.total_score));
    Ok(result)
}
