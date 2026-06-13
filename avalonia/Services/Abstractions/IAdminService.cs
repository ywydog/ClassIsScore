using System;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 管理员验证服务接口
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// 管理员验证通过事件
    /// </summary>
    event EventHandler<AdminVerifiedEventArgs>? AdminVerified;

    /// <summary>
    /// 是否启用管理员验证
    /// </summary>
    bool IsAdminEnabled { get; }

    /// <summary>
    /// 获取当前管理员设置
    /// </summary>
    AdminSettings Settings { get; }

    /// <summary>
    /// 通用验证方法，根据当前配置的验证方式进行验证
    /// </summary>
    /// <param name="input">验证输入（密码/U盘ID/人脸数据），可为 null 表示自动检测</param>
    /// <returns>验证是否通过</returns>
    Task<bool> VerifyAsync(string? input);

    /// <summary>
    /// 密码验证
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <returns>验证是否通过</returns>
    Task<bool> VerifyPasswordAsync(string password);

    /// <summary>
    /// U盘验证，检测指定U盘是否插入
    /// </summary>
    /// <returns>验证是否通过</returns>
    Task<bool> VerifyUsbAsync();

    /// <summary>
    /// 人脸验证（预留接口）
    /// </summary>
    /// <returns>验证是否通过</returns>
    Task<bool> VerifyFaceAsync();

    /// <summary>
    /// 设置管理员密码
    /// </summary>
    /// <param name="password">新密码</param>
    Task SetPasswordAsync(string password);

    /// <summary>
    /// 设置U盘设备ID
    /// </summary>
    /// <param name="deviceId">U盘设备标识</param>
    Task SetUsbDeviceAsync(string deviceId);

    /// <summary>
    /// 更新管理员设置
    /// </summary>
    /// <param name="settings">新的管理员设置</param>
    Task UpdateSettingsAsync(AdminSettings settings);

    /// <summary>
    /// 获取当前插入的可移动磁盘列表
    /// </summary>
    /// <returns>磁盘信息列表（盘符 - 卷标）</returns>
    List<UsbDeviceInfo> GetAvailableUsbDevices();
}

/// <summary>
/// U盘设备信息
/// </summary>
public class UsbDeviceInfo
{
    /// <summary>
    /// 盘符
    /// </summary>
    public string DriveLetter { get; init; } = string.Empty;

    /// <summary>
    /// 卷标
    /// </summary>
    public string VolumeLabel { get; init; } = string.Empty;

    /// <summary>
    /// 设备标识（盘符:卷标 格式）
    /// </summary>
    public string DeviceId => $"{DriveLetter}:{VolumeLabel}";
}
