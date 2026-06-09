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
/// 导出格式枚举
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// 日度
    /// </summary>
    Daily,
    /// <summary>
    /// 周度
    /// </summary>
    Weekly,
    /// <summary>
    /// 月度
    /// </summary>
    Monthly
}

/// <summary>
/// 结算页面 ViewModel
/// </summary>
public partial class SettlementViewModel : ObservableObject
{
    private readonly ISettlementService _settlementService;
    private readonly IStudentService _studentService;
    private readonly ILogger<SettlementViewModel> _logger;

    /// <summary>
    /// 结算历史记录列表
    /// </summary>
    public ObservableCollection<SettlementRecord> SettlementHistory { get; } = new();

    /// <summary>
    /// 是否可以结算（无正在进行的结算操作时可用）
    /// </summary>
    [ObservableProperty]
    private bool _canSettle = true;

    /// <summary>
    /// 选中的导出格式
    /// </summary>
    [ObservableProperty]
    private ExportFormat _selectedExportFormat = ExportFormat.Monthly;

    /// <summary>
    /// 月度导出选中的月份
    /// </summary>
    [ObservableProperty]
    private DateTime _selectedMonth = DateTime.Now;

    /// <summary>
    /// 周度导出选中的周起始日
    /// </summary>
    [ObservableProperty]
    private DateTime _selectedWeekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek + 1);

    /// <summary>
    /// 日度导出选中的日期
    /// </summary>
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Now;

    /// <summary>
    /// 操作结果提示消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 导出文件路径
    /// </summary>
    [ObservableProperty]
    private string _exportedFilePath = string.Empty;

    public SettlementViewModel(
        ISettlementService settlementService,
        IStudentService studentService,
        ILogger<SettlementViewModel> logger)
    {
        _settlementService = settlementService;
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// 加载结算历史
    /// </summary>
    [RelayCommand]
    private async Task LoadSettlementHistoryAsync()
    {
        try
        {
            IsLoading = true;
            var records = await _settlementService.GetSettlementHistoryAsync();
            SettlementHistory.Clear();
            foreach (var record in records)
            {
                SettlementHistory.Add(record);
            }

            // 有学生时才能结算
            var students = await _studentService.GetAllStudentsAsync();
            CanSettle = students.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载结算历史失败");
            StatusMessage = "加载结算历史失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 执行结算命令
    /// </summary>
    [RelayCommand]
    private async Task SettleAsync()
    {
        try
        {
            CanSettle = false;
            IsLoading = true;
            StatusMessage = "正在执行结算...";

            var record = await _settlementService.SettleAsync();
            StatusMessage = $"结算完成！共 {record.StudentCount} 名学生，总积分 {record.TotalScore}";
            SettlementHistory.Insert(0, record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结算失败");
            StatusMessage = "结算失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
            CanSettle = true;
        }
    }

    /// <summary>
    /// 撤销结算命令
    /// </summary>
    [RelayCommand]
    private async Task RevertSettlementAsync(Guid? settlementId)
    {
        if (settlementId == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在撤销结算，需要管理员验证...";

            var result = await _settlementService.RevertSettlementAsync(settlementId.Value);
            if (result)
            {
                StatusMessage = "撤销结算成功，数据已恢复";
                // 更新列表中对应记录的状态
                var record = SettlementHistory.FirstOrDefault(r => r.Id == settlementId.Value);
                if (record != null)
                {
                    record.IsReverted = true;
                }
            }
            else
            {
                StatusMessage = "撤销结算失败，管理员验证未通过或备份文件不存在";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销结算失败");
            StatusMessage = "撤销结算失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出命令
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在导出...";
            ExportedFilePath = string.Empty;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputPath;

            switch (SelectedExportFormat)
            {
                case ExportFormat.Monthly:
                    var monthStr = SelectedMonth.ToString("yyyyMM");
                    outputPath = $"export_monthly_{monthStr}_{timestamp}.xlsx";
                    ExportedFilePath = await _settlementService.ExportMonthlyAsync(SelectedMonth, outputPath);
                    break;

                case ExportFormat.Weekly:
                    var weekStr = SelectedWeekStart.ToString("yyyyMMdd");
                    outputPath = $"export_weekly_{weekStr}_{timestamp}.xlsx";
                    ExportedFilePath = await _settlementService.ExportWeeklyAsync(SelectedWeekStart, outputPath);
                    break;

                case ExportFormat.Daily:
                    var dayStr = SelectedDate.ToString("yyyyMMdd");
                    outputPath = $"export_daily_{dayStr}_{timestamp}.xlsx";
                    ExportedFilePath = await _settlementService.ExportDailyAsync(SelectedDate, outputPath);
                    break;

                default:
                    StatusMessage = "未知的导出格式";
                    return;
            }

            StatusMessage = $"导出成功：{ExportedFilePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出失败");
            StatusMessage = "导出失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
