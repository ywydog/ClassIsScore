namespace ClassIsScore.Models;

/// <summary>
/// 第三方库信息模型
/// </summary>
public class ThirdPartyLibInfo
{
    /// <summary>
    /// 库名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 许可证类型
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// 项目链接
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 库描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
