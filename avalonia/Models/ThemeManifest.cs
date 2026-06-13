namespace ClassIsScore.Models;

/// <summary>
/// 主题元数据模型，对应主题包中的 manifest.json
/// </summary>
public class ThemeManifest
{
    /// <summary>
    /// 主题唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 主题显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 主题版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 主题作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 主题描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 目标应用 API 版本，用于兼容性检查
    /// </summary>
    public string TargetApiVersion { get; set; } = string.Empty;
}
