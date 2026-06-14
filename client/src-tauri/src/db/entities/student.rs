use chrono::NaiveDateTime;
use sea_orm::entity::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Clone, Debug, PartialEq, DeriveEntityModel, Serialize, Deserialize)]
#[sea_orm(table_name = "students")]
pub struct Model {
    #[sea_orm(primary_key)]
    pub id: i64,
    pub name: String,
    pub student_number: Option<String>,
    pub group_id: Option<i64>,
    pub total_score: i32,
    pub avatar: Option<String>,
    pub pet_type: Option<String>,
    pub pet_name: Option<String>,
    pub pet_exp: i32,
    pub created_at: NaiveDateTime,
    pub updated_at: NaiveDateTime,
}

#[derive(Copy, Clone, Debug, EnumIter, DeriveRelation)]
pub enum Relation {
    #[sea_orm(has_many = "super::score_record::Entity")]
    ScoreRecord,
    #[sea_orm(
        belongs_to = "super::student_group::Entity",
        from = "Column::GroupId",
        to = "super::student_group::Column::Id"
    )]
    StudentGroup,
}

impl Related<super::score_record::Entity> for Entity {
    fn to() -> RelationDef {
        Relation::ScoreRecord.def()
    }
}

impl Related<super::student_group::Entity> for Entity {
    fn to() -> RelationDef {
        Relation::StudentGroup.def()
    }
}

impl ActiveModelBehavior for ActiveModel {}
