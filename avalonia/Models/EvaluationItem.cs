using System;

namespace ClassIsScore.Models;

/// <summary>
/// 常用评价项模型
/// </summary>
public class EvaluationItem
{
    /// <summary>
    /// 评价项唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 评价项名称，如"积极发言"、"迟到"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 积分变动值
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 是否为加分项
    /// </summary>
    public bool IsPositive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
