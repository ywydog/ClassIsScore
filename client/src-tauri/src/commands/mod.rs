pub mod student;
pub mod score;
pub mod group;
pub mod evaluation;
pub mod leaderboard;
pub mod settlement;
pub mod settings;
pub mod auth;
pub mod auto_score;
pub mod log;
pub mod app;
pub mod theme;
pub mod plugin;

use crate::state::AppState;
use parking_lot::RwLock;
use sea_orm::DatabaseConnection;
use std::sync::Arc;
use tauri::State;

pub fn get_db(state: &State<'_, Arc<RwLock<AppState>>>) -> Result<DatabaseConnection, String> {
    let guard = state.read();
    guard.get_db().map(|db| db.clone())
}
