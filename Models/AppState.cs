using System;

namespace ClassIsScore.Models;

/// <summary>
/// 应用状态模型，存储应用的全局状态信息
/// </summary>
public class AppState
{
    /// <summary>
    /// 是否已完成引导
    /// </summary>
    public bool IsOnboardingCompleted { get; set; }

    /// <summary>
    /// 首次启动日期
    /// </summary>
    public DateTime? FirstLaunchDate { get; set; }

    /// <summary>
    /// 应用版本号
    /// </summary>
    public string? AppVersion { get; set; }
}
