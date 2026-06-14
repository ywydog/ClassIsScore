use sea_orm::DatabaseConnection;
use sea_orm_migration::prelude::*;
use sea_orm_migration::SchemaManager;

pub struct Migration;

impl MigrationName for Migration {
    fn name(&self) -> &str {
        "m20240601_000001_initial"
    }
}

#[async_trait::async_trait]
impl MigrationTrait for Migration {
    async fn up(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        // 学生表
        manager
            .create_table(
                Table::create()
                    .table(Student::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(Student::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(Student::Name).string().not_null())
                    .col(ColumnDef::new(Student::StudentNumber).string().null())
                    .col(ColumnDef::new(Student::GroupId).big_integer().null())
                    .col(ColumnDef::new(Student::TotalScore).integer().not_null().default(0))
                    .col(ColumnDef::new(Student::Avatar).string().null())
                    .col(ColumnDef::new(Student::PetType).string().null())
                    .col(ColumnDef::new(Student::PetName).string().null())
                    .col(ColumnDef::new(Student::PetExp).integer().not_null().default(0))
                    .col(ColumnDef::new(Student::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(Student::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 积分记录表
        manager
            .create_table(
                Table::create()
                    .table(ScoreRecord::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(ScoreRecord::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(ScoreRecord::StudentId).big_integer().not_null())
                    .col(ColumnDef::new(ScoreRecord::ScoreChange).integer().not_null())
                    .col(ColumnDef::new(ScoreRecord::Reason).string().null())
                    .col(ColumnDef::new(ScoreRecord::Category).string().null())
                    .col(ColumnDef::new(ScoreRecord::OperatorId).big_integer().null())
                    .col(ColumnDef::new(ScoreRecord::CanQuickRevert).boolean().not_null().default(true))
                    .col(ColumnDef::new(ScoreRecord::Reverted).boolean().not_null().default(false))
                    .col(ColumnDef::new(ScoreRecord::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .foreign_key(
                        ForeignKey::create()
                            .from(ScoreRecord::Table, ScoreRecord::StudentId)
                            .to(Student::Table, Student::Id)
                            .on_delete(ForeignKeyAction::Cascade),
                    )
                    .to_owned(),
            )
            .await?;

        // 小组表
        manager
            .create_table(
                Table::create()
                    .table(StudentGroup::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(StudentGroup::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(StudentGroup::Name).string().not_null())
                    .col(ColumnDef::new(StudentGroup::Description).string().null())
                    .col(ColumnDef::new(StudentGroup::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 评估项表
        manager
            .create_table(
                Table::create()
                    .table(EvaluationItem::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(EvaluationItem::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(EvaluationItem::Name).string().not_null())
                    .col(ColumnDef::new(EvaluationItem::ScoreChange).integer().not_null())
                    .col(ColumnDef::new(EvaluationItem::Category).string().null())
                    .col(ColumnDef::new(EvaluationItem::IsQuickAccess).boolean().not_null().default(false))
                    .col(ColumnDef::new(EvaluationItem::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 结算记录表
        manager
            .create_table(
                Table::create()
                    .table(SettlementRecord::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(SettlementRecord::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(SettlementRecord::Name).string().not_null())
                    .col(ColumnDef::new(SettlementRecord::Period).string().null())
                    .col(ColumnDef::new(SettlementRecord::SnapshotData).string().null())
                    .col(ColumnDef::new(SettlementRecord::Status).integer().not_null().default(0))
                    .col(ColumnDef::new(SettlementRecord::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 自动评估配置表
        manager
            .create_table(
                Table::create()
                    .table(AutoEvaluationConfig::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(AutoEvaluationConfig::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(AutoEvaluationConfig::Name).string().not_null())
                    .col(ColumnDef::new(AutoEvaluationConfig::TriggerType).string().not_null())
                    .col(ColumnDef::new(AutoEvaluationConfig::TriggerTime).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::DayOfWeek).integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::DayOfMonth).integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::EvaluationItemId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::ScoreChange).double().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::Reason).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::TargetType).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::TargetGroupId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::TargetStudentId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::IsEnabled).boolean().not_null().default(false))
                    .col(ColumnDef::new(AutoEvaluationConfig::LastExecutedAt).date_time().null())
                    .col(ColumnDef::new(AutoEvaluationConfig::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 管理员设置表
        manager
            .create_table(
                Table::create()
                    .table(AdminSettings::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(AdminSettings::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(AdminSettings::SettingKey).string().not_null().unique_key())
                    .col(ColumnDef::new(AdminSettings::SettingValue).string().null())
                    .col(ColumnDef::new(AdminSettings::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .drop_table(Table::drop().table(AdminSettings::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(AutoEvaluationConfig::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(SettlementRecord::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(EvaluationItem::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(ScoreRecord::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(Student::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(StudentGroup::Table).to_owned())
            .await?;
        Ok(())
    }
}

#[derive(Iden)]
enum Student {
    Table,
    Id,
    Name,
    StudentNumber,
    GroupId,
    TotalScore,
    Avatar,
    PetType,
    PetName,
    PetExp,
    CreatedAt,
    UpdatedAt,
}

#[derive(Iden)]
enum ScoreRecord {
    Table,
    Id,
    StudentId,
    ScoreChange,
    Reason,
    Category,
    OperatorId,
    CanQuickRevert,
    Reverted,
    CreatedAt,
}

#[derive(Iden)]
enum StudentGroup {
    Table,
    Id,
    Name,
    Description,
    CreatedAt,
}

#[derive(Iden)]
enum EvaluationItem {
    Table,
    Id,
    Name,
    ScoreChange,
    Category,
    IsQuickAccess,
    CreatedAt,
}

#[derive(Iden)]
enum SettlementRecord {
    Table,
    Id,
    Name,
    Period,
    SnapshotData,
    Status,
    CreatedAt,
}

#[derive(Iden)]
enum AutoEvaluationConfig {
    Table,
    Id,
    Name,
    TriggerType,
    TriggerTime,
    DayOfWeek,
    DayOfMonth,
    EvaluationItemId,
    ScoreChange,
    Reason,
    TargetType,
    TargetGroupId,
    TargetStudentId,
    IsEnabled,
    LastExecutedAt,
    CreatedAt,
}

#[derive(Iden)]
enum AdminSettings {
    Table,
    Id,
    SettingKey,
    SettingValue,
    UpdatedAt,
}

pub async fn run_migration(db: &DatabaseConnection) -> Result<(), sea_orm::DbErr> {
    let schema_manager = SchemaManager::new(db);
    Migration.up(&schema_manager).await
}
