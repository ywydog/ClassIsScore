# ClassIsScore 插件与主题系统设计文档

## 目标
为 ClassIsScore 添加支持动态加载的插件系统和基于 XAML 的自定义主题系统。设计上参考 ClassIsland，使用基于依赖注入的插件初始化方式以及打包（ZIP）格式的主题文件。

## 1. 插件系统设计

### 1.1 核心抽象
- **`PluginBase`**: 插件基类，提供 `Initialize(HostBuilderContext context, IServiceCollection services)` 抽象方法供插件覆盖。插件可在此注册自己的服务或视图。
- **`PluginEntranceAttribute`**: 用于标记插件入口类的特性，方便反射查找。
- **`PluginManifest`**: 记录插件的元数据，如 `Id`, `Name`, `Version`, `Author`, `Description`, `EntranceAssembly`。通常对应插件目录下的 `manifest.json` 文件。

### 1.2 加载机制 (`PluginLoadContext`)
- 继承 `AssemblyLoadContext` 以实现隔离加载。
- `PluginService` 扫描 `data/Plugins/` 目录，读取各个子目录下的 `manifest.json`。
- 根据 `EntranceAssembly`，使用 `PluginLoadContext` 加载 `.dll`，并通过反射查找带有 `[PluginEntrance]` 且继承 `PluginBase` 的类型。
- 实例化插件入口类，并在应用构建 Host 时（或 Host 构建阶段）调用 `Initialize` 方法将插件服务注册到应用的依赖注入容器中。

### 1.3 服务管理 (`PluginService`)
- 维护已加载插件的列表 `LoadedPlugins`。
- 维护插件的加载状态（如启用、禁用、加载失败、异常信息）。
- UI 层可通过 `IPluginService` 获取插件列表并在“设置-插件管理”中展示，支持启用/禁用。

---

## 2. 主题系统设计

### 2.1 主题文件格式 (`.cist` / `.zip`)
自定义主题打包格式，内部结构如下：
```text
theme.cist (zip压缩)
 ├── manifest.json   # 包含主题名称、ID、作者、版本、目标应用版本等
 └── Theme.axaml     # Avalonia 资源字典 (Styles/ResourceDictionary)
```

### 2.2 核心模型
- **`ThemeManifest`**: 对应 `manifest.json`。
- **`ThemeInfo`**: 运行时维护的主题对象，包含元数据、文件路径、加载状态等。

### 2.3 主题服务 (`IXamlThemeService` / `XamlThemeService`)
- 职责：管理、解压、加载和卸载自定义主题。
- 存储：在 `data/Themes/` 目录下存储解压后的主题文件。
- 加载机制：
  1. 读取所有已安装的主题元数据。
  2. 根据启用列表，使用 `AvaloniaXamlLoader.Load(uri)` 加载 `Theme.axaml`。
  3. 将加载到的 `Styles` 插入到 `Application.Current.Styles` 集合中（或者应用层级的一个特定的 `Styles` 集合中），以覆盖默认样式。
- 导入功能：允许用户选择 `.cist` 文件，服务将其解压并复制到 `data/Themes/<ThemeId>/` 目录。

### 2.4 UI 交互
- 在“设置”页中增加“自定义主题”标签页。
- 列表展示当前已安装的主题。
- 提供“导入主题”、“启用”、“禁用”和“删除”按钮。
- 选择启用时，动态刷新应用的样式树。

## 3. 依赖关系与测试
- 引入或利用原有的 `System.Text.Json` 进行元数据序列化。
- 使用原有的 `SharpZipLib` (或内置的 `System.IO.Compression.ZipFile`) 处理主题包的解压。
- 为 `PluginService` 和 `XamlThemeService` 补充相应的单元测试。