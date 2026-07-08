use crate::db::entities::admin_settings;
use crate::db::entities::auto_evaluation_config;
use crate::db::entities::evaluation_item;
use crate::db::entities::score_record;
use crate::db::entities::settlement_record;
use crate::db::entities::student;
use crate::db::entities::student_group;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use sha2::{Digest, Sha256};
use std::sync::Arc;
use tauri::State;

use super::get_db;

fn hash_password(password: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(password.as_bytes());
    hex::encode(hasher.finalize())
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
            if stored_hash == hash_password(&password) {
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
            message: "管理员密码未设置".to_string(),
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
            if stored_hash != hash_password(&old_password) {
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
            message: "管理员密码未设置".to_string(),
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
            if stored_hash == hash_password(&password) {
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
            message: "管理员密码未设置".to_string(),
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
