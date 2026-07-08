# ClassIsScore 开发文档

> 面向开发者：架构分层、模块边界、调试技巧、发布流程、常见问题。

---

## 目录

1. [架构概览](#1-架构概览)
2. [模块边界](#2-模块边界)
3. [平台分支](#3-平台分支)
4. [开发指南](#4-开发指南)
5. [调试](#5-调试)
6. [代码规范](#6-代码规范)
7. [主题与样式](#7-主题与样式)
8. [发布](#8-发布)
9. [常见问题](#9-常见问题)

---

## 1. 架构概览

### 1.1 总体形态

```
┌──────────────────────────── Tauri 单进程 ────────────────────────────┐
│                                                                     │
│  ┌──────── Vue 3 + TS 前端 (WebView) ─────────┐  ┌── Rust 后端 ───┐ │
│  │ App.vue → router → views/...                │  │               │ │
│  │       ↓ invoke()                listen() ←──┼──┤ Tauri 命令    │ │
│  │ services/ (api 包装)                        │  │  (commands/)  │ │
│  └─────────────────────────────────────────────┘  │               │ │
│                                                 │ sea-orm        │ │
│                                                 │  + SQLite      │ │
│                                                 │               │ │
│                                                 │ axum HTTP      │ │
│                                                 │  (大屏 / IPC)  │ │
│                                                 └───────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

- **Tauri 2** 担任单进程壳，桌面与移动共享同一份后端代码
- **前端**（Vue 3）通过 `invoke()` 调用 Rust 命令、通过事件反向推送
- **后端**（Rust）承担所有业务逻辑、数据库、HTTP、日志
- **数据库**使用 SQLite，跨平台零配置

### 1.2 前端分层

```
views/                       页面（与路由一一对应）
  ↓ 引用
components/                  组件（按业务域拆：layout / display / score / ...）
  ↓ 引用
services/ + stores/          数据层（service = invoke 包装、store = 状态）
  ↓ 引用
utils/                       纯函数（平台判断、Excel、宠物系统等）
```

### 1.3 后端分层

```
main.rs / lib.rs             入口（桌面走 main、移动走 mobile_entry_point）
  ↓
commands/                    Tauri 命令（按领域拆：score / student / ...）
  ↓
db/ entities                 sea-orm 实体与数据库连接
services/                    横切关注点：paths（路径）、logger（日志）
platform/                    平台分支：desktop.rs / mobile.rs
```

---

## 2. 模块边界

### 2.1 前端模块

| 目录 | 职责 | 不要做 |
|---|---|---|
| `views/admin/` | 桌面端管理页面 | 不写平台判断（用 `meta.layout` 让路由决定） |
| `views/display/` | 大屏展示页 | 不混入管理逻辑 |
| `views/onboarding/` | 引导流程 | 仅首次启动使用 |
| `views/floating/` | 桌面端悬浮条 | 移动端禁用 |
| `components/common/` | 通用 UI 控件 | 不依赖具体业务 |
| `components/display/` | 大屏展示组件 | 独立于管理 UI 样式 |
| `components/layout/` | `AdminLayout` / `MobileLayout` | 不要把路由写死 |
| `components/xianxia/` | 修仙主题专属组件 | 默认主题不应引用 |
| `services/` | `invoke()` 包装 | 不要做业务编排，编排放 store |
| `stores/` | 状态管理 | 不要直接调 `invoke`（经 services） |
| `utils/platform.ts` | 平台判断 | 唯一允许写 `isAndroid()` 的地方 |
| `themes/` | 主题样式 | 主题包在 `themes/<name>/` 自包含 |

### 2.2 后端模块

| 目录 | 职责 | 约定 |
|---|---|---|
| `commands/<domain>.rs` | Tauri 命令入口 | 只做参数接收 → 调用服务层 |
| `db/entities/<entity>.rs` | sea-orm 实体 | 一个表一个文件 |
| `db/connection.rs` | DB 连接管理 | 通过 `AppHandle` 拿路径，不要用 `current_exe()` |
| `db/migration.rs` | 迁移 | 启动时自动跑 |
| `services/paths.rs` | 路径解析 | 统一用 `app.path().app_data_dir()` |
| `services/logger.rs` | 日志初始化 | 接收 `&Path`，不要假设 cwd |
| `platform/desktop.rs` | 桌面专属 | 系统托盘、菜单 |
| `platform/mobile.rs` | 移动专属 | Android 生命周期、权限申请 |

### 2.3 跨层通信

- **前端 → 后端**：`invoke('<command_name>', { ...args })`（见 [docs/URI.md](URI.md)）
- **后端 → 前端**：`app_handle.emit('<event>', payload)` → 前端 `listen('<event>', cb)`
- **常用事件**：`navigate`（路由跳转）、`score-updated`（积分实时推送）、`log-message`（日志实时回传）

---

## 3. 平台分支

### 3.1 Rust 端

#### 条件编译基础

```rust
// 仅桌面
#[cfg(not(target_os = "android"))]
fn open_external(_app: &tauri::AppHandle, path: &str) -> Result<(), String> {
    open::that(path).map_err(|e| e.to_string())
}

// 仅 Android
#[cfg(target_os = "android")]
fn open_external(app: &tauri::AppHandle, path: &str) -> Result<(), String> {
    use tauri_plugin_shell::ShellExt;
    app.shell().open(path, None).map_err(|e| e.to_string())
}
```

#### `platform/mod.rs` 分流

```rust
#[cfg(not(target_os = "android"))]
mod desktop;
#[cfg(target_os = "android")]
mod mobile;

pub fn init<R: tauri::Runtime>(app: &tauri::App<R>) -> Result<(), Box<dyn std::error::Error>> {
    #[cfg(not(target_os = "android"))]
    { desktop::init(app)?; }
    #[cfg(target_os = "android")]
    { mobile::init(app)?; }
    Ok(())
}
```

#### 依赖拆分

```toml
# Cargo.toml
[target.'cfg(not(target_os = "android"))'.dependencies]
open = "5"

[target.'cfg(target_os = "android")'.dependencies]
tauri-plugin-haptics = "2"
tauri-plugin-biometric = "2"
```

桌面端用 `open` 调起系统浏览器；Android 端用 `tauri-plugin-shell` 的 `Shell::open`（更可靠）。

#### 窗口模型

| 平台 | 窗口模型 | 大屏展示实现 |
|---|---|---|
| 桌面 | 多窗口 | `WebviewWindowBuilder` 创建新窗口 |
| 移动 | 单窗口 | 触发前端 `navigate` 事件跳 `/display` 路由 |

#### 路径 API

```rust
use tauri::Manager;
let app_data = app.path().app_data_dir()?;       // 各平台标准数据目录
let app_local = app.path().app_local_data_dir()?; // 各平台本地缓存
let log_dir = app_data.join("logs");
```

> **不要**用 `std::env::current_exe()`，在 Android 上会直接 panic。

### 3.2 前端端

#### 平台判断

所有平台判断集中在 [`client/src/utils/platform.ts`](../client/src/utils/platform.ts)：

```ts
import { isAndroid, isMobile, isDesktop } from '@/utils/platform'

if (isAndroid()) { /* ... */ }
if (isMobile())  { /* 移动端 MobileLayout */ }
if (isDesktop()) { /* 桌面端 AdminLayout */ }
```

#### 路由

桌面 / 移动用两套并列的子路由：

```ts
const routes = [
  { path: '/', redirect: '/admin/dashboard' },
  { path: '/admin', component: AdminLayout,   children: [/* 桌面 */] },
  { path: '/m',     component: MobileLayout,  children: [/* 移动 */] },
  { path: '/display',     component: ScoreDisplay, meta: { layout: 'none' } },
  { path: '/onboarding',  component: Onboarding,  meta: { layout: 'none' } },
]
```

#### 布局选择

`App.vue` 不再判断 layout，直接 `<router-view />`，由嵌套 layout 自包含。`meta.layout` 仅用于特殊场景。

#### 移动专属 UI

- `MobileLayout.vue`：固定顶栏（菜单 / 大屏 / 标题）+ 底部 5 项导航 + 左侧抽屉
- 路由全部在 `/m/...` 前缀下
- 表单、列表自动用响应式样式，无需单独组件

---

## 4. 开发指南

### 4.1 新增一个 Tauri 命令

```rust
// 1. 在 commands/<domain>.rs 定义
#[tauri::command]
pub async fn my_action(app: tauri::AppHandle, arg: String) -> Result<String, String> {
    // ... 业务 ...
    Ok("done".into())
}

// 2. 在 commands/mod.rs 导出
pub use my_action::*;

// 3. 在 lib.rs 注册
.invoke_handler(tauri::generate_handler![commands::my_action::my_action])
```

```ts
// 4. 前端 services/<domain>.ts 包装
import { invoke } from './tauri'
export const myAction = (arg: string) => invoke<string>('my_action', { arg })
```

### 4.2 新增一个数据库表

```rust
// 1. db/entities/<entity>.rs — sea-orm 实体
// 2. db/entities/mod.rs — 加导出
// 3. db/migration.rs — 加一条 Migrator
// 4. 重启应用，自动跑迁移
```

### 4.3 新增一个页面

1. 在 `views/admin/` 或 `views/mobile/` 加 `.vue`
2. 在 `router/index.ts` 加路由
3. 如需菜单入口，在 `MobileLayout.vue` 的 `bottomNav` / `drawerMenu` / `pageTitles` 加一项

### 4.4 新增一个主题包

参考 `themes/xianxia/`：
- `themes/<name>/` 自包含
- 注册：见 `client/src/stores/settings.ts` 主题枚举
- 通过 `client/src/themes/xianxia/useTerminology.ts` 注入术语

### 4.5 新增一个插件

- 插件代码放 `client/src/plugins/<name>/`
- `client/src/plugins/loader.ts` 提供加载器
- 计划引入 manifest.yml 描述元数据

---

## 5. 调试

### 5.1 前端调试

- `npm run tauri:dev` 启动后自动打开 DevTools
- Vue DevTools 通过浏览器扩展安装即可
- Pinia state 可在 DevTools 查看

### 5.2 后端日志

```rust
use tracing::{info, warn, error};

info!(student_id = %id, "新增学生");
warn!(score = ?s, "积分越界");
error!(e, "数据库写入失败");
```

日志文件位置（由 `services/paths.rs` 解析）：
- 桌面：`%APPDATA%/ClassIsScore/logs/app.log` 等
- Android：`/sdcard/Android/data/<pkg>/files/logs/`（仅 debug 可读）

### 5.3 常见调试场景

| 现象 | 排查方向 |
|---|---|
| 前端 invoke 报 "command not found" | 检查 `lib.rs` 的 `generate_handler!` 宏 |
| 数据库锁死 / 写入失败 | 检查是否在 setup 阶段直接跑（应 `tauri::async_runtime::spawn`） |
| Android 启动白屏 | 看 logcat：`adb logcat \| grep -i "classisscore\|tauri"` |
| 路径权限错误 | 改用 `app.path().app_data_dir()` |
| WebView 加载不了 | 关闭 CSP：`tauri.conf.json` `app.security.csp` |

### 5.4 跨平台本地联调

```bash
# 桌面端（带热重载）
cd client && npm run tauri:dev

# 移动端（需要 Android Studio + 真机 / 模拟器）
cd client
npx tauri android init --ci   # 首次
npm run tauri:android:dev     # 调试模式
```

---

## 6. 代码规范

### 6.1 通用

- **注释、提交、文档**：必须使用中文（用户规则）
- **类型优先**：TypeScript 严格模式、Python 类型注解、Rust 不用 `unwrap()`（业务路径）
- **条件编译**：所有平台差异通过 `#[cfg(target_os = "android")]` 显式标注，不允许运行时判断
- **不要写废弃路径**：不用 `std::env::current_exe()`、不用 `dialog::MessageDialog` 旧 API

### 6.2 TypeScript

- 组件名用 `PascalCase`
- 文件名用 `PascalCase.vue`、`kebab-case.ts`
- 接口前缀 `I`（`IStudent`），类型别名无前缀
- 跨平台代码必须用 `@/utils/platform` 判断，不要 `window.__TAURI__` 散落判断

### 6.3 Rust

- 命令 / 服务 / 实体分文件，按领域放目录
- 错误用 `Result<T, String>`（Tauri 默认转字符串）
- 日志用 `tracing` 宏，不引入 `println!`
- 异步命令用 `async fn`，内部用 `tokio::spawn` / `tauri::async_runtime::spawn`

### 6.4 Vue 3

- 用 `<script setup lang="ts">`
- 启用 `vue-tsc` 严格类型检查
- 状态放 Pinia，本地状态用 `ref` / `reactive`
- 组件 props / emits 必须有类型

### 6.5 提交

- 提交信息遵循 Conventional Commits（`feat:` / `fix:` / `chore:` / `docs:` / `refactor:`）
- 一次提交聚焦一件事
- 中文描述（用户规则）

---

## 7. 主题与样式

### 7.1 主题文件

- `client/src/themes/variables.css` — 主题变量定义（强调色、间距、圆角等）
- `client/src/themes/light.css` — 亮色模式
- `client/src/themes/dark.css` — 暗色模式
- `client/src/themes/<name>/` — 主题包（如 `xianxia`），包含术语、专属样式、专属组件

### 7.2 主题切换

- `client/src/stores/settings.ts` 持有当前主题名
- 通过 `<html data-theme="dark">` 切换 CSS
- 修仙主题需要通过 `useTerminology()` 注入业务术语

### 7.3 主题包约定

主题包自包含：
- 自有 `*.css`
- 自有 `*.ts`（如 `useTerminology.ts`）
- 自有组件（`components/xianxia/`）
- 不应修改其他主题

---

## 8. 发布

### 8.1 本地构建

```bash
# 桌面当前平台
cd client && npm run tauri:build

# Android debug
cd client && npx tauri android init --ci && npm run tauri:android:build -- --target aarch64 --debug
```

### 8.2 CI 流程

`.github/workflows/build.yml` 在 `push` / `pull_request` / `workflow_dispatch` 时跑：

1. `build-macos` / `build-windows` / `build-linux` — 桌面端 release
2. `build-android` — 移动端 debug APK
3. `release`（仅手动触发）— 下载所有产物，创建 GitHub Release

### 8.3 手动发布

GitHub → Actions → Build Tauri → Run workflow：

- `version`：tag 名（如 `v1.0.6`）
- `release_type`：`release` / `prerelease` / `draft`
- 等四平台全绿，自动出 Release

### 8.4 上架签名（待补）

当前未配置 Android 上架签名。计划：

```jsonc
// tauri.conf.json
"bundle": {
  "android": {
    "minSdkVersion": 26,
    "signingConfig": {
      // 引用 ~/.gradle/keystore.properties
    }
  }
}
```

并配套：
- `android/app/build.gradle` 的 `signingConfigs.release`
- `~/.gradle/keystore.properties`（不进 git）

---

## 9. 常见问题

### Q: 如何添加新的导出格式？
在 `commands/score.rs` 或 `commands/settlement.rs` 中加新命令，前端在 `services/` 加 invoke 包装。

### Q: 如何修改数据存储位置？
改 `client/src-tauri/src/services/paths.rs` 中的 `data_dir()`。**不要**改回 `current_exe()`。

### Q: 如何扩展管理员验证方式？
1. `db/entities/admin_settings.rs` 加枚举值
2. `commands/auth.rs` 加分支
3. 前端 `views/admin/AdminSettings.vue` 加 UI

### Q: Android 启动 ANR / 白屏？
- 业务初始化放 `tauri::async_runtime::spawn`，不要在 `setup()` 同步等
- 检查日志是否写到外置目录（Android 11+ scoped storage）

### Q: 桌面端报"找不到 webview"？
- Win10：装 WebView2 Runtime
- Linux：装 `libwebkit2gtk-4.1-dev`（runtime 共享库）

### Q: 移动端如何改应用 ID？
改 `tauri.conf.json` 的 `identifier`（如 `com.classiscore.app`），并清掉 `gen/android/` 后重跑 `tauri android init`。

### Q: 路径相关问题？
- **不要**用 `std::env::current_exe()`
- 用 `app.path().app_data_dir()` / `app_local_data_dir()` / `home_dir()`
- 在 Android 上这些是 `/data/data/<pkg>/files/` 等标准目录

### Q: 调试模式 vs release 模式？
- 调试：devtools 自动开、HMR、前端连本地服务
- release：完全本地运行、JS minify、Rust LTO、strip 符号

### Q: CI 上 artifact 路径没匹配上？
- 不要用 brace 展开 `{a,b,c}/*`，`upload-artifact@v4` 不支持
- 用多行 `path` 列表
- Android 实际产物路径是 `apk/universal/debug/`，用 `apk/**/debug/*.apk` 兜底
