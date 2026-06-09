using System;

namespace ClassIsScore.Models;

/// <summary>
/// 数据传输进度事件参数
/// </summary>
public class DataTransferProgressEventArgs : EventArgs
{
    /// <summary>
    /// 进度百分比（0~100）
    /// </summary>
    public double Progress { get; init; }

    /// <summary>
    /// 当前状态消息
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; init; }
}
