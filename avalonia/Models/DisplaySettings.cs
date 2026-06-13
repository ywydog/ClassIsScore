namespace ClassIsScore.Models;

/// <summary>
/// 显示模式枚举
/// </summary>
public enum DisplayMode
{
    /// <summary>
    /// 卡片模式
    /// </summary>
    Card,

    /// <summary>
    /// 圆形模式
    /// </summary>
    Circle,

    /// <summary>
    /// 宠物模式
    /// </summary>
    Pet
}

/// <summary>
/// 显示设置模型
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// 当前显示模式，默认为卡片模式
    /// </summary>
    public DisplayMode Mode { get; set; } = DisplayMode.Card;

    /// <summary>
    /// 自定义主题色（可选）
    /// </summary>
    public string? CustomAccentColor { get; set; }

    /// <summary>
    /// 宠物样式标识（可选）
    /// </summary>
    public string? PetStyle { get; set; }

    /// <summary>
    /// 宠物等级，默认为1
    /// </summary>
    public int PetLevel { get; set; } = 1;

    /// <summary>
    /// 宠物经验值
    /// </summary>
    public double PetExperience { get; set; }
}
