using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 管理员验证服务实现
/// </summary>
public class AdminService : IAdminService
{
    private readonly ILogger<AdminService> _logger;
    private readonly string _settingsFilePath;
    private AdminSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 管理员验证通过事件
    /// </summary>
    public event EventHandler<AdminVerifiedEventArgs>? AdminVerified;

    /// <summary>
    /// 是否启用管理员验证
    /// </summary>
    public bool IsAdminEnabled => _settings.IsEnabled;

    /// <summary>
    /// 获取当前管理员设置
    /// </summary>
    public AdminSettings Settings => _settings;

    public AdminService(ILogger<AdminService> logger)
    {
        _logger = logger;
        _settingsFilePath = Path.Combine(AppPaths.DataFolderPath, "admin.json");
        _settings = LoadSettings();
    }

    /// <summary>
    /// 通用验证方法，根据当前配置的验证方式进行验证
    /// </summary>
    /// <param name="input">验证输入（密码/U盘ID/人脸数据），可为 null 表示自动检测</param>
    /// <returns>验证是否通过</returns>
    public async Task<bool> VerifyAsync(string? input)
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogInformation("管理员验证未启用，直接通过");
            return true;
        }

        bool result = _settings.VerificationMethod switch
        {
            VerificationMethod.Password => await VerifyPasswordAsync(input ?? string.Empty),
            VerificationMethod.Usb => await VerifyUsbAsync(),
            VerificationMethod.Face => await VerifyFaceAsync(),
            _ => false
        };

        OnAdminVerified(result, _settings.VerificationMethod.ToString());
        return result;
    }

    /// <summary>
    /// 密码验证
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <returns>验证是否通过</returns>
    public Task<bool> VerifyPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(_settings.PasswordHash))
        {
            _logger.LogWarning("管理员密码未设置，验证失败");
            return Task.FromResult(false);
        }

        var inputHash = ComputeSha256Hash(password);
        var result = string.Equals(inputHash, _settings.PasswordHash, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("管理员密码验证: {Result}", result ? "通过" : "失败");
        return Task.FromResult(result);
    }

    /// <summary>
    /// U盘验证，检测指定U盘是否插入
    /// </summary>
    /// <returns>验证是否通过</returns>
    public Task<bool> VerifyUsbAsync()
    {
        if (string.IsNullOrEmpty(_settings.UsbDeviceId))
        {
            _logger.LogWarning("管理员U盘设备未设置，验证失败");
            return Task.FromResult(false);
        }

        // 检测当前所有可移动磁盘
        var usbDevices = GetAvailableUsbDevices();
        var result = usbDevices.Any(d => d.DeviceId == _settings.UsbDeviceId);

        _logger.LogInformation("管理员U盘验证: {Result} (目标设备: {DeviceId})",
            result ? "通过" : "失败", _settings.UsbDeviceId);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 人脸验证（预留接口）
    /// </summary>
    /// <returns>验证是否通过</returns>
    /// <exception cref="NotImplementedException">人脸验证功能尚未实现</exception>
    public Task<bool> VerifyFaceAsync()
    {
        _logger.LogWarning("人脸验证功能尚未实现");
        throw new NotImplementedException("人脸验证功能尚未实现，需要额外依赖库支持");
    }

    /// <summary>
    /// 设置管理员密码
    /// </summary>
    /// <param name="password">新密码</param>
    public async Task SetPasswordAsync(string password)
    {
        _settings.PasswordHash = ComputeSha256Hash(password);
        _settings.IsPasswordEnabled = true;
        await SaveSettingsAsync();
        _logger.LogInformation("管理员密码已更新");
    }

    /// <summary>
    /// 设置U盘设备ID
    /// </summary>
    /// <param name="deviceId">U盘设备标识</param>
    public async Task SetUsbDeviceAsync(string deviceId)
    {
        _settings.UsbDeviceId = deviceId;
        _settings.IsUsbEnabled = true;
        await SaveSettingsAsync();
        _logger.LogInformation("管理员U盘设备已设置: {DeviceId}", deviceId);
    }

    /// <summary>
    /// 更新管理员设置
    /// </summary>
    /// <param name="settings">新的管理员设置</param>
    public async Task UpdateSettingsAsync(AdminSettings settings)
    {
        _settings = settings;
        await SaveSettingsAsync();
        _logger.LogInformation("管理员设置已更新");
    }

    /// <summary>
    /// 获取当前插入的可移动磁盘列表
    /// </summary>
    /// <returns>磁盘信息列表</returns>
    public List<UsbDeviceInfo> GetAvailableUsbDevices()
    {
        var devices = new List<UsbDeviceInfo>();

        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Removable);

            foreach (var drive in drives)
            {
                devices.Add(new UsbDeviceInfo
                {
                    DriveLetter = drive.RootDirectory.FullName.TrimEnd('\\', '/'),
                    VolumeLabel = string.IsNullOrEmpty(drive.VolumeLabel) ? "可移动磁盘" : drive.VolumeLabel
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可移动磁盘列表失败");
        }

        return devices;
    }

    /// <summary>
    /// 触发管理员验证事件
    /// </summary>
    /// <param name="isVerified">是否验证通过</param>
    /// <param name="method">验证方式</param>
    protected virtual void OnAdminVerified(bool isVerified, string method)
    {
        AdminVerified?.Invoke(this, new AdminVerifiedEventArgs
        {
            IsVerified = isVerified,
            VerificationMethod = method,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 从文件加载管理员设置
    /// </summary>
    private AdminSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AdminSettings>(json, JsonOptions);
                if (settings != null)
                {
                    _logger.LogInformation("管理员设置已加载");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载管理员设置失败，使用默认设置");
        }

        return new AdminSettings();
    }

    /// <summary>
    /// 保存管理员设置到文件
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
            _logger.LogInformation("管理员设置已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存管理员设置失败");
        }
    }

    /// <summary>
    /// 计算 SHA256 哈希值
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>十六进制格式的哈希值</returns>
    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
