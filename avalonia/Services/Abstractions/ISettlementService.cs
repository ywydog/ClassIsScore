using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 结算服务接口，提供积分结算、撤销、导出等功能
/// </summary>
public interface ISettlementService
{
    /// <summary>
    /// 执行结算：积分清空、历史清空、生成备份
    /// </summary>
    /// <returns>结算记录</returns>
    Task<SettlementRecord> SettleAsync();

    /// <summary>
    /// 撤销结算（需管理员验证）
    /// </summary>
    /// <param name="settlementId">结算记录ID</param>
    /// <returns>是否撤销成功</returns>
    Task<bool> RevertSettlementAsync(Guid settlementId);

    /// <summary>
    /// 获取结算历史
    /// </summary>
    /// <returns>结算记录列表</returns>
    Task<List<SettlementRecord>> GetSettlementHistoryAsync();

    /// <summary>
    /// 导出月度表格
    /// </summary>
    /// <param name="month">月份（取该月第一天即可）</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>导出文件路径</returns>
    Task<string> ExportMonthlyAsync(DateTime month, string outputPath);

    /// <summary>
    /// 导出周度表格
    /// </summary>
    /// <param name="weekStart">周起始日期</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>导出文件路径</returns>
    Task<string> ExportWeeklyAsync(DateTime weekStart, string outputPath);

    /// <summary>
    /// 导出日度表格
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>导出文件路径</returns>
    Task<string> ExportDailyAsync(DateTime date, string outputPath);

    /// <summary>
    /// 导出排行榜数据
    /// </summary>
    /// <param name="timeRange">时间范围（如 "week"、"month"、"all"）</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>导出文件路径</returns>
    Task<string> ExportLeaderboardAsync(string timeRange, string outputPath);
}
