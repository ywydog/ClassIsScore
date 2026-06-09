using System;

namespace ClassIsScore.Models;

/// <summary>
/// 自动评价触发类型
/// </summary>
public enum TriggerType
{
    /// <summary>
    /// 每天触发
    /// </summary>
    Daily,

    /// <summary>
    /// 每周触发
    /// </summary>
    Weekly,

    /// <summary>
    /// 每月触发
    /// </summary>
    Monthly,

    /// <summary>
    /// 结算前触发（预留接口）
    /// </summary>
    BeforeSettlement
}

/// <summary>
/// 自动评价目标类型
/// </summary>
public enum TargetType
{
    /// <summary>
    /// 所有学生
    /// </summary>
    AllStudents,

    /// <summary>
    /// 指定小组
    /// </summary>
    SpecificGroup,

    /// <summary>
    /// 指定学生
    /// </summary>
    SpecificStudent
}

/// <summary>
/// 自动评价配置模型
/// </summary>
public class AutoEvaluationConfig
{
    /// <summary>
    /// 配置唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Daily;

    /// <summary>
    /// 触发时间，如每天8:00
    /// </summary>
    public TimeSpan TriggerTime { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// 周几触发，仅 Weekly 模式有效
    /// </summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// 每月几号触发，仅 Monthly 模式有效
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// 关联的常用评价项ID（可选）
    /// </summary>
    public Guid? EvaluationItemId { get; set; }

    /// <summary>
    /// 积分变动值
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型
    /// </summary>
    public TargetType TargetType { get; set; } = TargetType.AllStudents;

    /// <summary>
    /// 目标小组ID，仅 SpecificGroup 模式有效
    /// </summary>
    public Guid? TargetGroupId { get; set; }

    /// <summary>
    /// 目标学生ID，仅 SpecificStudent 模式有效
    /// </summary>
    public Guid? TargetStudentId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
