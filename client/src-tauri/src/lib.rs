pub mod commands;
pub mod db;
pub mod services;

use crate::db::connection::create_sqlite_connection;
use crate::db::migration::run_migration;
use crate::services::logger::init_logger;
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
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .setup(|app| {
            let state = AppState::new();
            let app_handle = app.handle().clone();

            // 初始化日志
            let logger = init_logger(&app_handle);
            let _ = state.logger.set(logger);
            let _ = state.app_handle.set(app_handle.clone());

            // 初始化数据库（同步阻塞）
            let db_result = tauri::async_runtime::block_on(async {
                let db = create_sqlite_connection(&app_handle).await?;
                run_migration(&db).await?;
                Ok::<sea_orm::DatabaseConnection, sea_orm::DbErr>(db)
            });

            match db_result {
                Ok(db) => {
                    let _ = state.db.set(db);
                    tracing::info!("数据库初始化完成");
                }
                Err(e) => {
                    tracing::error!("数据库初始化失败: {}", e);
                    // 数据库初始化失败，应用仍可启动但所有数据操作会报错
                    // 前端应通过 IPC 调用检测到此状态
                }
            }

            app.manage(Arc::new(RwLock::new(state)));

            // 设置托盘
            #[cfg(desktop)]
            setup_tray(app)?;

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
            commands::score::score_stats_all,
            // 小组管理
            commands::group::group_list,
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
            commands::app::open_floating_window,
            // 主题
            commands::theme::theme_list,
            commands::theme::theme_get,
            commands::theme::theme_toggle,
            commands::theme::theme_install,
            commands::theme::theme_delete,
            // 插件
            commands::plugin::plugin_list,
            commands::plugin::plugin_get,
            commands::plugin::plugin_install,
            commands::plugin::plugin_delete,
            commands::plugin::plugin_toggle,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(desktop)]
fn setup_tray(app: &mut tauri::App) -> Result<(), Box<dyn std::error::Error>> {
    use tauri::{
        image::Image,
        menu::{Menu, MenuItem},
        tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    };

    let show_item = MenuItem::with_id(app, "show", "显示窗口", true, None::<&str>)?;
    let quit_item = MenuItem::with_id(app, "quit", "退出", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&show_item, &quit_item])?;

    let _tray = TrayIconBuilder::new()
        .icon(Image::from_bytes(include_bytes!("../icons/32x32.png")).unwrap())
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "show" => {
                if let Some(window) = app.get_webview_window("main") {
                    let _ = window.show();
                    let _ = window.set_focus();
                }
            }
            "quit" => {
                app.exit(0);
            }
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                let app = tray.app_handle();
                if let Some(window) = app.get_webview_window("main") {
                    let _ = window.show();
                    let _ = window.set_focus();
                }
            }
        })
        .build(app)?;

    Ok(())
}
