using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using ClassIsScore.Views;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 悬浮窗服务实现
/// </summary>
public class FloatingWindowService : IFloatingWindowService
{
    private readonly ILogger<FloatingWindowService> _logger;
    private readonly IScoreService _scoreService;
    private readonly string _settingsFilePath;
    private FloatingWindowSettings _settings;
    private FloatingWindow? _floatingWindow;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 悬浮窗是否可见
    /// </summary>
    public bool IsVisible => _floatingWindow?.IsVisible ?? false;

    /// <summary>
    /// 获取当前悬浮窗设置
    /// </summary>
    public FloatingWindowSettings Settings => _settings;

    public FloatingWindowService(ILogger<FloatingWindowService> logger, IScoreService scoreService)
    {
        _logger = logger;
        _scoreService = scoreService;
        _settingsFilePath = Path.Combine(AppPaths.DataFolderPath, "floating_window.json");
        _settings = LoadSettings();

        // 订阅积分变动事件，触发脉冲动画
        _scoreService.ScoreChanged += OnScoreChanged;
    }

    /// <summary>
    /// 显示悬浮窗
    /// </summary>
    public void Show()
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogInformation("悬浮窗未启用，跳过显示");
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_floatingWindow == null)
                {
                    _floatingWindow = new FloatingWindow(_settings);
                    _floatingWindow.FloatingPositionChanged += OnFloatingWindowPositionChanged;
                    _floatingWindow.FloatingClicked += OnFloatingWindowClicked;
                    _floatingWindow.Closed += OnFloatingWindowClosed;
                }

                _floatingWindow.Show();
                _logger.LogInformation("悬浮窗已显示");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示悬浮窗失败");
            }
        });
    }

    /// <summary>
    /// 隐藏悬浮窗
    /// </summary>
    public void Hide()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _floatingWindow?.Hide();
                _logger.LogInformation("悬浮窗已隐藏");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏悬浮窗失败");
            }
        });
    }

    /// <summary>
    /// 切换悬浮窗显示状态
    /// </summary>
    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// 更新悬浮窗位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public void UpdatePosition(double x, double y)
    {
        _settings.PositionX = x;
        _settings.PositionY = y;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_floatingWindow != null)
                {
                    _floatingWindow.UpdatePosition(x, y);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新悬浮窗位置失败");
            }
        });

        _ = SaveSettingsAsync();
    }

    /// <summary>
    /// 更新悬浮窗设置
    /// </summary>
    /// <param name="settings">新的悬浮窗设置</param>
    public async Task UpdateSettingsAsync(FloatingWindowSettings settings)
    {
        _settings = settings;
        await SaveSettingsAsync();
        _logger.LogInformation("悬浮窗设置已更新");

        // 如果悬浮窗正在显示，更新外观而无需重建窗口
        if (_floatingWindow != null && _floatingWindow.IsVisible)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _floatingWindow.UpdateFromSettings(_settings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新悬浮窗外观失败");
                }
            });
        }
    }

    /// <summary>
    /// 通知积分变动，触发悬浮窗脉冲动画
    /// </summary>
    public void NotifyScoreChange()
    {
        if (_floatingWindow != null && _floatingWindow.IsVisible)
        {
            _floatingWindow.NotifyScoreChange();
        }
    }

    /// <summary>
    /// 积分变动事件回调，触发脉冲动画
    /// </summary>
    private void OnScoreChanged(object? sender, ScoreChangedEventArgs e)
    {
        NotifyScoreChange();
    }

    /// <summary>
    /// 悬浮窗点击回调，打开主窗口
    /// </summary>
    private void OnFloatingWindowClicked()
    {
        try
        {
            // 激活主窗口
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        mainWindow.Show();
                        mainWindow.Activate();
                        mainWindow.Focus();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活主窗口失败");
        }
    }

    /// <summary>
    /// 悬浮窗位置变更回调
    /// </summary>
    private void OnFloatingWindowPositionChanged(double x, double y)
    {
        _settings.PositionX = x;
        _settings.PositionY = y;
        _ = SaveSettingsAsync();
    }

    /// <summary>
    /// 悬浮窗关闭回调
    /// </summary>
    private void OnFloatingWindowClosed(object? sender, EventArgs e)
    {
        if (_floatingWindow != null)
        {
            _floatingWindow.FloatingPositionChanged -= OnFloatingWindowPositionChanged;
            _floatingWindow.FloatingClicked -= OnFloatingWindowClicked;
            _floatingWindow.Closed -= OnFloatingWindowClosed;
            _floatingWindow = null;
        }
    }

    /// <summary>
    /// 从文件加载悬浮窗设置
    /// </summary>
    private FloatingWindowSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<FloatingWindowSettings>(json, JsonOptions);
                if (settings != null)
                {
                    _logger.LogInformation("悬浮窗设置已加载");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载悬浮窗设置失败，使用默认设置");
        }

        return new FloatingWindowSettings();
    }

    /// <summary>
    /// 保存悬浮窗设置到文件
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存悬浮窗设置失败");
        }
    }
}
