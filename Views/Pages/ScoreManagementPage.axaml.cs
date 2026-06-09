using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.Models;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 积分管理页面代码逻辑
/// </summary>
public partial class ScoreManagementPage : UserControl
{
    public ScoreManagementPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 页面加载完成时，自动加载数据
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ScoreManagementViewModel vm)
        {
            vm.LoadStudentsCommand.Execute(null);
            vm.LoadEvaluationItemsCommand.Execute(null);
            vm.LoadHistoryCommand.Execute(null);
        }
    }
}
