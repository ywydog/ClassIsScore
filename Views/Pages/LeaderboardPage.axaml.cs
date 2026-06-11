using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.ViewModels;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 排行榜页面代码逻辑
/// </summary>
public partial class LeaderboardPage : UserControl
{
    public LeaderboardPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 页面加载完成时，自动加载排行榜数据
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is LeaderboardViewModel vm)
        {
            vm.RefreshCommand.Execute(null);
        }
    }

    /// <summary>
    /// 时间范围选择变更
    /// </summary>
    private void OnTimeRangeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LeaderboardViewModel vm && TimeRangeComboBox != null)
        {
            vm.SelectedTimeRange = TimeRangeComboBox.SelectedIndex switch
            {
                0 => TimeRange.Day,
                1 => TimeRange.Week,
                2 => TimeRange.Month,
                3 => TimeRange.All,
                _ => TimeRange.Day
            };

            // 全部排行时隐藏日期选择器
            if (DatePicker != null)
            {
                DatePicker.IsVisible = vm.SelectedTimeRange != TimeRange.All;
            }
        }
    }

    /// <summary>
    /// 切换到个人排行榜
    /// </summary>
    private void OnPersonalTabClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LeaderboardViewModel vm)
        {
            vm.IsPersonalTab = true;
        }
    }

    /// <summary>
    /// 切换到小组排行榜
    /// </summary>
    private void OnGroupTabClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LeaderboardViewModel vm)
        {
            vm.IsPersonalTab = false;
        }
    }

    /// <summary>
    /// 导出按钮点击事件，打开文件保存对话框
    /// </summary>
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LeaderboardViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "导出排行榜",
            DefaultExtension = "xlsx",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Excel文件") { Patterns = new[] { "*.xlsx" } },
                new Avalonia.Platform.Storage.FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
            }
        });

        if (file != null)
        {
            await vm.ExportCommand.ExecuteAsync(file.Path.LocalPath);
        }
    }
}
