using System;
using Avalonia.Controls;
using ClassIsScore.ViewModels;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 学生详情页面，作为对话框内容展示
/// </summary>
public partial class StudentProfilePage : UserControl
{
    public StudentProfilePage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 数据上下文变更时，触发数据加载
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StudentProfileViewModel vm)
        {
            // 设置头像首字
            AvatarInitialText.Text = string.IsNullOrEmpty(vm.StudentName) ? "?" : vm.StudentName[0].ToString();

            // 订阅关闭请求事件
            vm.CloseRequested -= OnCloseRequested;
            vm.CloseRequested += OnCloseRequested;

            // 订阅属性变更以更新头像首字
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// ViewModel属性变更处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StudentProfileViewModel.StudentName) && DataContext is StudentProfileViewModel vm)
        {
            AvatarInitialText.Text = string.IsNullOrEmpty(vm.StudentName) ? "?" : vm.StudentName[0].ToString();
        }
    }

    /// <summary>
    /// 关闭请求事件处理
    /// </summary>
    private void OnCloseRequested(object? sender, EventArgs e)
    {
        // 由父级对话框处理关闭
    }
}
