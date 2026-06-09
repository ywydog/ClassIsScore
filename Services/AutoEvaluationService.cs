using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 自动评价服务实现，使用 Timer 定时检查并执行自动评价配置
/// </summary>
public class AutoEvaluationService : IAutoEvaluationService, IDisposable
{
    private readonly ILogger<AutoEvaluationService> _logger;
    private readonly IScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly IGroupService _groupService;
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 定时检查计时器
    /// </summary>
    private readonly System.Timers.Timer _timer;

    /// <summary>
    /// 用于同步访问的锁对象
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 记录每个配置最近一次触发的日期，防止同一天重复触发
    /// </summary>
    private readonly Dictionary<Guid, DateTime> _lastTriggerDates = new();

    /// <inheritdoc/>
    public event EventHandler<AutoEvaluationExecutedEventArgs>? EvaluationExecuted;

    public AutoEvaluationService(
        ILogger<AutoEvaluationService> logger,
        IScoreService scoreService,
        IStudentService studentService,
        IGroupService groupService)
    {
        _logger = logger;
        _scoreService = scoreService;
        _studentService = studentService;
        _groupService = groupService;
        _configFilePath = Path.Combine(AppPaths.DataFolderPath, "auto_evaluation.json");

        // 每分钟检查一次
        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;

        EnsureConfigFileExists();
    }

