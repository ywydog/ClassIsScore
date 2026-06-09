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
/// 积分管理页面 ViewModel
/// </summary>
public partial class ScoreManagementViewModel : ObservableObject
{
    private readonly IScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly ILogger<ScoreManagementViewModel> _logger;

    /// <summary>
    /// 所有学生列表
    /// </summary>
    public ObservableCollection<Student> Students { get; } = new();

    /// <summary>
    /// 常用评价项列表
    /// </summary>
    public ObservableCollection<EvaluationItem> EvaluationItems { get; } = new();

    /// <summary>
    /// 历史记录列表
    /// </summary>
    public ObservableCollection<ScoreRecord> HistoryRecords { get; } = new();

    /// <summary>
    /// 当前选中的学生
    /// </summary>
    [ObservableProperty]
    private Student? _selectedStudent;

    /// <summary>
    /// 多选模式下选中的学生集合
    /// </summary>
    public ObservableCollection<Student> SelectedStudents { get; } = new();

    /// <summary>
    /// 是否处于多选模式
    /// </summary>
    [ObservableProperty]
    private bool _isMultiSelectMode;

    /// <summary>
    /// 要加减的分数
    /// </summary>
    [ObservableProperty]
    private double _scoreChange = 1;

    /// <summary>
    /// 原因
    /// </summary>
    [ObservableProperty]
    private string _reason = string.Empty;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 筛选 - 开始日期
    /// </summary>
    [ObservableProperty]
    private DateTime? _filterStartDate;

    /// <summary>
    /// 筛选 - 结束日期
    /// </summary>
    [ObservableProperty]
    private DateTime? _filterEndDate;

    /// <summary>
    /// 筛选 - 选中的学生ID
    /// </summary>
    [ObservableProperty]
    private Student? _filterStudent;

    /// <summary>
    /// 操作结果提示消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 已选择的学生数量
    /// </summary>
    [ObservableProperty]
    private int _selectedStudentCount;

    public ScoreManagementViewModel(
        IScoreService scoreService,
        IStudentService studentService,
        ILogger<ScoreManagementViewModel> logger)
    {
        _scoreService = scoreService;
        _studentService = studentService;
        _logger = logger;

        // 订阅积分变动事件
        _scoreService.ScoreChanged += OnScoreChanged;

        // 监听多选集合变更
        SelectedStudents.CollectionChanged += (_, _) =>
        {
            SelectedStudentCount = SelectedStudents.Count;
        };
    }

    /// <summary>
    /// 积分变动事件处理
    /// </summary>
    private void OnScoreChanged(object? sender, ScoreChangedEventArgs e)
    {
        // 刷新学生列表中对应学生的积分
        var student = Students.FirstOrDefault(s => s.Id == e.StudentId);
        if (student != null)
        {
            student.Score = e.NewScore;
        }
    }

    /// <summary>
    /// 多选模式变更时处理
    /// </summary>
    partial void OnIsMultiSelectModeChanged(bool value)
    {
        if (!value)
        {
            // 退出多选模式时清空已选列表
            SelectedStudents.Clear();
        }
    }

