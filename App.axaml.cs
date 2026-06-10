using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using ClassIsScore.ViewModels;
using ClassIsScore.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsScore;

/// <summary>
/// 应用主类
/// </summary>
public partial class App : Application
{
    private ILogger<App>? _logger;

    /// <summary>
    /// 初始化应用资源
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成时调用
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IControlledApplicationLifetime desktop)
        {
            desktop.Startup += OnDesktopStartup;
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 桌面应用启动时调用
    /// </summary>
    private async void OnDesktopStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        try
        {
            _logger = AppHost.Instance?.GetService<ILogger<App>>();
            _logger?.LogInformation("ClassIsScore 正在启动");

            // 初始化主题服务
            var themeService = AppHost.Instance?.GetService<IThemeService>();
            themeService?.SetTheme(0, null);

            // 创建并显示主窗口
            var mainWindow = AppHost.Instance?.GetService<MainWindow>();
            if (mainWindow != null)
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = mainWindow;
                }

                mainWindow.Show();

                // 检查是否首次启动，显示引导窗口
                var appStateService = AppHost.Instance?.GetService<IAppStateService>();
                if (appStateService != null && await appStateService.IsFirstLaunchAsync())
                {
                    _logger?.LogInformation("检测到首次启动，显示引导向导");

                    var onboardingViewModel = AppHost.Instance?.GetService<OnboardingViewModel>();
                    if (onboardingViewModel != null)
                    {
                        var onboardingWindow = new OnboardingWindow(onboardingViewModel);
                        await onboardingWindow.ShowDialog(mainWindow);
                    }
                }
            }

            // 根据设置显示悬浮窗
            var floatingWindowService = AppHost.Instance?.GetService<IFloatingWindowService>();
            if (floatingWindowService != null && floatingWindowService.Settings.IsEnabled)
            {
                floatingWindowService.Show();
            }

            // 初始化系统托盘图标
            var trayIconService = AppHost.Instance?.GetService<ITrayIconService>();
            trayIconService?.Initialize();

            // 注册 URI 路由
            RegisterUriRoutes();

            // 处理启动 URI 导航
            if (!string.IsNullOrWhiteSpace(Program.StartupUri))
            {
                try
                {
                    var uriService = AppHost.Instance?.GetService<IUriNavigationService>();
                    uriService?.Navigate(new Uri(Program.StartupUri));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "处理启动 URI 导航失败");
                }
            }

            // 启动自动评价服务
            try
            {
                var autoEvalService = AppHost.Instance?.GetService<IAutoEvaluationService>();
                if (autoEvalService != null)
                {
                    await autoEvalService.StartAsync();
                    _logger?.LogInformation("自动评价服务已启动");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动自动评价服务失败");
            }

            _logger?.LogInformation("ClassIsScore 启动完成");
        }
        catch (Exception ex)
        {
            // async void 异常会被吞掉，必须手动捕获写入崩溃日志
            Program.WriteCrashLog($"OnDesktopStartup 异常: {ex}");
        }
    }

    /// <summary>
    /// 注册 URI 路由
    /// </summary>
    private void RegisterUriRoutes()
    {
        var uriService = AppHost.Instance?.GetService<IUriNavigationService>();
        if (uriService == null) return;

        var mainWindow = AppHost.Instance?.GetService<MainWindow>();
        if (mainWindow == null) return;

        // 注册应用内部导航路径
        uriService.HandleAppNavigation("app/home", args => { mainWindow.NavigateToPage("Home"); });
        uriService.HandleAppNavigation("app/students", args => { mainWindow.NavigateToPage("StudentManagement"); });
        uriService.HandleAppNavigation("app/scores", args => { mainWindow.NavigateToPage("ScoreManagement"); });
        uriService.HandleAppNavigation("app/settlement", args => { mainWindow.NavigateToPage("Settlement"); });
        uriService.HandleAppNavigation("app/leaderboard", args => { mainWindow.NavigateToPage("Leaderboard"); });
        uriService.HandleAppNavigation("app/settings", args => { mainWindow.NavigateToPage("Settings"); });

        _logger?.LogDebug("URI 路由已注册");
    }

    /// <summary>
    /// 桌面应用退出时调用
    /// </summary>
    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _logger?.LogInformation("ClassIsScore 正在退出");

        // 停止自动评价服务
        try
        {
            var autoEvalService = AppHost.Instance?.GetService<IAutoEvaluationService>();
            autoEvalService?.StopAsync().Wait();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "停止自动评价服务失败");
        }

        // 停止 Host
        try
        {
            AppHost.Instance?.GetService<IHost>()?.StopAsync(TimeSpan.FromSeconds(5)).Wait();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "停止 Host 失败");
        }

        // 释放互斥锁
        Program.ReleaseMutex();
    }
}
