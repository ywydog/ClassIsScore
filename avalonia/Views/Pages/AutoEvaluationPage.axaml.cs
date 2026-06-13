using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 自动评价页面代码逻辑
/// </summary>
public partial class AutoEvaluationPage : UserControl
{
    public AutoEvaluationPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 页面加载完成时，自动加载数据
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is AutoEvaluationViewModel vm)
        {
            vm.LoadConfigsCommand.Execute(null);
            vm.LoadTargetDataCommand.Execute(null);
        }
    }
}
