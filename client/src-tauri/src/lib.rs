pub mod commands;
pub mod db;
pub mod platform;
pub mod services;

use crate::db::connection::create_sqlite_connection;
use crate::db::migration::run_migration;
use crate::services::logger::init_logger;
use crate::services::paths::log_dir;
use parking_lot::RwLock;
use state::AppState;
use std::sync::Arc;
use tauri::Manager;

pub mod state {
    use crate::services::logger::LoggerService;
    use once_cell::sync::OnceCell;
    use sea_orm::DatabaseConnection;
    use std::sync::Arc;
    use tauri::AppHandle;

    pub struct AppState {
        pub db: Arc<OnceCell<DatabaseConnection>>,
        pub logger: Arc<OnceCell<LoggerService>>,
        pub app_handle: Arc<OnceCell<AppHandle>>,
    }

    impl AppState {
        pub fn new() -> Self {
            Self {
                db: Arc::new(OnceCell::new()),
                logger: Arc::new(OnceCell::new()),
                app_handle: Arc::new(OnceCell::new()),
            }
        }

        pub fn get_db(&self) -> Result<&DatabaseConnection, String> {
            self.db
                .get()
                .ok_or_else(|| "数据库未初始化".to_string())
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let mut builder = tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_notification::init());

    // Android 专属插件
    #[cfg(target_os = "android")]
    {
        builder = builder
            .plugin(tauri_plugin_haptics::init())
            .plugin(tauri_plugin_biometric::init());
    }

    builder
        .setup(|app| {
            let state = AppState::new();
            let app_handle = app.handle().clone();

            // 初始化日志
            match log_dir(&app_handle) {
                Ok(dir) => {
                    let logger = init_logger(&app_handle, dir);
                    let _ = state.logger.set(logger);
                }
                Err(e) => {
                    tracing::error!("日志目录初始化失败: {}", e);
                }
            }
            let _ = state.app_handle.set(app_handle.clone());

            // 初始化数据库（异步任务，避免 setup 阻塞触发 ANR）
            let db_state = state.db.clone();
            let handle_for_db = app_handle.clone();
            tauri::async_runtime::spawn(async move {
                match create_sqlite_connection(&handle_for_db).await {
                    Ok(db) => {
                        if let Err(e) = run_migration(&db).await {
                            tracing::error!("数据库迁移失败: {}", e);
                            return;
                        }
                        let _ = db_state.set(db);
                        tracing::info!("数据库初始化完成");
                    }
                    Err(e) => {
                        tracing::error!("数据库初始化失败: {}", e);
                    }
                }
            });

            app.manage(Arc::new(RwLock::new(state)));

            // 平台专属初始化
            platform::init(app)?;

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            // 学生管理
            commands::student::student_list,
            commands::student::student_get,
            commands::student::student_create,
            commands::student::student_update,
            commands::student::student_delete,
            commands::student::student_batch_create,
            commands::student::student_reset_scores,
            // 积分管理
            commands::score::score_list,
            commands::score::score_add,
            commands::score::score_batch_add,
            commands::score::score_revert,
            commands::score::score_recent,
            commands::score::score_stats,
            // 小组管理
            commands::group::group_list,
            commands::group::group_get,
            commands::group::group_create,
            commands::group::group_update,
            commands::group::group_delete,
            // 评估项管理
            commands::evaluation::evaluation_list,
            commands::evaluation::evaluation_create,
            commands::evaluation::evaluation_update,
            commands::evaluation::evaluation_delete,
            // 排行榜
            commands::leaderboard::leaderboard_query,
            commands::leaderboard::leaderboard_by_group,
            commands::leaderboard::leaderboard_individual,
            // 结算
            commands::settlement::settlement_list,
            commands::settlement::settlement_create,
            commands::settlement::settlement_complete,
            commands::settlement::settlement_rollback,
            // 设置
            commands::settings::settings_get_all,
            commands::settings::settings_get,
            commands::settings::settings_set,
            commands::settings::settings_export,
            commands::settings::settings_import,
            // 认证
            commands::auth::auth_login,
            commands::auth::auth_change_password,
            commands::auth::auth_verify,
            commands::auth::auth_get_info,
            commands::auth::auth_set_passwords,
            // 自动评估
            commands::auto_score::auto_score_get_rules,
            commands::auto_score::auto_score_add_rule,
            commands::auto_score::auto_score_update_rule,
            commands::auto_score::auto_score_delete_rule,
            commands::auto_score::auto_score_toggle_rule,
            // 日志
            commands::log::log_query,
            commands::log::log_clear,
            commands::log::log_set_level,
            commands::log::log_write,
            // 应用控制
            commands::app::restart_app,
            commands::app::open_path,
            commands::app::open_display_window,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
