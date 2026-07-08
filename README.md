# ClassIsScore

> 教室大屏多功能积分管理系统，同时支持桌面端（Windows / macOS / Linux）和移动端（Android / iOS）。

Tauri 2 + Rust 后端，Vue 3 + TypeScript 前端，SQLite 持久化。单进程内嵌后端，瞬间启动、安装包小、可扩展主题与插件。

---

## 功能特性

### 核心功能
- **学生管理** — Excel / CSV 导入学生名单，按姓名 / 学号 / 座位号排序与筛选
- **积分管理** — 加减分、常用评价项快捷操作、3 分钟免验证撤销、批量评价
- **历史记录** — 完整的积分变动记录，按学生 / 日 / 周 / 月 / 学期筛选与导出
- **小组功能** — 创建小组、整组评价、小组排行榜
- **自动评价** — 按天 / 周 / 月 / 结算前定时自动加扣分
- **结算系统** — 手动 / 自动结算、积分与历史清空、zip 备份、撤销恢复
- **排行榜** — 个人 / 小组排行，日 / 周 / 月 / 全部维度，前三金 / 银 / 铜样式

### 展示与互动
- **大屏展示** — 卡片 / 圆形 / 宠物模式三种样式，TV / 投影全屏轮播
- **宠物养成** — 8 级宠物升级、20 余种宠物皮肤
- **修仙主题包** — 内置可选主题：道友境界、仙宠渡劫、突破机制（见 `client/src/themes/xianxia/`）
- **主题系统** — 亮 / 暗模式、9 种预设强调色 + 自定义

### 安全与设置
- **管理员验证** — 密码（SHA256 哈希） / U 盘 / 人脸（预留）
- **首次引导** — 5 步引导向导
- **数据导入导出** — 全量 zip 导出导入、Excel 导出导入
- **URI 协议** — `classisscore://` 外部唤起并跳转指定页面（见 [docs/URI.md](docs/URI.md)）
- **插件接口** — 内置插件加载器（见 `client/src/plugins/loader.ts`）
- **IPC / WebSocket** — 内置 axum HTTP 服务用于大屏 / 外部通信

### 平台覆盖
- 桌面：Windows（.msi）、macOS（universal .app）、Linux（.deb / .rpm / .AppImage）
- 移动：Android（aarch64 / armv7 / x86_64）、iOS（待启用）

---

## 技术栈

| 层 | 技术 | 用途 |
|---|---|---|
| 框架 | Tauri 2 | 单进程桌面 / 移动壳 |
| 后端 | Rust 1.89+ | 内嵌业务逻辑、命令、HTTP |
| 前端 | Vue 3.5 + TypeScript | UI 框架 |
| 路由 | vue-router 4 | hash 模式 |
| 状态 | Pinia | 应用状态管理 |
| UI | Element Plus | 桌面端组件 |
| 持久化 | sea-orm 1 + SQLite | 跨平台数据库 |
| HTTP | axum 0.7 | 大屏 / 外部访问接口 |
| 日志 | tracing + tracing-appender | 统一日志到文件 |
| 构建 | Vite 5 + vite-plugin-tauri | 前端构建 |
| CI | GitHub Actions | 4 平台 + Release |

完整版本见 [`client/package.json`](client/package.json) 与 [`client/src-tauri/Cargo.toml`](client/src-tauri/Cargo.toml)。

---

## 项目结构

```
ClassIsScore/
├── client/                       # 前端 + Tauri 壳
│   ├── src/                      # Vue 3 前端源码
│   │   ├── components/           # 通用 / 业务组件
│   │   │   ├── common/           # 状态条、主题切换
│   │   │   ├── layout/           # AdminLayout / MobileLayout
│   │   │   ├── display/          # 大屏展示、宠物展示
│   │   │   ├── score/            # 积分面板
│   │   │   ├── student/          # 学生卡 / 表单
│   │   │   └── xianxia/          # 修仙主题专属组件
│   │   ├── views/                # 页面
│   │   │   ├── admin/            # 桌面端管理页面
│   │   │   ├── display/          # 大屏展示
│   │   │   ├── floating/         # 桌面端悬浮条
│   │   │   └── onboarding/       # 引导
│   │   ├── router/               # 路由表（桌面 / 移动 / 大屏 / 引导）
│   │   ├── stores/               # Pinia 状态
│   │   ├── services/             # 前端 API 包装（invoke Tauri 命令）
│   │   ├── themes/               # 主题包（light / dark / xianxia）
│   │   ├── utils/                # 工具：平台判断、Excel、宠物、修仙
│   │   ├── plugins/              # 插件加载器
│   │   └── App.vue / main.ts
│   └── src-tauri/                # Rust 后端
│       ├── src/
│       │   ├── commands/         # Tauri 命令（按领域拆分）
│       │   ├── db/               # sea-orm 实体 / 连接 / 迁移
│       │   ├── platform/         # desktop.rs / mobile.rs 平台分支
│       │   ├── services/         # paths、logger 等横切服务
│       │   ├── lib.rs            # 入口（插件注册、命令注册）
│       │   └── main.rs           # 桌面入口（移动端用 mobile_entry_point）
│       ├── capabilities/         # Tauri 权限：default.json / mobile.json
│       ├── icons/                # 多尺寸图标
│       ├── tauri.conf.json       # 应用配置
│       └── Cargo.toml
├── docs/                         # 项目文档
│   ├── DEVELOPMENT.md            # 开发指南、架构、调试、发布
│   ├── URI.md                    # URI 协议调用文档
│   └── superpowers/specs/        # 历史设计文档
└── .github/workflows/build.yml   # CI：四平台构建 + Release
```

