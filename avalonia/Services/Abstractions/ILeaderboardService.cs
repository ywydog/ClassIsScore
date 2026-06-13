using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 排行榜服务接口，提供各类排行榜数据查询与导出功能
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// 获取日排行榜（个人）
    /// </summary>
    /// <param name="date">日期</param>
    List<LeaderboardEntry> GetDailyLeaderboard(DateTime date);

    /// <summary>
    /// 获取周排行榜（个人）
    /// </summary>
    /// <param name="weekStart">周起始日期</param>
    List<LeaderboardEntry> GetWeeklyLeaderboard(DateTime weekStart);

    /// <summary>
    /// 获取月排行榜（个人）
    /// </summary>
    /// <param name="month">月份起始日期</param>
    List<LeaderboardEntry> GetMonthlyLeaderboard(DateTime month);

    /// <summary>
    /// 获取全部排行榜（个人），使用学生当前积分
    /// </summary>
    List<LeaderboardEntry> GetAllTimeLeaderboard();

    /// <summary>
    /// 获取日小组排行榜
    /// </summary>
    /// <param name="date">日期</param>
    List<LeaderboardEntry> GetDailyGroupLeaderboard(DateTime date);

    /// <summary>
    /// 获取周小组排行榜
    /// </summary>
    /// <param name="weekStart">周起始日期</param>
    List<LeaderboardEntry> GetWeeklyGroupLeaderboard(DateTime weekStart);

    /// <summary>
    /// 获取月小组排行榜
    /// </summary>
    /// <param name="month">月份起始日期</param>
    List<LeaderboardEntry> GetMonthlyGroupLeaderboard(DateTime month);

    /// <summary>
    /// 获取全部小组排行榜
    /// </summary>
    List<LeaderboardEntry> GetAllTimeGroupLeaderboard();

    /// <summary>
    /// 是否有小组（无小组时不显示小组排行榜）
    /// </summary>
    bool HasGroups();

    /// <summary>
    /// 导出排行榜到 Excel 文件
    /// </summary>
    /// <param name="entries">排行榜条目列表</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="title">排行榜标题</param>
    Task<string> ExportLeaderboardAsync(List<LeaderboardEntry> entries, string outputPath, string title);
}
