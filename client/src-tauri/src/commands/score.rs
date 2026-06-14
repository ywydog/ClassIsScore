use crate::db::entities::score_record;
use crate::db::entities::student;
use crate::state::AppState;
use chrono::Datelike;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, QueryOrder, QuerySelect, Set};
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::{Emitter, State};

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

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
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

    // 创建积分记录
    let record = score_record::ActiveModel {
        student_id: Set(input.student_id),
        score_change: Set(input.score_change),
        reason: Set(input.reason),
        category: Set(input.category),
        operator_id: Set(input.operator_id),
        ..Default::default()
    };

    let result = record.insert(&db).await.map_err(|e| e.to_string())?;

    // 更新学生总分
    let student_model = student::Entity::find_by_id(input.student_id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "学生不存在".to_string())?;

    let mut active: student::ActiveModel = student_model.into();
    let current_score = active.total_score.as_ref().clone();
    let new_score = current_score + input.score_change;
    active.total_score = Set(new_score);
    active.updated_at = Set(chrono::Local::now().naive_utc());
    let updated = active.update(&db).await.map_err(|e| e.to_string())?;

    emit_score_update(
        &state,
        input.student_id,
        updated.name.clone(),
        input.score_change,
        updated.total_score,
        reason_text,
    );

    Ok(result)
}

#[tauri::command]
pub async fn score_batch_add(
    state: State<'_, Arc<RwLock<AppState>>>,
    input: ScoreBatchAddInput,
) -> Result<Vec<score_record::Model>, String> {
    let db = get_db(&state)?;

    let mut results = Vec::new();
    for student_id in input.student_ids {
        let record = score_record::ActiveModel {
            student_id: Set(student_id),
            score_change: Set(input.score_change),
            reason: Set(input.reason.clone()),
            category: Set(input.category.clone()),
            operator_id: Set(input.operator_id),
            ..Default::default()
        };

        let result = record.insert(&db).await.map_err(|e| e.to_string())?;

        // 更新学生总分
        if let Some(student_model) =
            student::Entity::find_by_id(student_id)
                .one(&db)
                .await
                .map_err(|e| e.to_string())?
        {
            let mut active: student::ActiveModel = student_model.into();
            let current_score = active.total_score.as_ref().clone();
            active.total_score = Set(current_score + input.score_change);
            active.updated_at = Set(chrono::Local::now().naive_utc());
            let updated = active.update(&db).await.map_err(|e| e.to_string())?;

            emit_score_update(
                &state,
                student_id,
                updated.name.clone(),
                input.score_change,
                updated.total_score,
                input.reason.clone().unwrap_or_default(),
            );
        }

        results.push(result);
    }

    Ok(results)
}

#[tauri::command]
pub async fn score_revert(
    state: State<'_, Arc<RwLock<AppState>>>,
    id: i64,
) -> Result<(), String> {
    let db = get_db(&state)?;

    let record = score_record::Entity::find_by_id(id)
        .one(&db)
        .await
        .map_err(|e| e.to_string())?
        .ok_or_else(|| "积分记录不存在".to_string())?;

    if record.reverted {
        return Err("该记录已撤销".to_string());
    }

    // 提取需要的值
    let score_change = record.score_change;
    let student_id = record.student_id;

    // 标记撤销
    let mut active: score_record::ActiveModel = record.into();
    active.reverted = Set(true);
    active.update(&db).await.map_err(|e| e.to_string())?;

    // 回退学生总分
    if let Some(student_model) =
        student::Entity::find_by_id(student_id)
            .one(&db)
            .await
            .map_err(|e| e.to_string())?
    {
        let mut student_active: student::ActiveModel = student_model.into();
        let current_score = student_active.total_score.as_ref().clone();
        student_active.total_score = Set(current_score - score_change);
        student_active.updated_at = Set(chrono::Local::now().naive_utc());
        let updated = student_active.update(&db).await.map_err(|e| e.to_string())?;

        emit_score_update(
            &state,
            student_id,
            updated.name.clone(),
            -score_change, // 撤销是反向变化
            updated.total_score,
            format!("撤销积分记录"),
        );
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

#[derive(Debug, Serialize, Deserialize)]
pub struct StudentScoreStatsEntry {
    pub student_id: i64,
    pub student_name: String,
    pub total_score: i32,
    pub day_plus: i64,
    pub day_minus: i64,
    pub day_net: i64,
    pub week_plus: i64,
    pub week_minus: i64,
    pub week_net: i64,
    pub month_plus: i64,
    pub month_minus: i64,
    pub month_net: i64,
}

#[tauri::command]
pub async fn score_stats_all(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<Vec<StudentScoreStatsEntry>, String> {
    let db = get_db(&state)?;

    let now = chrono::Local::now();
    let day_start = now.date_naive().and_hms_opt(0, 0, 0).unwrap_or_default();
    let week_start = (now - chrono::Duration::days(now.weekday().num_days_from_monday() as i64))
        .date_naive()
        .and_hms_opt(0, 0, 0)
        .unwrap_or_default();
    let month_start = now.date_naive().with_day(1).unwrap_or_default().and_hms_opt(0, 0, 0).unwrap_or_default();

    let students = student::Entity::find()
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let all_records = score_record::Entity::find()
        .filter(score_record::Column::Reverted.eq(false))
        .all(&db)
        .await
        .map_err(|e| e.to_string())?;

    let mut result = Vec::new();
    for s in &students {
        let sid = s.id;
        let student_records: Vec<&score_record::Model> = all_records.iter().filter(|r| r.student_id == sid).collect();

        let day_plus: i64 = student_records.iter()
            .filter(|r| r.created_at >= day_start)
            .map(|r| r.score_change.max(0) as i64)
            .sum();
        let day_minus: i64 = student_records.iter()
            .filter(|r| r.created_at >= day_start)
            .map(|r| r.score_change.min(0).abs() as i64)
            .sum();

        let week_plus: i64 = student_records.iter()
            .filter(|r| r.created_at >= week_start)
            .map(|r| r.score_change.max(0) as i64)
            .sum();
        let week_minus: i64 = student_records.iter()
            .filter(|r| r.created_at >= week_start)
            .map(|r| r.score_change.min(0).abs() as i64)
            .sum();

        let month_plus: i64 = student_records.iter()
            .filter(|r| r.created_at >= month_start)
            .map(|r| r.score_change.max(0) as i64)
            .sum();
        let month_minus: i64 = student_records.iter()
            .filter(|r| r.created_at >= month_start)
            .map(|r| r.score_change.min(0).abs() as i64)
            .sum();

        result.push(StudentScoreStatsEntry {
            student_id: sid,
            student_name: s.name.clone(),
            total_score: s.total_score,
            day_plus,
            day_minus,
            day_net: day_plus - day_minus,
            week_plus,
            week_minus,
            week_net: week_plus - week_minus,
            month_plus,
            month_minus,
            month_net: month_plus - month_minus,
        });
    }

    Ok(result)
}
