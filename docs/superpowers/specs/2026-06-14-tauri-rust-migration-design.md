# Tauri 2 + Rust 架构迁移设计

## 概述

将 ClassIsScore 从 Electron + Spring Boot 架构迁移到 Tauri 2 + Rust 架构，实现后端内嵌进程、瞬间启动、统一日志系统。

## 当前架构

```
Electron (主进程)
  ├── spawn java -jar server.jar (独立子进程，启动10-30秒)
  ├── 窗口管理 (BrowserWindow)
  └── 前端 (Vue 3 + Element Plus)
        └── HTTP API → Spring Boot (localhost:18888)
```

问题：
- 后端是独立 JAR 进程，启动慢导致 Network Error
- 两个进程管理复杂，日志分散
- 依赖 JRE/JDK，安装包大
- Electron 安装包 ~150MB

## 目标架构

```
Tauri 2 (单进程)
  ├── Rust 后端 (内嵌，启动瞬间)
  │     ├── axum HTTP 服务器 (大屏/外部访问)
  │     ├── Tauri IPC 命令 (主通信方式)
  │     ├── sea-orm + SQLite (数据层)
  │     ├── tokio (异步运行时)
  │     └── tracing + 日志文件 (统一日志)
  └── 前端 (Vue 3 + Element Plus，不变)
        ├── Tauri IPC → Rust 命令 (主要)
        └── HTTP API → axum (大屏展示等)
```

## 技术选型

| 组件 | 当前 | 目标 | 理由 |
|------|------|------|------|
| 桌面框架 | Electron | Tauri 2 | 安装包小、后端内嵌、安全 |
| 后端语言 | Java (Spring Boot) | Rust (axum) | 内嵌进程、高性能 |
| 数据库 | H2 | SQLite | Rust 生态成熟、单文件部署 |
| ORM | MyBatis Plus | sea-orm | Rust 主流异步 ORM |
| 前端 | Vue 3 + Element Plus | 不变 | 保持现有前端代码 |
| 通信方式 | HTTP API | Tauri IPC + HTTP | IPC 更快，HTTP 用于大屏 |
| 日志 | console.log | tracing + 文件 | 参考 SecScore 模式 |

## 通信架构

### 主通信：Tauri IPC 命令

前端通过 `invoke()` 调用 Rust 命令，零网络开销：

```rust
#[tauri::command]
async fn student_list(state: State<'_, AppState>) -> Result<Vec<Student>, String> {
    // ...
}
```

```typescript
// 前端调用
const students = await invoke<Student[]>('student_list')
```

### 辅助通信：axum HTTP API

用于大屏展示、外部工具访问等场景，按需启动：

```rust
// 大屏展示窗口通过 HTTP 访问
// GET /api/leaderboard
// GET /api/scores/recent
```

### 实时推送：Tauri 事件

替代 WebSocket，用于积分变更实时推送：

```rust
app.emit("score-update", payload)?;
```

```typescript
// 前端监听
listen('score-update', (event) => { ... })
```

## 数据层设计

### SQLite + sea-orm

参考 SecScore 的数据库设计：

```rust
// 实体定义
#[derive(Clone, Debug, PartialEq, DeriveEntityModel)]
#[sea_orm(table_name = "students")]
pub struct Model {
    #[sea_orm(primary_key)]
    pub id: i64,
    pub name: String,
    pub group_id: Option<i64>,
    pub avatar: Option<String>,
    pub total_score: i32,
    pub pet_type: Option<String>,
    pub pet_name: Option<String>,
    pub pet_exp: i32,
    pub created_at: DateTime,
    pub updated_at: DateTime,
}
```

### 数据迁移

H2 → SQLite 数据迁移策略：
1. 启动时检测 H2 数据文件是否存在
2. 如存在，读取 H2 数据写入 SQLite
3. 迁移完成后重命名 H2 文件为 `.bak`
4. 后续启动直接使用 SQLite

## 日志系统设计

参考 SecScore 的 LoggerService + ExamAware2 的 winston 模式：

### Rust 端日志

```rust
// 使用 tracing + tracing-appender
use tracing_subscriber::{fmt, EnvFilter};
use tracing_appender::{non_blocking, rolling};

fn init_logger(app_handle: &AppHandle) {
    let log_dir = app_handle.path().app_data_dir().unwrap().join("logs");
    let file_appender = rolling::daily(log_dir, "classisscore.log");
    let (non_blocking, _guard) = non_blocking(file_appender);

    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env().add_directive("info".parse().unwrap()))
        .fmt_fields(fmt::format::PrettyFields::new())
        .with_writer(non_blocking)
        .init();
}
```

### 前端日志转发

参考 SecScore 的 `patchConsole` 模式，前端 console 输出转发到 Rust 写入文件：

```typescript
// preload 中拦截 console
const origLog = console.log
console.log = (...args) => {
    origLog.apply(console, args)
    invoke('log_write', { level: 'info', message: args.join(' ') })
}
```

### 日志文件位置

`app_data_dir/logs/classisscore.YYYY-MM-DD.log`

- macOS: `~/Library/Application Support/com.classisscore.app/logs/`
- Windows: `%APPDATA%/com.classisscore.app/logs/`
- Linux: `~/.local/share/com.classisscore.app/logs/`

### 日志管理

- 按日期滚动，保留 30 天
- 单文件最大 20MB
- 提供 IPC 命令：`log_query`、`log_clear`、`log_set_level`
- 前端设置页面可查看/清理日志

## API 迁移映射

### 学生管理 (StudentController)

| Java 端点 | Rust IPC 命令 |
|-----------|---------------|
| GET /api/students | `student_list` |
| GET /api/students/{id} | `student_get { id }` |
| POST /api/students | `student_create { name, group_id, ... }` |
| PUT /api/students/{id} | `student_update { id, name, ... }` |
| DELETE /api/students/{id} | `student_delete { id }` |

