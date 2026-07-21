//! 鉴权命令：管理员密码 / U 盘 / 人脸 / 网络伺服 PIN。
//!
//! 架构约束：
//! - 密码散列/校验全部走 `crate::services::crypto`（无 tauri / sea_orm 依赖），
//!   这是为了打破"本模块 ↔ server 模块"的循环依赖。
//! - 设置 key 全部走 `AdminSettingKey` enum，禁止散落字符串字面量。

use crate::commands::auth_settings_keys::AdminSettingKey;
use crate::db::entities::admin_settings;
use crate::db::entities::auto_evaluation_config;
use crate::db::entities::evaluation_item;
use crate::db::entities::score_record;
use crate::db::entities::settlement_record;
use crate::db::entities::student;
use crate::db::entities::student_group;
use crate::services::crypto::{hash_password, verify_password};
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, IntoActiveModel, QueryFilter, Set};
use std::sync::Arc;
use tauri::State;

use super::get_db;

/// 如果当前存储是旧 SHA-256 格式，登录成功后自动升级为 Argon2id。
/// 安全最佳实践：透明升级用户凭证，避免强制重置。
async fn maybe_upgrade_password(
    db: &sea_orm::DatabaseConnection,
    model: admin_settings::Model,
    raw_password: &str,
) {
    if model.setting_value.as_deref().map_or(true, |v| v.len() != 64) {
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

/// 通用设置 upsert。
async fn upsert_setting(
    db: &sea_orm::DatabaseConnection,
    key: AdminSettingKey,
    value: String,
) -> Result<(), String> {
    let key_str = key.as_str();
    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(key_str))
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
            setting_key: Set(key_str.to_string()),
            setting_value: Set(Some(value)),
            ..Default::default()
        };
        new_setting.insert(db).await.map_err(|e| e.to_string())?;
    }
    Ok(())
}

/// 通用设置查询。
async fn get_setting(
    db: &sea_orm::DatabaseConnection,
    key: AdminSettingKey,
) -> Result<Option<admin_settings::Model>, String> {
    admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq(key.as_str()))
        .one(db)
        .await
        .map_err(|e| e.to_string())
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

    let setting = get_setting(&db, AdminSettingKey::AdminPassword).await?;

    match setting {
        Some(model) => {
            let stored_hash = model.setting_value.clone().unwrap_or_default();
            if verify_password(&stored_hash, &password) {
                maybe_upgrade_password(&db, model, &password).await;
                Ok(AuthResult {
                    success: true,
                    message: "登录成功".to_string(),
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
pub async fn auth_change_password(
    state: State<'_, Arc<RwLock<AppState>>>,
    old_password: String,
    new_password: String,
) -> Result<AuthResult, String> {
    let db = get_db(&state)?;

    let setting = get_setting(&db, AdminSettingKey::AdminPassword).await?;

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

    let setting = get_setting(&db, AdminSettingKey::AdminPassword).await?;

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

    let admin_pwd = get_setting(&db, AdminSettingKey::AdminPassword).await?;
    let usb_pwd = get_setting(&db, AdminSettingKey::UsbPassword).await?;
    let face_pwd = get_setting(&db, AdminSettingKey::FacePassword).await?;

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

    upsert_setting(&db, AdminSettingKey::AdminPassword, hash_password(&admin_password)).await?;

    if let Some(usb_pwd) = usb_password {
        upsert_setting(&db, AdminSettingKey::UsbPassword, hash_password(&usb_pwd)).await?;
    }

    if let Some(face_pwd) = face_password {
        upsert_setting(&db, AdminSettingKey::FacePassword, hash_password(&face_pwd)).await?;
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
        upsert_setting(&db, AdminSettingKey::NetworkServePin, String::new()).await?;
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

    upsert_setting(
        &db,
        AdminSettingKey::NetworkServePin,
        hash_password(&network_serve_pin),
    )
    .await?;

    Ok(AuthResult {
        success: true,
        message: "网络伺服 PIN 已设置".to_string(),
    })
}

/// 清空所有业务数据：学生、积分、小组、评估项、自动评估配置、结算记录。
/// 保留管理员密码、USB/面部密码、网络 PIN、设置项（floating、主题、插件状态等）。
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
