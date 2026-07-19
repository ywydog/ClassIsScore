use crate::db::entities::admin_settings;
use crate::db::entities::auto_evaluation_config;
use crate::db::entities::evaluation_item;
use crate::db::entities::score_record;
use crate::db::entities::settlement_record;
use crate::db::entities::student;
use crate::db::entities::student_group;
use crate::state::AppState;
use argon2::password_hash::rand_core::OsRng;
use argon2::password_hash::{PasswordHash, PasswordHasher, PasswordVerifier, SaltString};
use argon2::Argon2;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, IntoActiveModel, QueryFilter, Set};
use sha2::{Digest, Sha256};
use std::sync::Arc;
use tauri::State;

use super::get_db;

/// 安全最佳实践：使用 Argon2id（OWASP 推荐）散列密码，自动生成随机 salt。
/// 旧版本以裸 SHA-256 散列（无 salt），通过 `verify_password` 兼容旧数据并自动迁移。
fn hash_password(password: &str) -> String {
    let salt = SaltString::generate(&mut OsRng);
    let argon2 = Argon2::default();
    argon2
        .hash_password(password.as_bytes(), &salt)
        .expect("Argon2 hash 失败")
        .to_string()
}

/// 验证密码：优先尝试 Argon2id 格式，失败时回退到旧版 SHA-256 格式（用于平滑迁移）。
/// 安全最佳实践：禁止明文比较，防止时序攻击；恒定时间由 Argon2 verifier 保证。
pub(crate) fn verify_password(stored: &str, candidate: &str) -> bool {
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
        // 长度相等且用恒定时间比较
        if candidate_hash.len() == stored.len() {
            return constant_time_eq(stored.as_bytes(), candidate_hash.as_bytes());
        }
    }
    false
}

/// 恒定时间字节比较（避免时序侧信道）。
fn constant_time_eq(a: &[u8], b: &[u8]) -> bool {
    if a.len() != b.len() {
        return false;
    }
    let mut diff: u8 = 0;
    for (x, y) in a.iter().zip(b.iter()) {
        diff |= x ^ y;
    }
    diff == 0
}

/// 如果当前存储是旧 SHA-256 格式，登录成功后自动升级为 Argon2id。
/// 安全最佳实践：透明升级用户凭证，避免强制重置。
async fn maybe_upgrade_password(
    db: &sea_orm::DatabaseConnection,
    model: admin_settings::Model,
    raw_password: &str,
) {
    if model.setting_value.as_deref().map_or(true, |v| v.len() != 64) {
        // 已经是 Argon2id 格式或无值，无需升级
        return;
    }
    if !model
        .setting_value
        .as_deref()
        .map(|v| v.chars().all(|c| c.is_ascii_hexdigit()))
        .unwrap_or(false)
    {
        return;
    }
    let new_hash = hash_password(raw_password);
    let mut active: admin_settings::ActiveModel = model.into_active_model();
    active.setting_value = Set(Some(new_hash));
    active.updated_at = Set(chrono::Local::now().naive_utc());
    let _ = active.update(db).await;
}