    /// <summary>
    /// 加载学生列表
    /// </summary>
    [RelayCommand]
    private async Task LoadStudentsAsync()
    {
        try
        {
            IsLoading = true;
            var students = await _studentService.GetAllStudentsAsync();
            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载学生列表失败");
            StatusMessage = "加载学生列表失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 加载常用评价项
    /// </summary>
    [RelayCommand]
    private async Task LoadEvaluationItemsAsync()
    {
        try
        {
            var items = await _scoreService.GetEvaluationItemsAsync();
            EvaluationItems.Clear();
            foreach (var item in items)
            {
                EvaluationItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载常用评价项失败");
        }
    }

    /// <summary>
    /// 加载历史记录
    /// </summary>
    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        try
        {
            var records = await _scoreService.GetHistoryAsync(
                FilterStudent?.Id,
                FilterStartDate,
                FilterEndDate);

            HistoryRecords.Clear();
            foreach (var record in records)
            {
                HistoryRecords.Add(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载历史记录失败");
            StatusMessage = "加载历史记录失败";
        }
    }

    /// <summary>
    /// 加分命令
    /// </summary>
    [RelayCommand]
    private async Task AddScoreAsync()
    {
        if (IsMultiSelectMode)
        {
            if (SelectedStudents.Count == 0)
            {
                StatusMessage = "请先选择学生";
                return;
            }

            if (ScoreChange <= 0)
            {
                StatusMessage = "加分分数必须大于0";
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                StatusMessage = "请输入原因";
                return;
            }

            try
            {
                var ids = SelectedStudents.Select(s => s.Id).ToList();
                await _scoreService.AddScoreToMultipleStudentsAsync(ids, ScoreChange, Reason);
                StatusMessage = $"已为 {SelectedStudents.Count} 名学生加 {ScoreChange} 分";
                Reason = string.Empty;
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量加分失败");
                StatusMessage = "批量加分失败";
            }
            return;
        }

        if (SelectedStudent == null)
        {
            StatusMessage = "请先选择学生";
            return;
        }

        if (ScoreChange <= 0)
        {
            StatusMessage = "加分分数必须大于0";
            return;
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            StatusMessage = "请输入原因";
            return;
        }

        try
        {
            await _scoreService.AddScoreAsync(SelectedStudent.Id, ScoreChange, Reason);
            StatusMessage = $"已为 {SelectedStudent.Name} 加 {ScoreChange} 分";
            Reason = string.Empty;
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加分失败");
            StatusMessage = "加分失败";
        }
    }

    /// <summary>
    /// 减分命令
    /// </summary>
    [RelayCommand]
    private async Task SubtractScoreAsync()
    {
        if (IsMultiSelectMode)
        {
            if (SelectedStudents.Count == 0)
            {
                StatusMessage = "请先选择学生";
                return;
            }

            if (ScoreChange <= 0)
            {
                StatusMessage = "减分分数必须大于0";
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                StatusMessage = "请输入原因";
                return;
            }

            try
            {
                var ids = SelectedStudents.Select(s => s.Id).ToList();
                await _scoreService.AddScoreToMultipleStudentsAsync(ids, -ScoreChange, Reason);
                StatusMessage = $"已为 {SelectedStudents.Count} 名学生减 {ScoreChange} 分";
                Reason = string.Empty;
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量减分失败");
                StatusMessage = "批量减分失败";
            }
            return;
        }

        if (SelectedStudent == null)
        {
            StatusMessage = "请先选择学生";
            return;
        }

        if (ScoreChange <= 0)
        {
            StatusMessage = "减分分数必须大于0";
            return;
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            StatusMessage = "请输入原因";
            return;
        }

        try
        {
            await _scoreService.AddScoreAsync(SelectedStudent.Id, -ScoreChange, Reason);
            StatusMessage = $"已为 {SelectedStudent.Name} 减 {ScoreChange} 分";
            Reason = string.Empty;
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "减分失败");
            StatusMessage = "减分失败";
        }
    }

    /// <summary>
    /// 使用常用评价项快捷加减分
    /// </summary>
    [RelayCommand]
    private async Task QuickScoreAsync(EvaluationItem? item)
    {
        if (item == null) return;

        if (IsMultiSelectMode)
        {
            if (SelectedStudents.Count == 0)
            {
                StatusMessage = "请先选择学生";
                return;
            }

            try
            {
                var ids = SelectedStudents.Select(s => s.Id).ToList();
                await _scoreService.AddScoreToMultipleStudentsAsync(ids, item.ScoreChange, item.Name);
                StatusMessage = $"已为 {SelectedStudents.Count} 名学生{(item.IsPositive ? "加" : "减")} {Math.Abs(item.ScoreChange)} 分（{item.Name}）";
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量快捷加减分失败");
                StatusMessage = "批量快捷加减分失败";
            }
            return;
        }

        if (SelectedStudent == null)
        {
            StatusMessage = "请先选择学生";
            return;
        }

        try
        {
            await _scoreService.AddScoreAsync(SelectedStudent.Id, item.ScoreChange, item.Name);
            StatusMessage = $"已为 {SelectedStudent.Name} {(item.IsPositive ? "加" : "减")} {Math.Abs(item.ScoreChange)} 分（{item.Name}）";
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快捷加减分失败");
            StatusMessage = "快捷加减分失败";
        }
    }

    /// <summary>
    /// 切换多选模式
    /// </summary>
    [RelayCommand]
    private void ToggleMultiSelect()
    {
        IsMultiSelectMode = !IsMultiSelectMode;
    }

    /// <summary>
    /// 全选学生
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        SelectedStudents.Clear();
        foreach (var student in Students)
        {
            SelectedStudents.Add(student);
        }
    }

    /// <summary>
    /// 清除选择
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        SelectedStudents.Clear();
    }

    /// <summary>
    /// 撤销积分记录
    /// </summary>
    [RelayCommand]
    private async Task RevertScoreAsync(ScoreRecord? record)
    {
        if (record == null) return;

        try
        {
            var result = await _scoreService.RevertScoreAsync(record.Id);
            if (result)
            {
                StatusMessage = $"已撤销 {record.StudentName} 的积分记录";
                await LoadHistoryAsync();
                await LoadStudentsAsync();
            }
            else
            {
                StatusMessage = "撤销失败，可能需要管理员验证";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销积分记录失败");
            StatusMessage = "撤销积分记录失败";
        }
    }

    /// <summary>
    /// 判断积分记录是否在3分钟免验证窗口内
    /// </summary>
    public bool IsWithinRevertWindow(ScoreRecord record)
    {
        return (DateTime.Now - record.CreatedAt).TotalMinutes <= 3;
    }

    /// <summary>
    /// 应用筛选条件
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        await LoadHistoryAsync();
    }

    /// <summary>
    /// 清除筛选条件
    /// </summary>
    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        FilterStartDate = null;
        FilterEndDate = null;
        FilterStudent = null;
        await LoadHistoryAsync();
    }
}
