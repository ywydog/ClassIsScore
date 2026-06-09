namespace ClassIsScore.Models;

/// <summary>
/// 插件信息模型
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 插件版本号
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// 插件描述（可选）
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 插件作者（可选）
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 插件文件夹路径
    /// </summary>
    public string PluginFolderPath { get; init; } = string.Empty;
}
