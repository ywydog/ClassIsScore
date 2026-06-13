using System;

namespace ClassIsScore.Models;

/// <summary>
/// 积分变动事件参数
/// </summary>
public class ScoreChangedEventArgs : EventArgs
{
    /// <summary>
    /// 学生ID
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// 积分变动值
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 变动后的新积分
    /// </summary>
    public double NewScore { get; set; }

    /// <summary>
    /// 变动原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
