//! 管理员设置项 Key 的强类型定义。
//!
//! 用 enum 替代散落在代码各处的字符串字面量（"admin_password" / "usb_password" /
//! "face_password" / "network_serve_pin" / "admin_password" / "floating_bar" 等），
//! 让 setting key 的增删改在编译期可见。
//!
//! 安全最佳实践：所有 Rust 端访问 admin_settings 表的代码都必须通过本 enum，
//! 禁止直接使用字符串字面量。

use serde::{Deserialize, Serialize};
use std::fmt;

/// 管理员设置项 Key。`as_str()` 返回与 `admin_settings.setting_key` 列中存储的值。
///
/// ⚠️ 改这里任何一个变体的 `as_str()` 都会破坏现有用户数据库里的 setting_key
/// 映射，必须配套写数据库迁移。
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash, Serialize, Deserialize)]
pub enum AdminSettingKey {
    /// 管理员密码（Argon2id 散列）
    AdminPassword,
    /// U 盘验证密码（Argon2id 散列）
    UsbPassword,
    /// 人脸验证密码（Argon2id 散列）
    FacePassword,
    /// 网络伺服 PIN（Argon2id 散列）
    NetworkServePin,
    /// 浮窗开关（"1" / "0"）
    FloatingBar,
}

impl AdminSettingKey {
    /// 数据库列中实际存储的字符串值。
    pub fn as_str(&self) -> &'static str {
        match self {
            AdminSettingKey::AdminPassword => "admin_password",
            AdminSettingKey::UsbPassword => "usb_password",
            AdminSettingKey::FacePassword => "face_password",
            AdminSettingKey::NetworkServePin => "network_serve_pin",
            AdminSettingKey::FloatingBar => "floating_bar",
        }
    }

    /// 解析字符串为 enum。未识别时返回 None 而非 panic，
    /// 避免未来 schema 升级时旧客户端加载新数据崩溃。
    pub fn parse(s: &str) -> Option<Self> {
        match s {
            "admin_password" => Some(AdminSettingKey::AdminPassword),
            "usb_password" => Some(AdminSettingKey::UsbPassword),
            "face_password" => Some(AdminSettingKey::FacePassword),
            "network_serve_pin" => Some(AdminSettingKey::NetworkServePin),
            "floating_bar" => Some(AdminSettingKey::FloatingBar),
            _ => None,
        }
    }
}

impl fmt::Display for AdminSettingKey {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(self.as_str())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn roundtrip_all_keys() {
        let keys = [
            AdminSettingKey::AdminPassword,
            AdminSettingKey::UsbPassword,
            AdminSettingKey::FacePassword,
            AdminSettingKey::NetworkServePin,
            AdminSettingKey::FloatingBar,
        ];
        for k in keys {
            assert_eq!(AdminSettingKey::parse(k.as_str()), Some(k));
        }
    }

    #[test]
    fn parse_unknown_returns_none() {
        assert_eq!(AdminSettingKey::parse("totally_made_up_key"), None);
        assert_eq!(AdminSettingKey::parse(""), None);
    }

    #[test]
    fn display_matches_as_str() {
        assert_eq!(
            AdminSettingKey::AdminPassword.to_string(),
            "admin_password"
        );
    }
}
