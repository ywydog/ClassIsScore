using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 排行榜服务实现，基于积分记录汇总排行数据
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly IScoreService _scoreService;
    private readonly IGroupService _groupService;
    private readonly IStudentService _studentService;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(
        IScoreService scoreService,
        IGroupService groupService,
        IStudentService studentService,
        ILogger<LeaderboardService> logger)
    {
        _scoreService = scoreService;
        _groupService = groupService;
        _studentService = studentService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetDailyLeaderboard(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var records = _scoreService.GetHistoryAsync(startDate: start, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        // 按学生汇总积分变动
        var grouped = records
            .GroupBy(r => new { r.StudentId, r.StudentName })
            .Select(g => new LeaderboardEntry
            {
                Name = g.Key.StudentName,
                Score = g.Sum(r => r.ScoreChange),
                IsGroup = false
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(grouped);
        return grouped;
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetWeeklyLeaderboard(DateTime weekStart)
    {
        var start = weekStart.Date;
        // 计算该周的周一
        var diff = (int)start.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        var monday = start.AddDays(-diff);
        var end = monday.AddDays(7);

        var records = _scoreService.GetHistoryAsync(startDate: monday, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        var grouped = records
            .GroupBy(r => new { r.StudentId, r.StudentName })
            .Select(g => new LeaderboardEntry
            {
                Name = g.Key.StudentName,
                Score = g.Sum(r => r.ScoreChange),
                IsGroup = false
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(grouped);
        return grouped;
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetMonthlyLeaderboard(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var records = _scoreService.GetHistoryAsync(startDate: start, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        var grouped = records
            .GroupBy(r => new { r.StudentId, r.StudentName })
            .Select(g => new LeaderboardEntry
            {
                Name = g.Key.StudentName,
                Score = g.Sum(r => r.ScoreChange),
                IsGroup = false
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(grouped);
        return grouped;
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetAllTimeLeaderboard()
    {
        var students = _studentService.GetAllStudentsAsync().Result;

        var entries = students
            .Select(s => new LeaderboardEntry
            {
                Name = s.Name,
                Score = s.Score,
                IsGroup = false
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(entries);
        return entries;
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetDailyGroupLeaderboard(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var records = _scoreService.GetHistoryAsync(startDate: start, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        return BuildGroupLeaderboard(records);
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetWeeklyGroupLeaderboard(DateTime weekStart)
    {
        var start = weekStart.Date;
        var diff = (int)start.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        var monday = start.AddDays(-diff);
        var end = monday.AddDays(7);

        var records = _scoreService.GetHistoryAsync(startDate: monday, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        return BuildGroupLeaderboard(records);
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetMonthlyGroupLeaderboard(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var records = _scoreService.GetHistoryAsync(startDate: start, endDate: end).Result
            .Where(r => !r.IsReverted)
            .ToList();

        return BuildGroupLeaderboard(records);
    }

    /// <inheritdoc/>
    public List<LeaderboardEntry> GetAllTimeGroupLeaderboard()
    {
        var groups = _groupService.GetAllGroupsAsync().Result;
        var students = _studentService.GetAllStudentsAsync().Result;

        var entries = groups
            .Select(g =>
            {
                var memberScores = students
                    .Where(s => g.StudentIds.Contains(s.Id))
                    .Sum(s => s.Score);
                return new LeaderboardEntry
                {
                    Name = g.Name,
                    Score = memberScores,
                    IsGroup = true
                };
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(entries);
        return entries;
    }

    /// <inheritdoc/>
    public bool HasGroups()
    {
        var groups = _groupService.GetAllGroupsAsync().Result;
        return groups.Count > 0;
    }

    /// <inheritdoc/>
    public async Task<string> ExportLeaderboardAsync(List<LeaderboardEntry> entries, string outputPath, string title)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(title);

                // 设置标题行
                worksheet.Cell(1, 1).Value = "排名";
                worksheet.Cell(1, 2).Value = entries.Count > 0 && entries[0].IsGroup ? "小组名" : "姓名";
                worksheet.Cell(1, 3).Value = "积分";

                // 设置标题行样式
                var headerRange = worksheet.Range(1, 1, 1, 3);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // 填充数据
                for (var i = 0; i < entries.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = entries[i].Rank;
                    worksheet.Cell(row, 2).Value = entries[i].Name;
                    worksheet.Cell(row, 3).Value = Math.Round(entries[i].Score, 2);
                }

                // 自动调整列宽
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(outputPath);
                _logger.LogInformation("排行榜已导出到: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出排行榜失败");
                throw;
            }
        });
    }

    /// <summary>
    /// 根据积分记录构建小组排行榜
    /// </summary>
    private List<LeaderboardEntry> BuildGroupLeaderboard(List<ScoreRecord> records)
    {
        var groups = _groupService.GetAllGroupsAsync().Result;
        var students = _studentService.GetAllStudentsAsync().Result;

        // 构建学生ID到小组的映射
        var studentToGroup = new Dictionary<Guid, StudentGroup>();
        foreach (var group in groups)
        {
            foreach (var studentId in group.StudentIds)
            {
                studentToGroup[studentId] = group;
            }
        }

        // 按小组汇总积分变动
        var groupScores = new Dictionary<Guid, double>();
        var groupNames = new Dictionary<Guid, string>();

        foreach (var record in records)
        {
            if (studentToGroup.TryGetValue(record.StudentId, out var group))
            {
                if (!groupScores.ContainsKey(group.Id))
                {
                    groupScores[group.Id] = 0;
                    groupNames[group.Id] = group.Name;
                }
                groupScores[group.Id] += record.ScoreChange;
            }
        }

        var entries = groupScores
            .Select(kv => new LeaderboardEntry
            {
                Name = groupNames[kv.Key],
                Score = kv.Value,
                IsGroup = true
            })
            .OrderByDescending(e => e.Score)
            .ToList();

        AssignRanks(entries);
        return entries;
    }

    /// <summary>
    /// 为排行榜条目分配排名（相同积分并列）
    /// </summary>
    private static void AssignRanks(List<LeaderboardEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0 && Math.Abs(entries[i].Score - entries[i - 1].Score) < 0.001)
            {
                entries[i].Rank = entries[i - 1].Rank;
            }
            else
            {
                entries[i].Rank = i + 1;
            }
        }
    }
}
