# ClassIsScore 开发文档

## 架构概览

ClassIsScore 采用 MVVM 架构模式，基于 Microsoft.Extensions.Hosting 实现依赖注入。

### 架构分层

```
┌─────────────────────────────────────┐
│            Views (AXAML)            │  UI 层
├─────────────────────────────────────┤
│          ViewModels (C#)            │  视图模型层
├─────────────────────────────────────┤
│     Services / Abstractions (C#)    │  业务逻辑层
├─────────────────────────────────────┤
│           Models (C#)               │  数据模型层
├─────────────────────────────────────┤
│      Helpers / Converters (C#)      │  辅助工具层
└─────────────────────────────────────┘
```

### 依赖注入

所有服务在 `Program.ConfigureServices` 中注册：

- **单例服务**：`IStudentService`、`IScoreService`、`ISettlementService` 等
- **瞬态服务**：ViewModel 和 Page（每次导航创建新实例）

通过 `AppHost.Instance.GetService<T>()` 获取服务实例。

### 导航系统

主窗口使用 `NavigationView` 实现页面切换：

1. `MainWindow.NavigateToPage(tag)` 根据标签创建对应页面
2. 每个页面通过 DI 容器获取，绑定对应 ViewModel
3. URI 导航通过 `IUriNavigationService` 映射到 `NavigateToPage`

### 数据持久化

- 所有数据使用 JSON 格式存储（`System.Text.Json`）
- 数据目录：`data/Data/`
- 备份目录：`data/Backup/`（结算时 zip 备份）
- Excel 导出使用 ClosedXML
- 压缩/解压使用 SharpZipLib

## 开发指南

### 添加新功能页面

1. 在 `Models/` 创建数据模型
2. 在 `Services/Abstractions/` 定义服务接口
3. 在 `Services/` 实现服务
4. 在 `ViewModels/` 创建 ViewModel
5. 在 `Views/Pages/` 创建页面 AXAML + CS
6. 在 `Program.ConfigureServices` 注册服务和页面
7. 在 `MainWindow.axaml` 添加导航项
8. 在 `MainWindow.axaml.cs` 添加页面创建方法

### 代码规范

- 代码注释使用中文
- 服务接口以 `I` 前缀命名，放在 `Abstractions/` 目录
- ViewModel 使用 `CommunityToolkit.Mvvm` 的 `ObservableObject` 和 `RelayCommand`
- AXAML 使用 `x:DataType` 启用编译绑定
- 数据模型使用纯 C# 类，不依赖 UI 框架

### 主题与样式

- 使用 FluentAvalonia 主题
- 主题色通过 `IThemeService.SetAccentColor()` 动态修改
- 深浅模式通过 `IThemeService.SetTheme()` 切换
- 自定义样式优先使用 `DynamicResource` 引用主题资源

### 悬浮窗开发

悬浮窗是独立窗口，需注意：
- `ShowActivated = false` 不抢焦点
- `Topmost = true` 置顶显示
- 通过 `IFloatingWindowService` 管理生命周期
- 拖拽移动通过 Pointer 事件实现

### 插件开发（预留）

插件接口定义在 `IPluginService`：
- 插件信息模型：`PluginInfo`
- 插件目录：`data/Plugins/`
- 计划通过 manifest.yml 描述插件元数据

### IPC 通信（预留）

IPC 基于 `System.IO.Pipes` 命名管道：
- 管道名称：`ClassIsScore_IPC`
- 支持发送命令和接收响应
- 为与 ClassIsland 联动做准备

## 测试

```bash
# 构建验证
dotnet build

# 运行应用
dotnet run
```

## 发布

```bash
# 自包含发布（无需安装 .NET 运行时）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# 框架依赖发布（需要 .NET 8 运行时）
dotnet publish -c Release -r win-x64
```

## 常见问题

### Q: 如何添加新的导出格式？
在 `SettlementService` 或 `DataTransferService` 中添加新的导出方法，使用 ClosedXML 生成 Excel。

### Q: 如何修改数据存储位置？
修改 `Helpers/AppPaths.cs` 中的 `AppRootFolderPath` 属性。

### Q: 如何扩展管理员验证方式？
1. 在 `AdminSettings.VerificationMethod` 枚举中添加新方式
2. 在 `IAdminService` 接口添加验证方法
3. 在 `AdminService` 中实现验证逻辑
4. 在 `AdminVerifyDialog` 中添加对应的 UI
