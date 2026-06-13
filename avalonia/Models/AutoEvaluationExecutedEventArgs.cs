using System;

namespace ClassIsScore.Models;

/// <summary>
/// 自动评价执行事件参数
/// </summary>
public class AutoEvaluationExecutedEventArgs : EventArgs
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 受影响的学生数量
    /// </summary>
    public int AffectedCount { get; set; }

    /// <summary>
    /// 积分变动值
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.Now;
}
