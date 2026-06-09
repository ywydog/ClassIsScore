using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 插件服务接口，管理插件的加载、卸载和生命周期
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// 获取已加载的插件数量
    /// </summary>
    int LoadedPluginCount => LoadedPlugins.Count;

    /// <summary>
    /// 获取已加载的插件列表
    /// </summary>
    IReadOnlyList<PluginInfo> LoadedPlugins { get; }

    /// <summary>
    /// 加载所有插件
    /// </summary>
    Task LoadPluginsAsync();

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    Task UnloadPluginsAsync();

    /// <summary>
    /// 插件加载完成事件
    /// </summary>
    event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
}
