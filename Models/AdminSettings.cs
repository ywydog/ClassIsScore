using System;

namespace ClassIsScore.Models;

/// <summary>
/// 管理员验证方式枚举
/// </summary>
public enum VerificationMethod
{
    /// <summary>
    /// 密码验证
    /// </summary>
    Password,

    /// <summary>
    /// U盘验证
    /// </summary>
    Usb,

    /// <summary>
    /// 人脸验证
    /// </summary>
    Face
}

/// <summary>
/// 管理员设置模型
/// </summary>
public class AdminSettings
{
    /// <summary>
    /// 是否启用管理员验证
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 密码哈希（SHA256）
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// U盘设备ID
    /// </summary>
    public string? UsbDeviceId { get; set; }

    /// <summary>
    /// 人脸数据路径
    /// </summary>
    public string? FaceDataPath { get; set; }

    /// <summary>
    /// 当前验证方式
    /// </summary>
    public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Password;

    /// <summary>
    /// 是否启用密码验证
    /// </summary>
    public bool IsPasswordEnabled { get; set; } = true;

    /// <summary>
    /// 是否启用U盘验证
    /// </summary>
    public bool IsUsbEnabled { get; set; }

    /// <summary>
    /// 是否启用人脸验证
    /// </summary>
    public bool IsFaceEnabled { get; set; }
}
