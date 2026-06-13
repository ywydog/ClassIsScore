using System;

namespace ClassIsScore.Models;

/// <summary>
/// 管理员验证事件参数
/// </summary>
public class AdminVerifiedEventArgs : EventArgs
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// 验证方式名称
    /// </summary>
    public string VerificationMethod { get; init; } = string.Empty;

    /// <summary>
    /// 验证时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
