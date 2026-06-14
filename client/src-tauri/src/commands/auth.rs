use crate::db::entities::admin_settings;
use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::{ActiveModelTrait, ColumnTrait, EntityTrait, QueryFilter, Set};
use sha2::{Digest, Sha256};
use std::sync::Arc;
use tauri::State;

fn hash_password(password: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(password.as_bytes());
    hex::encode(hasher.finalize())
}

fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<sea_orm::DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
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

    // 设置管理员密码
    let existing = admin_settings::Entity::find()
        .filter(admin_settings::Column::SettingKey.eq("admin_password"))
        .one(&db)
        .await
        .map_err(|e| e.to_string())?;

    if let Some(model) = existing {
        let mut active: admin_settings::ActiveModel = model.into();
        active.setting_value = Set(Some(hash_password(&admin_password)));
        active.updated_at = Set(chrono::Local::now().naive_utc());
        active.update(&db).await.map_err(|e| e.to_string())?;
    } else {
        let new_setting = admin_settings::ActiveModel {
            setting_key: Set("admin_password".to_string()),
            setting_value: Set(Some(hash_password(&admin_password))),
            ..Default::default()
        };
        new_setting.insert(&db).await.map_err(|e| e.to_string())?;
    }

    // 设置 USB 密码
    if let Some(usb_pwd) = usb_password {
        let existing = admin_settings::Entity::find()
            .filter(admin_settings::Column::SettingKey.eq("usb_password"))
            .one(&db)
            .await
            .map_err(|e| e.to_string())?;

        if let Some(model) = existing {
            let mut active: admin_settings::ActiveModel = model.into();
            active.setting_value = Set(Some(hash_password(&usb_pwd)));
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
        } else {
            let new_setting = admin_settings::ActiveModel {
                setting_key: Set("usb_password".to_string()),
                setting_value: Set(Some(hash_password(&usb_pwd))),
                ..Default::default()
            };
            new_setting.insert(&db).await.map_err(|e| e.to_string())?;
        }
    }

    // 设置人脸密码
    if let Some(face_pwd) = face_password {
        let existing = admin_settings::Entity::find()
            .filter(admin_settings::Column::SettingKey.eq("face_password"))
            .one(&db)
            .await
            .map_err(|e| e.to_string())?;

        if let Some(model) = existing {
            let mut active: admin_settings::ActiveModel = model.into();
            active.setting_value = Set(Some(hash_password(&face_pwd)));
            active.updated_at = Set(chrono::Local::now().naive_utc());
            active.update(&db).await.map_err(|e| e.to_string())?;
        } else {
            let new_setting = admin_settings::ActiveModel {
                setting_key: Set("face_password".to_string()),
                setting_value: Set(Some(hash_password(&face_pwd))),
                ..Default::default()
            };
            new_setting.insert(&db).await.map_err(|e| e.to_string())?;
        }
    }

    Ok(AuthResult {
        success: true,
        message: "密码设置成功".to_string(),
    })
}
