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
    public async Task<ImportColumnMapping> ReadTableHeadersAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var mapping = new ImportColumnMapping();

        try
        {
            if (extension == ".csv")
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0) return mapping;

                var header = ParseCsvLine(lines[0]);
                mapping.ColumnHeaders = header.Select(h => h.Trim()).ToList();
                mapping.TotalRows = lines.Length;

                // 自动猜测列映射
                mapping.NameColumnIndex = FindColumnIndex(header, "姓名", "学生", "名字", "Name");
                mapping.NumberColumnIndex = FindColumnIndex(header, "学号", "编号", "StudentNumber");
                mapping.ScoreColumnIndex = FindColumnIndex(header, "积分", "分数", "变动", "分值", "Score", "Change");
                mapping.ReasonColumnIndex = FindColumnIndex(header, "原因", "理由", "备注", "说明", "Reason");

                // 预览前5行数据
                var previewCount = Math.Min(5, lines.Length - 1);
                for (var i = 1; i <= previewCount; i++)
                {
                    var fields = ParseCsvLine(lines[i]);
                    mapping.PreviewRows.Add(fields);
                }
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return mapping;

                var headerRow = worksheet.Row(1);
                var usedCells = headerRow.CellsUsed().ToList();
                mapping.ColumnHeaders = usedCells.Select(c => c.GetString().Trim()).ToList();
                mapping.TotalRows = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                // 自动猜测列映射
                mapping.NameColumnIndex = FindExcelColumnIndex(headerRow, "姓名", "学生", "名字", "Name");
                mapping.NumberColumnIndex = FindExcelColumnIndex(headerRow, "学号", "编号", "StudentNumber");
                mapping.ScoreColumnIndex = FindExcelColumnIndex(headerRow, "积分", "分数", "变动", "分值", "Score", "Change");
                mapping.ReasonColumnIndex = FindExcelColumnIndex(headerRow, "原因", "理由", "备注", "说明", "Reason");

                // 预览前5行数据
                var previewCount = Math.Min(5, mapping.TotalRows - 1);
                for (var r = 2; r <= 1 + previewCount; r++)
                {
                    var row = new List<string>();
                    foreach (var cell in usedCells)
                    {
                        row.Add(worksheet.Cell(r, cell.Address.ColumnNumber).GetString().Trim());
                    }
                    mapping.PreviewRows.Add(row);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取表格文件表头失败");
        }

        return mapping;
    }

    /// <inheritdoc/>
    public async Task<ImportScoreResult> PreviewImportWithMappingAsync(string filePath, ImportColumnMapping mapping)
    {
        var result = new ImportScoreResult();

        try
        {
            var entries = ReadTableFileWithMapping(filePath, mapping);
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
    /// 根据列映射配置读取表格文件
    /// </summary>
    private List<ImportScoreEntry> ReadTableFileWithMapping(string filePath, ImportColumnMapping mapping)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".csv" => ReadCsvFileWithMapping(filePath, mapping),
            ".xlsx" or ".xls" => ReadExcelFileWithMapping(filePath, mapping),
            _ => throw new NotSupportedException($"不支持的文件格式: {extension}，请使用 xlsx/xls/csv 文件")
        };
    }

    /// <summary>
    /// 根据列映射配置读取 CSV 文件
    /// </summary>
    private List<ImportScoreEntry> ReadCsvFileWithMapping(string filePath, ImportColumnMapping mapping)
    {
        var entries = new List<ImportScoreEntry>();
        var lines = File.ReadAllLines(filePath);

        if (lines.Length == 0) return entries;

        var startRow = Math.Max(mapping.DataStartRow - 1, 1); // 转为0索引
        var endRow = mapping.DataEndRow > 0 ? Math.Min(mapping.DataEndRow, lines.Length) : lines.Length;

        for (var i = startRow; i < endRow; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = ParseCsvLine(line);

            var entry = new ImportScoreEntry
            {
                StudentName = mapping.NameColumnIndex >= 0 && mapping.NameColumnIndex < fields.Count
                    ? fields[mapping.NameColumnIndex].Trim() : "",
                StudentNumber = mapping.NumberColumnIndex >= 0 && mapping.NumberColumnIndex < fields.Count
                    ? fields[mapping.NumberColumnIndex].Trim() : null,
                Reason = mapping.ReasonColumnIndex >= 0 && mapping.ReasonColumnIndex < fields.Count
                    ? fields[mapping.ReasonColumnIndex].Trim() : null
            };

            if (mapping.ScoreColumnIndex >= 0 && mapping.ScoreColumnIndex < fields.Count
                && double.TryParse(fields[mapping.ScoreColumnIndex].Trim(), out var score))
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
    /// 根据列映射配置读取 Excel 文件
    /// </summary>
    private List<ImportScoreEntry> ReadExcelFileWithMapping(string filePath, ImportColumnMapping mapping)
    {
        var entries = new List<ImportScoreEntry>();

        using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return entries;

        var headerRow = worksheet.Row(1);
        var usedCells = headerRow.CellsUsed().ToList();

        // 将映射索引转换为Excel列号
        var nameColNum = mapping.NameColumnIndex >= 0 && mapping.NameColumnIndex < usedCells.Count
            ? usedCells[mapping.NameColumnIndex].Address.ColumnNumber : -1;
        var numberColNum = mapping.NumberColumnIndex >= 0 && mapping.NumberColumnIndex < usedCells.Count
            ? usedCells[mapping.NumberColumnIndex].Address.ColumnNumber : -1;
        var scoreColNum = mapping.ScoreColumnIndex >= 0 && mapping.ScoreColumnIndex < usedCells.Count
            ? usedCells[mapping.ScoreColumnIndex].Address.ColumnNumber : -1;
        var reasonColNum = mapping.ReasonColumnIndex >= 0 && mapping.ReasonColumnIndex < usedCells.Count
            ? usedCells[mapping.ReasonColumnIndex].Address.ColumnNumber : -1;

        var lastRow = worksheet.LastRowUsed();
        var maxRow = lastRow != null ? lastRow.RowNumber() : 1;
        var startRow = Math.Max(mapping.DataStartRow, 2);
        var endRow = mapping.DataEndRow > 0 ? Math.Min(mapping.DataEndRow, maxRow) : maxRow;

        for (var row = startRow; row <= endRow; row++)
        {
            var entry = new ImportScoreEntry
            {
                StudentName = nameColNum >= 0 ? worksheet.Cell(row, nameColNum).GetString().Trim() : "",
                StudentNumber = numberColNum >= 0 ? worksheet.Cell(row, numberColNum).GetString().Trim() : null,
                Reason = reasonColNum >= 0 ? worksheet.Cell(row, reasonColNum).GetString().Trim() : null
            };

            if (scoreColNum >= 0)
            {
                var scoreCell = worksheet.Cell(row, scoreColNum);
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
    /// 在 Excel 表头行中查找匹配的列索引（0开始）
    /// </summary>
    private static int FindExcelColumnIndex(ClosedXML.Excel.IXLRow headerRow, params string[] names)
    {
        var usedCells = headerRow.CellsUsed().ToList();
        foreach (var name in names)
        {
            for (var i = 0; i < usedCells.Count; i++)
            {
                if (usedCells[i].GetString().Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }
}
