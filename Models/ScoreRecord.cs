using System;

namespace ClassIsScore.Models;

/// <summary>
/// 积分记录模型
/// </summary>
public class ScoreRecord
{
    /// <summary>
    /// 记录唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 学生ID
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// 学生姓名（冗余存储便于显示）
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// 积分变动，正为加，负为减
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 变动原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 操作人
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// 记录时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsReverted { get; set; }

    /// <summary>
    /// 是否在3分钟内可撤销（无需管理员验证）
    /// </summary>
    public bool CanQuickRevert => !IsReverted && (DateTime.Now - CreatedAt).TotalMinutes <= 3;

    /// <summary>
    /// 是否需要管理员验证才能撤销（已超3分钟但未撤销）
    /// </summary>
    public bool NeedsAdminRevert => !IsReverted && !CanQuickRevert;
}
