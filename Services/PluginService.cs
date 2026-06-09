using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 插件服务实现（预留接口，暂不实现加载逻辑）
/// </summary>
public class PluginService : IPluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly List<PluginInfo> _loadedPlugins = new();

    public PluginService(ILogger<PluginService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取已加载的插件列表
    /// </summary>
    public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// 插件加载完成事件
    /// </summary>
    public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

    /// <summary>
    /// 加载所有插件
    /// </summary>
    public async Task LoadPluginsAsync()
    {
        _logger.LogInformation("正在扫描插件目录...");
        // TODO: 实现插件加载逻辑，扫描 plugins/ 目录下的 manifest.yml
        await Task.CompletedTask;
    }

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public async Task UnloadPluginsAsync()
    {
        _logger.LogInformation("正在卸载所有插件...");
        _loadedPlugins.Clear();
        await Task.CompletedTask;
    }
}
