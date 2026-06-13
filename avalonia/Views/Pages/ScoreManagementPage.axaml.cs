using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsScore.Models;
using ClassIsScore.ViewModels;
using FluentAvalonia.UI.Controls;

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

            // 监听多选模式变更以更新列表选择模式
            vm.PropertyChanged += OnViewModelPropertyChanged;

            // 注册导入文件选择事件
            vm.ImportFileRequested += OnImportFileRequested;
        }

        // 监听列表选择变更
        StudentListBox.SelectionChanged += OnStudentListBoxSelectionChanged;

        // 监听查看详情按钮点击
        ViewProfileButton.Click += OnViewProfileClick;
    }

    /// <summary>
    /// 查看详情按钮点击事件
    /// </summary>
    private async void OnViewProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreManagementViewModel vm && vm.SelectedStudent != null)
        {
            await ShowStudentProfileAsync(vm.SelectedStudent.Id);
        }
    }

    /// <summary>
    /// 显示学生详情对话框
    /// </summary>
    private async Task ShowStudentProfileAsync(Guid studentId)
    {
        var appHost = Services.AppHost.Instance;
        if (appHost == null) return;

        var profileViewModel = appHost.GetService<StudentProfileViewModel>();
        var profilePage = appHost.GetService<StudentProfilePage>();

        if (profileViewModel == null || profilePage == null) return;

        profilePage.DataContext = profileViewModel;

        // 加载学生数据
        await profileViewModel.LoadStudentCommand.ExecuteAsync(studentId);
        await profileViewModel.LoadScoreHistoryCommand.ExecuteAsync(null);
        await profileViewModel.CalculateTrendCommand.ExecuteAsync(null);

        var profileDialog = new ContentDialog
        {
            Title = $"学生详情 - {profileViewModel.StudentName}",
            CloseButtonText = "关闭",
            Content = profilePage,
            MinWidth = 640
        };

        await profileDialog.ShowAsync();
    }

    /// <summary>
    /// ViewModel属性变更处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScoreManagementViewModel.IsMultiSelectMode))
        {
            if (DataContext is ScoreManagementViewModel vm)
            {
                if (vm.IsMultiSelectMode)
                {
                    // 切换到多选模式
                    StudentListBox.SelectionMode = SelectionMode.Multiple;
                    StudentListBox.SelectedItem = null;
                }
                else
                {
                    // 切换回单选模式
                    StudentListBox.SelectionMode = SelectionMode.Single;
                    StudentListBox.SelectedItems!.Clear();
                }
            }
        }
    }

    /// <summary>
    /// 学生列表选择变更处理，同步多选集合
    /// </summary>
    private void OnStudentListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ScoreManagementViewModel vm || !vm.IsMultiSelectMode) return;

        // 同步选中项到ViewModel的SelectedStudents集合
        foreach (var item in e.AddedItems)
        {
            if (item is Student student && !vm.SelectedStudents.Contains(student))
            {
                vm.SelectedStudents.Add(student);
            }
        }

        foreach (var item in e.RemovedItems)
        {
            if (item is Student student)
            {
                vm.SelectedStudents.Remove(student);
            }
        }
    }

    /// <summary>
    /// 导入文件选择事件处理
    /// </summary>
    private async Task<string?> OnImportFileRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择评价导入文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("表格文件")
                {
                    Patterns = new[] { "*.xlsx", "*.xls", "*.csv" }
                },
                new FilePickerFileType("Excel 文件")
                {
                    Patterns = new[] { "*.xlsx", "*.xls" }
                },
                new FilePickerFileType("CSV 文件")
                {
                    Patterns = new[] { "*.csv" }
                }
            }
        });

        if (files.Count > 0)
        {
            return files[0].TryGetLocalPath();
        }

        return null;
    }
}
