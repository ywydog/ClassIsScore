using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 排行榜时间范围枚举
/// </summary>
public enum TimeRange
{
    /// <summary>
    /// 日排行
    /// </summary>
    Day,
    /// <summary>
    /// 周排行
    /// </summary>
    Week,
    /// <summary>
    /// 月排行
    /// </summary>
    Month,
    /// <summary>
    /// 全部排行
    /// </summary>
    All
}

/// <summary>
/// 排行榜页面 ViewModel
/// </summary>
public partial class LeaderboardViewModel : ObservableObject
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardViewModel> _logger;

    /// <summary>
    /// 排行榜条目列表
    /// </summary>
    public ObservableCollection<LeaderboardEntry> LeaderboardEntries { get; } = new();

    /// <summary>
    /// 当前选中的时间范围
    /// </summary>
    [ObservableProperty]
    private TimeRange _selectedTimeRange = TimeRange.Day;

    /// <summary>
    /// 是否为个人排行榜（false 为小组排行榜）
    /// </summary>
    [ObservableProperty]
    private bool _isPersonalTab = true;

    /// <summary>
    /// 是否有小组
    /// </summary>
    [ObservableProperty]
    private bool _hasGroups;

    /// <summary>
    /// 选择的日期
    /// </summary>
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 状态消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public LeaderboardViewModel(
        ILeaderboardService leaderboardService,
        ILogger<LeaderboardViewModel> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;

        // 初始化时检查是否有小组
        _hasGroups = _leaderboardService.HasGroups();
    }

    /// <summary>
    /// 时间范围变更时刷新排行榜
    /// </summary>
    partial void OnSelectedTimeRangeChanged(TimeRange value)
    {
        RefreshCommand.Execute(null);
    }

    /// <summary>
    /// 个人/小组切换时刷新排行榜
    /// </summary>
    partial void OnIsPersonalTabChanged(bool value)
    {
        RefreshCommand.Execute(null);
    }

    /// <summary>
    /// 日期变更时刷新排行榜
    /// </summary>
    partial void OnSelectedDateChanged(DateTime value)
    {
        RefreshCommand.Execute(null);
    }

    /// <summary>
    /// 刷新排行榜数据
    /// </summary>
    [RelayCommand]
    private void Refresh()
    {
        try
        {
            IsLoading = true;
            HasGroups = _leaderboardService.HasGroups();

            var entries = IsPersonalTab
                ? GetPersonalLeaderboard()
                : GetGroupLeaderboard();

            LeaderboardEntries.Clear();
            foreach (var entry in entries)
            {
                LeaderboardEntries.Add(entry);
            }

            StatusMessage = $"共 {entries.Count} 条记录";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新排行榜失败");
            StatusMessage = "刷新排行榜失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出排行榜
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var entries = new System.Collections.Generic.List<LeaderboardEntry>(LeaderboardEntries);
            var timeLabel = SelectedTimeRange switch
            {
                TimeRange.Day => $"日排行_{SelectedDate:yyyyMMdd}",
                TimeRange.Week => $"周排行_{SelectedDate:yyyyMMdd}",
                TimeRange.Month => $"月排行_{SelectedDate:yyyyMM}",
                TimeRange.All => "全部排行",
                _ => "排行榜"
            };
            var title = $"{(IsPersonalTab ? "个人" : "小组")}{timeLabel}";
            var result = await _leaderboardService.ExportLeaderboardAsync(entries, filePath, title);
            StatusMessage = $"已导出到: {result}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出排行榜失败");
            StatusMessage = "导出排行榜失败";
        }
    }

    /// <summary>
    /// 切换到个人排行榜
    /// </summary>
    [RelayCommand]
    private void SwitchToPersonal()
    {
        IsPersonalTab = true;
    }

    /// <summary>
    /// 切换到小组排行榜
    /// </summary>
    [RelayCommand]
    private void SwitchToGroup()
    {
        IsPersonalTab = false;
    }

    /// <summary>
    /// 切换时间范围到日
    /// </summary>
    [RelayCommand]
    private void SwitchToDay()
    {
        SelectedTimeRange = TimeRange.Day;
    }

    /// <summary>
    /// 切换时间范围到周
    /// </summary>
    [RelayCommand]
    private void SwitchToWeek()
    {
        SelectedTimeRange = TimeRange.Week;
    }

    /// <summary>
    /// 切换时间范围到月
    /// </summary>
    [RelayCommand]
    private void SwitchToMonth()
    {
        SelectedTimeRange = TimeRange.Month;
    }

    /// <summary>
    /// 切换时间范围到全部
    /// </summary>
    [RelayCommand]
    private void SwitchToAll()
    {
        SelectedTimeRange = TimeRange.All;
    }

    /// <summary>
    /// 获取个人排行榜数据
    /// </summary>
    private System.Collections.Generic.List<LeaderboardEntry> GetPersonalLeaderboard()
    {
        return SelectedTimeRange switch
        {
            TimeRange.Day => _leaderboardService.GetDailyLeaderboard(SelectedDate),
            TimeRange.Week => _leaderboardService.GetWeeklyLeaderboard(SelectedDate),
            TimeRange.Month => _leaderboardService.GetMonthlyLeaderboard(SelectedDate),
            TimeRange.All => _leaderboardService.GetAllTimeLeaderboard(),
            _ => _leaderboardService.GetAllTimeLeaderboard()
        };
    }

    /// <summary>
    /// 获取小组排行榜数据
    /// </summary>
    private System.Collections.Generic.List<LeaderboardEntry> GetGroupLeaderboard()
    {
        return SelectedTimeRange switch
        {
            TimeRange.Day => _leaderboardService.GetDailyGroupLeaderboard(SelectedDate),
            TimeRange.Week => _leaderboardService.GetWeeklyGroupLeaderboard(SelectedDate),
            TimeRange.Month => _leaderboardService.GetMonthlyGroupLeaderboard(SelectedDate),
            TimeRange.All => _leaderboardService.GetAllTimeGroupLeaderboard(),
            _ => _leaderboardService.GetAllTimeGroupLeaderboard()
        };
    }
}
