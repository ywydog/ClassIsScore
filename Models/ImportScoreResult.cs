using System.Collections.Generic;

namespace ClassIsScore.Models;

/// <summary>
/// 导入评价结果模型
/// </summary>
public class ImportScoreResult
{
    /// <summary>
    /// 成功导入的记录数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败的记录数
    /// </summary>
    public int FailCount { get; set; }

    /// <summary>
    /// 跳过的记录数（如学生未找到）
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 预览数据（导入前展示用）
    /// </summary>
    public List<ImportScoreEntry> PreviewEntries { get; set; } = new();

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => FailCount == 0 || SuccessCount > 0;
}

/// <summary>
/// 导入评价条目模型
/// </summary>
public class ImportScoreEntry
{
    /// <summary>
    /// 学生姓名
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// 学号（可选，用于精确匹配）
    /// </summary>
    public string? StudentNumber { get; set; }

    /// <summary>
    /// 积分变动值
    /// </summary>
    public double ScoreChange { get; set; }

    /// <summary>
    /// 原因（可选）
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 是否匹配到学生
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 匹配状态描述
    /// </summary>
    public string MatchStatus => IsMatched ? "已匹配" : "未找到";
}
