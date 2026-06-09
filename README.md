# ClassIsScore

教室大屏多功能积分管理软件，基于 .NET 8 + Avalonia UI 构建。

## 功能特性

### 核心功能
- **学生管理** — 支持 Excel/CSV 导入学生名单，按姓名首字母/学号排序
- **积分管理** — 加减分操作、常用评价项快捷操作、3分钟免验证撤销机制
- **历史记录** — 完整的积分变动记录，支持按学生/日/周/月筛选

### 积分显示
- **卡片样式** — 头像 + 姓名 + 积分并排展示
- **圆形样式** — 圆形头像 + 底部积分叠加
- **宠物模式** — 宠物喂养升级效果，支持自定义样式

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

### 其他功能
- **小组功能** — 创建小组、整组评价
- **自动评价** — 定时评价（天/周/月/结算前）
- **悬浮窗** — 无焦点置顶快捷入口
- **首次引导** — 5步引导向导
- **数据导出/导入** — 全量 zip 导出导入、学生数据 Excel 导出导入
- **自定义主题色** — 9种预设色 + 自定义
- **URI 启动** — 支持 `classisscore://app/` 协议导航
- **IPC 通信** — 预留命名管道通信接口
- **插件接口** — 预留插件加载接口

## 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时 |
| Avalonia UI | 11.3.13 | 跨平台 UI 框架 |
| FluentAvalonia | 2.3.0+ | Fluent Design 风格 |
| CommunityToolkit.Mvvm | 8.2.1 | MVVM 框架 |
| Microsoft.Extensions.Hosting | 8.0 | 依赖注入 |
| ClosedXML | 0.104.2 | Excel 读写 |
| SharpZipLib | 1.4.2 | Zip 压缩/解压 |

## 项目结构

```
ClassIsScore/
├── App.axaml / .cs          # 应用入口
├── Program.cs                # 程序启动、DI 配置
├── MainWindow.axaml / .cs    # 主窗口、导航
├── Controls/                 # 自定义控件
│   ├── AdminVerifyDialog     # 管理员验证对话框
│   ├── StudentCardControl    # 卡片样式控件
│   ├── StudentCircleControl  # 圆形样式控件
│   └── PetDisplayControl     # 宠物模式控件
├── Models/                   # 数据模型
├── Services/                 # 业务服务
│   └── Abstractions/         # 服务接口
├── ViewModels/               # 视图模型
├── Views/                    # 视图
│   ├── FloatingWindow        # 悬浮窗
│   ├── OnboardingWindow      # 引导向导
│   └── Pages/                # 功能页面
├── Helpers/                  # 辅助工具
├── Converters/               # 值转换器
└── data/                     # 运行时数据目录
    ├── Data/                 # 数据文件（JSON）
    ├── Logs/                 # 日志
    ├── Backup/               # 结算备份
    ├── Plugins/              # 插件目录
    └── Config/               # 配置文件
```

## 构建与运行

### 环境要求
- .NET 8.0 SDK
- Windows 10+ / macOS 12+ / Linux（X11/Wayland）

### 构建
```bash
dotnet build
```

### 运行
```bash
dotnet run
```

### 发布
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## URI 协议

支持 `classisscore://` 协议启动并导航到指定页面：

| URI | 页面 |
|-----|------|
| `classisscore://app/home` | 主页 |
| `classisscore://app/students` | 学生管理 |
| `classisscore://app/scores` | 积分管理 |
| `classisscore://app/settlement` | 结算 |
| `classisscore://app/leaderboard` | 排行榜 |
| `classisscore://app/settings` | 设置 |

命令行使用：`ClassIsScore --uri classisscore://app/scores`

## 数据存储

所有数据以 JSON 格式存储在 `data/Data/` 目录下：

| 文件 | 内容 |
|------|------|
| students.json | 学生信息 |
| score_records.json | 积分记录 |
| groups.json | 小组信息 |
| evaluation_items.json | 常用评价项 |
| auto_evaluation.json | 自动评价配置 |
| admin.json | 管理员设置 |
| floating_window.json | 悬浮窗设置 |
| app_state.json | 应用状态 |
| settlement_records.json | 结算记录 |

## 许可证

MIT License
