use chrono::Local;
use serde::{Deserialize, Serialize};
use std::fs::{self, File, OpenOptions};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use tauri::AppHandle;
use tracing_subscriber::EnvFilter;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum LogLevel {
    Debug,
    Info,
    Warn,
    Error,
}

impl LogLevel {
    pub fn as_str(&self) -> &'static str {
        match self {
            LogLevel::Debug => "debug",
            LogLevel::Info => "info",
            LogLevel::Warn => "warn",
            LogLevel::Error => "error",
        }
    }

    pub fn from_str(s: &str) -> Option<Self> {
        match s {
            "debug" => Some(LogLevel::Debug),
            "info" => Some(LogLevel::Info),
            "warn" => Some(LogLevel::Warn),
            "error" => Some(LogLevel::Error),
            _ => None,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LogEntry {
    pub timestamp: String,
    pub level: String,
    pub message: String,
    pub source: Option<String>,
}

pub struct LoggerService {
    log_dir: PathBuf,
    current_level: parking_lot::RwLock<LogLevel>,
    max_files: usize,
}

impl Clone for LoggerService {
    fn clone(&self) -> Self {
        Self {
            log_dir: self.log_dir.clone(),
            current_level: parking_lot::RwLock::new(*self.current_level.read()),
            max_files: self.max_files,
        }
    }
}

impl LoggerService {
    pub fn new(log_dir: PathBuf) -> Self {
        Self {
            log_dir,
            current_level: parking_lot::RwLock::new(LogLevel::Info),
            max_files: 30,
        }
    }

    pub fn set_level(&self, level: LogLevel) {
        *self.current_level.write() = level;
    }

    pub fn get_level(&self) -> LogLevel {
        *self.current_level.read()
    }

    pub fn get_log_dir(&self) -> &PathBuf {
        &self.log_dir
    }

    fn get_log_file_path(&self) -> PathBuf {
        let date = Local::now().format("%Y-%m-%d").to_string();
        self.log_dir.join(format!("classisscore-{}.log", date))
    }

    pub fn write_log(&self, level: LogLevel, message: &str, source: Option<&str>) {
        if !self.should_log(level) {
            return;
        }

        let timestamp = Local::now().format("%Y-%m-%d %H:%M:%S%.3f").to_string();
        let entry = LogEntry {
            timestamp: timestamp.clone(),
            level: level.as_str().to_string(),
            message: message.to_string(),
            source: source.map(|s| s.to_string()),
        };

        let log_line = match serde_json::to_string(&entry) {
            Ok(s) => s,
            Err(_) => format!(
                r#"{{"timestamp":"{}","level":"{}","message":"{}"}}"#,
                timestamp,
                level.as_str(),
                message.replace('"', "'")
            ),
        };

        if let Ok(file) = OpenOptions::new()
            .create(true)
            .append(true)
            .open(self.get_log_file_path())
        {
            let mut writer = std::io::BufWriter::new(file);
            let _ = writeln!(writer, "{}", log_line);
        }
    }

    fn should_log(&self, level: LogLevel) -> bool {
        let current = *self.current_level.read();
        let current_rank = match current {
            LogLevel::Debug => 0,
            LogLevel::Info => 1,
            LogLevel::Warn => 2,
            LogLevel::Error => 3,
        };
        let level_rank = match level {
            LogLevel::Debug => 0,
            LogLevel::Info => 1,
            LogLevel::Warn => 2,
            LogLevel::Error => 3,
        };
        level_rank >= current_rank
    }

    pub fn read_logs(&self, lines: usize) -> Vec<String> {
        let mut result: Vec<String> = Vec::new();
        let files = self.get_log_files();

        for file_path in files.into_iter().rev() {
            if result.len() >= lines {
                break;
            }
            if let Ok(file) = File::open(&file_path) {
                let reader = BufReader::new(file);
                let file_lines: Vec<String> = reader.lines().filter_map(|l| l.ok()).collect();

                for line in file_lines.into_iter().rev() {
                    if result.len() >= lines {
                        break;
                    }
                    if !line.trim().is_empty() {
                        result.push(line);
                    }
                }
            }
        }

        result.reverse();
        result
    }

    pub fn clear_logs(&self) -> Result<(), String> {
        let files = self.get_log_files();
        for file_path in files {
            let _ = fs::remove_file(file_path);
        }
        Ok(())
    }

    fn get_log_files(&self) -> Vec<PathBuf> {
        if !self.log_dir.exists() {
            return Vec::new();
        }

        let mut files: Vec<PathBuf> = match fs::read_dir(&self.log_dir) {
            Ok(entries) => entries
                .filter_map(|entry| entry.ok())
                .filter(|entry| entry.path().extension().map_or(false, |ext| ext == "log"))
                .map(|entry| entry.path())
                .collect(),
            Err(_) => Vec::new(),
        };

        files.sort_by(|a, b| {
            let meta_a = fs::metadata(a).ok();
            let meta_b = fs::metadata(b).ok();
            match (meta_a, meta_b) {
                (Some(ma), Some(mb)) => ma
                    .modified()
                    .unwrap_or(std::time::SystemTime::UNIX_EPOCH)
                    .cmp(&mb.modified().unwrap_or(std::time::SystemTime::UNIX_EPOCH)),
                _ => std::cmp::Ordering::Equal,
            }
        });

        files
    }
}

/// 初始化日志系统，返回 LoggerService 实例
pub fn init_logger(_app_handle: &AppHandle, log_dir: PathBuf) -> LoggerService {
    let _ = fs::create_dir_all(&log_dir);

    // 初始化 tracing（输出到控制台 + 文件）
    let file_appender = tracing_appender::rolling::daily(&log_dir, "classisscore-tracing.log");
    let (non_blocking, _guard) = tracing_appender::non_blocking(file_appender);

    // 将 _guard 泄漏以避免在 main 函数结束前被 drop
    std::mem::forget(_guard);

    tracing_subscriber::fmt()
        .with_env_filter(
            EnvFilter::try_from_default_env()
                .unwrap_or_else(|_| EnvFilter::new("info")),
        )
        .with_writer(non_blocking)
        .with_ansi(false)
        .init();

    tracing::info!("日志系统已初始化，日志目录: {:?}", log_dir);

    LoggerService::new(log_dir)
}
