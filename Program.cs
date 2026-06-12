using System;
using System.IO;
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

    /// <summary>
    /// 全局日志文件路径
    /// </summary>
    private static string? _logFilePath;

    // Avalonia 配置，请勿删除此方法
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
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

        // 初始化日志文件路径
        _logFilePath = Path.Combine(AppPaths.LogFolderPath, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        // 全局未捕获异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            // 构建 Host 并启动 Avalonia 应用
            var host = Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFile(_logFilePath, minimumLevel: Microsoft.Extensions.Logging.LogLevel.Debug);
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            // 将 Host 注入到 AppHost 服务
            AppHost.Instance = new AppHost(host);

            // 启动 Host 后台服务
            var hostTask = host.RunAsync();

            // 监控 Host 异常
            _ = hostTask.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    WriteCrashLog($"Host 异常退出: {t.Exception}");
                }
            }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

            // 启动 Avalonia
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            WriteCrashLog($"应用启动失败: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 全局未捕获异常处理
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var message = e.ExceptionObject is Exception ex
            ? $"未捕获异常 (IsTerminating={e.IsTerminating}): {ex}"
            : $"未捕获异常对象: {e.ExceptionObject}";
        WriteCrashLog(message);
    }

    /// <summary>
    /// 未观察到的 Task 异常处理
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog($"未观察到的 Task 异常: {e.Exception}");
        e.SetObserved();
    }

    /// <summary>
    /// 写入崩溃日志到文件（独立于日志系统，确保总能写入）
    /// </summary>
    internal static void WriteCrashLog(string message)
    {
        try
        {
            var logDir = AppPaths.LogFolderPath;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var crashLogPath = Path.Combine(logDir, "crash.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(crashLogPath, line);
        }
        catch
        {
            // 崩溃日志写入失败，无能为力
        }
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
        services.AddSingleton<IXamlThemeService, XamlThemeService>();

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
