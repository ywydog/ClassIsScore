using System;

namespace ClassIsScore.Models;

/// <summary>
/// 插件元数据模型
/// </summary>
public class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntranceAssembly { get; set; } = string.Empty;
}
