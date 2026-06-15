use sea_orm::{ConnectOptions, Database, DatabaseConnection};
use std::time::Duration;
use tauri::Manager;

pub async fn create_sqlite_connection(
    app_handle: &tauri::AppHandle,
) -> Result<DatabaseConnection, sea_orm::DbErr> {
    // 数据目录：在软件所在目录下创建 data 文件夹
    let exe_dir = std::env::current_exe()
        .map(|p| p.parent().map(|pp| pp.to_path_buf()).unwrap_or_else(|| std::path::PathBuf::from(".")))
        .unwrap_or_else(|_| std::path::PathBuf::from("."));

    let data_dir = exe_dir.join("data");
    std::fs::create_dir_all(&data_dir)
        .map_err(|e| sea_orm::DbErr::Custom(format!("创建数据目录失败: {}", e)))?;

    let db_path = data_dir.join("classisscore.db");

    let db_url = format!(
        "sqlite://{}?mode=rwc",
        db_path.to_str().unwrap_or("classisscore.db").replace('\\', "/")
    );

    tracing::info!("数据库路径: {:?}", db_path);

    let mut opt = ConnectOptions::new(&db_url);
    opt.max_connections(10)
        .min_connections(1)
        .connect_timeout(Duration::from_secs(10))
        .sqlx_logging(false);

    let db = Database::connect(opt).await?;
    Ok(db)
}
