//! 桌面平台初始化：托盘 + 多窗口
//!
//! 注：原 setup_tray 多窗口逻辑已拆分至此。窗口创建由前端 invoke 触发，
//! 此处仅负责系统托盘。

use tauri::{Manager, Wry};
use tauri::tray::TrayIconBuilder;
use tauri::menu::{Menu, MenuItem};

pub fn init(app: &tauri::App<Wry>) -> Result<(), Box<dyn std::error::Error>> {
    build_tray(app)?;
    Ok(())
}

fn build_tray(app: &tauri::App<Wry>) -> Result<(), Box<dyn std::error::Error>> {
    let show_main_item = MenuItem::with_id(app, "show_main", "显示主窗口", true, None::<&str>)?;
    let show_display_item = MenuItem::with_id(app, "show_display", "打开大屏展示", true, None::<&str>)?;
    let quit_item = MenuItem::with_id(app, "quit", "退出", true, None::<&str>)?;

    let menu = Menu::with_items(
        app,
        &[&show_main_item, &show_display_item, &quit_item],
    )?;

    let _tray = TrayIconBuilder::with_id("main-tray")
        .tooltip("ClassIsScore")
        .icon(app.default_window_icon().cloned().ok_or("缺少窗口图标")?)
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "show_main" => {
                if let Some(win) = app.get_webview_window("main") {
                    let _ = win.show();
                    let _ = win.set_focus();
                }
            }
            "show_display" => {
                use tauri::Emitter;
                let _ = app.emit("open-display-window", ());
            }
            "quit" => {
                app.exit(0);
            }
            _ => {}
        })
        .build(app)?;

    Ok(())
}
