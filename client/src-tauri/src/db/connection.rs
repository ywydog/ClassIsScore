use sea_orm::{ConnectOptions, Database, DatabaseConnection};
use std::time::Duration;
use tauri::Manager;

pub async fn create_sqlite_connection(
    app_handle: &tauri::AppHandle,
) -> Result<DatabaseConnection, sea_orm::DbErr> {
    let db_path = if cfg!(debug_assertions) {
        std::path::PathBuf::from("data.db")
    } else {
        let app_data_dir = app_handle
            .path()
            .app_data_dir()
            .map_err(|e| sea_orm::DbErr::Custom(format!("获取应用数据目录失败: {}", e)))?;
        std::fs::create_dir_all(&app_data_dir)
            .map_err(|e| sea_orm::DbErr::Custom(format!("创建数据目录失败: {}", e)))?;
        app_data_dir.join("classisscore.db")
    };

    let db_path_str = db_path.to_str().unwrap_or("classisscore.db").replace('\\', "/");
    let db_url = format!("sqlite://{}?mode=rwc", db_path_str);

    tracing::info!("数据库路径: {:?}", db_path);

    let mut opt = ConnectOptions::new(&db_url);
    opt.max_connections(10)
        .min_connections(1)
        .connect_timeout(Duration::from_secs(10))
        .sqlx_logging(false);

    let db = Database::connect(opt).await?;
    Ok(db)
}