    /// <summary>
    /// 确保配置文件存在
    /// </summary>
    private void EnsureConfigFileExists()
    {
        if (!File.Exists(_configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
            File.WriteAllText(_configFilePath, "[]");
        }
    }

    /// <summary>
    /// 从 JSON 文件读取配置
    /// </summary>
    private List<AutoEvaluationConfig> ReadConfigs()
    {
        try
        {
            var json = File.ReadAllText(_configFilePath);
            return JsonSerializer.Deserialize<List<AutoEvaluationConfig>>(json, _jsonOptions) ?? new List<AutoEvaluationConfig>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取自动评价配置文件失败");
            return new List<AutoEvaluationConfig>();
        }
    }

    /// <summary>
    /// 将配置写入 JSON 文件
    /// </summary>
    private void WriteConfigs(List<AutoEvaluationConfig> configs)
    {
        try
        {
            var json = JsonSerializer.Serialize(configs, _jsonOptions);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入自动评价配置文件失败");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StartAsync()
    {
        _logger.LogInformation("启动自动评价定时器");
        _timer.Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync()
    {
        _logger.LogInformation("停止自动评价定时器");
        _timer.Stop();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<List<AutoEvaluationConfig>> GetConfigsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(ReadConfigs());
        }
    }

    /// <inheritdoc/>
    public Task AddConfigAsync(AutoEvaluationConfig config)
    {
        lock (_lock)
        {
            var configs = ReadConfigs();
            config.Id = Guid.NewGuid();
            configs.Add(config);
            WriteConfigs(configs);
        }

        _logger.LogInformation("添加自动评价配置: {Name}", config.Name);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateConfigAsync(AutoEvaluationConfig config)
    {
        lock (_lock)
        {
            var configs = ReadConfigs();
            var index = configs.FindIndex(c => c.Id == config.Id);
            if (index >= 0)
            {
                configs[index] = config;
                WriteConfigs(configs);
                _logger.LogInformation("更新自动评价配置: {Name}", config.Name);
            }
            else
            {
                _logger.LogWarning("未找到自动评价配置: {Id}", config.Id);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteConfigAsync(Guid id)
    {
        lock (_lock)
        {
            var configs = ReadConfigs();
            var removed = configs.RemoveAll(c => c.Id == id);
            if (removed > 0)
            {
                WriteConfigs(configs);
                _lastTriggerDates.Remove(id);
                _logger.LogInformation("删除自动评价配置: {Id}", id);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 定时器触发回调，检查是否有需要执行的自动评价
    /// </summary>
    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await CheckAndExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动评价定时检查执行失败");
        }
    }

    /// <summary>
    /// 检查并执行到期的自动评价配置
    /// </summary>
    private async Task CheckAndExecuteAsync()
    {
        List<AutoEvaluationConfig> configs;
        lock (_lock)
        {
            configs = ReadConfigs().Where(c => c.IsEnabled).ToList();
        }

        var now = DateTime.Now;
        var today = now.Date;

        foreach (var config in configs)
        {
            // BeforeSettlement 类型由外部调用触发，不在定时器中执行
            if (config.TriggerType == TriggerType.BeforeSettlement)
                continue;

            if (!ShouldTrigger(config, now, today))
                continue;

            // 记录触发日期，防止同一天重复触发
            _lastTriggerDates[config.Id] = today;

            await ExecuteConfigAsync(config);
        }
    }

    /// <summary>
    /// 判断配置是否应该在当前时间触发
    /// </summary>
    private bool ShouldTrigger(AutoEvaluationConfig config, DateTime now, DateTime today)
    {
        // 检查是否已在今天触发过
        if (_lastTriggerDates.TryGetValue(config.Id, out var lastDate) && lastDate == today)
            return false;

        // 检查时间是否匹配（允许1分钟误差）
        var triggerTime = config.TriggerTime;
        var diff = Math.Abs((now - now.Date - triggerTime).TotalMinutes);
        if (diff > 1)
            return false;

        return config.TriggerType switch
        {
            TriggerType.Daily => true,
            TriggerType.Weekly => config.DayOfWeek.HasValue && now.DayOfWeek == config.DayOfWeek.Value,
            TriggerType.Monthly => config.DayOfMonth.HasValue && now.Day == config.DayOfMonth.Value,
            _ => false
        };
    }

    /// <summary>
    /// 执行单个自动评价配置
    /// </summary>
    private async Task ExecuteConfigAsync(AutoEvaluationConfig config)
    {
        try
        {
            var affectedCount = 0;

            switch (config.TargetType)
            {
                case TargetType.AllStudents:
                    var allStudents = await _studentService.GetAllStudentsAsync();
                    foreach (var student in allStudents)
                    {
                        await _scoreService.AddScoreAsync(student.Id, config.ScoreChange, config.Reason, "自动评价");
                        affectedCount++;
                    }
                    break;

                case TargetType.SpecificGroup:
                    if (config.TargetGroupId.HasValue)
                    {
                        await _scoreService.AddScoreToGroupAsync(config.TargetGroupId.Value, config.ScoreChange, config.Reason, "自动评价");
                        var groups = await _groupService.GetAllGroupsAsync();
                        var group = groups.FirstOrDefault(g => g.Id == config.TargetGroupId.Value);
                        affectedCount = group?.StudentIds.Count ?? 0;
                    }
                    break;

                case TargetType.SpecificStudent:
                    if (config.TargetStudentId.HasValue)
                    {
                        await _scoreService.AddScoreAsync(config.TargetStudentId.Value, config.ScoreChange, config.Reason, "自动评价");
                        affectedCount = 1;
                    }
                    break;
            }

            _logger.LogInformation("自动评价执行完成: {Name}，影响人数: {Count}，积分变动: {Change}",
                config.Name, affectedCount, config.ScoreChange);

            // 触发事件
            EvaluationExecuted?.Invoke(this, new AutoEvaluationExecutedEventArgs
            {
                ConfigName = config.Name,
                AffectedCount = affectedCount,
                ScoreChange = config.ScoreChange,
                ExecutedAt = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行自动评价配置失败: {Name}", config.Name);
        }
    }

    /// <summary>
    /// 结算前触发所有 BeforeSettlement 类型的配置（预留接口）
    /// </summary>
    public async Task TriggerBeforeSettlementAsync()
    {
        List<AutoEvaluationConfig> configs;
        lock (_lock)
        {
            configs = ReadConfigs()
                .Where(c => c.IsEnabled && c.TriggerType == TriggerType.BeforeSettlement)
                .ToList();
        }

        foreach (var config in configs)
        {
            await ExecuteConfigAsync(config);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