---

## 构建与运行

### 环境要求

- **Node.js 20+** 与 npm
- **Rust 1.89+**（`rustup update`）
- 平台依赖：
  - **Windows**：WebView2（Win11 自带，Win10 需安装）
  - **macOS**：Xcode Command Line Tools
  - **Linux**：`libwebkit2gtk-4.1-dev libgtk-3-dev libayatana-appindicator3-dev librsvg2-dev libsoup-3.0-dev libjavascriptcoregtk-4.1-dev patchelf`
  - **Android**（可选）：JDK 17、Android SDK 34、NDK 26.1.10909125

### 本地启动

```bash
# 安装依赖
cd client
npm install

# 启动桌面端（开发模式，热重载）
npm run tauri:dev

# 仅启动 Vite（不带 Tauri 壳）
npm run dev
```

### 桌面端构建

```bash
cd client

# 当前平台 release 包
npm run tauri:build

# 跨平台（需要对应平台工具链）
# macOS 通用包
npm run tauri:build -- --target universal-apple-darwin
```

产物位置：
- 桌面：`client/src-tauri/target/release/bundle/{deb,rpm,appimage,msi,dmg}/`
- 可执行文件：`client/src-tauri/target/release/classisscore(.exe)`

### 移动端构建

```bash
cd client

# 首次构建需要先 init Android 工程
npx tauri android init --ci

# debug 包（使用 SDK 自带 debug.keystore，无需用户签名）
npm run tauri:android:build -- --target aarch64 --debug

# 全部 ABI
npm run tauri:android:build -- --target all --debug
```

产物位置：`client/src-tauri/gen/android/app/build/outputs/apk/{universal,aarch64,...}/debug/*.apk`

> 暂未配置上架签名。如需上架 Google Play，请自备 keystore 并在 `tauri.conf.json` 中配置 `bundle.android.signingConfig`。

---

## 数据存储

所有数据由 Rust 后端通过 sea-orm 写入 SQLite，按平台不同落到：

| 平台 | 数据目录 |
|---|---|
| Windows | `%APPDATA%\ClassIsScore\` |
| macOS | `~/Library/Application Support/ClassIsScore/` |
| Linux | `~/.local/share/ClassIsScore/` |
| Android | `/data/data/com.classiscore.app/files/` |

主要表：
- `students` / `student_groups` / `student_group_members`
- `score_records` / `evaluation_items`
- `auto_evaluation_config`
- `settlement_records`
- `admin_settings`

路径由 `client/src-tauri/src/services/paths.rs` 统一解析，不再使用 `std::env::current_exe()`（在 Android 下会失败）。

---

## URI 协议

支持从外部应用（ClassIsland、命令脚本等）通过 `classisscore://` 唤起 ClassIsScore 并跳转指定页面。

| URI | 行为 |
|---|---|
| `classisscore://app/home` | 桌面端打开主页 |
| `classisscore://app/scores` | 打开积分管理 |
| `classisscore://app/students` | 打开学生管理 |
| `classisscore://app/display` | 桌面端打开新大屏窗口 / 移动端跳 `/display` |
| `classisscore://app/leaderboard` | 打开排行榜 |
| `classisscore://app/settings` | 打开设置 |

桌面 / 移动端会按当前平台自动选择跳转方式（多窗口 / 单窗口内 `navigate` 事件）。详细文档、各语言调用示例、协议注册方法见 [docs/URI.md](docs/URI.md)。

---

## 开发与调试

- 完整开发指南、模块边界、调试技巧、发布流程见 [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)
- 平台抽象与条件编译规范见 DEVELOPMENT.md 中的「平台分支」一节
- 主题包开发示例见 [client/src/themes/xianxia/](client/src/themes/xianxia/)

---

## 持续集成

`.github/workflows/build.yml` 提供：

| Job | 平台 | 产物 |
|---|---|---|
| `build-macos` | macOS-latest | `ClassIsScore-macOS`（.app） |
| `build-windows` | windows-latest | `ClassIsScore-Windows`（.msi） |
| `build-linux` | ubuntu-22.04 | `ClassIsScore-Linux`（.deb / .rpm / .AppImage） |
| `build-android` | ubuntu-22.04 | `ClassIsScore-Android`（debug APK） |
| `release` | 仅手动触发 | 下载全部 artifact，创建 GitHub Release |

**手动发布流程**（GitHub → Actions → Build Tauri → Run workflow）：
1. 填 `version`（如 `v1.0.6`）
2. 选 `release_type`：`release` / `prerelease` / `draft`
3. 等四平台全绿后自动创建 Release 并附上所有产物

---

## 许可证

MIT License
