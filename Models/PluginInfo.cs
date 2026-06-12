using ClassIsScore.Abstractions;

namespace ClassIsScore.Models;

/// <summary>
/// 插件信息模型
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    public PluginManifest Manifest { get; set; } = new();

    /// <summary>
    /// 插件文件夹路径
    /// </summary>
    public string PluginFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 插件是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 插件实例对象
    /// </summary>
    public PluginBase? Instance { get; set; }
}
