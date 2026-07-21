//! 密码学原语：密码散列与恒定时间比较。
//!
//! 安全最佳实践：
//! - Argon2id（OWASP 推荐）作为默认密码散列函数，自动生成随机 salt。
//! - 旧版本以裸 SHA-256 散列（无 salt）存储的密码通过 `verify_password` 透明兼容，
//!   首次成功登录时由 `commands::auth::maybe_upgrade_password` 升级为 Argon2id。
//! - 字节比较必须使用恒定时间实现，防止时序侧信道推断 hash 内容。
//!
//! 关键约束：本模块**不应**依赖 `tauri`、`sea_orm` 或 `commands` 任何子模块，
//! 以便 `server` 模块在不引入 `commands::auth` 反向依赖的前提下复用 `verify_password`。
//! 这是打破 `commands::auth ↔ server` 模块循环依赖的关键。

use argon2::password_hash::rand_core::OsRng;
use argon2::password_hash::{PasswordHash, PasswordHasher, PasswordVerifier, SaltString};
use argon2::Argon2;
use sha2::{Digest, Sha256};

/// Argon2id 散列密码。
///
/// 自动生成 16 字节随机 salt，使用默认 Argon2 参数（OWASP 推荐的 m=19456, t=2, p=1）。
pub fn hash_password(password: &str) -> String {
    let salt = SaltString::generate(&mut OsRng);
    let argon2 = Argon2::default();
    argon2
        .hash_password(password.as_bytes(), &salt)
        .expect("Argon2 hash 失败")
        .to_string()
}

/// 验证密码：优先尝试 Argon2id 格式，失败时回退到旧版 SHA-256 格式（用于平滑迁移）。
///
/// 安全性：禁止明文比较，防止时序攻击；Argon2 verifier 自身保证恒定时间；
/// 旧 SHA-256 路径使用 [`constant_time_eq`]。
pub fn verify_password(stored: &str, candidate: &str) -> bool {
    if let Ok(parsed) = PasswordHash::new(stored) {
        if Argon2::default()
            .verify_password(candidate.as_bytes(), &parsed)
            .is_ok()
        {
            return true;
        }
    }
    // 旧格式兼容：32 字节 hex（SHA-256）共 64 字符。比对使用恒定时间。
    if stored.len() == 64 && stored.chars().all(|c| c.is_ascii_hexdigit()) {
        let mut hasher = Sha256::new();
        hasher.update(candidate.as_bytes());
        let candidate_hash = hex::encode(hasher.finalize());
        if candidate_hash.len() == stored.len() {
            return constant_time_eq(stored.as_bytes(), candidate_hash.as_bytes());
        }
    }
    false
}

/// 恒定时间字节比较（避免时序侧信道）。
pub fn constant_time_eq(a: &[u8], b: &[u8]) -> bool {
    if a.len() != b.len() {
        return false;
    }
    let mut diff: u8 = 0;
    for (x, y) in a.iter().zip(b.iter()) {
        diff |= x ^ y;
    }
    diff == 0
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn hash_then_verify_argon2_roundtrip() {
        let h = hash_password("hunter2");
        assert!(verify_password(&h, "hunter2"));
        assert!(!verify_password(&h, "wrong"));
        assert!(h.starts_with("$argon2"));
    }

    #[test]
    fn verify_legacy_sha256_hex_64() {
        // 64-char hex = legacy SHA-256 format
        let legacy = "2a82b9b65a3ec0aa3dc7d50b6e2c5d6f0a4f8c1b2d3e4f5a6b7c8d9e0f1a2b3c";
        // sha256("hunter2") = ...
        let mut hasher = Sha256::new();
        hasher.update(b"hunter2");
        let real = hex::encode(hasher.finalize());
        assert_eq!(real.len(), 64);
        let stored = if real == legacy { real.clone() } else { real };
        // The real sha256 of "hunter2" should verify
        assert!(verify_password(&stored, "hunter2"));
        assert!(!verify_password(&stored, "wrong"));
    }

    #[test]
    fn verify_rejects_random_non_hex() {
        // Not 64 hex chars → not legacy format, not Argon2 format → reject
        assert!(!verify_password("not-a-valid-hash", "anything"));
        assert!(!verify_password("", "anything"));
    }

    #[test]
    fn constant_time_eq_basic() {
        assert!(constant_time_eq(b"abc", b"abc"));
        assert!(!constant_time_eq(b"abc", b"abd"));
        assert!(!constant_time_eq(b"abc", b"abcd"));
        assert!(constant_time_eq(b"", b""));
    }

    #[test]
    fn hash_different_salts_produce_different_outputs() {
        let a = hash_password("same");
        let b = hash_password("same");
        assert_ne!(a, b, "Argon2 salt randomness should produce different hashes");
        assert!(verify_password(&a, "same"));
        assert!(verify_password(&b, "same"));
    }
}
