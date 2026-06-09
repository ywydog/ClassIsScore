using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using ClosedXML.Excel;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 结算服务实现，提供积分结算、撤销、备份和导出功能
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly ILogger<SettlementService> _logger;
    private readonly IStudentService _studentService;
    private readonly IScoreService _scoreService;
    private readonly IAdminService _adminService;
    private readonly string _settlementRecordsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // 用于同步访问的锁对象
    private readonly object _lock = new();

    public SettlementService(
        ILogger<SettlementService> logger,
        IStudentService studentService,
        IScoreService scoreService,
        IAdminService adminService)
    {
        _logger = logger;
        _studentService = studentService;
        _scoreService = scoreService;
        _adminService = adminService;
        _settlementRecordsFilePath = Path.Combine(AppPaths.DataFolderPath, "settlement_records.json");
        EnsureDataFileExists();
    }

    /// <summary>
    /// 确保数据文件存在
    /// </summary>
    private void EnsureDataFileExists()
    {
        if (!File.Exists(_settlementRecordsFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settlementRecordsFilePath)!);
            File.WriteAllText(_settlementRecordsFilePath, "[]");
        }
    }

    /// <summary>
    /// 从 JSON 文件读取结算记录
    /// </summary>
    private List<SettlementRecord> ReadSettlementRecords()
    {
        try
        {
            var json = File.ReadAllText(_settlementRecordsFilePath);
            return JsonSerializer.Deserialize<List<SettlementRecord>>(json, _jsonOptions) ?? new List<SettlementRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取结算记录文件失败");
            return new List<SettlementRecord>();
        }
    }

    /// <summary>
    /// 将结算记录写入 JSON 文件
    /// </summary>
    private void WriteSettlementRecords(List<SettlementRecord> records)
    {
        try
        {
            var json = JsonSerializer.Serialize(records, _jsonOptions);
            File.WriteAllText(_settlementRecordsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入结算记录文件失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SettlementRecord> SettleAsync()
    {
        // 1. 获取所有学生当前积分和历史记录
        var students = await _studentService.GetAllStudentsAsync();
        var scoreRecords = await _scoreService.GetHistoryAsync();

        var totalScore = students.Sum(s => s.Score);
        var studentCount = students.Count;

        // 2. 使用 SharpZipLib 创建 zip 备份
        var backupFileName = $"settlement_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var backupFilePath = Path.Combine(AppPaths.BackupFolderPath, backupFileName);
        Directory.CreateDirectory(AppPaths.BackupFolderPath);
        CreateBackupZip(backupFilePath, students, scoreRecords);

        // 3. 清空所有学生积分（设为0）
        foreach (var student in students)
        {
            student.Score = 0;
            await _studentService.UpdateStudentAsync(student);
        }

        // 4. 清空历史记录
        ClearScoreRecords();

        // 5. 创建结算记录
        var settlementRecord = new SettlementRecord
        {
            Id = Guid.NewGuid(),
            SettledAt = DateTime.Now,
            StudentCount = studentCount,
            TotalScore = totalScore,
            BackupFilePath = backupFilePath,
            IsReverted = false
        };

        // 6. 保存结算记录
        lock (_lock)
        {
            var records = ReadSettlementRecords();
            records.Add(settlementRecord);
            WriteSettlementRecords(records);
        }

        _logger.LogInformation("执行结算：学生数 {Count}，总积分 {Total}，备份路径 {Path}",
            studentCount, totalScore, backupFilePath);

        return settlementRecord;
    }

    /// <summary>
    /// 创建备份 zip 文件，包含学生数据和积分记录
    /// </summary>
    private void CreateBackupZip(string zipPath, List<Student> students, List<ScoreRecord> scoreRecords)
    {
        using var fs = File.Create(zipPath);
        using var zipStream = new ZipOutputStream(fs);

        // 添加学生数据
        var studentsJson = JsonSerializer.Serialize(students, _jsonOptions);
        AddEntryToZip(zipStream, "students.json", studentsJson);

        // 添加积分记录
        var scoreRecordsJson = JsonSerializer.Serialize(scoreRecords, _jsonOptions);
        AddEntryToZip(zipStream, "score_records.json", scoreRecordsJson);

        zipStream.Finish();
    }

    /// <summary>
    /// 向 zip 流中添加一个文件条目
    /// </summary>
    private static void AddEntryToZip(ZipOutputStream zipStream, string entryName, string content)
    {
        var entry = new ZipEntry(entryName);
        zipStream.PutNextEntry(entry);

        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        zipStream.Write(bytes, 0, bytes.Length);
        zipStream.CloseEntry();
    }

    /// <summary>
    /// 清空积分记录文件
    /// </summary>
    private void ClearScoreRecords()
    {
        var scoreRecordsPath = Path.Combine(AppPaths.DataFolderPath, "score_records.json");
        File.WriteAllText(scoreRecordsPath, "[]");
        _logger.LogInformation("已清空积分记录");
    }

    /// <inheritdoc/>
    public async Task<bool> RevertSettlementAsync(Guid settlementId)
    {
        // 1. 管理员验证
        var verified = await _adminService.VerifyAsync(null);
        if (!verified)
        {
            _logger.LogWarning("撤销结算需要管理员验证，验证失败: {SettlementId}", settlementId);
            return false;
        }

        // 2. 获取结算记录
        SettlementRecord? record;
        lock (_lock)
        {
            var records = ReadSettlementRecords();
            record = records.FirstOrDefault(r => r.Id == settlementId);
        }

        if (record == null)
        {
            _logger.LogWarning("未找到结算记录: {SettlementId}", settlementId);
            return false;
        }

        if (record.IsReverted)
        {
            _logger.LogWarning("结算记录已撤销: {SettlementId}", settlementId);
            return false;
        }

        // 3. 从 zip 备份恢复数据
        if (!File.Exists(record.BackupFilePath))
        {
            _logger.LogError("备份文件不存在: {Path}", record.BackupFilePath);
            return false;
        }

        RestoreFromBackupZip(record.BackupFilePath);

        // 4. 标记结算记录为已撤销
        lock (_lock)
        {
            var records = ReadSettlementRecords();
            var currentRecord = records.FirstOrDefault(r => r.Id == settlementId);
            if (currentRecord != null)
            {
                currentRecord.IsReverted = true;
                WriteSettlementRecords(records);
            }
        }

        _logger.LogInformation("已撤销结算: {SettlementId}", settlementId);
        return true;
    }

    /// <summary>
    /// 从备份 zip 恢复学生数据和积分记录
    /// </summary>
    private void RestoreFromBackupZip(string zipPath)
    {
        using var fs = File.OpenRead(zipPath);
        using var zipFile = new ZipFile(fs);

        // 恢复学生数据
        var studentsEntry = zipFile.GetEntry("students.json");
        if (studentsEntry != null)
        {
            using var studentsStream = zipFile.GetInputStream(studentsEntry);
            using var reader = new StreamReader(studentsStream);
            var studentsJson = reader.ReadToEnd();
            var students = JsonSerializer.Deserialize<List<Student>>(studentsJson, _jsonOptions);
            if (students != null)
            {
                var studentsFilePath = Path.Combine(AppPaths.DataFolderPath, "students.json");
                File.WriteAllText(studentsFilePath, JsonSerializer.Serialize(students, _jsonOptions));
                _logger.LogInformation("已从备份恢复学生数据，共 {Count} 名学生", students.Count);
            }
        }

        // 恢复积分记录
        var scoreRecordsEntry = zipFile.GetEntry("score_records.json");
        if (scoreRecordsEntry != null)
        {
            using var scoreRecordsStream = zipFile.GetInputStream(scoreRecordsEntry);
            using var reader = new StreamReader(scoreRecordsStream);
            var scoreRecordsJson = reader.ReadToEnd();
            var scoreRecordsPath = Path.Combine(AppPaths.DataFolderPath, "score_records.json");
            File.WriteAllText(scoreRecordsPath, scoreRecordsJson);
            _logger.LogInformation("已从备份恢复积分记录");
        }
    }

    /// <inheritdoc/>
    public Task<List<SettlementRecord>> GetSettlementHistoryAsync()
    {
        lock (_lock)
        {
            var records = ReadSettlementRecords();
            // 按结算时间倒序排列
            records = records.OrderByDescending(r => r.SettledAt).ToList();
            return Task.FromResult(records);
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExportMonthlyAsync(DateTime month, string outputPath)
    {
        var students = await _studentService.GetAllStudentsAsync();
        var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // 获取该月所有积分记录
        var allRecords = await _scoreService.GetHistoryAsync(startDate: firstDayOfMonth, endDate: lastDayOfMonth);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{month:yyyy年M月}月度报表");

        // 表头
        worksheet.Cell(1, 1).Value = "姓名";
        for (var week = 1; week <= 5; week++)
        {
            worksheet.Cell(1, week + 1).Value = $"第{week}周";
        }
        worksheet.Cell(1, 7).Value = "总积分";

        // 设置表头样式
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // 计算每周的日期范围
        var weekRanges = new List<(DateTime Start, DateTime End)>();
        var weekStart = firstDayOfMonth;
        for (var i = 0; i < 5; i++)
        {
            var weekEnd = weekStart.AddDays(7).AddSeconds(-1);
            if (weekEnd > lastDayOfMonth) weekEnd = lastDayOfMonth;
            if (weekStart <= lastDayOfMonth)
            {
                weekRanges.Add((weekStart, weekEnd));
            }
            weekStart = weekStart.AddDays(7);
        }

        // 填充数据
        var row = 2;
        foreach (var student in students)
        {
            worksheet.Cell(row, 1).Value = student.Name;

            var totalScore = 0.0;
            for (var w = 0; w < weekRanges.Count; w++)
            {
                var weekScore = allRecords
                    .Where(r => r.StudentId == student.Id && !r.IsReverted
                                && r.CreatedAt >= weekRanges[w].Start
                                && r.CreatedAt <= weekRanges[w].End)
                    .Sum(r => r.ScoreChange);
                worksheet.Cell(row, w + 2).Value = Math.Round(weekScore, 2);
                totalScore += weekScore;
            }

            // 空白周列填0
            for (var w = weekRanges.Count; w < 5; w++)
            {
                worksheet.Cell(row, w + 2).Value = 0;
            }

            worksheet.Cell(row, 7).Value = Math.Round(totalScore, 2);
            row++;
        }

        // 调整列宽
        worksheet.Columns().AdjustToContents();

        // 确保输出目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        workbook.SaveAs(outputPath);

        _logger.LogInformation("已导出月度报表: {Path}", outputPath);
        return outputPath;
    }

    /// <inheritdoc/>
    public async Task<string> ExportWeeklyAsync(DateTime weekStart, string outputPath)
    {
        var students = await _studentService.GetAllStudentsAsync();
        var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

        // 获取该周所有积分记录
        var allRecords = await _scoreService.GetHistoryAsync(startDate: weekStart, endDate: weekEnd);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{weekStart:M月d日}周报表");

        // 表头：姓名 | 日期1 | 日期2 | ... | 日期7 | 总积分
        worksheet.Cell(1, 1).Value = "姓名";
        for (var i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            worksheet.Cell(1, i + 2).Value = day.ToString("M/d");
        }
        worksheet.Cell(1, 9).Value = "总积分";

        // 设置表头样式
        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // 填充数据
        var row = 2;
        foreach (var student in students)
        {
            worksheet.Cell(row, 1).Value = student.Name;

            var totalScore = 0.0;
            for (var i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                var nextDay = day.AddDays(1);
                var dayScore = allRecords
                    .Where(r => r.StudentId == student.Id && !r.IsReverted
                                && r.CreatedAt >= day
                                && r.CreatedAt < nextDay)
                    .Sum(r => r.ScoreChange);
                worksheet.Cell(row, i + 2).Value = Math.Round(dayScore, 2);
                totalScore += dayScore;
            }

            worksheet.Cell(row, 9).Value = Math.Round(totalScore, 2);
            row++;
        }

        // 调整列宽
        worksheet.Columns().AdjustToContents();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        workbook.SaveAs(outputPath);

        _logger.LogInformation("已导出周度报表: {Path}", outputPath);
        return outputPath;
    }

    /// <inheritdoc/>
    public async Task<string> ExportDailyAsync(DateTime date, string outputPath)
    {
        var students = await _studentService.GetAllStudentsAsync();
        var nextDay = date.AddDays(1);

        // 获取该日所有积分记录
        var allRecords = await _scoreService.GetHistoryAsync(startDate: date, endDate: nextDay);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{date:yyyy年M月d日}日报表");

        // 表头：姓名 | 积分明细 | 总积分
        worksheet.Cell(1, 1).Value = "姓名";
        worksheet.Cell(1, 2).Value = "积分明细";
        worksheet.Cell(1, 3).Value = "总积分";

        // 设置表头样式
        var headerRange = worksheet.Range(1, 1, 1, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // 填充数据
        var row = 2;
        foreach (var student in students)
        {
            worksheet.Cell(row, 1).Value = student.Name;

            var studentRecords = allRecords
                .Where(r => r.StudentId == student.Id && !r.IsReverted)
                .ToList();

            // 积分明细：原因(+分数) 格式
            var details = string.Join("；", studentRecords.Select(r =>
                $"{r.Reason}({(r.ScoreChange >= 0 ? "+" : "")}{r.ScoreChange})"));
            worksheet.Cell(row, 2).Value = details;

            var totalScore = studentRecords.Sum(r => r.ScoreChange);
            worksheet.Cell(row, 3).Value = Math.Round(totalScore, 2);
            row++;
        }

        // 调整列宽
        worksheet.Columns().AdjustToContents();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        workbook.SaveAs(outputPath);

        _logger.LogInformation("已导出日度报表: {Path}", outputPath);
        return outputPath;
    }

    /// <inheritdoc/>
    public async Task<string> ExportLeaderboardAsync(string timeRange, string outputPath)
    {
        var students = await _studentService.GetAllStudentsAsync();
        List<ScoreRecord> allRecords;

        // 根据时间范围筛选记录
        var now = DateTime.Now;
        switch (timeRange.ToLower())
        {
            case "week":
                var weekStart = now.AddDays(-(int)now.DayOfWeek + 1);
                allRecords = await _scoreService.GetHistoryAsync(startDate: weekStart);
                break;
            case "month":
                var monthStart = new DateTime(now.Year, now.Month, 1);
                allRecords = await _scoreService.GetHistoryAsync(startDate: monthStart);
                break;
            default: // "all"
                allRecords = await _scoreService.GetHistoryAsync();
                break;
        }

        using var workbook = new XLWorkbook();
        var timeRangeName = timeRange.ToLower() switch
        {
            "week" => "周排行榜",
            "month" => "月排行榜",
            _ => "总排行榜"
        };
        var worksheet = workbook.Worksheets.Add(timeRangeName);

        // 表头：排名 | 姓名 | 积分
        worksheet.Cell(1, 1).Value = "排名";
        worksheet.Cell(1, 2).Value = "姓名";
        worksheet.Cell(1, 3).Value = "积分";

        // 设置表头样式
        var headerRange = worksheet.Range(1, 1, 1, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // 计算每个学生的积分并排序
        var leaderboard = students.Select(s => new
        {
            s.Name,
            Score = allRecords.Where(r => r.StudentId == s.Id && !r.IsReverted).Sum(r => r.ScoreChange)
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        // 填充数据
        var row = 2;
        for (var i = 0; i < leaderboard.Count; i++)
        {
            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = leaderboard[i].Name;
            worksheet.Cell(row, 3).Value = Math.Round(leaderboard[i].Score, 2);
            row++;
        }

        // 调整列宽
        worksheet.Columns().AdjustToContents();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        workbook.SaveAs(outputPath);

        _logger.LogInformation("已导出排行榜数据: {Path}", outputPath);
        return outputPath;
    }
}
