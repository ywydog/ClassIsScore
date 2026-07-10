pub mod commands;
pub mod db;
pub mod platform;
#[cfg(not(target_os = "android"))]
pub mod server;
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
    use std::path::PathBuf;
    use std::sync::Arc;
    use tauri::AppHandle;

    pub struct AppState {
        pub db: Arc<OnceCell<DatabaseConnection>>,
        pub logger: Arc<OnceCell<LoggerService>>,
        pub app_handle: Arc<OnceCell<AppHandle>>,
        /// 后端数据目录（SQLite 文件所在目录）
        pub data_dir: Arc<OnceCell<PathBuf>>,
    }

    impl AppState {
        pub fn new() -> Self {
            Self {
                db: Arc::new(OnceCell::new()),
                logger: Arc::new(OnceCell::new()),
                app_handle: Arc::new(OnceCell::new()),
                data_dir: Arc::new(OnceCell::new()),
            }
        }

        pub fn get_db(&self) -> Result<&DatabaseConnection, String> {
            self.db
                .get()
                .ok_or_else(|| "数据库未初始化".to_string())
        }

        /// 读取后端数据目录路径（前端用来展示"数据存放在哪里"）
        pub fn get_data_dir(&self) -> &PathBuf {
            self.data_dir
                .get()
                .expect("数据目录未初始化")
        }
    }
}

/// 生成命令处理器（避免 cfg 条件下重复书写命令列表）
macro_rules! commands_handler {
    (server) => {
        tauri::generate_handler![
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
            commands::score::score_today_count,
            commands::score::score_trend,
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
            commands::leaderboard::leaderboard_all_groups,
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
            commands::settings::settings_data_path,
            // 认证
            commands::auth::auth_login,
            commands::auth::auth_change_password,
            commands::auth::auth_verify,
            commands::auth::auth_get_info,
            commands::auth::auth_set_passwords,
            commands::auth::admin_reset,
            // 自动评估
            commands::auto_score::auto_score_get_rules,
            commands::auto_score::auto_score_add_rule,
            commands::auto_score::auto_score_update_rule,
            commands::auto_score::auto_score_delete_rule,
            commands::auto_score::auto_score_toggle_rule,
            // 主题
            commands::theme::theme_list,
            commands::theme::theme_get,
            commands::theme::theme_install,
            commands::theme::theme_toggle,
            commands::theme::theme_delete,
            // 插件
            commands::plugin::plugin_list,
            commands::plugin::plugin_get,
            commands::plugin::plugin_install,
            commands::plugin::plugin_toggle,
            commands::plugin::plugin_delete,
            // 日志
            commands::log::log_query,
            commands::log::log_clear,
            commands::log::log_set_level,
            commands::log::log_write,
            // 应用控制
            commands::app::restart_app,
            commands::app::open_path,
            commands::app::open_display_window,
            // 网络伺服（仅桌面平台）
            commands::server::server_start,
            commands::server::server_stop,
            commands::server::server_status,
        ]
    };
    () => {
        tauri::generate_handler![
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
            commands::score::score_today_count,
            commands::score::score_trend,
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
            commands::leaderboard::leaderboard_all_groups,
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
            commands::settings::settings_data_path,
            // 认证
            commands::auth::auth_login,
            commands::auth::auth_change_password,
            commands::auth::auth_verify,
            commands::auth::auth_get_info,
            commands::auth::auth_set_passwords,
            commands::auth::admin_reset,
            // 自动评估
            commands::auto_score::auto_score_get_rules,
            commands::auto_score::auto_score_add_rule,
            commands::auto_score::auto_score_update_rule,
            commands::auto_score::auto_score_delete_rule,
            commands::auto_score::auto_score_toggle_rule,
            // 主题
            commands::theme::theme_list,
            commands::theme::theme_get,
            commands::theme::theme_install,
            commands::theme::theme_toggle,
            commands::theme::theme_delete,
            // 插件
            commands::plugin::plugin_list,
            commands::plugin::plugin_get,
            commands::plugin::plugin_install,
            commands::plugin::plugin_toggle,
            commands::plugin::plugin_delete,
            // 日志
            commands::log::log_query,
            commands::log::log_clear,
            commands::log::log_set_level,
            commands::log::log_write,
            // 应用控制
            commands::app::restart_app,
            commands::app::open_path,
            commands::app::open_display_window,
        ]
    };
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let builder = tauri::Builder::default()
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

            // 初始化数据目录（同步算出来即可，不依赖数据库）
            if let Ok(dir) = crate::services::paths::data_dir(&app_handle) {
                let _ = state.data_dir.set(dir);
            }

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
        .invoke_handler({
            #[cfg(not(target_os = "android"))]
            { commands_handler!(server) }
            #[cfg(target_os = "android")]
            { commands_handler!() }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
