using System;

namespace ClassIsScore.Models;

/// <summary>
/// 学生小组数据模型
/// </summary>
public class StudentGroup
{
    /// <summary>
    /// 小组唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 小组名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 小组成员学生ID列表
    /// </summary>
    public List<Guid> StudentIds { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
