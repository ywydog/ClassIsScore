//! 平台分发模块
//!
//! 桌面端：托盘 + 多窗口 + 桌面交互
//! Android：单窗口 + 触觉 + 通知

#[cfg(not(target_os = "android"))]
mod desktop;
#[cfg(target_os = "android")]
mod mobile;

#[cfg(not(target_os = "android"))]
pub use desktop::init;
#[cfg(target_os = "android")]
pub use mobile::init;
