use tauri::Manager;

#[tauri::command]
pub async fn restart_app(app_handle: tauri::AppHandle) -> Result<(), String> {
    // restart() 返回 never type，不会返回
    app_handle.restart();
    #[allow(unreachable_code)]
    Ok(())
}

#[tauri::command]
pub async fn open_path(app_handle: tauri::AppHandle, path: String) -> Result<(), String> {
    // 安全白名单：仅允许 http/https/tel/mailto 协议
    if let Some(proto) = path.split("://").next() {
        match proto {
            "http" | "https" | "tel" | "mailto" => {}
            _ => return Err(format!("不支持的协议: {}", proto)),
        }
    }
    open_external(&app_handle, &path)
}

#[cfg(not(target_os = "android"))]
fn open_external(_app_handle: &tauri::AppHandle, path: &str) -> Result<(), String> {
    open::that(path).map_err(|e| format!("打开失败: {}", e))
}

#[cfg(target_os = "android")]
fn open_external(app_handle: &tauri::AppHandle, path: &str) -> Result<(), String> {
    use tauri_plugin_shell::ShellExt;
    let shell = app_handle.shell();
    shell
        .open(path, None)
        .map_err(|e| format!("打开失败: {}", e))
}

/// 打开大屏展示
///
/// 桌面：创建独立窗口
/// Android：发送 navigate 事件，前端 router.push('/display')
#[tauri::command]
pub async fn open_display_window(app_handle: tauri::AppHandle) -> Result<(), String> {
    #[cfg(not(target_os = "android"))]
    {
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
        .build()
        .map_err(|e| format!("创建大屏窗口失败: {}", e))?;
    }

    #[cfg(target_os = "android")]
    {
        use tauri::Emitter;
        let _ = app_handle.emit("navigate", "/display");
    }

    Ok(())
}

