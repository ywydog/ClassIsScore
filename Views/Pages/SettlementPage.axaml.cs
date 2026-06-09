using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 结算页面代码逻辑
/// </summary>
public partial class SettlementPage : UserControl
{
    public SettlementPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 页面加载完成时，自动加载结算历史
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SettlementViewModel vm)
        {
            vm.LoadSettlementHistoryCommand.Execute(null);
        }
    }
}
