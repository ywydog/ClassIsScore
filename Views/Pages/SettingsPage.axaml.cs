using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 设置页面代码逻辑
/// </summary>
public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 带 ViewModel 的构造函数（用于依赖注入）
    /// </summary>
    public SettingsPage(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;

        // 订阅文件选择请求事件
        viewModel.ExportDataRequested += OnExportDataRequested;
        viewModel.ImportDataRequested += OnImportDataRequested;
        viewModel.ExportStudentsRequested += OnExportStudentsRequested;
        viewModel.ImportStudentsRequested += OnImportStudentsRequested;
    }

    /// <summary>
    /// 主题色按钮点击事件
    /// </summary>
    private void OnAccentColorClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string colorHex)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.ChangeAccentColorCommand.Execute(colorHex);
            }
        }
    }

    /// <summary>
    /// 深色模式切换事件
    /// </summary>
    private void OnDarkModeToggled(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.ToggleDarkModeCommand.Execute(null);
        }
    }

    /// <summary>
    /// 处理导出所有数据文件选择请求
    /// </summary>
    private async Task<string?> OnExportDataRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var fileName = $"ClassIsScore_Data_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出所有数据",
            SuggestedFileName = fileName,
            FileTypeChoices =
            [
                new FilePickerFileType("ZIP 压缩文件")
                {
                    Patterns = ["*.zip"]
                }
            ]
        });

        return result?.TryGetLocalPath();
    }

    /// <summary>
    /// 处理导入数据文件选择请求
    /// </summary>
    private async Task<string?> OnImportDataRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入数据",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("ZIP 压缩文件")
                {
                    Patterns = ["*.zip"]
                }
            ]
        });

        return results.Count > 0 ? results[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// 处理导出学生数据文件选择请求
    /// </summary>
    private async Task<string?> OnExportStudentsRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var fileName = $"学生数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出学生数据",
            SuggestedFileName = fileName,
            FileTypeChoices =
            [
                new FilePickerFileType("Excel 文件")
                {
                    Patterns = ["*.xlsx"]
                }
            ]
        });

        return result?.TryGetLocalPath();
    }

    /// <summary>
    /// 处理导入学生数据文件选择请求
    /// </summary>
    private async Task<string?> OnImportStudentsRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入学生数据",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("支持的学生数据文件")
                {
                    Patterns = ["*.xlsx", "*.xls", "*.csv", "*.zip"]
                }
            ]
        });

        return results.Count > 0 ? results[0].TryGetLocalPath() : null;
    }
}
