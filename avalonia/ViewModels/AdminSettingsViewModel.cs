using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 管理员设置 ViewModel
/// </summary>
public partial class AdminSettingsViewModel : ObservableObject
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminSettingsViewModel> _logger;

    /// <summary>
    /// 是否启用管理员验证
    /// </summary>
    [ObservableProperty]
    private bool _isAdminEnabled;

    /// <summary>
    /// 是否启用密码验证
    /// </summary>
    [ObservableProperty]
    private bool _isPasswordEnabled;

    /// <summary>
    /// 是否启用U盘验证
    /// </summary>
    [ObservableProperty]
    private bool _isUsbEnabled;

    /// <summary>
    /// 是否启用人脸验证
    /// </summary>
    [ObservableProperty]
    private bool _isFaceEnabled;

    /// <summary>
    /// 当前选中的验证方式
    /// </summary>
    [ObservableProperty]
    private VerificationMethod _selectedVerificationMethod;

    /// <summary>
    /// 新密码输入
    /// </summary>
    [ObservableProperty]
    private string _newPassword = string.Empty;

    /// <summary>
    /// 确认密码输入
    /// </summary>
    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    /// <summary>
    /// 当前选中的U盘设备
    /// </summary>
    [ObservableProperty]
    private UsbDeviceInfo? _selectedUsbDevice;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 是否正在处理
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// 是否已设置密码
    /// </summary>
    [ObservableProperty]
    private bool _hasPassword;

    /// <summary>
    /// 可用的U盘设备列表
    /// </summary>
    public ObservableCollection<UsbDeviceInfo> UsbDevices { get; } = [];

    /// <summary>
    /// 验证方式选项
    /// </summary>
    public ObservableCollection<VerificationMethod> VerificationMethods { get; } =
        [VerificationMethod.Password, VerificationMethod.Usb, VerificationMethod.Face];

    public AdminSettingsViewModel(IAdminService adminService, ILogger<AdminSettingsViewModel> logger)
    {
        _adminService = adminService;
        _logger = logger;
        LoadSettings();
    }

    /// <summary>
    /// 加载当前设置
    /// </summary>
    private void LoadSettings()
    {
        var settings = _adminService.Settings;
        IsAdminEnabled = settings.IsEnabled;
        IsPasswordEnabled = settings.IsPasswordEnabled;
        IsUsbEnabled = settings.IsUsbEnabled;
        IsFaceEnabled = settings.IsFaceEnabled;
        SelectedVerificationMethod = settings.VerificationMethod;
        HasPassword = !string.IsNullOrEmpty(settings.PasswordHash);
    }

    /// <summary>
    /// 保存管理员验证开关设置
    /// </summary>
    [RelayCommand]
    private async Task SaveEnabledAsync()
    {
        try
        {
            IsProcessing = true;
            var settings = _adminService.Settings;
            settings.IsEnabled = IsAdminEnabled;
            settings.IsPasswordEnabled = IsPasswordEnabled;
            settings.IsUsbEnabled = IsUsbEnabled;
            settings.IsFaceEnabled = IsFaceEnabled;
            settings.VerificationMethod = SelectedVerificationMethod;
            await _adminService.UpdateSettingsAsync(settings);
            StatusMessage = "设置已保存";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存管理员设置失败");
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 设置密码
    /// </summary>
    [RelayCommand]
    private async Task SetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            StatusMessage = "请输入密码";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "两次输入的密码不一致";
            return;
        }

        try
        {
            IsProcessing = true;
            await _adminService.SetPasswordAsync(NewPassword);
            HasPassword = true;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            StatusMessage = "密码设置成功";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置密码失败");
            StatusMessage = $"设置失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 刷新U盘设备列表
    /// </summary>
    [RelayCommand]
    private void RefreshUsbDevices()
    {
        UsbDevices.Clear();
        var devices = _adminService.GetAvailableUsbDevices();
        foreach (var device in devices)
        {
            UsbDevices.Add(device);
        }

        if (UsbDevices.Count == 0)
        {
            StatusMessage = "未检测到可移动磁盘";
        }
        else
        {
            StatusMessage = $"检测到 {UsbDevices.Count} 个可移动磁盘";
        }
    }

    /// <summary>
    /// 选择U盘设备作为验证设备
    /// </summary>
    [RelayCommand]
    private async Task SelectUsbDeviceAsync()
    {
        if (SelectedUsbDevice == null)
        {
            StatusMessage = "请先选择一个U盘设备";
            return;
        }

        try
        {
            IsProcessing = true;
            await _adminService.SetUsbDeviceAsync(SelectedUsbDevice.DeviceId);
            StatusMessage = $"U盘设备已设置: {SelectedUsbDevice.VolumeLabel}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置U盘设备失败");
            StatusMessage = $"设置失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// 验证当前U盘
    /// </summary>
    [RelayCommand]
    private async Task TestUsbVerifyAsync()
    {
        try
        {
            IsProcessing = true;
            var result = await _adminService.VerifyUsbAsync();
            StatusMessage = result ? "U盘验证通过" : "U盘验证失败，未检测到指定设备";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "U盘验证测试失败");
            StatusMessage = $"验证失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
