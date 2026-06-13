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
/// 学生详情页面 ViewModel，提供学生信息、积分统计和趋势分析
/// </summary>
public partial class StudentProfileViewModel : ObservableObject
{
    private readonly IScoreService _scoreService;
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentProfileViewModel> _logger;

    /// <summary>
    /// 当前学生ID
    /// </summary>
    private Guid _studentId;

    /// <summary>
    /// 学生姓名
    /// </summary>
    [ObservableProperty]
    private string _studentName = string.Empty;

    /// <summary>
    /// 学号
    /// </summary>
    [ObservableProperty]
    private string? _studentNumber;

    /// <summary>
    /// 当前积分
    /// </summary>
    [ObservableProperty]
    private double _currentScore;

    /// <summary>
    /// 宠物类型ID
    /// </summary>
    [ObservableProperty]
    private string? _petType;

    /// <summary>
    /// 宠物等级
    /// </summary>
    [ObservableProperty]
    private int _petLevel;

    /// <summary>
    /// 宠物经验值
    /// </summary>
    [ObservableProperty]
    private double _petExp;

    /// <summary>
    /// 宠物名称
    /// </summary>
    [ObservableProperty]
    private string _petName = string.Empty;

    /// <summary>
    /// 宠物emoji
    /// </summary>
    [ObservableProperty]
    private string _petEmoji = string.Empty;

    /// <summary>
    /// 是否已领养宠物
    /// </summary>
    [ObservableProperty]
    private bool _hasPet;

    /// <summary>
    /// 等级称号
    /// </summary>
    [ObservableProperty]
    private string _levelTitle = string.Empty;

    /// <summary>
    /// 总变动次数
    /// </summary>
    [ObservableProperty]
    private int _totalScoreChanges;

    /// <summary>
    /// 加分次数
    /// </summary>
    [ObservableProperty]
    private int _positiveChanges;

    /// <summary>
    /// 减分次数
    /// </summary>
    [ObservableProperty]
    private int _negativeChanges;

    /// <summary>
    /// 净积分变动
    /// </summary>
    [ObservableProperty]
    private double _netChange;

    /// <summary>
    /// 最近积分记录
    /// </summary>
    public ObservableCollection<ScoreRecord> RecentRecords { get; } = new();

    /// <summary>
    /// 积分趋势数据
    /// </summary>
    public ObservableCollection<ScoreTrendPoint> ScoreTrendData { get; } = new();

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    public StudentProfileViewModel(
        IScoreService scoreService,
        IStudentService studentService,
        ILogger<StudentProfileViewModel> logger)
    {
        _scoreService = scoreService;
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// 加载学生基本信息
    /// </summary>
    [RelayCommand]
    private async Task LoadStudentAsync(Guid studentId)
    {
        _studentId = studentId;

        try
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                _logger.LogWarning("未找到学生: {StudentId}", studentId);
                return;
            }

            StudentName = student.Name;
            StudentNumber = student.StudentNumber;
            CurrentScore = student.Score;
            PetType = student.PetType;
            PetExp = student.PetExp;
            PetLevel = PetSystem.CalculateLevel(student.PetExp);
            HasPet = !string.IsNullOrEmpty(student.PetType);
            PetName = HasPet ? (PetSystem.GetPetTypeInfo(student.PetType!)?.Name ?? "未知") : "未领养";
            PetEmoji = PetSystem.GetPetEmoji(student.PetType);
            LevelTitle = PetSystem.GetLevelTitle(PetLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载学生信息失败");
        }
    }

    /// <summary>
    /// 加载积分历史记录
    /// </summary>
    [RelayCommand]
    private async Task LoadScoreHistoryAsync()
    {
        try
        {
            var records = await _scoreService.GetHistoryAsync(_studentId);

            // 过滤掉已撤销的记录
            var validRecords = records.Where(r => !r.IsReverted).ToList();

            // 统计数据
            TotalScoreChanges = validRecords.Count;
            PositiveChanges = validRecords.Count(r => r.ScoreChange > 0);
            NegativeChanges = validRecords.Count(r => r.ScoreChange < 0);
            NetChange = validRecords.Sum(r => r.ScoreChange);

            // 最近20条记录
            RecentRecords.Clear();
            foreach (var record in validRecords.Take(20))
            {
                RecentRecords.Add(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载积分历史失败");
        }
    }

    /// <summary>
    /// 计算积分趋势数据，按日汇总
    /// </summary>
    [RelayCommand]
    private async Task CalculateTrendAsync()
    {
        try
        {
            var records = await _scoreService.GetHistoryAsync(_studentId);
            var validRecords = records.Where(r => !r.IsReverted)
                                      .OrderBy(r => r.CreatedAt)
                                      .ToList();

            if (validRecords.Count == 0)
            {
                ScoreTrendData.Clear();
                return;
            }

            // 按日期分组，计算每日变动和累计积分
            var dailyGroups = validRecords
                .GroupBy(r => r.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .ToList();

            ScoreTrendData.Clear();
            double cumulativeScore = 0;

            foreach (var group in dailyGroups)
            {
                var dailyChange = group.Sum(r => r.ScoreChange);
                cumulativeScore += dailyChange;

                ScoreTrendData.Add(new ScoreTrendPoint
                {
                    Date = group.Key,
                    Score = cumulativeScore,
                    Change = dailyChange
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算积分趋势失败");
        }
    }

    /// <summary>
    /// 关闭对话框命令
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // 由页面处理关闭逻辑
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 关闭请求事件
    /// </summary>
    public event EventHandler? CloseRequested;
}
