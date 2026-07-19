use chrono::NaiveDateTime;
use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "auto_evaluation_configs")]
pub struct Model {
    #[sea_orm(primary_key)]
    pub id: i64,
    /// 安全最佳实践：外部 API 一律使用 public_id（UUID v4），不暴露自增 i64，
    /// 避免攻击者从 ID 推算其他规则或系统规模。
    #[sea_orm(unique)]
    pub public_id: String,
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
    pub is_enabled: bool,
    pub last_executed_at: Option<NaiveDateTime>,
    pub created_at: NaiveDateTime,
    pub updated_at: NaiveDateTime,
}

#[derive(Copy, Clone, Debug, EnumIter, DeriveRelation)]
pub enum Relation {}

impl ActiveModelBehavior for ActiveModel {}
