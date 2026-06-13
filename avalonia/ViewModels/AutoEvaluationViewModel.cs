using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 自动评价页面 ViewModel
/// </summary>
public partial class AutoEvaluationViewModel : ObservableObject
{
    private readonly IAutoEvaluationService _autoEvaluationService;
    private readonly IScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly IGroupService _groupService;
    private readonly ILogger<AutoEvaluationViewModel> _logger;

    /// <summary>
    /// 自动评价配置列表
    /// </summary>
    public ObservableCollection<AutoEvaluationConfig> Configs { get; } = new();

    /// <summary>
    /// 所有学生列表（用于目标选择）
    /// </summary>
    public ObservableCollection<Student> Students { get; } = new();

    /// <summary>
    /// 所有小组合列表（用于目标选择）
    /// </summary>
    public ObservableCollection<StudentGroup> Groups { get; } = new();

    /// <summary>
    /// 常用评价项列表
    /// </summary>
    public ObservableCollection<EvaluationItem> EvaluationItems { get; } = new();

    /// <summary>
    /// 触发类型选项
    /// </summary>
    public ObservableCollection<TriggerType> TriggerTypes { get; } = new(
        Enum.GetValues<TriggerType>());

    /// <summary>
    /// 目标类型选项
    /// </summary>
    public ObservableCollection<TargetType> TargetTypes { get; } = new(
        Enum.GetValues<TargetType>());

    /// <summary>
    /// 星期选项
    /// </summary>
    public ObservableCollection<DayOfWeek> WeekDays { get; } = new(
        Enum.GetValues<DayOfWeek>());

    /// <summary>
    /// 当前选中的配置
    /// </summary>
    [ObservableProperty]
    private AutoEvaluationConfig? _selectedConfig;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 操作结果提示消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 是否显示编辑对话框
    /// </summary>
    [ObservableProperty]
    private bool _isEditDialogOpen;

    /// <summary>
    /// 编辑中的配置（副本）
    /// </summary>
    [ObservableProperty]
    private AutoEvaluationConfig _editingConfig = new();

    /// <summary>
    /// 是否为新增模式
    /// </summary>
    [ObservableProperty]
    private bool _isAddingNew;

    /// <summary>
    /// 对话框标题
    /// </summary>
    [ObservableProperty]
    private string _dialogTitle = "添加配置";

    public AutoEvaluationViewModel(
        IAutoEvaluationService autoEvaluationService,
        IScoreService scoreService,
        IStudentService studentService,
        IGroupService groupService,
        ILogger<AutoEvaluationViewModel> logger)
    {
        _autoEvaluationService = autoEvaluationService;
        _scoreService = scoreService;
        _studentService = studentService;
        _groupService = groupService;
        _logger = logger;

        // 订阅自动评价执行事件
        _autoEvaluationService.EvaluationExecuted += OnEvaluationExecuted;
    }

    /// <summary>
    /// 自动评价执行事件处理
    /// </summary>
    private void OnEvaluationExecuted(object? sender, AutoEvaluationExecutedEventArgs e)
    {
        StatusMessage = $"自动评价已执行: {e.ConfigName}，影响 {e.AffectedCount} 人，积分变动 {e.ScoreChange}";
    }

