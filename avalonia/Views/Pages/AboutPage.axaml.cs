using Avalonia.Controls;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 关于页面代码逻辑
/// </summary>
public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 带 ViewModel 的构造函数（用于依赖注入）
    /// </summary>
    public AboutPage(AboutViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
