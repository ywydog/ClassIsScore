using System;

namespace ClassIsScore.Models;

/// <summary>
/// 结算记录模型
/// </summary>
public class SettlementRecord
{
    /// <summary>
    /// 结算记录唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 结算时间
    /// </summary>
    public DateTime SettledAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 参与结算的学生数量
    /// </summary>
    public int StudentCount { get; set; }

    /// <summary>
    /// 总积分
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 备份zip文件路径
    /// </summary>
    public string BackupFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsReverted { get; set; }
}
