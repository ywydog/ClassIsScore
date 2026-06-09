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
    public double GetStudentScore(Guid studentId)
    {
        var student = _studentService.GetStudentByIdAsync(studentId).Result;
        return student?.Score ?? 0;
    }
}
