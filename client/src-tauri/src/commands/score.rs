use crate::db::entities::score_record;
use crate::db::entities::student;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, PaginatorTrait, QueryFilter, QueryOrder, QuerySelect, Set, TransactionTrait};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::{Emitter, State};

use super::get_db;

#[derive(Debug, Serialize, Deserialize)]
pub struct ScoreAddInput {
    pub student_id: i64,
    pub score_change: i32,
    pub reason: Option<String>,
    pub category: Option<String>,
    pub operator_id: Option<i64>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ScoreBatchAddInput {
    pub student_ids: Vec<i64>,
    pub score_change: i32,
    pub reason: Option<String>,
    pub category: Option<String>,
    pub operator_id: Option<i64>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ScoreStats {
    pub total_positive: i64,
    pub total_negative: i64,
    pub count: i64,
}

fn emit_score_update(
    state: &State<'_, Arc<RwLock<AppState>>>,
    student_id: i64,
    student_name: String,
    score_change: i32,
    new_score: i32,
    reason: String,
) {
    let guard = state.read();
    if let Some(handle) = guard.app_handle.get() {
        let payload = serde_json::json!({
            "studentId": student_id.to_string(),
            "studentName": student_name,
            "scoreChange": score_change,
            "newScore": new_score,
            "reason": reason,
        });
        let _ = handle.emit("score-update", payload);
    }
}

#[tauri::command]
pub async fn score_list(
    state: State<'_, Arc<RwLock<AppState>>>,
    student_id: Option<i64>,
    limit: Option<u64>,
) -> Result<Vec<score_record::Model>, String> {
    let db = get_db(&state)?;

    let mut query = score_record::Entity::find();
    if let Some(sid) = student_id {
        query = query.filter(score_record::Column::StudentId.eq(sid));
    }
    if let Some(lim) = limit {
        query = query.limit(lim);
    }

    let records = query
        .order_by_desc(score_record::Column::CreatedAt)
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(records)
}

#[tauri::command]
pub async fn score_add(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: ScoreAddInput,
) -> Result<score_record::Model, String> {
    let db = get_db(&state)?;

    let reason_text = input.reason.clone().unwrap_or_default();

    // 安全最佳实践：积分记录与学生总分必须原子写入，
    // 避免"积分记录存在但 total_score 未更新"的脏数据。
    let txn = db.begin().await.map_err(|e| e.to_string())?;

    // 创建积分记录
    let record = score_record::ActiveModel {
        student_id: Set(input.student_id),
        score_change: Set(input.score_change),
        reason: Set(input.reason),
        category: Set(input.category),
        operator_id: Set(input.operator_id),
        ..Default::default()
    };

    let result = record.insert(&txn).await.map_err(|e| e.to_string())?;

    // 更新学生总分
    let student_model = student::Entity::find_by_id(input.student_id)
        .one(&txn)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "学生不存在".to_string())?;

    let student_name = student_model.name.clone();
    let mut active: student::ActiveModel = student_model.into();
    let current_score = active.total_score.as_ref().clone();
    let new_score = current_score + input.score_change;
    active.total_score = Set(new_score);
    active.updated_at = Set(chrono::Local::now().naive_utc());
    active.update(&txn).await.map_err(|e| e.to_string())?;

    txn.commit().await.map_err(|e| e.to_string())?;

    // 事务提交后发出积分更新事件
    emit_score_update(&state, input.student_id, student_name, input.score_change, new_score, reason_text);

    Ok(result)
}

#[tauri::command]
pub async fn score_batch_add(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: ScoreBatchAddInput,
) -> Result<Vec<score_record::Model>, String> {
    let db = get_db(&state)?;

    let reason_text = input.reason.clone().unwrap_or_default();
    let txn = db.begin().await.map_err(|e| e.to_string())?;

    let mut results = Vec::new();
    for student_id in input.student_ids.iter() {
        let record = score_record::ActiveModel {
            student_id: Set(*student_id),
            score_change: Set(input.score_change),
            reason: Set(input.reason.clone()),
            category: Set(input.category.clone()),
            operator_id: Set(input.operator_id),
            ..Default::default()
        };

        // 事务在出错时通过 Drop 自动回滚，无需显式调用 rollback()
        let result = record
            .insert(&txn)
            .await
            .map_err(|e| e.to_string())?;

        // 更新学生总分
        if let Some(student_model) = student::Entity::find_by_id(*student_id)
            .one(&txn)
            .await
            .map_err(|e| e.to_string())?
        {
            let student_name = student_model.name.clone();
            let mut active: student::ActiveModel = student_model.into();
            let current_score = active.total_score.as_ref().clone();
            let new_score = current_score + input.score_change;
            active.total_score = Set(new_score);
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&txn).await.map_err(|e| e.to_string())?;

            results.push((result, student_name, new_score));
        } else {
            results.push((result, String::new(), 0));
        }
    }

    txn.commit().await.map_err(|e| e.to_string())?;

    // 事务提交后，发出所有积分更新事件
    for (result, student_name, new_score) in &results {
        emit_score_update(&state, result.student_id, student_name.clone(), input.score_change, *new_score, reason_text.clone());
    }

