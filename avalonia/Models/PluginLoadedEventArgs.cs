namespace ClassIsScore.Models;

/// <summary>
/// 插件加载事件参数
/// </summary>
public class PluginLoadedEventArgs : EventArgs
{
    /// <summary>
    /// 已加载的插件信息
    /// </summary>
    public PluginInfo Plugin { get; init; } = new();

    /// <summary>
    /// 已加载的插件数量
    /// </summary>
    public int LoadedCount { get; init; }

    /// <summary>
    /// 加载时间
    /// </summary>
    public DateTime LoadedAt { get; init; } = DateTime.Now;
}
