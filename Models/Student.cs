using System;

namespace ClassIsScore.Models;

/// <summary>
/// 学生数据模型
/// </summary>
public class Student
{
    /// <summary>
    /// 学生唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 姓名（必填）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 学号（可选）
    /// </summary>
    public string? StudentNumber { get; set; }

    /// <summary>
    /// 性别（可选）
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 头像路径（可选）
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 当前积分，默认为 0
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 所属小组ID（可选）
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
