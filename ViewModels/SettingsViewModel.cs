using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IFloatingWindowService _floatingWindowService;
    private readonly IDataTransferService _dataTransferService;
    private readonly ITrayIconService _trayIconService;
    private readonly IPluginService _pluginService;
    private readonly IXamlThemeService _xamlThemeService;
    private readonly ILogger<SettingsViewModel> _logger;

    /// <summary>
    /// 默认字体（HarmonyOS Sans SC）
    /// </summary>
    public static readonly FontFamily DefaultFontFamily =
        new("HarmonyOS Sans SC, Noto Sans CJK SC, Microsoft YaHei UI, SimHei, sans-serif");

    /// <summary>
    /// 系统字体列表 + 默认字体（参考ClassIsland）
    /// </summary>
    public ObservableCollection<FontFamily> FontFamilies { get; }

    /// <summary>
    /// 当前选中的字体
    /// </summary>
    [ObservableProperty]
    private FontFamily _selectedFontFamily = DefaultFontFamily;

    /// <summary>
    /// 字体预览文本
    /// </summary>
    [ObservableProperty]
    private string _fontPreviewText = "我要买一线希望\nABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789";

    partial void OnSelectedFontFamilyChanged(FontFamily value)
    {
        // TODO: 应用字体到全局（通过主题服务或资源更新）
        _logger.LogInformation("字体已切换为: {FontName}", value.Name);
    }

    /// <summary>
    /// 主题色（十六进制字符串）
    /// </summary>
    [ObservableProperty]
    private string _accentColor = "#4CC2FF";

    /// <summary>
    /// 是否为深色模式
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// 是否启用悬浮窗
    /// </summary>
    [ObservableProperty]
    private bool _isFloatingWindowEnabled;

    /// <summary>
    /// 悬浮窗样式
    /// </summary>
    [ObservableProperty]
    private FloatingWindowStyle _floatingWindowStyle = FloatingWindowStyle.Classic;

    /// <summary>
    /// 是否显示托盘图标
    /// </summary>
    [ObservableProperty]
    private bool _showTrayIcon = true;

    /// <summary>
    /// 是否关闭时最小化到托盘
    /// </summary>
    [ObservableProperty]
    private bool _closeToTray = true;

    /// <summary>
    /// 悬浮窗透明度
    /// </summary>
    [ObservableProperty]
    private double _floatingWindowOpacity = 1.0;

    /// <summary>
    /// 悬浮窗大小
    /// </summary>
    [ObservableProperty]
    private double _floatingWindowSize = 56;

    /// <summary>
    /// 悬浮窗显示文本
    /// </summary>
    [ObservableProperty]
    private string _floatingWindowDisplayText = "CS";

    /// <summary>
    /// 是否显示悬浮窗文本标签
    /// </summary>
    [ObservableProperty]
    private bool _floatingWindowShowLabel = true;

    /// <summary>
    /// 悬浮窗主题色（十六进制字符串，空表示使用系统主题色）
    /// </summary>
    [ObservableProperty]
    private string _floatingWindowAccentColor = string.Empty;

    /// <summary>
    /// 是否为经典模式（用于UI绑定）
    /// </summary>
    public bool IsClassicStyle => FloatingWindowStyle == FloatingWindowStyle.Classic;

    partial void OnFloatingWindowStyleChanged(FloatingWindowStyle value)
    {
        OnPropertyChanged(nameof(IsClassicStyle));
    }

    /// <summary>
    /// 应用版本号
    /// </summary>
    [ObservableProperty]
    private string _appVersion = string.Empty;

    /// <summary>
    /// 数据目录路径
    /// </summary>
    [ObservableProperty]
    private string _dataFolderPath = string.Empty;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 是否正在传输数据
    /// </summary>
    [ObservableProperty]
    private bool _isTransferring;

    /// <summary>
    /// 传输进度（0~100）
    /// </summary>
    [ObservableProperty]
    private double _transferProgress;

    /// <summary>
    /// 传输状态消息
    /// </summary>
    [ObservableProperty]
    private string _transferStatus = string.Empty;

    /// <summary>
    /// 已加载的插件列表
    /// </summary>
    public ObservableCollection<PluginInfo> Plugins { get; } = new();

    /// <summary>
    /// 是否有已安装的插件
    /// </summary>
    public bool HasPlugins => Plugins.Count > 0;

    /// <summary>
    /// 插件目录路径
    /// </summary>
    [ObservableProperty]
    private string _pluginFolderPath = string.Empty;

    /// <summary>
    /// 已安装的自定义主题列表
    /// </summary>
    public ObservableCollection<ThemeInfo> CustomThemes { get; } = new();

    /// <summary>
    /// 是否有自定义主题
    /// </summary>
    public bool HasCustomThemes => CustomThemes.Count > 0;

    /// <summary>
    /// 主题目录路径
    /// </summary>
    [ObservableProperty]
    private string _themesFolderPath = string.Empty;

    /// <summary>
    /// 预设主题色列表
    /// </summary>
    public string[] PresetAccentColors { get; } =
    [
        "#4CC2FF", // 默认蓝
        "#0078D4", // 微软蓝
        "#005A9E", // 深蓝
        "#744DA9", // 紫色
        "#E74856", // 红色
        "#FF8C00", // 橙色
        "#00CC6A", // 绿色
        "#E81123", // 鲜红
        "#767676", // 灰色
    ];

    /// <summary>
    /// 悬浮窗预设主题色列表
    /// </summary>
    public string[] FloatingPresetAccentColors { get; } =
    [
        "#4CC2FF", // 默认蓝
        "#0078D4", // 微软蓝
        "#744DA9", // 紫色
        "#E74856", // 红色
        "#FF8C00", // 橙色
        "#00CC6A", // 绿色
        "#767676", // 灰色
    ];

    public SettingsViewModel(
        IThemeService themeService,
        IFloatingWindowService floatingWindowService,
        IDataTransferService dataTransferService,
        ITrayIconService trayIconService,
        IPluginService pluginService,
        IXamlThemeService xamlThemeService,
        ILogger<SettingsViewModel> logger)
    {
        _themeService = themeService;
        _floatingWindowService = floatingWindowService;
        _dataTransferService = dataTransferService;
        _trayIconService = trayIconService;
        _pluginService = pluginService;
        _xamlThemeService = xamlThemeService;
        _logger = logger;

        // 初始化字体列表：系统字体 + 默认HarmonyOS Sans SC（参考ClassIsland）
        FontFamilies = new ObservableCollection<FontFamily>(
            [..FontManager.Current.SystemFonts, DefaultFontFamily]);

        // 订阅数据传输进度事件
        _dataTransferService.ProgressChanged += OnDataTransferProgressChanged;

        // 订阅插件和主题集合变更
        Plugins.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPlugins));
        CustomThemes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCustomThemes));

        LoadCurrentSettings();
    }

    /// <summary>
    /// 加载当前设置
    /// </summary>
    private void LoadCurrentSettings()
    {
        // 加载主题设置
        IsDarkMode = _themeService.CurrentRealThemeMode == 1;

        // 加载悬浮窗设置
        var floatingSettings = _floatingWindowService.Settings;
        IsFloatingWindowEnabled = floatingSettings.IsEnabled;
        FloatingWindowStyle = floatingSettings.Style;
        FloatingWindowOpacity = floatingSettings.Opacity;
        FloatingWindowSize = floatingSettings.Size;
        FloatingWindowDisplayText = floatingSettings.DisplayText;
        FloatingWindowShowLabel = floatingSettings.ShowLabel;
        FloatingWindowAccentColor = floatingSettings.AccentColor ?? string.Empty;

        // 加载版本号
        AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "未知";

        // 加载数据目录路径
        DataFolderPath = AppPaths.DataFolderPath;

        // 加载插件列表
        Plugins.Clear();
        foreach (var plugin in _pluginService.LoadedPlugins)
        {
            Plugins.Add(plugin);
        }
        PluginFolderPath = AppPaths.PluginFolderPath;

        // 加载自定义主题列表
        CustomThemes.Clear();
        foreach (var theme in _xamlThemeService.Themes)
        {
            CustomThemes.Add(theme);
        }
        ThemesFolderPath = AppPaths.ThemesFolderPath;
    }

    /// <summary>
    /// 切换主题色
    /// </summary>
    [RelayCommand]
    private void ChangeAccentColor(string colorHex)
    {
        try
        {
            AccentColor = colorHex;
            var color = Avalonia.Media.Color.Parse(colorHex);
            _themeService.SetTheme(_themeService.CurrentThemeMode, color);
            StatusMessage = $"主题色已切换为 {colorHex}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换主题色失败");
            StatusMessage = "切换主题色失败";
        }
    }

    /// <summary>
    /// 切换深色/浅色模式
    /// </summary>
    [RelayCommand]
    private void ToggleDarkMode()
    {
        try
        {
            var themeMode = IsDarkMode ? 2 : 1;
            _themeService.SetTheme(themeMode, null);
            StatusMessage = IsDarkMode ? "已切换为深色模式" : "已切换为浅色模式";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换主题模式失败");
            StatusMessage = "切换主题模式失败";
        }
    }

    /// <summary>
    /// 打开数据目录
    /// </summary>
    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            if (!Directory.Exists(DataFolderPath))
            {
                Directory.CreateDirectory(DataFolderPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = DataFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
            StatusMessage = "已打开数据目录";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开数据目录失败");
            StatusMessage = "打开数据目录失败";
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // 保存悬浮窗设置
            var floatingSettings = _floatingWindowService.Settings;
            floatingSettings.IsEnabled = IsFloatingWindowEnabled;
            floatingSettings.Style = FloatingWindowStyle;
            floatingSettings.Opacity = FloatingWindowOpacity;
            floatingSettings.Size = FloatingWindowSize;
            floatingSettings.DisplayText = FloatingWindowDisplayText;
            floatingSettings.ShowLabel = FloatingWindowShowLabel;
            floatingSettings.AccentColor = string.IsNullOrEmpty(FloatingWindowAccentColor) ? null : FloatingWindowAccentColor;
            await ((Services.FloatingWindowService)_floatingWindowService).UpdateSettingsAsync(floatingSettings);

            StatusMessage = "设置已保存";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存设置失败");
            StatusMessage = "保存设置失败";
        }
    }

    /// <summary>
    /// 导航到管理员设置页面
    /// </summary>
    public event Action? NavigateToAdminSettingsRequested;

    /// <summary>
    /// 导航到自动评价设置页面
    /// </summary>
    public event Action? NavigateToAutoEvaluationRequested;

    /// <summary>
    /// 导航到关于页面
    /// </summary>
    public event Action? NavigateToAboutRequested;

    /// <summary>
    /// 请求导航到管理员设置页面
    /// </summary>
    [RelayCommand]
    private void NavigateToAdminSettings()
    {
        NavigateToAdminSettingsRequested?.Invoke();
    }

    /// <summary>
    /// 请求导航到自动评价设置页面
    /// </summary>
    [RelayCommand]
    private void NavigateToAutoEvaluation()
    {
        NavigateToAutoEvaluationRequested?.Invoke();
    }

    /// <summary>
    /// 请求导航到关于页面
    /// </summary>
    [RelayCommand]
    private void NavigateToAbout()
    {
        NavigateToAboutRequested?.Invoke();
    }

    /// <summary>
    /// 导入主题请求事件，由 View 层处理文件选择对话框
    /// </summary>
    public event Func<Task<string?>>? ImportThemeRequested;

    /// <summary>
    /// 删除主题请求事件
    /// </summary>
    public event Func<string, Task>? DeleteThemeRequested;

    /// <summary>
    /// 导出所有数据请求事件，由 View 层处理文件选择对话框
    /// </summary>
    public event Func<Task<string?>>? ExportDataRequested;

    /// <summary>
    /// 导入数据请求事件，由 View 层处理文件选择对话框
    /// </summary>
    public event Func<Task<string?>>? ImportDataRequested;

    /// <summary>
    /// 仅导出学生数据请求事件，由 View 层处理文件选择对话框
    /// </summary>
    public event Func<Task<string?>>? ExportStudentsRequested;

    /// <summary>
    /// 仅导入学生数据请求事件，由 View 层处理文件选择对话框
    /// </summary>
    public event Func<Task<string?>>? ImportStudentsRequested;

    /// <summary>
    /// 导出所有数据命令
    /// </summary>
    [RelayCommand]
    private async Task ExportDataAsync()
    {
        if (IsTransferring) return;

        try
        {
            var outputPath = await GetExportPathAsync();
            if (string.IsNullOrEmpty(outputPath)) return;

            IsTransferring = true;
            TransferProgress = 0;
            TransferStatus = "正在导出数据...";

            var result = await _dataTransferService.ExportAllDataAsync(outputPath);
            StatusMessage = $"数据已导出到: {result}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出数据失败");
            StatusMessage = "导出数据失败";
        }
        finally
        {
            IsTransferring = false;
        }
    }

    /// <summary>
    /// 导入数据命令
    /// </summary>
    [RelayCommand]
    private async Task ImportDataAsync()
    {
        if (IsTransferring) return;

        try
        {
            var filePath = await GetImportPathAsync();
            if (string.IsNullOrEmpty(filePath)) return;

            IsTransferring = true;
            TransferProgress = 0;
            TransferStatus = "正在导入数据...";

            var success = await _dataTransferService.ImportAllDataAsync(filePath);
            StatusMessage = success ? "数据导入成功" : "数据导入失败";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入数据失败");
            StatusMessage = "导入数据失败";
        }
        finally
        {
            IsTransferring = false;
        }
    }

    /// <summary>
    /// 仅导出学生数据命令
    /// </summary>
    [RelayCommand]
    private async Task ExportStudentsAsync()
    {
        if (IsTransferring) return;

        try
        {
            var outputPath = await GetExportStudentsPathAsync();
            if (string.IsNullOrEmpty(outputPath)) return;

            IsTransferring = true;
            TransferProgress = 0;
            TransferStatus = "正在导出学生数据...";

            var result = await _dataTransferService.ExportStudentsOnlyAsync(outputPath);
            StatusMessage = $"学生数据已导出到: {result}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出学生数据失败");
            StatusMessage = "导出学生数据失败";
        }
        finally
        {
            IsTransferring = false;
        }
    }

    /// <summary>
    /// 仅导入学生数据命令
    /// </summary>
    [RelayCommand]
    private async Task ImportStudentsAsync()
    {
        if (IsTransferring) return;

        try
        {
            var filePath = await GetImportStudentsPathAsync();
            if (string.IsNullOrEmpty(filePath)) return;

            IsTransferring = true;
            TransferProgress = 0;
            TransferStatus = "正在导入学生数据...";

            var success = await _dataTransferService.ImportStudentsOnlyAsync(filePath);
            StatusMessage = success ? "学生数据导入成功" : "学生数据导入失败";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入学生数据失败");
            StatusMessage = "导入学生数据失败";
        }
        finally
        {
            IsTransferring = false;
        }
    }

    /// <summary>
    /// 数据传输进度变更回调
    /// </summary>
    private void OnDataTransferProgressChanged(object? sender, Models.DataTransferProgressEventArgs e)
    {
        TransferProgress = e.Progress;
        TransferStatus = e.StatusMessage;

        if (e.IsCompleted)
        {
            IsTransferring = false;
        }
    }

    /// <summary>
    /// 获取导出文件路径（触发 View 层的文件选择对话框）
    /// </summary>
    private async Task<string?> GetExportPathAsync()
    {
        if (ExportDataRequested != null)
        {
            return await ExportDataRequested();
        }
        return null;
    }

    /// <summary>
    /// 获取导入文件路径（触发 View 层的文件选择对话框）
    /// </summary>
    private async Task<string?> GetImportPathAsync()
    {
        if (ImportDataRequested != null)
        {
            return await ImportDataRequested();
        }
        return null;
    }

    /// <summary>
    /// 获取导出学生数据文件路径（触发 View 层的文件选择对话框）
    /// </summary>
    private async Task<string?> GetExportStudentsPathAsync()
    {
        if (ExportStudentsRequested != null)
        {
            return await ExportStudentsRequested();
        }
        return null;
    }

    /// <summary>
    /// 获取导入学生数据文件路径（触发 View 层的文件选择对话框）
    /// </summary>
    private async Task<string?> GetImportStudentsPathAsync()
    {
        if (ImportStudentsRequested != null)
        {
            return await ImportStudentsRequested();
        }
        return null;
    }

    /// <summary>
    /// 打开插件目录
    /// </summary>
    [RelayCommand]
    private void OpenPluginFolder()
    {
        try
        {
            if (!Directory.Exists(PluginFolderPath))
            {
                Directory.CreateDirectory(PluginFolderPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = PluginFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开插件目录失败");
        }
    }

    /// <summary>
    /// 打开主题目录
    /// </summary>
    [RelayCommand]
    private void OpenThemesFolder()
    {
        try
        {
            if (!Directory.Exists(ThemesFolderPath))
            {
                Directory.CreateDirectory(ThemesFolderPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = ThemesFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开主题目录失败");
        }
    }

    /// <summary>
    /// 导入自定义主题
    /// </summary>
    [RelayCommand]
    private async Task ImportThemeAsync()
    {
        try
        {
            var filePath = ImportThemeRequested != null ? await ImportThemeRequested() : null;
            if (string.IsNullOrEmpty(filePath)) return;

            await _xamlThemeService.ImportThemeAsync(filePath);

            // 刷新主题列表
            CustomThemes.Clear();
            foreach (var theme in _xamlThemeService.Themes)
            {
                CustomThemes.Add(theme);
            }

            StatusMessage = "主题导入成功";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入主题失败");
            StatusMessage = "导入主题失败";
        }
    }

    /// <summary>
    /// 删除自定义主题
    /// </summary>
    public async Task DeleteThemeAsync(string themeId)
    {
        try
        {
            await _xamlThemeService.DeleteThemeAsync(themeId);

            // 刷新主题列表
            CustomThemes.Clear();
            foreach (var theme in _xamlThemeService.Themes)
            {
                CustomThemes.Add(theme);
            }

            StatusMessage = "主题已删除";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除主题失败");
            StatusMessage = "删除主题失败";
        }
    }
}
