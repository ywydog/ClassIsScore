use crate::db::entities::score_record;
use crate::db::entities::student;
use crate::db::entities::student_group;
use crate::state::AppState;
use chrono::NaiveDateTime;
use parking_lot::RwLock;
use sea_orm::{ColumnTrait, EntityTrait, QueryFilter, QueryOrder};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::State;

use super::get_db;

/// 解析前端传来的 ISO 时间字符串（形如 "2024-12-01T00:00:00"）。
/// 解析失败返回错误。
fn parse_iso(s: &str) -> Result<NaiveDateTime, String> {
    NaiveDateTime::parse_from_str(s, "%Y-%m-%dT%H:%M:%S")
        .map_err(|e| format!("时间格式错误 {}: {}", s, e))
}

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

/// 个人排行榜（带可选时间范围）
///
/// - `start_time` / `end_time` 都是 ISO 字符串（"2024-12-01T00:00:00"）。
/// - 两个都为 `None` 时按学生表里累计的 `total_score` 排序（最快路径）。
/// - 任意一个给出时，按时间范围过滤 `score_records` 重新聚合。
#[tauri::command]
pub async fn leaderboard_query(
    state: State<'_, Arc<RwLock<AppState>>>,
    start_time: Option<String>,
    end_time: Option<String>,
) -> Result<Vec<LeaderboardEntry>, String> {
    let db = get_db(&state)?;

    // 无时间范围：用 students.total_score 直接排序（最高效）
    if start_time.is_none() && end_time.is_none() {
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
        return Ok(entries);
    }

    // 有时间范围：按 score_records 聚合
    let start = start_time.as_deref().map(parse_iso).transpose()?;
    let end = end_time.as_deref().map(parse_iso).transpose()?;

    let mut query = score_record::Entity::find()
        .filter(score_record::Column::Reverted.eq(false));
    if let Some(s) = start {
        query = query.filter(score_record::Column::CreatedAt.gte(s));
    }
    if let Some(e) = end {
        query = query.filter(score_record::Column::CreatedAt.lte(e));
    }
    let records = query
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    // 内存聚合：student_id -> total_score
    use std::collections::HashMap;
    let mut buckets: HashMap<i64, i64> = HashMap::new();
    for r in records {
        *buckets.entry(r.student_id).or_insert(0) += r.score_change as i64;
    }

    // 拉所有学生按聚合值排序
    let students = student::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let mut entries: Vec<LeaderboardEntry> = students
        .into_iter()
        .map(|s| LeaderboardEntry { student: s, rank: 0 })
        .collect();
    // 按该时间范围的总分降序
    entries.sort_by(|a, b| {
        let sa = buckets.get(&a.student.id).copied().unwrap_or(0);
        let sb = buckets.get(&b.student.id).copied().unwrap_or(0);
        sb.cmp(&sa)
    });
    for (i, e) in entries.iter_mut().enumerate() {
        e.rank = (i + 1) as i64;
    }
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

/// 小组排行榜（带可选时间范围）
///
/// - 无时间范围：按 students.total_score 按 group 聚合（最高效）。
/// - 有时间范围：按 score_records 在窗口内按 group 聚合。
#[tauri::command]
pub async fn leaderboard_all_groups(
    state: State<'_, Arc<RwLock<AppState>>>,
    start_time: Option<String>,
    end_time: Option<String>,
) -> Result<Vec<GroupLeaderboardEntry>, String> {
    let db = get_db(&state)?;

    let groups = student_group::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    use std::collections::HashMap;
    let mut buckets: HashMap<i64, (String, i64, i64)> = HashMap::new();
    for g in &groups {
        buckets.insert(g.id, (g.name.clone(), 0, 0));
    }

    if start_time.is_none() && end_time.is_none() {
        // 快路径：用 students.total_score 聚合
        let students = student::Entity::find()
            .all(&db)
            .await
            .map_err(|e| e.to_string())?;
        for s in students {
            if let Some(gid) = s.group_id {
                if let Some(entry) = buckets.get_mut(&gid) {
                    entry.1 += s.total_score as i64;
                    entry.2 += 1;
                }
            }
        }
    } else {
        // 慢路径：按 score_records 时间窗口聚合
        let start = start_time.as_deref().map(parse_iso).transpose()?;
        let end = end_time.as_deref().map(parse_iso).transpose()?;

        let mut query = score_record::Entity::find()
            .filter(score_record::Column::Reverted.eq(false));
        if let Some(s) = start {
            query = query.filter(score_record::Column::CreatedAt.gte(s));
        }
        if let Some(e) = end {
            query = query.filter(score_record::Column::CreatedAt.lte(e));
        }
        let records = query
            .all(&db)
            .await
            .map_err(|e| e.to_string())?;

        // 先做 student_id -> group_id 映射
        let students = student::Entity::find()
            .all(&db)
            .await
            .map_err(|e| e.to_string())?;
        let student_to_group: HashMap<i64, i64> = students
            .iter()
            .filter_map(|s| s.group_id.map(|g| (s.id, g)))
            .collect();

        for r in records {
            if let Some(&gid) = student_to_group.get(&r.student_id) {
                if let Some(entry) = buckets.get_mut(&gid) {
                    entry.1 += r.score_change as i64;
                }
            }
        }
        // 人数仍按全量 students 统计
        let students = student::Entity::find()
            .all(&db)
            .await
            .map_err(|e| e.to_string())?;
        for s in students {
            if let Some(gid) = s.group_id {
                if let Some(entry) = buckets.get_mut(&gid) {
                    entry.2 += 1;
                }
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