    Ok(results.into_iter().map(|(r, _, _)| r).collect())
}

#[tauri::command]
pub async fn score_revert(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    // 安全最佳实践：标记撤销 + 回退总分必须原子写入。
    let txn = db.begin().await.map_err(|e| e.to_string())?;

    let record = score_record::Entity::find_by_id(id)
        .one(&txn)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "积分记录不存在".to_string())?;

    if record.reverted {
        return Err("该记录已撤销".to_string());
    }

    // 提取需要的值
    let score_change = record.score_change;
    let student_id = record.student_id;
    let reason_text = format!("撤销: {}", record.reason.clone().unwrap_or_default());

    // 标记撤销
    let mut active: score_record::ActiveModel = record.into();
    active.reverted = Set(true);
    active.update(&txn).await.map_err(|e| e.to_string())?;

    // 回退学生总分
    let mut emit_payload: Option<(String, i32, String)> = None;
    if let Some(student_model) =
        student::Entity::find_by_id(student_id)
            .one(&txn)
            .await
            .map_err(|e| e.to_string())?
    {
        let student_name = student_model.name.clone();
        let mut student_active: student::ActiveModel = student_model.into();
        let current_score = student_active.total_score.as_ref().clone();
        let new_score = current_score - score_change;
        student_active.total_score = Set(new_score);
        student_active.updated_at = Set(chrono::Local::now().naive_utc());
        student_active.update(&txn).await.map_err(|e| e.to_string())?;

        emit_payload = Some((student_name, new_score, reason_text));
    }

    txn.commit().await.map_err(|e| e.to_string())?;

    // 事务提交后发出积分更新事件
    if let Some((student_name, new_score, reason_text)) = emit_payload {
        emit_score_update(&state, student_id, student_name, -score_change, new_score, reason_text);
    }

    Ok(())
}

#[tauri::command]
pub async fn score_recent(
    state: State<'_, Arc<RwLock<AppState>>>,
    limit: Option<u64>,
) -> Result<Vec<score_record::Model>, String> {
    let db = get_db(&state)?;

    let records = score_record::Entity::find()
        .filter(score_record::Column::Reverted.eq(false))
        .order_by_desc(score_record::Column::CreatedAt)
        .limit(limit.or(Some(20)))
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(records)
}

#[tauri::command]
pub async fn score_stats(
    state: State<'_, Arc<RwLock<AppState>>>,
    student_id: i64,
) -> Result<ScoreStats, String> {
    let db = get_db(&state)?;

    let records = score_record::Entity::find()
        .filter(score_record::Column::StudentId.eq(student_id))
        .filter(score_record::Column::Reverted.eq(false))
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let total_positive: i64 = records.iter().map(|r| r.score_change.max(0) as i64).sum();
    let total_negative: i64 = records.iter().map(|r| r.score_change.min(0).abs() as i64).sum();

    Ok(ScoreStats {
        total_positive,
        total_negative,
        count: records.len() as i64,
    })
}

/// 今日积分记录数（未撤销）。给 Dashboard 用。
#[tauri::command]
pub async fn score_today_count(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<i64, String> {
    let db = get_db(&state)?;
    let now = chrono::Local::now();
    let today_start = now
        .date_naive()
        .and_hms_opt(0, 0, 0)
        .ok_or_else(|| "无法计算今日开始时间".to_string())?;

    let count = score_record::Entity::find()
        .filter(score_record::Column::Reverted.eq(false))
        .filter(score_record::Column::CreatedAt.gte(today_start))
        .count(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(count as i64)
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ScoreTrendPoint {
    pub date: String,
    pub count: i64,
}

/// 最近 `days` 天每日积分记录数（未撤销）。给 Dashboard 用。
#[tauri::command]
pub async fn score_trend(
    state: State<'_, Arc<RwLock<AppState>>>,
    days: Option<i64>,
) -> Result<Vec<ScoreTrendPoint>, String> {
    let db = get_db(&state)?;
    let days = days.unwrap_or(7).clamp(1, 90);

    let now = chrono::Local::now();
    let start_date = now.date_naive() - chrono::Duration::days(days - 1);
    let start_dt = start_date
        .and_hms_opt(0, 0, 0)
        .ok_or_else(|| "无法计算起始时间".to_string())?;

    let records = score_record::Entity::find()
        .filter(score_record::Column::Reverted.eq(false))
        .filter(score_record::Column::CreatedAt.gte(start_dt))
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    // 用 HashMap 聚合；空缺的日期补 0
    let mut buckets: std::collections::BTreeMap<String, i64> = std::collections::BTreeMap::new();
    for i in 0..days {
        let d = start_date + chrono::Duration::days(i);
        buckets.insert(d.format("%Y-%m-%d").to_string(), 0);
    }
    for r in &records {
        let key = r.created_at.date().format("%Y-%m-%d").to_string();
        if let Some(v) = buckets.get_mut(&key) {
            *v += 1;
        }
    }

    Ok(buckets
        .into_iter()
        .map(|(date, count)| ScoreTrendPoint { date, count })
        .collect())
}