    /// <summary>
    /// 加载配置列表
    /// </summary>
    [RelayCommand]
    private async Task LoadConfigsAsync()
    {
        try
        {
            IsLoading = true;
            var configs = await _autoEvaluationService.GetConfigsAsync();
            Configs.Clear();
            foreach (var config in configs)
            {
                Configs.Add(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载自动评价配置失败");
            StatusMessage = "加载自动评价配置失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载学生和小组数据
    /// </summary>
    [RelayCommand]
    private async Task LoadTargetDataAsync()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }

            var groups = await _groupService.GetAllGroupsAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            var evalItems = await _scoreService.GetEvaluationItemsAsync();
            EvaluationItems.Clear();
            foreach (var item in evalItems)
            {
                EvaluationItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载目标数据失败");
        }
    }

    /// <summary>
    /// 打开添加配置对话框
    /// </summary>
    [RelayCommand]
    private async Task AddConfigAsync()
    {
        await LoadTargetDataAsync();

        IsAddingNew = true;
        DialogTitle = "添加配置";
        EditingConfig = new AutoEvaluationConfig
        {
            Name = string.Empty,
            TriggerType = TriggerType.Daily,
            TriggerTime = TimeSpan.FromHours(8),
            TargetType = TargetType.AllStudents,
            ScoreChange = 1,
            Reason = string.Empty,
            IsEnabled = true
        };
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// 打开编辑配置对话框
    /// </summary>
    [RelayCommand]
    private async Task EditConfigAsync(AutoEvaluationConfig? config)
    {
        if (config == null) return;

        await LoadTargetDataAsync();

        IsAddingNew = false;
        DialogTitle = "编辑配置";
        EditingConfig = new AutoEvaluationConfig
        {
            Id = config.Id,
            Name = config.Name,
            TriggerType = config.TriggerType,
            TriggerTime = config.TriggerTime,
            DayOfWeek = config.DayOfWeek,
            DayOfMonth = config.DayOfMonth,
            EvaluationItemId = config.EvaluationItemId,
            ScoreChange = config.ScoreChange,
            Reason = config.Reason,
            TargetType = config.TargetType,
            TargetGroupId = config.TargetGroupId,
            TargetStudentId = config.TargetStudentId,
            IsEnabled = config.IsEnabled
        };
        IsEditDialogOpen = true;
    }

    /// <summary>
    /// 保存配置（新增或更新）
    /// </summary>
    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditingConfig.Name))
            {
                StatusMessage = "请输入配置名称";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingConfig.Reason))
            {
                StatusMessage = "请输入原因";
                return;
            }

            if (IsAddingNew)
            {
                await _autoEvaluationService.AddConfigAsync(EditingConfig);
                StatusMessage = $"已添加配置: {EditingConfig.Name}";
            }
            else
            {
                await _autoEvaluationService.UpdateConfigAsync(EditingConfig);
                StatusMessage = $"已更新配置: {EditingConfig.Name}";
            }

            IsEditDialogOpen = false;
            await LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存自动评价配置失败");
            StatusMessage = "保存配置失败";
        }
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    [RelayCommand]
    private async Task DeleteConfigAsync(AutoEvaluationConfig? config)
    {
        if (config == null) return;

        try
        {
            await _autoEvaluationService.DeleteConfigAsync(config.Id);
            StatusMessage = $"已删除配置: {config.Name}";
            await LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除自动评价配置失败");
            StatusMessage = "删除配置失败";
        }
    }

    /// <summary>
    /// 切换启用/禁用状态
    /// </summary>
    [RelayCommand]
    private async Task ToggleEnabledAsync(AutoEvaluationConfig? config)
    {
        if (config == null) return;

        try
        {
            config.IsEnabled = !config.IsEnabled;
            await _autoEvaluationService.UpdateConfigAsync(config);
            StatusMessage = config.IsEnabled ? $"已启用: {config.Name}" : $"已禁用: {config.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换配置启用状态失败");
            StatusMessage = "操作失败";
            // 回滚
            config.IsEnabled = !config.IsEnabled;
        }
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditDialogOpen = false;
    }

    /// <summary>
    /// 关联评价项时自动填充积分和原因
    /// </summary>
    [RelayCommand]
    private void ApplyEvaluationItem(EvaluationItem? item)
    {
        if (item == null) return;
        EditingConfig.ScoreChange = item.ScoreChange;
        EditingConfig.Reason = item.Name;
        OnPropertyChanged(nameof(EditingConfig));
    }

    /// <summary>
    /// 获取触发类型的显示文本
    /// </summary>
    public static string GetTriggerTypeDisplayText(TriggerType type) => type switch
    {
        TriggerType.Daily => "每天",
        TriggerType.Weekly => "每周",
        TriggerType.Monthly => "每月",
        TriggerType.BeforeSettlement => "结算前",
        _ => type.ToString()
    };

    /// <summary>
    /// 获取目标类型的显示文本
    /// </summary>
    public static string GetTargetTypeDisplayText(TargetType type) => type switch
    {
        TargetType.AllStudents => "所有学生",
        TargetType.SpecificGroup => "指定小组",
        TargetType.SpecificStudent => "指定学生",
        _ => type.ToString()
    };

    /// <summary>
    /// 获取星期的显示文本
    /// </summary>
    public static string GetDayOfWeekDisplayText(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        DayOfWeek.Saturday => "周六",
        DayOfWeek.Sunday => "周日",
        _ => day.ToString()
    };
}
