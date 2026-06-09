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

    /// <summary>宠物类型ID</summary>
    public string? PetType => _student.PetType;

    /// <summary>宠物经验值</summary>
    public double PetExp => _student.PetExp;

    /// <summary>宠物等级（根据经验值计算）</summary>
    public int PetLevel => PetSystem.CalculateLevel(_student.PetExp);

    /// <summary>宠物emoji</summary>
    public string PetEmoji => PetSystem.GetPetEmoji(_student.PetType);

    /// <summary>宠物名称</summary>
    public string PetName => PetSystem.GetPetTypeInfo(_student.PetType ?? "")?.Name ?? "未领养";

    /// <summary>等级进度</summary>
    public LevelProgress LevelProgress => PetSystem.GetLevelProgress(_student.PetExp);

    /// <summary>等级边框颜色</summary>
    public string LevelBorderColor => PetSystem.GetLevelBorderColor(PetLevel);

    /// <summary>等级称号</summary>
    public string LevelTitle => PetSystem.GetLevelTitle(PetLevel);

    /// <summary>是否已领养宠物</summary>
    public bool HasPet => !string.IsNullOrEmpty(_student.PetType);

    /// <summary>是否已毕业</summary>
    public bool IsGraduated => PetLevel >= PetSystem.MaxLevel;
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

    /// <summary>
    /// 所有可用宠物类型列表
    /// </summary>
    public PetTypeInfo[] PetTypes => PetSystem.AllPetTypes;

    /// <summary>
    /// 是否显示宠物选择对话框
    /// </summary>
    [ObservableProperty]
    private bool _showPetSelection;

    /// <summary>
    /// 当前选择宠物的学生
    /// </summary>
    [ObservableProperty]
    private StudentDisplayItem? _petSelectingStudent;

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

    /// <summary>
    /// 选择宠物命令
    /// </summary>
    [RelayCommand]
    private async Task SelectPetAsync(string? petTypeId)
    {
        if (PetSelectingStudent == null || string.IsNullOrEmpty(petTypeId)) return;

        try
        {
            var student = PetSelectingStudent.Student;
            student.PetType = petTypeId;
            await _studentService.UpdateStudentAsync(student);
            StatusMessage = $"已为 {student.Name} 领养 {PetSystem.GetPetEmoji(petTypeId)} {PetSystem.GetPetTypeInfo(petTypeId)?.Name}";
            ShowPetSelection = false;

            // 刷新列表
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "选择宠物失败");
            StatusMessage = "选择宠物失败";
        }
    }
}
