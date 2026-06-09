using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 积分服务接口，提供积分的加减、撤销、历史查询及常用评价项管理功能
/// </summary>
public interface IScoreService
{
    /// <summary>
    /// 积分变动事件
    /// </summary>
    event EventHandler<ScoreChangedEventArgs>? ScoreChanged;

    /// <summary>
    /// 单个加减分
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="scoreChange">积分变动，正为加，负为减</param>
    /// <param name="reason">变动原因</param>
    /// <param name="operatorName">操作人</param>
    Task AddScoreAsync(Guid studentId, double scoreChange, string reason, string? operatorName = null);

    /// <summary>
    /// 整组加减分
    /// </summary>
    /// <param name="groupId">小组ID</param>
    /// <param name="scoreChange">积分变动，正为加，负为减</param>
    /// <param name="reason">变动原因</param>
    /// <param name="operatorName">操作人</param>
    Task AddScoreToGroupAsync(Guid groupId, double scoreChange, string reason, string? operatorName = null);

    /// <summary>
    /// 撤销积分记录，3分钟内免验证，超时需管理员验证
    /// </summary>
    /// <param name="recordId">积分记录ID</param>
    /// <returns>是否撤销成功</returns>
    Task<bool> RevertScoreAsync(Guid recordId);

    /// <summary>
    /// 获取历史记录，支持筛选
    /// </summary>
    /// <param name="studentId">学生ID（可选）</param>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    Task<List<ScoreRecord>> GetHistoryAsync(Guid? studentId = null, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取常用评价项
    /// </summary>
    Task<List<EvaluationItem>> GetEvaluationItemsAsync();

    /// <summary>
    /// 添加常用评价项
    /// </summary>
    /// <param name="item">评价项</param>
    Task AddEvaluationItemAsync(EvaluationItem item);

    /// <summary>
    /// 删除常用评价项
    /// </summary>
    /// <param name="id">评价项ID</param>
    Task DeleteEvaluationItemAsync(Guid id);

    /// <summary>
    /// 批量加减分
    /// </summary>
    /// <param name="studentIds">学生ID列表</param>
    /// <param name="scoreChange">积分变动，正为加，负为减</param>
    /// <param name="reason">变动原因</param>
    /// <param name="operatorName">操作人</param>
    Task AddScoreToMultipleStudentsAsync(List<Guid> studentIds, double scoreChange, string reason, string? operatorName = null);

    /// <summary>
    /// 预览导入评价数据（不实际写入）
    /// </summary>
    /// <param name="filePath">文件路径（支持xlsx/xls/csv）</param>
    /// <returns>导入结果（含预览数据，但不写入积分）</returns>
    Task<ImportScoreResult> PreviewImportScoresAsync(string filePath);

    /// <summary>
    /// 执行导入评价（实际写入积分记录）
    /// </summary>
    /// <param name="entries">待导入的评价条目</param>
    /// <returns>导入结果</returns>
    Task<ImportScoreResult> ExecuteImportScoresAsync(List<ImportScoreEntry> entries);

    /// <summary>
    /// 获取学生当前积分
    /// </summary>
    /// <param name="studentId">学生ID</param>
    double GetStudentScore(Guid studentId);
}
