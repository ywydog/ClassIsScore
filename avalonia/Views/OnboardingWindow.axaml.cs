using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views;

/// <summary>
/// 引导窗口代码逻辑
/// </summary>
public partial class OnboardingWindow : Window
{
    /// <summary>
    /// 引导窗口构造函数
    /// </summary>
    public OnboardingWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 引导窗口构造函数（带 ViewModel）
    /// </summary>
    public OnboardingWindow(OnboardingViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.OnboardingCompleted += OnOnboardingCompleted;
    }

    /// <summary>
    /// 引导完成时的回调
    /// </summary>
    private void OnOnboardingCompleted()
    {
        Close();
    }

    /// <summary>
    /// 主题色点击事件
    /// </summary>
    private void OnAccentColorClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Background is ISolidColorBrush brush)
        {
            var viewModel = DataContext as OnboardingViewModel;
            viewModel?.ChangeAccentColorCommand.Execute(brush.Color.ToString());
        }
    }

    /// <summary>
    /// 从 Excel/CSV 导入点击事件
    /// </summary>
    private async void OnImportExcelClick(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var storageProvider = GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "选择学生名单文件",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("Excel 文件")
                    {
                        Patterns = ["*.xlsx", "*.xls"]
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV 文件")
                    {
                        Patterns = ["*.csv"]
                    }
                ]
            });

            // TODO: 实现文件导入逻辑，此处仅做文件选择演示
        }
        catch (Exception)
        {
            // 忽略文件选择取消等异常
        }
    }

    /// <summary>
    /// 手动添加点击事件 - 跳到下一步
    /// </summary>
    private void OnManualAddClick(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as OnboardingViewModel;
        viewModel?.NextCommand.Execute(null);
    }

    /// <summary>
    /// 跳过导入点击事件
    /// </summary>
    private void OnSkipImportClick(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as OnboardingViewModel;
        viewModel?.SkipCommand.Execute(null);
    }

    /// <summary>
    /// 窗口关闭时清理
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is OnboardingViewModel viewModel)
        {
            viewModel.OnboardingCompleted -= OnOnboardingCompleted;
        }
    }
}
