using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 系统托盘图标服务实现
/// </summary>
public class TrayIconService : ITrayIconService
{
    private readonly ILogger<TrayIconService> _logger;
    private readonly IFloatingWindowService _floatingWindowService;
    private TrayIcon? _trayIcon;

    /// <summary>
    /// 托盘图标是否可见
    /// </summary>
    public bool IsVisible => _trayIcon != null;

    public TrayIconService(
        ILogger<TrayIconService> logger,
        IFloatingWindowService floatingWindowService)
    {
        _logger = logger;
        _floatingWindowService = floatingWindowService;
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    public void Initialize()
    {
        try
        {
            _trayIcon = new TrayIcon();

            // 尝试加载应用图标
            try
            {
                var iconUri = new Uri("avares://ClassIsScore/Assets/AppLogo.ico");
                _trayIcon.Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(iconUri));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载托盘图标失败，使用默认图标");
            }

            // 获取版本号
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "未知";

            _trayIcon.ToolTipText = "ClassIsScore - 班级积分管理系统";
            _trayIcon.Menu = CreateMenu(version);
            _trayIcon.Clicked += OnTrayIconClicked;

            _logger.LogInformation("系统托盘图标已初始化");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化系统托盘图标失败");
        }
    }

    /// <summary>
    /// 显示气泡通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    public void ShowNotification(string title, string message)
    {
        // Avalonia 11 的 TrayIcon 不原生支持气泡通知
        // 可通过平台特定 API 实现，此处仅记录日志
        _logger.LogInformation("通知 - {Title}: {Message}", title, message);
    }

    /// <summary>
    /// 创建右键菜单
    /// </summary>
    private NativeMenu CreateMenu(string version)
    {
        var menu = new NativeMenu();

        // 版本信息（禁用项）
        var versionItem = new NativeMenuItem($"ClassIsScore v{version}")
        {
            IsEnabled = false
        };
        menu.Add(versionItem);

        menu.Add(new NativeMenuItemSeparator());

        // 打开主窗口
        var openWindowItem = new NativeMenuItem("打开主窗口");
        openWindowItem.Click += OnOpenWindowClicked;
        menu.Add(openWindowItem);

        // 积分管理
        var scoreManagementItem = new NativeMenuItem("积分管理");
        scoreManagementItem.Click += OnScoreManagementClicked;
        menu.Add(scoreManagementItem);

        // 学生管理
        var studentManagementItem = new NativeMenuItem("学生管理");
        studentManagementItem.Click += OnStudentManagementClicked;
        menu.Add(studentManagementItem);

        // 排行榜
        var leaderboardItem = new NativeMenuItem("排行榜");
        leaderboardItem.Click += OnLeaderboardClicked;
        menu.Add(leaderboardItem);

        menu.Add(new NativeMenuItemSeparator());

        // 显示/隐藏悬浮窗
        var toggleFloatingItem = new NativeMenuItem("显示/隐藏悬浮窗");
        toggleFloatingItem.Click += OnToggleFloatingWindowClicked;
        menu.Add(toggleFloatingItem);

        menu.Add(new NativeMenuItemSeparator());

        // 退出
        var exitItem = new NativeMenuItem("退出");
        exitItem.Click += OnExitClicked;
        menu.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// 点击托盘图标时显示主窗口
    /// </summary>
    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    /// <summary>
    /// 打开主窗口菜单项点击
    /// </summary>
    private void OnOpenWindowClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    /// <summary>
    /// 积分管理菜单项点击
    /// </summary>
    private void OnScoreManagementClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
        NavigateToPage("ScoreManagement");
    }

    /// <summary>
    /// 学生管理菜单项点击
    /// </summary>
    private void OnStudentManagementClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
        NavigateToPage("StudentManagement");
    }

    /// <summary>
    /// 排行榜菜单项点击
    /// </summary>
    private void OnLeaderboardClicked(object? sender, EventArgs e)
    {
        ShowMainWindow();
        NavigateToPage("Leaderboard");
    }

    /// <summary>
    /// 切换悬浮窗菜单项点击
    /// </summary>
    private void OnToggleFloatingWindowClicked(object? sender, EventArgs e)
    {
        _floatingWindowService.Toggle();
    }

    /// <summary>
    /// 退出菜单项点击
    /// </summary>
    private void OnExitClicked(object? sender, EventArgs e)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow != null)
        {
            mainWindow.ForceClose();
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>
    /// 显示并激活主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow != null)
        {
            mainWindow.ShowAndActivate();
        }
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    private void NavigateToPage(string tag)
    {
        var mainWindow = GetMainWindow();
        mainWindow?.NavigateToPage(tag);
    }

    /// <summary>
    /// 获取主窗口实例
    /// </summary>
    private MainWindow? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow as MainWindow;
        }
        return null;
    }
}
