use sea_orm::{ConnectionTrait, DatabaseBackend, DatabaseConnection, Statement};
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
        // 兼容旧版：清理单数表名（学生 / 积分记录 / 小组 / 评估项 / 结算记录 / 自动评估配置）
        // 旧版 Iden enum 是单数（Student / ScoreRecord / ...），与 entity 的复数 table_name 不匹配，
        // 旧表会被丢弃；新版本会用复数表名创建正确表。
        let conn = manager.get_connection();
        let legacy_tables = [
            "student", "score_record", "student_group",
            "evaluation_item", "settlement_record", "auto_evaluation_config",
        ];
        for legacy in legacy_tables.iter() {
            let _ = conn
                .execute(Statement::from_string(
                    DatabaseBackend::Sqlite,
                    format!("DROP TABLE IF EXISTS {}", legacy),
                ))
                .await;
        }

        // 学生表
        manager
            .create_table(
                Table::create()
                    .table(Students::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(Students::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(Students::Name).string().not_null())
                    .col(ColumnDef::new(Students::StudentNumber).string().null())
                    .col(ColumnDef::new(Students::GroupId).big_integer().null())
                    .col(ColumnDef::new(Students::TotalScore).integer().not_null().default(0))
                    .col(ColumnDef::new(Students::Avatar).string().null())
                    .col(ColumnDef::new(Students::PetType).string().null())
                    .col(ColumnDef::new(Students::PetName).string().null())
                    .col(ColumnDef::new(Students::PetExp).integer().not_null().default(0))
                    .col(ColumnDef::new(Students::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(Students::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 积分记录表
        manager
            .create_table(
                Table::create()
                    .table(ScoreRecords::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(ScoreRecords::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(ScoreRecords::StudentId).big_integer().not_null())
                    .col(ColumnDef::new(ScoreRecords::ScoreChange).integer().not_null())
                    .col(ColumnDef::new(ScoreRecords::Reason).string().null())
                    .col(ColumnDef::new(ScoreRecords::Category).string().null())
                    .col(ColumnDef::new(ScoreRecords::OperatorId).big_integer().null())
                    .col(ColumnDef::new(ScoreRecords::CanQuickRevert).boolean().not_null().default(true))
                    .col(ColumnDef::new(ScoreRecords::Reverted).boolean().not_null().default(false))
                    .col(ColumnDef::new(ScoreRecords::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .foreign_key(
                        ForeignKey::create()
                            .from(ScoreRecords::Table, ScoreRecords::StudentId)
                            .to(Students::Table, Students::Id)
                            .on_delete(ForeignKeyAction::Cascade),
                    )
                    .to_owned(),
            )
            .await?;

        // 小组表
        manager
            .create_table(
                Table::create()
                    .table(StudentGroups::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(StudentGroups::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(StudentGroups::Name).string().not_null())
                    .col(ColumnDef::new(StudentGroups::Description).string().null())
                    .col(ColumnDef::new(StudentGroups::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(StudentGroups::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 评估项表
        manager
            .create_table(
                Table::create()
                    .table(EvaluationItems::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(EvaluationItems::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(EvaluationItems::Name).string().not_null())
                    .col(ColumnDef::new(EvaluationItems::ScoreChange).integer().not_null())
                    .col(ColumnDef::new(EvaluationItems::Category).string().null())
                    .col(ColumnDef::new(EvaluationItems::IsQuickAccess).boolean().not_null().default(false))
                    .col(ColumnDef::new(EvaluationItems::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(EvaluationItems::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 结算记录表
        manager
            .create_table(
                Table::create()
                    .table(SettlementRecords::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(SettlementRecords::Id).big_integer().not_null().auto_increment().primary_key())
                    .col(ColumnDef::new(SettlementRecords::Name).string().not_null())
                    .col(ColumnDef::new(SettlementRecords::Period).string().null())
                    .col(ColumnDef::new(SettlementRecords::SnapshotData).string().null())
                    .col(ColumnDef::new(SettlementRecords::Status).integer().not_null().default(0))
                    .col(ColumnDef::new(SettlementRecords::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(SettlementRecords::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .to_owned(),
            )
            .await?;

        // 自动评估配置表
        manager
            .create_table(
                Table::create()
                    .table(AutoEvaluationConfigs::Table)
                    .if_not_exists()
                    .col(ColumnDef::new(AutoEvaluationConfigs::Id).big_integer().not_null().auto_increment().primary_key())
            .col(ColumnDef::new(AutoEvaluationConfigs::PublicId).string().not_null().unique_key())
            .col(ColumnDef::new(AutoEvaluationConfigs::Name).string().not_null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::TriggerType).string().not_null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::TriggerTime).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::DayOfWeek).integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::DayOfMonth).integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::EvaluationItemId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::ScoreChange).double().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::Reason).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::TargetType).string().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::TargetGroupId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::TargetStudentId).big_integer().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::IsEnabled).boolean().not_null().default(false))
                    .col(ColumnDef::new(AutoEvaluationConfigs::LastExecutedAt).date_time().null())
                    .col(ColumnDef::new(AutoEvaluationConfigs::CreatedAt).date_time().not_null().default(Expr::current_timestamp()))
                    .col(ColumnDef::new(AutoEvaluationConfigs::UpdatedAt).date_time().not_null().default(Expr::current_timestamp()))
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

        // 为已有数据库补齐 updated_at 列（SQLite 支持的 ALTER TABLE）
        let conn = manager.get_connection();
        let tables = ["student_groups", "evaluation_items", "settlement_records", "auto_evaluation_configs"];
        for table in tables.iter() {
            let _ = conn
                .execute(Statement::from_string(
                    DatabaseBackend::Sqlite,
                    format!("ALTER TABLE {} ADD COLUMN updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP", table),
                ))
                .await;
        }

        // 安全最佳实践：为 auto_evaluation_configs 补齐 public_id (UUID) 列
        // 已存在行用占位 UUID 填充，等待应用层在下一次写入时覆盖。
        let _ = conn
            .execute(Statement::from_string(
                DatabaseBackend::Sqlite,
                "ALTER TABLE auto_evaluation_configs ADD COLUMN public_id TEXT",
            ))
            .await;
        // 把存量行填上占位 UUID（应用启动时 auto_score 模块的兼容层会回填正式 UUID）
        let _ = conn
            .execute(Statement::from_string(
                DatabaseBackend::Sqlite,
                "UPDATE auto_evaluation_configs SET public_id = lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))),2) || '-' || substr('89ab',abs(random())%4+1,1) || substr(lower(hex(randomblob(2))),2) || '-' || lower(hex(randomblob(6))) WHERE public_id IS NULL OR public_id = ''",
            ))
            .await;

        Ok(())
    }

    async fn down(&self, manager: &SchemaManager) -> Result<(), DbErr> {
        manager
            .drop_table(Table::drop().table(AdminSettings::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(AutoEvaluationConfigs::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(SettlementRecords::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(EvaluationItems::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(ScoreRecords::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(Students::Table).to_owned())
            .await?;
        manager
            .drop_table(Table::drop().table(StudentGroups::Table).to_owned())
            .await?;
        Ok(())
    }
}

#[derive(Iden)]
enum Students {
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
enum ScoreRecords {
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
enum StudentGroups {
    Table,
    Id,
    Name,
    Description,
    CreatedAt,
    UpdatedAt,
}

#[derive(Iden)]
enum EvaluationItems {
    Table,
    Id,
    Name,
    ScoreChange,
    Category,
    IsQuickAccess,
    CreatedAt,
    UpdatedAt,
}

#[derive(Iden)]
enum SettlementRecords {
    Table,
    Id,
    Name,
    Period,
    SnapshotData,
    Status,
    CreatedAt,
    UpdatedAt,
}

#[derive(Iden)]
enum AutoEvaluationConfigs {
    Table,
    Id,
    PublicId,
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
    UpdatedAt,
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
