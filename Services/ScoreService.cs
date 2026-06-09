using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 积分服务实现，使用 JSON 文件存储积分记录和常用评价项
/// </summary>
public class ScoreService : IScoreService
{
    private readonly ILogger<ScoreService> _logger;
    private readonly IStudentService _studentService;
    private readonly IGroupService _groupService;
    private readonly IAdminService _adminService;
    private readonly string _scoreRecordsFilePath;
    private readonly string _evaluationItemsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // 用于同步访问的锁对象
    private readonly object _lock = new();

    /// <summary>
    /// 撤销免验证时间窗口（分钟）
    /// </summary>
    private const int RevertFreeMinutes = 3;

    /// <inheritdoc/>
    public event EventHandler<ScoreChangedEventArgs>? ScoreChanged;

    public ScoreService(
        ILogger<ScoreService> logger,
        IStudentService studentService,
        IGroupService groupService,
        IAdminService adminService)
    {
        _logger = logger;
        _studentService = studentService;
        _groupService = groupService;
        _adminService = adminService;
        _scoreRecordsFilePath = Path.Combine(AppPaths.DataFolderPath, "score_records.json");
        _evaluationItemsFilePath = Path.Combine(AppPaths.DataFolderPath, "evaluation_items.json");
        EnsureDataFilesExist();
    }

    /// <summary>
    /// 确保数据文件存在，不存在则创建空文件
    /// </summary>
    private void EnsureDataFilesExist()
    {
        EnsureFileExists(_scoreRecordsFilePath);
        EnsureFileExists(_evaluationItemsFilePath);
    }

