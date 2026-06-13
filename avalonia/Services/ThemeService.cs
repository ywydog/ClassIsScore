using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsScore.Services.Abstractions;
using FluentAvalonia.Styling;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 主题服务实现，支持 FluentAvalonia 主题切换和自定义主题色
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private int _currentThemeMode;
    private Color? _currentPrimaryColor;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 主题更新事件
    /// </summary>
    public event EventHandler<ThemeUpdatedEventArgs>? ThemeUpdated;

    /// <summary>
    /// 当前主题模式
    /// </summary>
    public int CurrentThemeMode => _currentThemeMode;

    /// <summary>
    /// 当前实际主题（浅色/深色）
    /// </summary>
    public int CurrentRealThemeMode { get; private set; }

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="themeMode">主题模式：0=跟随系统，1=浅色，2=深色</param>
    /// <param name="primary">自定义主题色</param>
    public void SetTheme(int themeMode, Color? primary)
    {
        _currentThemeMode = themeMode;
        _currentPrimaryColor = primary;

        var app = Application.Current;
        if (app == null) return;

        // 获取 FluentAvaloniaTheme 实例
        var faTheme = app.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
        if (faTheme == null)
        {
            _logger.LogWarning("未找到 FluentAvaloniaTheme 实例");
            return;
        }

        // 设置主题模式
        app.RequestedThemeVariant = themeMode switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default // 跟随系统
        };

        // 设置自定义主题色
        faTheme.CustomAccentColor = primary;
        faTheme.PreferUserAccentColor = primary == null;
        faTheme.PreferSystemTheme = themeMode == 0;

        // 计算实际主题
        CurrentRealThemeMode = app.ActualThemeVariant == ThemeVariant.Dark ? 1 : 0;

        _logger.LogInformation("主题已切换: 模式={Mode}, 主题色={Color}",
            themeMode, primary?.ToString() ?? "默认");

        ThemeUpdated?.Invoke(this, new ThemeUpdatedEventArgs
        {
            ThemeMode = themeMode,
            PrimaryColor = primary
        });
    }
}
