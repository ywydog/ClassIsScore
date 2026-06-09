using System;
using System.Threading;
using Avalonia;
using ClassIsScore.Helpers;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using ClassIsScore.ViewModels;
using ClassIsScore.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsScore;

/// <summary>
/// 程序入口
/// </summary>
public static class Program
{
    /// <summary>
    /// 应用互斥锁
    /// </summary>
    private static Mutex? _mutex;

    /// <summary>
    /// 是否是新创建的互斥锁实例
    /// </summary>
    private static bool _isMutexCreateNew;

    /// <summary>
    /// 启动时要导航到的 Uri
    /// </summary>
    public static string? StartupUri { get; private set; }

    // Avalonia 配置，请勿删除此方法
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// 程序入口方法
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // 解析命令行参数
        ParseCommandLineArgs(args);

        // 单实例检测
        _mutex = new Mutex(true, "Global\\ClassIsScore.Lock", out _isMutexCreateNew);

        if (!_isMutexCreateNew)
        {
            // 已有实例运行，尝试通过 URI 导航到已运行实例
            if (!string.IsNullOrWhiteSpace(StartupUri))
            {
                try
                {
                    // TODO: 通过 IPC 将 URI 发送给已运行实例
                    Console.WriteLine($"应用已在运行，尝试导航到: {StartupUri}");
                }
                catch
                {
                    // 忽略 IPC 通信失败
                }
            }

            Environment.Exit(0);
            return;
        }

        // 确保应用目录存在
        AppPaths.EnsureDirectoriesExist();

        // 构建 Host 并启动 Avalonia 应用
        var host = Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        // 将 Host 注入到 AppHost 服务
        AppHost.Instance = new AppHost(host);

        // 启动 Host 后台服务
        _ = host.RunAsync();

        // 启动 Avalonia
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<IAppHost>(sp => AppHost.Instance!);
        services.AddSingleton<IAppStateService, AppStateService>();
        services.AddSingleton<IUriNavigationService, UriNavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IPluginService, PluginService>();
        services.AddSingleton<IIpcService, IpcService>();
        services.AddSingleton<IAdminService, AdminService>();
        services.AddSingleton<IStudentService, StudentService>();
        services.AddSingleton<IGroupService, GroupService>();
        services.AddSingleton<IScoreService, ScoreService>();
        services.AddSingleton<ISettlementService, SettlementService>();
        services.AddSingleton<ILeaderboardService, LeaderboardService>();
        services.AddSingleton<IFloatingWindowService, FloatingWindowService>();
        services.AddSingleton<IAutoEvaluationService, AutoEvaluationService>();
        services.AddSingleton<IDataTransferService, DataTransferService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();

        // 注册 ViewModel
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<StudentManagementViewModel>();
        services.AddTransient<ScoreManagementViewModel>();
        services.AddTransient<SettlementViewModel>();
        services.AddTransient<LeaderboardViewModel>();
        services.AddTransient<AutoEvaluationViewModel>();
        services.AddTransient<AdminSettingsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<StudentProfileViewModel>();

        // 注册页面
        services.AddTransient<StudentManagementPage>();
        services.AddTransient<ScoreManagementPage>();
        services.AddTransient<SettlementPage>();
        services.AddTransient<LeaderboardPage>();
        services.AddTransient<AutoEvaluationPage>();
        services.AddTransient<AdminSettingsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AboutPage>();
        services.AddTransient<StudentProfilePage>();

        // 注册主窗口
        services.AddTransient<MainWindow>();
    }

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    private static void ParseCommandLineArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--uri" && i + 1 < args.Length)
            {
                StartupUri = args[i + 1];
                i++;
            }
        }
    }

    /// <summary>
    /// 释放应用互斥锁
    /// </summary>
    public static void ReleaseMutex()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // 忽略释放失败
        }
    }
}