### 积分管理 (ScoreController)

| Java 端点 | Rust IPC 命令 |
|-----------|---------------|
| GET /api/scores | `score_list { student_id?, ... }` |
| POST /api/scores | `score_add { student_id, score, reason, ... }` |
| POST /api/scores/batch | `score_batch { student_ids, score, ... }` |
| POST /api/scores/{id}/revert | `score_revert { id }` |
| GET /api/scores/recent | `score_recent { limit }` |

### 其他控制器类似映射

- GroupController → `group_list/create/update/delete`
- EvaluationController → `evaluation_list/create/update/delete`
- LeaderboardController → `leaderboard_query/by_group/individual`
- SettlementController → `settlement_list/create/complete/rollback`
- SettingsController → `settings_get_all/set/get/set_value`
- AdminController → `auth_login/change_password/verify/...`
- AutoEvaluationConfigController → `auto_score_get_rules/add/update/delete/toggle`
- PluginController → `plugin_list/upload/toggle/delete`
- ThemeController → `theme_list/get_css/upload/toggle/delete`

## 启动流程

```
1. Tauri 应用启动
2. setup() 回调：
   a. 初始化日志系统 (tracing)
   b. 初始化数据库 (SQLite 连接 + 迁移)
   c. 初始化应用状态 (AppState)
   d. 启动 axum HTTP 服务器 (可选，用于大屏)
3. 前端窗口加载
4. 前端通过 IPC 直接调用 Rust 命令
   → 无需等待后端就绪，因为后端已在 setup() 中同步初始化
```

对比当前架构，关键改进：
- **无需健康检查**：Rust 后端在 `setup()` 中同步初始化，窗口加载时后端一定就绪
- **无 Network Error**：IPC 通信不走网络，不可能出现连接错误
- **启动瞬间**：SQLite 打开 + 迁移通常 < 100ms

## 项目结构

```
classisscore/
├── src-tauri/                    # Rust 后端
│   ├── Cargo.toml
│   ├── tauri.conf.json
│   ├── build.rs
│   ├── capabilities/
│   │   └── default.json
│   ├── icons/
│   └── src/
│       ├── main.rs               # 入口
│       ├── lib.rs                # Tauri 配置 + setup
│       ├── state.rs              # AppState
│       ├── commands/             # IPC 命令
│       │   ├── mod.rs
│       │   ├── student.rs
│       │   ├── score.rs
│       │   ├── group.rs
│       │   ├── evaluation.rs
│       │   ├── leaderboard.rs
│       │   ├── settlement.rs
│       │   ├── settings.rs
│       │   ├── auth.rs
│       │   ├── auto_score.rs
│       │   ├── plugin.rs
│       │   ├── theme.rs
│       │   ├── log.rs
│       │   └── http_server.rs
│       ├── db/                   # 数据层
│       │   ├── mod.rs
│       │   ├── connection.rs
│       │   ├── migration.rs
│       │   ├── entities/
│       │   └── repositories/
│       ├── services/             # 业务逻辑
│       │   ├── mod.rs
│       │   ├── student_service.rs
│       │   ├── score_service.rs
│       │   └── ...
│       └── models/               # 数据模型
│           ├── mod.rs
│           └── ...
├── src/                          # Vue 3 前端 (保持不变)
│   ├── App.vue
│   ├── main.ts
│   ├── views/
│   ├── components/
│   ├── stores/
│   ├── services/
│   │   └── api.ts               # 改为 Tauri IPC 调用
│   └── themes/
├── package.json
├── vite.config.ts
└── tsconfig.json
```

## 前端改造范围

### 需要修改的文件

1. **`src/services/api.ts`** → 改为 Tauri IPC 调用封装
2. **`src/stores/*.ts`** → API 调用方式从 HTTP 改为 invoke
3. **`src/App.vue`** → 移除后端等待逻辑（不再需要）
4. **`vite.config.ts`** → 添加 Tauri 插件
5. **`package.json`** → 移除 Electron 依赖，添加 Tauri 依赖

### 不需要修改的文件

- 所有 Vue 组件（视图、布局等）
- 主题系统（terminology、cultivationLevels 等）
- 修仙特色功能（仙宠渡劫、道友切磋等）
- Pinia stores 的状态逻辑（只改 API 调用方式）

### API 调用迁移示例

```typescript
// 之前 (HTTP)
const res = await api.get('/api/students')
return res.data.data

// 之后 (Tauri IPC)
const students = await invoke<Student[]>('student_list')
return students
```

## 可删除的文件

迁移完成后可删除：
- `client/electron/` 整个目录（Electron 主进程）
- `server/` 整个目录（Spring Boot 后端）
- `client/src/App.vue` 中的后端等待逻辑
- `client/src/services/api.ts` 中的 Network Error 处理

## 实施阶段

### 阶段1：Tauri 项目骨架
- 初始化 Tauri 2 项目
- 配置 Vue 3 前端集成
- 实现 SQLite 数据库连接和迁移
- 实现日志系统

### 阶段2：核心 API 迁移
- 学生管理 CRUD
- 积分管理 CRUD
- 小组管理 CRUD
- 评估项管理 CRUD

### 阶段3：高级功能迁移
- 排行榜查询
- 结算系统
- 管理员认证
- 自动评估调度
- WebSocket → Tauri 事件

### 阶段4：扩展功能迁移
- 插件系统
- 主题管理
- HTTP 服务器（大屏展示）
- 数据导入导出

### 阶段5：前端适配 + 清理
- API 调用从 HTTP 改为 IPC
- 移除 Electron 相关代码
- 移除 Spring Boot 相关代码
- 端到端测试
