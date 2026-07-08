//! 移动平台初始化：单窗口 + 移动特性占位
//!
//! Android WebView 启动后由前端通过 navigate 事件驱动单窗口路由。

use tauri::{Manager, Wry};

pub fn init(_app: &tauri::App<Wry>) -> Result<(), Box<dyn std::error::Error>> {
    // 移动端暂不创建额外窗口，后续可在此处：
    // - 配置状态栏 / 导航栏样式
    // - 申请运行时权限（存储 / 通知）
    // - 监听 Android 生命周期（onResume / onPause）以暂停 / 恢复评分推送
    Ok(())
}
