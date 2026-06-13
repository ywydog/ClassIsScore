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

/// <summary>
/// 导入列映射配置
/// </summary>
public class ImportColumnMapping
{
    /// <summary>
    /// 表格中所有列的表头名称
    /// </summary>
    public List<string> ColumnHeaders { get; set; } = new();

    /// <summary>
    /// 姓名列索引（0开始），-1表示未选择
    /// </summary>
    public int NameColumnIndex { get; set; } = -1;

    /// <summary>
    /// 学号列索引（0开始），-1表示未选择
    /// </summary>
    public int NumberColumnIndex { get; set; } = -1;

    /// <summary>
    /// 积分列索引（0开始），-1表示未选择
    /// </summary>
    public int ScoreColumnIndex { get; set; } = -1;

    /// <summary>
    /// 原因列索引（0开始），-1表示未选择
    /// </summary>
    public int ReasonColumnIndex { get; set; } = -1;

    /// <summary>
    /// 数据起始行（1开始），默认2（第1行为表头）
    /// </summary>
    public int DataStartRow { get; set; } = 2;

    /// <summary>
    /// 数据结束行（1开始），0表示到最后一行
    /// </summary>
    public int DataEndRow { get; set; } = 0;

    /// <summary>
    /// 表格预览数据（前几行原始数据，用于展示给用户选择列）
    /// </summary>
    public List<List<string>> PreviewRows { get; set; } = new();

    /// <summary>
    /// 总行数
    /// </summary>
    public int TotalRows { get; set; }
}
