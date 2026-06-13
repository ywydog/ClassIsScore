namespace ClassIsScore.Models;

/// <summary>
/// 运行时主题信息模型
/// </summary>
public class ThemeInfo
{
    /// <summary>
    /// 主题元数据
    /// </summary>
    public ThemeManifest Manifest { get; set; } = new();

    /// <summary>
    /// 主题文件夹路径（解压后的目录）
    /// </summary>
    public string ThemeFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 主题是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
