using Avalonia.Media;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 主题服务接口，控制应用的主题外观
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// 主题更新事件
    /// </summary>
    event EventHandler<ThemeUpdatedEventArgs>? ThemeUpdated;

    /// <summary>
    /// 当前主题模式
    /// <list type="bullet">
    ///   <item>0 - 浅色</item>
    ///   <item>1 - 深色</item>
    ///   <item>2 - 跟随系统</item>
    /// </list>
    /// </summary>
    int CurrentThemeMode { get; }

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="themeMode">主题模式：0=浅色，1=深色，2=跟随系统</param>
    /// <param name="primary">自定义主题色，为 null 则使用默认色</param>
    void SetTheme(int themeMode, Color? primary);

    /// <summary>
    /// 获取当前实际主题（浅色/深色）
    /// </summary>
    int CurrentRealThemeMode { get; }
}

/// <summary>
/// 主题更新事件参数
/// </summary>
public class ThemeUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// 主题模式
    /// </summary>
    public int ThemeMode { get; init; }

    /// <summary>
    /// 主题色
    /// </summary>
    public Color? PrimaryColor { get; init; }
}