    /// <summary>
    /// 确保指定文件存在
    /// </summary>
    private static void EnsureFileExists(string path)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "[]");
        }
    }

    /// <summary>
    /// 从 JSON 文件读取积分记录
    /// </summary>
    private List<ScoreRecord> ReadScoreRecords()
    {
        try
        {
            var json = File.ReadAllText(_scoreRecordsFilePath);
            return JsonSerializer.Deserialize<List<ScoreRecord>>(json, _jsonOptions) ?? new List<ScoreRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取积分记录文件失败");
            return new List<ScoreRecord>();
        }
    }

    /// <summary>
    /// 将积分记录写入 JSON 文件
    /// </summary>
    private void WriteScoreRecords(List<ScoreRecord> records)
    {
        try
        {
            var json = JsonSerializer.Serialize(records, _jsonOptions);
            File.WriteAllText(_scoreRecordsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入积分记录文件失败");
            throw;
        }
    }

    /// <summary>
    /// 从 JSON 文件读取常用评价项
    /// </summary>
    private List<EvaluationItem> ReadEvaluationItems()
    {
        try
        {
            var json = File.ReadAllText(_evaluationItemsFilePath);
            return JsonSerializer.Deserialize<List<EvaluationItem>>(json, _jsonOptions) ?? new List<EvaluationItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取常用评价项文件失败");
            return new List<EvaluationItem>();
        }
    }

    /// <summary>
    /// 将常用评价项写入 JSON 文件
    /// </summary>
    private void WriteEvaluationItems(List<EvaluationItem> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(items, _jsonOptions);
            File.WriteAllText(_evaluationItemsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入常用评价项文件失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddScoreAsync(Guid studentId, double scoreChange, string reason, string? operatorName = null)
    {
        var student = await _studentService.GetStudentByIdAsync(studentId);
        if (student == null)
        {
            throw new InvalidOperationException($"未找到ID为 {studentId} 的学生");
        }

        lock (_lock)
        {
            var records = ReadScoreRecords();
            var record = new ScoreRecord
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                StudentName = student.Name,
                ScoreChange = scoreChange,
                Reason = reason,
                Operator = operatorName,
                CreatedAt = DateTime.Now,
                IsReverted = false
            };
            records.Add(record);
            WriteScoreRecords(records);
        }

        // 更新学生积分
        student.Score += scoreChange;

        // 积分增加时，宠物经验值同步增加（扣分不影响宠物成长）
        if (scoreChange > 0)
        {
            student.PetExp += scoreChange;
        }

        await _studentService.UpdateStudentAsync(student);

        // 触发积分变动事件
        ScoreChanged?.Invoke(this, new ScoreChangedEventArgs
        {
            StudentId = studentId,
            ScoreChange = scoreChange,
            NewScore = student.Score,
            Reason = reason
        });

        _logger.LogInformation("学生 {Name} 积分变动: {Change}，原因: {Reason}，当前积分: {Score}",
            student.Name, scoreChange, reason, student.Score);
    }

    /// <inheritdoc/>
    public async Task AddScoreToGroupAsync(Guid groupId, double scoreChange, string reason, string? operatorName = null)
    {
        var groups = await _groupService.GetAllGroupsAsync();
        var group = groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null)
        {
            throw new InvalidOperationException($"未找到ID为 {groupId} 的小组");
        }

        // 逐个为小组成员加减分
        foreach (var studentId in group.StudentIds)
        {
            await AddScoreAsync(studentId, scoreChange, reason, operatorName);
        }

        _logger.LogInformation("小组 {GroupId} 整组积分变动: {Change}，原因: {Reason}，人数: {Count}",
            groupId, scoreChange, reason, group.StudentIds.Count);
    }

    /// <inheritdoc/>
    public async Task<bool> RevertScoreAsync(Guid recordId)
    {
        ScoreRecord? record;
        bool needsAdminVerify;

        // 先在锁内读取记录并判断是否需要管理员验证
        lock (_lock)
        {
            var records = ReadScoreRecords();
            record = records.FirstOrDefault(r => r.Id == recordId);

            if (record == null)
            {
                _logger.LogWarning("未找到积分记录: {RecordId}", recordId);
                return false;
            }

            if (record.IsReverted)
            {
                _logger.LogWarning("积分记录已撤销: {RecordId}", recordId);
                return false;
            }

            // 检查是否在3分钟免验证窗口内
            var timeSinceCreated = DateTime.Now - record.CreatedAt;
            needsAdminVerify = timeSinceCreated.TotalMinutes > RevertFreeMinutes;
        }

        // 超时需要管理员验证（在锁外执行异步操作）
        if (needsAdminVerify)
        {
            var verified = await _adminService.VerifyAsync(null);
            if (!verified)
            {
                _logger.LogWarning("撤销积分记录需要管理员验证，验证失败: {RecordId}", recordId);
                return false;
            }
        }

        // 验证通过后，在锁内标记为已撤销
        lock (_lock)
        {
            var records = ReadScoreRecords();
            var currentRecord = records.FirstOrDefault(r => r.Id == recordId);

            if (currentRecord == null || currentRecord.IsReverted)
            {
                return false;
            }

            currentRecord.IsReverted = true;
            WriteScoreRecords(records);
        }

        // 反向调整学生积分
        var student = await _studentService.GetStudentByIdAsync(record.StudentId);
        if (student != null)
        {
            student.Score -= record.ScoreChange;
            await _studentService.UpdateStudentAsync(student);

            // 触发积分变动事件
            ScoreChanged?.Invoke(this, new ScoreChangedEventArgs
            {
                StudentId = record.StudentId,
                ScoreChange = -record.ScoreChange,
                NewScore = student.Score,
                Reason = $"撤销: {record.Reason}"
            });
        }

        _logger.LogInformation("撤销积分记录: {RecordId}，学生: {StudentName}，变动: {Change}",
            recordId, record.StudentName, record.ScoreChange);
        return true;
    }

    /// <inheritdoc/>
    public Task<List<ScoreRecord>> GetHistoryAsync(Guid? studentId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        lock (_lock)
        {
            var records = ReadScoreRecords();

            // 按条件筛选
            if (studentId.HasValue)
            {
                records = records.Where(r => r.StudentId == studentId.Value).ToList();
            }

            if (startDate.HasValue)
            {
                records = records.Where(r => r.CreatedAt >= startDate.Value).ToList();
            }

            if (endDate.HasValue)
            {
                records = records.Where(r => r.CreatedAt <= endDate.Value).ToList();
            }

            // 按时间倒序排列
            records = records.OrderByDescending(r => r.CreatedAt).ToList();

            return Task.FromResult(records);
        }
    }

    /// <inheritdoc/>
    public Task<List<EvaluationItem>> GetEvaluationItemsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(ReadEvaluationItems());
        }
    }

    /// <inheritdoc/>
    public Task AddEvaluationItemAsync(EvaluationItem item)
    {
        lock (_lock)
        {
            var items = ReadEvaluationItems();
            item.Id = Guid.NewGuid();
            item.CreatedAt = DateTime.Now;
            items.Add(item);
            WriteEvaluationItems(items);
        }

        _logger.LogInformation("添加常用评价项: {Name}，积分: {ScoreChange}", item.Name, item.ScoreChange);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteEvaluationItemAsync(Guid id)
    {
        lock (_lock)
        {
            var items = ReadEvaluationItems();
            var removed = items.RemoveAll(i => i.Id == id);
            if (removed > 0)
            {
                WriteEvaluationItems(items);
                _logger.LogInformation("删除常用评价项: {Id}", id);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task AddScoreToMultipleStudentsAsync(List<Guid> studentIds, double scoreChange, string reason, string? operatorName = null)
    {
        foreach (var studentId in studentIds)
        {
            await AddScoreAsync(studentId, scoreChange, reason, operatorName);
        }

        _logger.LogInformation("批量积分变动: {Change}，原因: {Reason}，人数: {Count}",
            scoreChange, reason, studentIds.Count);
    }

    /// <inheritdoc/>
    public double GetStudentScore(Guid studentId)
    {
        var student = _studentService.GetStudentByIdAsync(studentId).Result;
        return student?.Score ?? 0;
    }

    /// <inheritdoc/>
    public async Task<ImportScoreResult> PreviewImportScoresAsync(string filePath)
    {
        var result = new ImportScoreResult();

        try
        {
            var entries = ReadTableFile(filePath);
            result.TotalCount = entries.Count;
            result.PreviewEntries = entries;

            var students = await _studentService.GetAllStudentsAsync();

            foreach (var entry in entries)
            {
                Student? matchedStudent = null;

                if (!string.IsNullOrWhiteSpace(entry.StudentNumber))
                {
                    matchedStudent = students.FirstOrDefault(s =>
                        s.StudentNumber?.Equals(entry.StudentNumber, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (matchedStudent == null && !string.IsNullOrWhiteSpace(entry.StudentName))
                {
                    matchedStudent = students.FirstOrDefault(s =>
                        s.Name.Equals(entry.StudentName, StringComparison.OrdinalIgnoreCase));
                }

                if (matchedStudent == null)
                {
                    entry.IsMatched = false;
                    result.SkipCount++;
                    result.Errors.Add($"未找到学生: {entry.StudentName}{(entry.StudentNumber != null ? $" (学号: {entry.StudentNumber})" : "")}");
                }
                else
                {
                    entry.IsMatched = true;
                    result.SuccessCount++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预览导入评价文件失败");
            result.Errors.Add($"文件读取失败: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ImportScoreResult> ExecuteImportScoresAsync(List<ImportScoreEntry> entries)
    {
        var result = new ImportScoreResult { TotalCount = entries.Count };

        var students = await _studentService.GetAllStudentsAsync();

        foreach (var entry in entries)
        {
            if (!entry.IsMatched)
            {
                result.SkipCount++;
                continue;
            }

            Student? matchedStudent = null;

            if (!string.IsNullOrWhiteSpace(entry.StudentNumber))
            {
                matchedStudent = students.FirstOrDefault(s =>
                    s.StudentNumber?.Equals(entry.StudentNumber, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (matchedStudent == null && !string.IsNullOrWhiteSpace(entry.StudentName))
            {
                matchedStudent = students.FirstOrDefault(s =>
                    s.Name.Equals(entry.StudentName, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedStudent == null)
            {
                result.SkipCount++;
                continue;
            }

            try
            {
                var reason = !string.IsNullOrWhiteSpace(entry.Reason) ? entry.Reason : "外部导入";
                await AddScoreAsync(matchedStudent.Id, entry.ScoreChange, reason, "导入");
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailCount++;
                result.Errors.Add($"学生 {entry.StudentName} 加分失败: {ex.Message}");
            }
        }

        _logger.LogInformation("导入评价完成: 成功 {Success}, 失败 {Fail}, 跳过 {Skip}",
            result.SuccessCount, result.FailCount, result.SkipCount);

        return result;
    }

    /// <summary>
    /// 读取表格文件，支持 xlsx/xls/csv 格式
    /// </summary>
    private List<ImportScoreEntry> ReadTableFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".csv" => ReadCsvFile(filePath),
            ".xlsx" or ".xls" => ReadExcelFile(filePath),
            _ => throw new NotSupportedException($"不支持的文件格式: {extension}，请使用 xlsx/xls/csv 文件")
        };
    }

    /// <summary>
    /// 读取 CSV 文件
    /// </summary>
    private List<ImportScoreEntry> ReadCsvFile(string filePath)
    {
        var entries = new List<ImportScoreEntry>();
        var lines = File.ReadAllLines(filePath);

        if (lines.Length == 0) return entries;

        // 解析表头，确定列索引
        var header = ParseCsvLine(lines[0]);
        var nameIdx = FindColumnIndex(header, "姓名", "学生", "名字", "Name");
        var numberIdx = FindColumnIndex(header, "学号", "编号", "StudentNumber");
        var scoreIdx = FindColumnIndex(header, "积分", "分数", "变动", "分值", "Score", "Change");
        var reasonIdx = FindColumnIndex(header, "原因", "理由", "备注", "说明", "Reason");

        if (nameIdx < 0 && scoreIdx < 0)
        {
            throw new InvalidDataException("CSV文件缺少必要列：需要至少包含\"姓名\"和\"积分\"列");
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = ParseCsvLine(line);

            var entry = new ImportScoreEntry
            {
                StudentName = nameIdx >= 0 && nameIdx < fields.Count ? fields[nameIdx].Trim() : "",
                StudentNumber = numberIdx >= 0 && numberIdx < fields.Count ? fields[numberIdx].Trim() : null,
                Reason = reasonIdx >= 0 && reasonIdx < fields.Count ? fields[reasonIdx].Trim() : null
            };

            if (scoreIdx >= 0 && scoreIdx < fields.Count && double.TryParse(fields[scoreIdx].Trim(), out var score))
            {
                entry.ScoreChange = score;
            }

            if (!string.IsNullOrWhiteSpace(entry.StudentName) || entry.ScoreChange != 0)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>
    /// 解析CSV行，支持引号包裹的字段
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    /// <summary>
    /// 读取 Excel 文件
    /// </summary>
    private List<ImportScoreEntry> ReadExcelFile(string filePath)
    {
        var entries = new List<ImportScoreEntry>();

        using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return entries;

        // 解析表头
        var headerRow = worksheet.Row(1);
        var nameCol = FindExcelColumn(headerRow, "姓名", "学生", "名字", "Name");
        var numberCol = FindExcelColumn(headerRow, "学号", "编号", "StudentNumber");
        var scoreCol = FindExcelColumn(headerRow, "积分", "分数", "变动", "分值", "Score", "Change");
        var reasonCol = FindExcelColumn(headerRow, "原因", "理由", "备注", "说明", "Reason");

        if (nameCol == null && scoreCol == null)
        {
            throw new InvalidDataException("Excel文件缺少必要列：需要至少包含\"姓名\"和\"积分\"列");
        }

        var lastRow = worksheet.LastRowUsed();
        var maxRow = lastRow != null ? lastRow.RowNumber() : 1;

        for (var row = 2; row <= maxRow; row++)
        {
            var entry = new ImportScoreEntry
            {
                StudentName = nameCol != null ? worksheet.Cell(row, nameCol.Address.ColumnNumber).GetString().Trim() : "",
                StudentNumber = numberCol != null ? worksheet.Cell(row, numberCol.Address.ColumnNumber).GetString().Trim() : null,
                Reason = reasonCol != null ? worksheet.Cell(row, reasonCol.Address.ColumnNumber).GetString().Trim() : null
            };

            if (scoreCol != null)
            {
                var scoreCell = worksheet.Cell(row, scoreCol.Address.ColumnNumber);
                entry.ScoreChange = scoreCell.TryGetValue(out double scoreVal) ? scoreVal : 0;
            }

            if (!string.IsNullOrWhiteSpace(entry.StudentName) || entry.ScoreChange != 0)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>
    /// 在表头中查找匹配的列索引
    /// </summary>
    private static int FindColumnIndex(List<string> headers, params string[] names)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            foreach (var name in names)
            {
                if (header.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// 在 Excel 表头行中查找匹配的列
    /// </summary>
    private static ClosedXML.Excel.IXLCell? FindExcelColumn(ClosedXML.Excel.IXLRow headerRow, params string[] names)
    {
        foreach (var name in names)
        {
            var cell = headerRow.CellsUsed().FirstOrDefault(c =>
                c.GetString().Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
            if (cell != null) return cell;
        }
        return null;
    }
}
