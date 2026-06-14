# ClassIsScore

教室大屏多功能积分管理软件，基于 Tauri 2 + Rust + Vue 3 构建。

## 功能特性

### 核心功能
- **学生管理** — 支持 Excel/CSV 导入学生名单，按姓名首字母/学号排序
- **积分管理** — 加减分操作、常用评价项快捷操作、3分钟免验证撤销机制
- **历史记录** — 完整的积分变动记录，支持按学生/日/周/月筛选
- **周期统计** — 日/周/月加减分统计面板

### 积分显示
- **卡片样式** — 头像 + 姓名 + 积分并排展示
- **圆形样式** — 圆形头像 + 底部积分叠加
- **宠物模式** — 宠物喂养升级效果，支持自定义样式

### 大屏展示
- 独立全屏窗口，支持个人/小组排行榜切换
- 可配置自动刷新（10s/30s/60s）
- 修仙主题卡片模式，显示境界信息
- 实时事件推送，积分变化即时刷新

### 结算系统
- 手动结算，积分与历史清空
- 自动 zip 备份，支持撤销恢复
- 导出表格支持三种格式：
  - 月度：姓名 | 第一周~第五周 | 总积分
  - 周度：姓名 | 日期1~7 | 总积分
  - 日度：姓名 | 积分明细 | 总积分

### 排行榜
- 个人/小组排行榜，日/周/月/全部维度
- 前三名金银铜牌样式
- 支持数据导出

### 管理员验证
- 密码验证（SHA256 哈希）
- U盘验证（检测可移动磁盘）
- 人脸验证（预留接口）

### 修仙主题
- 64级境界体系（练气→大乘）
- 修仙术语映射（积分→灵力，小组→宗门，管理员→掌门）
- 切磋系统、渡劫系统、突破系统
- 仙宠养成

### 其他功能
- **小组功能** — 创建小组、整组评价
- **自动评价** — 定时评价（天/周/月/结算前）
- **悬浮窗** — 无焦点置顶快捷入口
- **首次引导** — 5步引导向导
- **数据导出/导入** — 全量 zip 导出导入、学生数据 Excel 导出导入
- **自定义主题色** — 9种预设色 + 自定义
- **插件接口** — 预留插件加载接口

## 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| Tauri | 2.x | 桌面应用框架 |
| Rust | 2021 edition | 后端逻辑 + IPC 命令 |
| Vue | 3.5 | 前端 UI 框架 |
| TypeScript | 5.x | 前端类型安全 |
| Element Plus | 2.9 | UI 组件库 |
| sea-orm | 1.x | ORM（SQLite） |
| SQLite | — | 嵌入式数据库 |
| Pinia | 2.x | 状态管理 |
| Vite | 6.x | 前端构建工具 |
| tracing | 0.1 | Rust 日志系统 |

## 项目结构

```
client/
├── src/                          # 前端源码
│   ├── components/               # 组件
│   │   ├── common/               # 通用组件
│   │   ├── display/              # 大屏展示组件
│   │   ├── layout/               # 布局组件
│   │   ├── score/                # 积分相关组件
│   │   ├── student/              # 学生相关组件
│   │   └── xianxia/              # 修仙主题组件
│   ├── plugins/                  # 插件加载器
│   ├── router/                   # 路由配置
│   ├── services/                 # IPC 服务层
│   │   ├── tauri.ts              # Tauri invoke 封装
│   │   ├── websocket.ts          # 事件系统适配
│   │   ├── student.ts            # 学生服务
│   │   ├── score.ts              # 积分服务
│   │   ├── group.ts              # 小组服务
│   │   ├── evaluation.ts         # 评价项服务
│   │   ├── leaderboard.ts        # 排行榜服务
│   │   ├── settlement.ts         # 结算服务
│   │   ├── settings.ts           # 设置服务
│   │   ├── autoScore.ts          # 自动评价服务
│   │   ├── theme.ts              # 主题服务
│   │   ├── plugin.ts             # 插件服务
│   │   └── log.ts                # 日志服务
│   ├── stores/                   # Pinia 状态
│   ├── themes/                   # 主题资源
│   │   ├── xianxia/              # 修仙主题
│   │   ├── dark.css              # 暗色主题
│   │   ├── light.css             # 亮色主题
│   │   └── variables.css         # CSS 变量
│   ├── types/                    # 类型定义
│   ├── utils/                    # 工具函数
│   └── views/                    # 页面视图
│       ├── admin/                # 管理页面
│       ├── display/              # 大屏展示
│       ├── floating/             # 悬浮窗
│       └── onboarding/           # 引导向导
├── src-tauri/                    # Rust 后端源码
│   ├── src/
│   │   ├── commands/             # Tauri IPC 命令
│   │   │   ├── student.rs        # 学生管理
│   │   │   ├── score.rs          # 积分管理
│   │   │   ├── group.rs          # 小组管理
│   │   │   ├── evaluation.rs     # 评价项管理
│   │   │   ├── leaderboard.rs    # 排行榜
│   │   │   ├── settlement.rs     # 结算
│   │   │   ├── settings.rs       # 设置
│   │   │   ├── auth.rs           # 认证
│   │   │   ├── auto_score.rs     # 自动评价
│   │   │   ├── log.rs            # 日志
│   │   │   ├── app.rs            # 应用控制
│   │   │   ├── theme.rs          # 主题管理
│   │   │   └── plugin.rs         # 插件管理
│   │   ├── db/                   # 数据库层
│   │   │   ├── entities/         # sea-orm 实体
│   │   │   ├── connection.rs     # 连接管理
│   │   │   └── migration.rs      # 数据库迁移
│   │   ├── services/
│   │   │   └── logger.rs         # 日志服务
│   │   ├── lib.rs                # 应用入口
│   │   └── main.rs               # main 函数
│   ├── Cargo.toml
│   └── tauri.conf.json
├── package.json
├── vite.config.ts
└── tsconfig.json
```

## 构建与运行

### 环境要求
- Node.js 18+
- Rust 1.77+（安装 [rustup](https://rustup.rs/)）
- Windows 10+ / macOS 12+ / Linux（X11/Wayland）

### 开发模式
```bash
cd client
npm install
npm run tauri:dev
```

### 构建发布
```bash
cd client
npm run tauri:build
```

构建产物位于 `client/src-tauri/target/release/bundle/`：
- Windows: `.msi` 安装包 + `.zip` 绿色便携版
- macOS: `.dmg`
- Linux: `.deb` / `.AppImage`

## 架构说明

### 通信方式
- **IPC 命令**：前端通过 `invoke()` 调用 Rust `#[tauri::command]` 函数
- **事件系统**：Rust 通过 `app.emit()` 推送实时事件，前端通过 `listen()` 监听
- **封装层**：`services/tauri.ts` 统一封装 invoke，非 Tauri 环境回退 HTTP API

### 数据库
- 嵌入式 SQLite，数据文件存储在应用数据目录
- sea-orm 实体定义 + 自动迁移
- 7 张核心表：student, score_record, student_group, evaluation_item, admin_settings, settlement_record, auto_evaluation_config

### 日志系统
- Rust 端：tracing + tracing-appender（双层日志：控制台 + 文件）
- 前端端：Vue errorHandler + window.error + unhandledrejection → `log_write` IPC 转发

## 许可证

MIT License