async fn upsert_setting(
    db: &sea_orm::DatabaseConnection,
    key: &str,
    value: String,
) -> Result<(), String> {
    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(key))
        .one(db)
        .await
        .map_err(|e| e.to_string())?;

    if let Some(model) = existing {
        let mut active: admin_settings::ActiveModel = model.into();
        active.setting_value = Set(Some(value));
        active.updated_at = Set(chrono::Local::now().naive_utc());
        active.update(db).await.map_err(|e| e.to_string())?;
    } else {
        let new_setting = admin_settings::ActiveModel {
            setting_key: Set(key.to_string()),
            setting_value: Set(Some(value)),
            ..Default::default()
        };
        new_setting.insert(db).await.map_err(|e| e.to_string())?;
    }
    Ok(())
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
pub struct AuthResult {
    pub success: bool,
    pub message: String,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
pub struct AdminInfo {
    pub password_set: bool,
    pub usb_password_set: bool,
    pub face_password_set: bool,
}

#[tauri::command]
pub async fn auth_login(
    state: State<'_, Arc<RwLock<AppState>>>,
    password: String,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    let setting = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("admin_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    match setting {
        Some(model) => {
            let stored_hash = model.setting_value.clone().unwrap_or_default();
            if verify_password(&stored_hash, &password) {
                // 安全最佳实践：透明升级旧 SHA-256 凭据为 Argon2id
                maybe_upgrade_password(&db, model, &password).await;
                Ok(AuthResult {
                    success: true,
                    message: "登录成功".to_string(),
                })
            } else {
                // 安全最佳实践：统一错误信息，避免泄露"未设置"指纹
                Ok(AuthResult {
                    success: false,
                    message: "密码错误".to_string(),
                })
            }
        }
        None => Ok(AuthResult {
            success: false,
            message: "密码错误".to_string(),
        }),
    }
}

#[tauri::command]
pub async fn auth_change_password(
    state: State<'_, Arc<RwLock<AppState>>>,
    old_password: String,
    new_password: String,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    let setting = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("admin_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    match setting {
        Some(model) => {
            let stored_hash = model.setting_value.clone().unwrap_or_default();
            if !verify_password(&stored_hash, &old_password) {
                return Ok(AuthResult {
                    success: false,
                    message: "旧密码错误".to_string(),
                });
            }
            let mut active: admin_settings::ActiveModel = model.into();
            active.setting_value = Set(Some(hash_password(&new_password)));
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
            Ok(AuthResult {
                success: true,
                message: "密码修改成功".to_string(),
            })
        }
        None => Ok(AuthResult {
            success: false,
            message: "旧密码错误".to_string(),
        }),
    }
}

#[tauri::command]
pub async fn auth_verify(
    state: State<'_, Arc<RwLock<AppState>>>,
    password: String,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    let setting = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("admin_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    match setting {
        Some(model) => {
            let stored_hash = model.setting_value.clone().unwrap_or_default();
            if verify_password(&stored_hash, &password) {
                Ok(AuthResult {
                    success: true,
                    message: "验证通过".to_string(),
                })
            } else {
                Ok(AuthResult {
                    success: false,
                    message: "密码错误".to_string(),
                })
            }
        }
        None => Ok(AuthResult {
            success: false,
            message: "密码错误".to_string(),
        }),
    }
}

#[tauri::command]
pub async fn auth_get_info(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<AdminInfo, String> {
    let db = get_db(&state)?;

    let admin_pwd = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("admin_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    let usb_pwd = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("usb_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    let face_pwd = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("face_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(AdminInfo {
        password_set: admin_pwd.is_some(),
        usb_password_set: usb_pwd.is_some(),
        face_password_set: face_pwd.is_some(),
    })
}

#[tauri::command]
pub async fn auth_set_passwords(
    state: State<'_, Arc<RwLock<AppState>>>,
    admin_password: String,
    usb_password: Option<String>,
    face_password: Option<String>,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    upsert_setting(&db, "admin_password", hash_password(&admin_password)).await?;

    if let Some(usb_pwd) = usb_password {
        upsert_setting(&db, "usb_password", hash_password(&usb_pwd)).await?;
    }

    if let Some(face_pwd) = face_password {
        upsert_setting(&db, "face_password", hash_password(&face_pwd)).await?;
    }

    Ok(AuthResult {
        success: true,
        message: "密码设置成功".to_string(),
    })
}

/// 设置/更新网络伺服 PIN
///
/// 安全最佳实践：PIN 通过 Argon2id 散列后存储于 admin_settings.network_serve_pin。
/// 启动网络伺服时该值被加载到 ServerState，所有 HTTP 请求必须携带匹配的 Bearer 头。
/// 设空字符串或空 Option 表示清除（不允许网络伺服匿名访问）。
#[tauri::command]
pub async fn auth_set_network_pin(
    state: State<'_, Arc<RwLock<AppState>>>,
    network_serve_pin: String,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    if network_serve_pin.trim().is_empty() {
        // 清除 PIN：拒绝网络伺服任何访问
        upsert_setting(&db, "network_serve_pin", String::new()).await?;
        // 同时立即关闭已运行的网络伺服（先取 clone 释放 guard 再跨 await）
        let server_state_clone = crate::commands::server::server_state().map(|s| s.read().clone());
        if let Some(state) = server_state_clone {
            crate::server::shutdown(&state).await?;
        }
        return Ok(AuthResult {
            success: true,
            message: "已清除网络伺服 PIN".to_string(),
        });
    }

    upsert_setting(&db, "network_serve_pin", hash_password(&network_serve_pin)).await?;

    Ok(AuthResult {
        success: true,
        message: "网络伺服 PIN 已设置".to_string(),
    })
}

/// 清空所有业务数据：学生、积分、小组、评估项、自动评估配置、结算记录。
/// 保留管理员密码、USB/面部密码、设置项（floating、主题、插件状态等）。
#[tauri::command]
pub async fn admin_reset(
    state: State<'_, Arc<RwLock<AppState>>>,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    // 按依赖关系顺序清理：先删 score_records（依赖 student），
    // 再删 student，再删其余无依赖的表。
    score_record::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;
    student::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;
    student_group::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;
    evaluation_item::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;
    auto_evaluation_config::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;
    settlement_record::Entity::delete_many()
        .exec(&db)
        .await
        .map_err(|e| e.to_string())?;

    Ok(AuthResult {
        success: true,
        message: "数据已重置".to_string(),
    })
}
