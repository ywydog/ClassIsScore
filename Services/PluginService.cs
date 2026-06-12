using System.Reflection;
using System.Text.Json;
using ClassIsScore.Abstractions;
using ClassIsScore.Attributes;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 插件服务实现，扫描、加载和管理插件
/// </summary>
public class PluginService : IPluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly List<PluginInfo> _loadedPlugins = new();
    private readonly List<PluginLoadContext> _loadContexts = new();

    public PluginService(ILogger<PluginService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取已加载的插件列表
    /// </summary>
    public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins.AsReadOnly();

#pragma warning disable CS0067
    /// <summary>
    /// 插件加载完成事件
    /// </summary>
    public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
#pragma warning restore CS0067

    /// <summary>
    /// 加载所有插件，扫描 data/Plugins/ 目录
    /// </summary>
    public async Task LoadPluginsAsync()
    {
        _logger.LogInformation("正在扫描插件目录...");

        var pluginsDir = AppPaths.PluginFolderPath;
        if (!Directory.Exists(pluginsDir))
        {
            _logger.LogInformation("插件目录不存在，跳过加载");
            return;
        }

        foreach (var dir in Directory.GetDirectories(pluginsDir))
        {
            await TryLoadPluginAsync(dir);
        }

        _logger.LogInformation("插件加载完成，共加载 {Count} 个插件", _loadedPlugins.Count);
        PluginLoaded?.Invoke(this, new PluginLoadedEventArgs { LoadedCount = _loadedPlugins.Count });
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public async Task UnloadPluginsAsync()
    {
        _logger.LogInformation("正在卸载所有插件...");

        _loadedPlugins.Clear();

        foreach (var context in _loadContexts)
        {
            try
            {
                context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "卸载插件加载上下文失败");
            }
        }
        _loadContexts.Clear();

        await Task.CompletedTask;
    }

    /// <summary>
    /// 尝试加载指定目录下的插件
    /// </summary>
    private async Task TryLoadPluginAsync(string pluginDir)
    {
        var manifestPath = Path.Combine(pluginDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning("跳过无 manifest.json 的目录: {Dir}", pluginDir);
            return;
        }

        try
        {
            // 1. 读取清单
            var json = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(json);
            if (manifest == null || string.IsNullOrEmpty(manifest.Id))
            {
                _logger.LogWarning("插件清单无效: {Path}", manifestPath);
                return;
            }

            // 2. 查找入口程序集
            var entranceAssembly = manifest.EntranceAssembly;
            if (string.IsNullOrEmpty(entranceAssembly))
            {
                _logger.LogWarning("插件清单缺少 EntranceAssembly: {Id}", manifest.Id);
                return;
            }

            var assemblyPath = Path.Combine(pluginDir, entranceAssembly);
            if (!File.Exists(assemblyPath))
            {
                _logger.LogWarning("插件入口程序集不存在: {Path}", assemblyPath);
                return;
            }

            // 3. 使用 PluginLoadContext 隔离加载
            var loadContext = new PluginLoadContext(assemblyPath);
            _loadContexts.Add(loadContext);

            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // 4. 反射查找带 [PluginEntrance] 且继承 PluginBase 的类型
            var entranceType = assembly.GetTypes()
                .FirstOrDefault(t => t.IsClass
                                     && !t.IsAbstract
                                     && t.IsSubclassOf(typeof(PluginBase))
                                     && t.GetCustomAttribute<PluginEntranceAttribute>() != null);

            if (entranceType == null)
            {
                _logger.LogWarning("未找到插件入口类（需继承 PluginBase 并标记 [PluginEntrance]）: {Assembly}", entranceAssembly);
                return;
            }

            // 5. 实例化并初始化
            var pluginInstance = (PluginBase)Activator.CreateInstance(entranceType)!;
            var pluginInfo = new PluginInfo
            {
                Manifest = manifest,
                PluginFolderPath = pluginDir,
                IsEnabled = true,
                Instance = pluginInstance
            };
            pluginInstance.Info = pluginInfo;

            _loadedPlugins.Add(pluginInfo);
            _logger.LogInformation("已加载插件: {Name} ({Id}) v{Version}", manifest.Name, manifest.Id, manifest.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载插件失败: {Dir}", pluginDir);
        }
    }

    /// <summary>
    /// 初始化所有已加载插件的 DI 注册
    /// </summary>
    /// <param name="context">Host 构建上下文</param>
    /// <param name="services">服务集合</param>
    public void InitializePlugins(HostBuilderContext context, IServiceCollection services)
    {
        foreach (var plugin in _loadedPlugins.Where(p => p.IsEnabled))
        {
            try
            {
                plugin.Instance?.Initialize(context, services);
                _logger.LogInformation("已初始化插件: {Name}", plugin.Manifest.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化插件失败: {Name}", plugin.Manifest.Name);
                plugin.IsEnabled = false;
            }
        }
    }
}
