use crate::services::paths::db_path;
use sea_orm::{ConnectOptions, Database, DatabaseConnection};
use std::time::Duration;
use tauri::AppHandle;

pub async fn create_sqlite_connection(
    app_handle: &AppHandle,
) -> Result<DatabaseConnection, sea_orm::DbErr> {
    let db_file = db_path(app_handle).map_err(|e| sea_orm::DbErr::Custom(e))?;
    let db_url = format!(
        "sqlite://{}?mode=rwc",
        db_file.to_str().unwrap_or("classisscore.db").replace('\\', "/")
    );

    tracing::info!("数据库路径: {:?}", db_file);

    let mut opt = ConnectOptions::new(&db_url);
    opt.max_connections(10)
        .min_connections(1)
        .connect_timeout(Duration::from_secs(10))
        .sqlx_logging(false);

    let db = Database::connect(opt).await?;
    Ok(db)
}
