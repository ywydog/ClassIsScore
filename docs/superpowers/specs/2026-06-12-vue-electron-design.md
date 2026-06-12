# ClassIsScore Vue+Electron 版本设计

## 概述

将 ClassIsScore 从 .NET 8 + Avalonia UI 桌面端迁移为 **Vue 3 + Electron + Spring Boot + H2** 架构，保留全部功能。

## 技术栈

| 层 | 技术 | 说明 |
|---|---|---|
| 桌面壳 | Electron 33+ | 多窗口、托盘、系统通知 |
| 前端 | Vue 3 + Vite + Pinia + Vue Router | SPA管理后台 + 大屏展示 |
| UI库 | Element Plus | 管理后台组件 |
| 后端 | Spring Boot 3.x + MyBatis-Plus | 内嵌本地服务，随应用启停 |
| 数据库 | H2 (MySQL兼容模式) | 嵌入式，零配置 |
| 构建 | electron-builder | 打包为安装程序 |

## 架构

```
Electron Main Process
├── 托盘图标 (Tray API)
├── 窗口管理 (BrowserWindow)
│   ├── 主窗口 (管理后台)
│   ├── 大屏展示窗口 (全屏)
│   └── 浮动积分条 (frameless/always-on-top/focusable:false)
├── Spring Boot 子进程管理
│   ├── 启动: spawn java -jar server.jar
│   ├── 健康检查: GET /actuator/health
│   └── 关闭: POST /actuator/shutdown
└── IPC 通信
    ├── 前端 → Main: ipcRenderer.invoke()
    └── Main → 前端: webContents.send()

Vue 3 Renderer (主窗口)
├── /admin/scores — 积分管理
├── /admin/students — 学生管理
├── /admin/groups — 分组管理
├── /admin/leaderboard — 排行榜
├── /admin/evaluation — 自动评估
├── /admin/settlement — 结算
├── /admin/settings — 设置(主题/插件/通用)
├── /admin/admin-settings — 管理员设置
├── /admin/about — 关于
├── /display — 大屏展示(独立窗口)
└── /onboarding — 引导

Spring Boot (localhost:18888)
├── /api/students — 学生CRUD
├── /api/scores — 积分CRUD
├── /api/groups — 分组CRUD
├── /api/leaderboard — 排行榜
├── /api/evaluation — 评估规则
├── /api/settlement — 结算
├── /api/plugins — 插件管理
├── /api/themes — 主题管理
├── /api/settings — 设置
├── /ws/scores — WebSocket积分推送
└── /ws/display — WebSocket大屏同步

H2 Database (~/.classisscore/data)
├── students — 学生表
├── score_records — 积分记录表
├── student_groups — 分组表
├── evaluation_items — 评估项表
├── settlement_records — 结算记录表
├── admin_settings — 管理员设置表
├── app_settings — 应用设置表
├── pet_states — 宠物状态表
└── plugins — 插件注册表
```

## 功能迁移对照

| 原功能 | 原实现 | 新实现 |
|---|---|---|
| 系统托盘 | Avalonia TrayIcon | Electron Tray API (Menu/MenuItem) |
| 浮动窗口 | Avalonia BrowserWindow | Electron BrowserWindow (frameless/always-on-top) |
| 无焦点窗口 | Win32 WS_EX_NOACTIVATE | BrowserWindow { focusable: false } |
| 窗口拖拽 | BeginMoveDrag | CSS -webkit-app-region: drag |
| Mica标题栏 | ExtendClientArea + TransparencyLevel | CSS backdrop-filter + frameless window |
| 插件系统 | .NET AssemblyLoadContext + PluginBase | Java SPI + 前端动态组件 |
| 主题系统 | XAML资源字典 + .cisui包 | CSS变量注入 + 主题JSON + .cistheme包 |
| ShutdownMode | Avalonia ShutdownMode.OnExplicitShutdown | app.on('window-all-closed', () => {}) |
| SetForegroundWindow | Win32 P/Invoke | BrowserWindow.focus() |
| 宠物系统 | C# PetSystem模型 | Vue Canvas动画 + 后端状态存储 |
| 数据持久化 | JSON文件 | H2嵌入式数据库 |
| 积分推送 | 事件总线 | WebSocket |
| 首次引导 | Avalonia Window | Vue Router + Onboarding组件 |

## 项目结构

```
ClassIsScore/
├── client/                    # Vue 3 前端
│   ├── src/
│   │   ├── views/             # 页面组件
│   │   │   ├── admin/         # 管理后台页面
│   │   │   ├── display/       # 大屏展示
│   │   │   ├── floating/      # 浮动积分条
│   │   │   └── onboarding/    # 引导页
│   │   ├── components/        # 公共组件
│   │   ├── stores/            # Pinia状态管理
│   │   ├── router/            # 路由配置
│   │   ├── services/          # API调用层
│   │   ├── plugins/           # 前端插件加载器
│   │   ├── themes/            # 主题系统
│   │   └── utils/             # 工具函数
│   ├── electron/              # Electron主进程
│   │   ├── main.ts            # 主入口
│   │   ├── tray.ts            # 托盘管理
│   │   ├── windows.ts         # 窗口管理
│   │   └── server.ts          # Spring Boot进程管理
│   ├── package.json
│   └── vite.config.ts
├── server/                    # Spring Boot 后端
│   ├── src/main/java/com/classisscore/server/
│   │   ├── config/            # 配置
│   │   ├── controller/        # REST控制器
│   │   ├── service/           # 业务逻辑
│   │   ├── mapper/            # MyBatis-Plus Mapper
│   │   ├── entity/            # 数据库实体
│   │   ├── dto/               # 数据传输对象
│   │   ├── plugin/            # 插件系统(SPI)
│   │   ├── theme/             # 主题系统
│   │   └── websocket/         # WebSocket
│   ├── src/main/resources/
│   │   ├── application.yml
│   │   └── db/migration/      # H2初始化脚本
│   └── pom.xml
├── .github/workflows/
│   ├── build.yml              # 原Avalonia构建
│   └── build-vue.yml          # Vue+Electron构建
└── docs/
```

## 启动流程

1. Electron Main 启动
2. 检查/启动 Spring Boot 子进程，轮询 `/actuator/health` 等待就绪
3. 创建主窗口加载 Vue SPA
4. 初始化系统托盘
5. Vue 前端通过 HTTP/WebSocket 与后端通信
6. 关闭时：POST `/actuator/shutdown` 优雅停止后端 → 退出 Electron

## 插件系统设计

### 后端 (Java SPI)
- 接口: `PluginBase` (initialize / destroy)
- 加载: `java.util.ServiceLoader` + 自定义 ClassLoader
- 包格式: `.cispg` (ZIP, 内含 jar + manifest.json)
- 生命周期: 扫描 → 加载 → 初始化 → 运行 → 销毁

### 前端 (Vue动态组件)
- 插件可注册 Vue 组件到指定插槽
- 通过 API 获取插件列表和组件定义
- 动态 import + defineAsyncComponent 加载

## 主题系统设计

- 主题包格式: `.cistheme` (ZIP, 内含 theme.json + CSS文件)
- theme.json: { id, name, version, author, cssFile, preview }
- 加载: 后端存储主题包 → 前端请求CSS → 动态注入 `<style>` 标签
- CSS变量体系: `--cis-primary`, `--cis-bg`, `--cis-font-size-base` 等
- 内置浅色/深色主题，支持第三方主题包导入
