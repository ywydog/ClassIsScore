use tauri::Manager;

#[tauri::command]
pub async fn restart_app(app_handle: tauri::AppHandle) -> Result<(), String> {
    // restart() 返回 never type，不会返回
    app_handle.restart();
    #[allow(unreachable_code)]
    Ok(())
}

#[tauri::command]
pub async fn open_path(path: String) -> Result<(), String> {
    open::that(&path).map_err(|e| format!("打开路径失败: {}", e))
}

#[tauri::command]
pub async fn open_display_window(app_handle: tauri::AppHandle) -> Result<(), String> {
    use tauri::{WebviewUrl, WebviewWindowBuilder};

    let existing = app_handle.get_webview_window("display");
    if let Some(win) = existing {
        win.show().map_err(|e| e.to_string())?;
        win.set_focus().map_err(|e| e.to_string())?;
        return Ok(());
    }

    WebviewWindowBuilder::new(
        &app_handle,
        "display",
        WebviewUrl::App("index.html#/display".into()),
    )
    .title("ClassIsScore 大屏展示")
    .fullscreen(true)
    .build()
    .map_err(|e| format!("创建大屏窗口失败: {}", e))?;

    Ok(())
}

#[tauri::command]
pub async fn open_floating_window(app_handle: tauri::AppHandle) -> Result<(), String> {
    use tauri::{WebviewUrl, WebviewWindowBuilder};

    let existing = app_handle.get_webview_window("floating");
    if let Some(win) = existing {
        win.show().map_err(|e| e.to_string())?;
        win.set_focus().map_err(|e| e.to_string())?;
        return Ok(());
    }

    WebviewWindowBuilder::new(
        &app_handle,
        "floating",
        WebviewUrl::App("index.html#/floating".into()),
    )
    .title("ClassIsScore 浮动积分条")
    .inner_size(400.0, 60.0)
    .always_on_top(true)
    .decorations(false)
    .skip_taskbar(true)
    .build()
    .map_err(|e| format!("创建浮动窗口失败: {}", e))?;

    Ok(())
}
