use chrono::NaiveDateTime;
use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "score_records")]
pub struct Model {
    #[sea_orm(primary_key)]
    pub id: i64,
    pub student_id: i64,
    pub score_change: i32,
    pub reason: Option<String>,
    pub category: Option<String>,
    pub operator_id: Option<i64>,
    pub can_quick_revert: bool,
    pub reverted: bool,
    pub created_at: NaiveDateTime,
}

#[derive(Copy, Clone, Debug, EnumIter, DeriveRelation)]
pub enum Relation {
    #[sea_orm(
        belongs_to = "super::student::Entity",
        from = "Column::StudentId",
        to = "super::student::Column::Id"
    )]
    Student,
}

impl Related<super::student::Entity> for Entity {
    fn to() -> RelationDef {
        Relation::Student.def()
    }
}

impl ActiveModelBehavior for ActiveModel {}
