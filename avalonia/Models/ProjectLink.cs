namespace ClassIsScore.Models;

/// <summary>
/// 项目链接模型
/// </summary>
public class ProjectLink
{
    /// <summary>
    /// 链接标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 链接地址
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Segoe MDL2 图标码
    /// </summary>
    public string IconGlyph { get; set; } = string.Empty;
}
