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
/// 学生显示项，封装学生数据并提供显示用属性
/// </summary>
public class StudentDisplayItem : ObservableObject
{
    private readonly Student _student;

    public StudentDisplayItem(Student student)
    {
        _student = student;
    }

    /// <summary>
    /// 原始学生对象
    /// </summary>
    public Student Student => _student;

    /// <summary>
    /// 学生ID
    /// </summary>
    public Guid Id => _student.Id;

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name => _student.Name;

    /// <summary>
    /// 头像路径
    /// </summary>
    public string? Avatar => _student.Avatar;

    /// <summary>
    /// 姓名首字，用于默认头像
    /// </summary>
    public string Initial => string.IsNullOrEmpty(_student.Name) ? "?" : _student.Name[0].ToString();

    /// <summary>
    /// 积分文本
    /// </summary>
    public string ScoreText => _student.Score.ToString("F1");

    /// <summary>
    /// 当前积分
    /// </summary>
    public double Score
    {
        get => _student.Score;
        set
        {
            if (Math.Abs(_student.Score - value) > 0.001)
            {
                _student.Score = value;
                OnPropertyChanged(nameof(Score));
                OnPropertyChanged(nameof(ScoreText));
            }
        }
    }
}

/// <summary>
/// 积分显示页面 ViewModel
/// </summary>
public partial class ScoreDisplayViewModel : ObservableObject
{
    private readonly IScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly ILogger<ScoreDisplayViewModel> _logger;

    /// <summary>
    /// 学生显示项列表
    /// </summary>
    public ObservableCollection<StudentDisplayItem> Students { get; } = new();

    /// <summary>
    /// 当前显示模式
    /// </summary>
    [ObservableProperty]
    private DisplayMode _displayMode = DisplayMode.Card;

    /// <summary>
    /// 显示设置
    /// </summary>
    [ObservableProperty]
    private DisplaySettings _displaySettings = new();

    /// <summary>
    /// 当前选中的学生
    /// </summary>
    [ObservableProperty]
    private StudentDisplayItem? _selectedStudent;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 快捷加减分数值
    /// </summary>
    [ObservableProperty]
    private double _quickScoreValue = 1;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ScoreDisplayViewModel(
        IScoreService scoreService,
        IStudentService studentService,
        ILogger<ScoreDisplayViewModel> logger)
    {
        _scoreService = scoreService;
        _studentService = studentService;
        _logger = logger;

        // 订阅积分变动事件
        _scoreService.ScoreChanged += OnScoreChanged;
    }

    /// <summary>
    /// 积分变动事件处理，自动刷新显示
    /// </summary>
    private void OnScoreChanged(object? sender, ScoreChangedEventArgs e)
    {
        var item = Students.FirstOrDefault(s => s.Id == e.StudentId);
        if (item != null)
        {
            item.Score = e.NewScore;
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
                Students.Add(new StudentDisplayItem(student));
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
    /// 切换到卡片模式
    /// </summary>
    [RelayCommand]
    private void SwitchToCardMode()
    {
        DisplayMode = DisplayMode.Card;
    }

    /// <summary>
    /// 切换到圆形模式
    /// </summary>
    [RelayCommand]
    private void SwitchToCircleMode()
    {
        DisplayMode = DisplayMode.Circle;
    }

    /// <summary>
    /// 切换到宠物模式
    /// </summary>
    [RelayCommand]
    private void SwitchToPetMode()
    {
        DisplayMode = DisplayMode.Pet;
    }

    /// <summary>
    /// 快捷加分
    /// </summary>
    [RelayCommand]
    private async Task QuickAddScoreAsync(StudentDisplayItem? item)
    {
        if (item == null) return;

        try
        {
            await _scoreService.AddScoreAsync(item.Id, QuickScoreValue, "快捷加分");
            StatusMessage = $"已为 {item.Name} 加 {QuickScoreValue} 分";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快捷加分失败");
            StatusMessage = "快捷加分失败";
        }
    }

    /// <summary>
    /// 快捷减分
    /// </summary>
    [RelayCommand]
    private async Task QuickSubtractScoreAsync(StudentDisplayItem? item)
    {
        if (item == null) return;

        try
        {
            await _scoreService.AddScoreAsync(item.Id, -QuickScoreValue, "快捷减分");
            StatusMessage = $"已为 {item.Name} 减 {QuickScoreValue} 分";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快捷减分失败");
            StatusMessage = "快捷减分失败";
        }
    }

    /// <summary>
    /// 选中学生变更时处理
    /// </summary>
    partial void OnSelectedStudentChanged(StudentDisplayItem? value)
    {
        // 可扩展：选中学生时触发额外操作
    }
}
