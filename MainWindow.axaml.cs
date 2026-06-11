using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using ClassIsScore.ViewModels;
using ClassIsScore.Views.Pages;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClassIsScore;

/// <summary>
/// 主窗口代码逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow>? _logger;
    private readonly IUriNavigationService? _uriNavigationService;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        SetupNavigationItems();
        SetupTitleBar();
    }

    public MainWindow(
        ILogger<MainWindow> logger,
        IUriNavigationService uriNavigationService)
    {
        _logger = logger;
        _uriNavigationService = uriNavigationService;

        InitializeComponent();
        SetupNavigationItems();
        SetupTitleBar();
    }

    /// <summary>
    /// 设置标题栏集成（参考ClassIsland窗口设计）
    /// 仅在Windows 11 (Build 22000+)上启用Mica沉浸式标题栏
    /// </summary>
    private void SetupTitleBar()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var version = Environment.OSVersion.Version;
                // Windows 11 = Build 22000+ (Major 10, Minor 0, Build 22000+)
                if (version.Major > 10 || (version.Major == 10 && version.Build >= 22000))
                {
                    // Windows 11：启用Mica沉浸式标题栏
                    ExtendClientAreaToDecorationsHint = true;
                    ExtendClientAreaChromeHints = Avalonia.Controls.ExtendClientAreaChromeHints.PreferSystemChrome;
                    TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent };
                    Background = null;
                }
            }
            catch
            {
                // 版本检测失败，使用默认设置
            }
        }

        SystemDecorations = SystemDecorations.Full;
    }

    /// <summary>
    /// 强制关闭窗口（不最小化到托盘）
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    /// <summary>
    /// 显示并激活窗口（从托盘恢复）
    /// 使用SetForegroundWindow替代Topmost hack，避免窗口闪烁
    /// </summary>
    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();

        // Windows平台：使用SetForegroundWindow将窗口带到前台
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var handle = TryGetPlatformHandle();
                if (handle != null)
                {
                    SetForegroundWindow(handle.Handle);
                }
            }
            catch
            {
                // 降级：使用Topmost hack
                Topmost = true;
                Topmost = false;
            }
        }
    }

    // Windows P/Invoke：将窗口设为前台窗口
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// 窗口关闭时，根据设置决定是最小化到托盘还是真正关闭
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_forceClose)
        {
            // 取消关闭，隐藏到托盘
            e.Cancel = true;
            _logger?.LogDebug("窗口关闭已取消，隐藏到系统托盘");
            Hide();
            return;
        }

        _logger?.LogInformation("主窗口正在关闭");
        base.OnClosing(e);
    }

    /// <summary>
    /// 设置导航项图标和事件
    /// </summary>
    private void SetupNavigationItems()
    {
        // 为每个导航项设置图标
        var menuItems = NavView.MenuItemsSource?.Cast<NavigationViewItem>().ToList();
        if (menuItems != null)
        {
            foreach (var item in menuItems)
            {
                if (item.Tag is string tag)
                {
                    item.IconSource = new SymbolIconSource { Symbol = GetSymbolForTag(tag) };
                }
            }
        }

        // 为底部导航项设置图标
        var footerItems = NavView.FooterMenuItemsSource?.Cast<NavigationViewItem>().ToList();
        if (footerItems != null)
        {
            foreach (var item in footerItems)
            {
                if (item.Tag is string tag)
                {
                    item.IconSource = new SymbolIconSource { Symbol = GetSymbolForTag(tag) };
                }
            }
        }

        // 注册选择变更事件
        NavView.SelectionChanged += OnNavViewSelectionChanged;
    }

    /// <summary>
    /// 导航视图加载完成时，默认选中主页
    /// </summary>
    private void NavView_Loaded(object? sender, RoutedEventArgs e)
    {
        // 默认选中主页
        var menuItems = NavView.MenuItemsSource?.Cast<object>().ToList();
        if (menuItems is { Count: > 0 })
        {
            NavView.SelectedItem = menuItems[0];
        }
    }

    /// <summary>
    /// 导航项选择变更时，切换页面内容
    /// </summary>
    private void OnNavViewSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateToPage(tag);
        }
    }

    /// <summary>
    /// 根据标签导航到对应页面
    /// </summary>
    public void NavigateToPage(string tag)
    {
        _logger?.LogDebug("导航到页面: {Tag}", tag);

        // 清空当前内容
        ContentPanel.Children.Clear();

        // 根据标签创建对应页面
        Control? pageContent = tag switch
        {
            "Home" => CreateScoreDisplayPage(),
            "ScoreDisplay" => CreateScoreDisplayPage(),
            "StudentManagement" => CreateStudentManagementPage(),
            "ScoreManagement" => CreateScoreManagementPage(),
            "Settlement" => CreateSettlementPage(),
            "Leaderboard" => CreateLeaderboardPage(),
            "Settings" => CreateSettingsPage(),
            "AdminSettings" => CreateAdminSettingsPage(),
            "AutoEvaluation" => CreateAutoEvaluationPage(),
            "About" => CreateAboutPage(),
            _ => new TextBlock
            {
                Text = GetPageTitle(tag),
                FontSize = 28,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 100, 0, 0)
            }
        };

        if (pageContent != null)
        {
            ContentPanel.Children.Add(pageContent);
        }
    }

    /// <summary>
    /// 创建学生管理页面
    /// </summary>
    private StudentManagementPage? CreateStudentManagementPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<StudentManagementPage>();
            var viewModel = appHost.GetService<StudentManagementViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建学生管理页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建积分管理页面
    /// </summary>
    private ScoreManagementPage? CreateScoreManagementPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<ScoreManagementPage>();
            var viewModel = appHost.GetService<ScoreManagementViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建积分管理页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建结算页面
    /// </summary>
    private SettlementPage? CreateSettlementPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<SettlementPage>();
            var viewModel = appHost.GetService<SettlementViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建结算页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建积分显示页面
    /// </summary>
    private ScoreDisplayPage? CreateScoreDisplayPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<ScoreDisplayPage>();
            var viewModel = appHost.GetService<ScoreDisplayViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建积分显示页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建排行榜页面
    /// </summary>
    private LeaderboardPage? CreateLeaderboardPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<LeaderboardPage>();
            var viewModel = appHost.GetService<LeaderboardViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建排行榜页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建设置页面
    /// </summary>
    private SettingsPage? CreateSettingsPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<SettingsPage>();
            var viewModel = appHost.GetService<SettingsViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;

                // 订阅设置页面的导航请求事件
                viewModel.NavigateToAdminSettingsRequested += () => NavigateToPage("AdminSettings");
                viewModel.NavigateToAutoEvaluationRequested += () => NavigateToPage("AutoEvaluation");
                viewModel.NavigateToAboutRequested += () => NavigateToPage("About");
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建设置页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建管理员设置页面
    /// </summary>
    private AdminSettingsPage? CreateAdminSettingsPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<AdminSettingsPage>();
            var viewModel = appHost.GetService<AdminSettingsViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建管理员设置页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建自动评价页面
    /// </summary>
    private AutoEvaluationPage? CreateAutoEvaluationPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<AutoEvaluationPage>();
            var viewModel = appHost.GetService<AutoEvaluationViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建自动评价页面失败");
            return null;
        }
    }

    /// <summary>
    /// 创建关于页面
    /// </summary>
    private AboutPage? CreateAboutPage()
    {
        try
        {
            var appHost = AppHost.Instance;
            if (appHost == null) return null;

            var page = appHost.GetService<AboutPage>();
            var viewModel = appHost.GetService<AboutViewModel>();

            if (page != null && viewModel != null)
            {
                page.DataContext = viewModel;
            }

            return page;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建关于页面失败");
            return null;
        }
    }

    /// <summary>
    /// 根据标签获取对应的图标符号
    /// </summary>
    private static Symbol GetSymbolForTag(string tag) => tag switch
    {
        "Home" => Symbol.Home,
        "ScoreDisplay" => Symbol.View,
        "StudentManagement" => Symbol.People,
        "ScoreManagement" => Symbol.Star,
        "Settlement" => Symbol.Calculator,
        "Leaderboard" => Symbol.SolidStar,
        "History" => Symbol.Clock,
        "Settings" => Symbol.Settings,
        "About" => Symbol.Help,
        _ => Symbol.Help
    };

    /// <summary>
    /// 获取页面标题
    /// </summary>
    private static string GetPageTitle(string tag) => tag switch
    {
        "Home" => "主页",
        "ScoreDisplay" => "积分显示",
        "StudentManagement" => "学生管理",
        "ScoreManagement" => "积分管理",
        "Settlement" => "结算",
        "Leaderboard" => "排行榜",
        "History" => "历史记录",
        "Settings" => "设置",
        "About" => "关于",
        _ => tag
    };
}
