namespace ClassIsScore.Models;

/// <summary>
/// 悬浮窗图标类型枚举
/// </summary>
public enum FloatingWindowIconType
{
    /// <summary>
    /// 默认图标
    /// </summary>
    Default,

    /// <summary>
    /// 自定义图标
    /// </summary>
    Custom
}

/// <summary>
/// 悬浮窗设置模型
/// </summary>
public class FloatingWindowSettings
{
    /// <summary>
    /// 是否启用悬浮窗
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 图标类型，默认为默认图标
    /// </summary>
    public FloatingWindowIconType IconType { get; set; } = FloatingWindowIconType.Default;

    /// <summary>
    /// 自定义图标路径（可选）
    /// </summary>
    public string? CustomIconPath { get; set; }

    /// <summary>
    /// 窗口X位置
    /// </summary>
    public double PositionX { get; set; }

    /// <summary>
    /// 窗口Y位置
    /// </summary>
    public double PositionY { get; set; }

    /// <summary>
    /// 是否无焦点，默认为true
    /// </summary>
    public bool IsNoFocus { get; set; } = true;

    /// <summary>
    /// 透明度，默认为1.0
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// 悬浮按钮大小，默认56，范围40-80
    /// </summary>
    public double Size { get; set; } = 56;

    /// <summary>
    /// 悬浮按钮显示文本，默认"CS"
    /// </summary>
    public string DisplayText { get; set; } = "CS";

    /// <summary>
    /// 是否显示文本标签，默认true
    /// </summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// 自定义主题色（十六进制字符串，null表示使用系统主题色）
    /// </summary>
    public string? AccentColor { get; set; }
}
