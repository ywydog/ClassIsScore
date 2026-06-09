using System;
using System.Threading.Tasks;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 引导页面 ViewModel，管理引导向导的步骤导航
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly IAppStateService _appStateService;
    private readonly IThemeService _themeService;
    private readonly ILogger<OnboardingViewModel> _logger;

    /// <summary>
    /// 总步骤数
    /// </summary>
    public const int TotalSteps = 5;

    /// <summary>
    /// 当前步骤（0~4）
    /// </summary>
    [ObservableProperty]
    private int _currentStep;

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
    /// 管理员密码
    /// </summary>
    [ObservableProperty]
    private string _adminPassword = string.Empty;

    /// <summary>
    /// 确认管理员密码
    /// </summary>
    [ObservableProperty]
    private string _confirmAdminPassword = string.Empty;

    /// <summary>
    /// 是否启用管理员密码
    /// </summary>
    [ObservableProperty]
    private bool _isAdminPasswordEnabled;

    /// <summary>
    /// 密码错误提示
    /// </summary>
    [ObservableProperty]
    private string _passwordError = string.Empty;

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
        "#767676", // 灰色
    ];

    /// <summary>
    /// 引导完成事件
    /// </summary>
    public event Action? OnboardingCompleted;

    public OnboardingViewModel(
        IAppStateService appStateService,
        IThemeService themeService,
        ILogger<OnboardingViewModel> logger)
    {
        _appStateService = appStateService;
        _themeService = themeService;
        _logger = logger;
    }

    /// <summary>
    /// 是否为欢迎步骤
    /// </summary>
    public bool IsWelcomeStep => CurrentStep == 0;

    /// <summary>
    /// 是否为基本设置步骤
    /// </summary>
    public bool IsSetupStep => CurrentStep == 1;

    /// <summary>
    /// 是否为导入学生步骤
    /// </summary>
    public bool IsImportStep => CurrentStep == 2;

    /// <summary>
    /// 是否为管理员设置步骤
    /// </summary>
    public bool IsAdminStep => CurrentStep == 3;

    /// <summary>
    /// 是否为完成步骤
    /// </summary>
    public bool IsCompleteStep => CurrentStep == 4;

    /// <summary>
    /// 是否可以返回上一步
    /// </summary>
    public bool CanGoBack => CurrentStep > 0;

    /// <summary>
    /// 是否为最后一步
    /// </summary>
    public bool IsLastStep => CurrentStep == TotalSteps - 1;

    /// <summary>
    /// 步骤指示文本
    /// </summary>
    public string StepIndicator => $"步骤 {CurrentStep + 1}/{TotalSteps}";

    /// <summary>
    /// 当前步骤变更时更新相关属性
    /// </summary>
    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsWelcomeStep));
        OnPropertyChanged(nameof(IsSetupStep));
        OnPropertyChanged(nameof(IsImportStep));
        OnPropertyChanged(nameof(IsAdminStep));
        OnPropertyChanged(nameof(IsCompleteStep));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepIndicator));
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
        FinishCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 下一步命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            // 管理员设置步骤验证密码
            if (CurrentStep == 3 && IsAdminPasswordEnabled)
            {
                if (string.IsNullOrWhiteSpace(AdminPassword))
                {
                    PasswordError = "请输入管理员密码";
                    return;
                }

                if (AdminPassword != ConfirmAdminPassword)
                {
                    PasswordError = "两次输入的密码不一致";
                    return;
                }

                PasswordError = string.Empty;
            }

            CurrentStep++;
        }
    }

    /// <summary>
    /// 是否可以下一步
    /// </summary>
    private bool CanGoNext => CurrentStep < TotalSteps - 1;

    /// <summary>
    /// 上一步命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
        }
    }

    /// <summary>
    /// 跳过当前步骤命令
    /// </summary>
    [RelayCommand]
    private void Skip()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
        }
    }

    /// <summary>
    /// 完成引导命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsLastStep))]
    private async Task FinishAsync()
    {
        try
        {
            // 应用主题设置
            var themeMode = IsDarkMode ? 2 : 1;
            var color = Avalonia.Media.Color.Parse(AccentColor);
            _themeService.SetTheme(themeMode, color);

            // 设置管理员密码（如果启用）
            if (IsAdminPasswordEnabled && !string.IsNullOrWhiteSpace(AdminPassword))
            {
                var adminService = AppHost.Instance?.GetService<IAdminService>();
                if (adminService != null)
                {
                    await adminService.SetPasswordAsync(AdminPassword);
                    var settings = adminService.Settings;
                    settings.IsEnabled = true;
                    settings.IsPasswordEnabled = true;
                    settings.VerificationMethod = Models.VerificationMethod.Password;
                    await adminService.UpdateSettingsAsync(settings);
                }
            }

            // 标记引导完成
            await _appStateService.MarkOnboardingCompletedAsync();

            _logger.LogInformation("引导向导已完成");

            // 触发完成事件
            OnboardingCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成引导时出错");
        }
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换主题色失败");
        }
    }

    /// <summary>
    /// 切换深浅模式预览
    /// </summary>
    [RelayCommand]
    private void ToggleDarkModePreview()
    {
        var themeMode = IsDarkMode ? 2 : 1;
        _themeService.SetTheme(themeMode, null);
    }
}
