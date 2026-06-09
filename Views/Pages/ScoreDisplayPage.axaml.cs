using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsScore.Controls;
using ClassIsScore.Models;
using ClassIsScore.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 积分显示主页面
/// </summary>
public partial class ScoreDisplayPage : UserControl
{
    /// <summary>
    /// 当前显示模式
    /// </summary>
    private DisplayMode _currentMode = DisplayMode.Card;

    public ScoreDisplayPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 数据上下文变更时，加载数据并刷新显示
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.Students.CollectionChanged += OnStudentsCollectionChanged;
            LoadAndRefreshAsync();
        }
    }

    /// <summary>
    /// ViewModel 属性变更处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScoreDisplayViewModel.DisplayMode))
        {
            if (DataContext is ScoreDisplayViewModel vm)
            {
                SwitchDisplayMode(vm.DisplayMode);
            }
        }
    }

    /// <summary>
    /// 学生集合变更时刷新显示
    /// </summary>
    private void OnStudentsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshDisplay();
    }

    /// <summary>
    /// 异步加载数据并刷新
    /// </summary>
    private async void LoadAndRefreshAsync()
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            await vm.LoadStudentsCommand.ExecuteAsync(null);
            RefreshDisplay();
        }
    }

    /// <summary>
    /// 刷新按钮点击
    /// </summary>
    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            await vm.LoadStudentsCommand.ExecuteAsync(null);
            RefreshDisplay();
        }
    }

    /// <summary>
    /// 卡片模式按钮点击
    /// </summary>
    private void OnCardModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.SwitchToCardModeCommand.Execute(null);
        }
    }

    /// <summary>
    /// 圆形模式按钮点击
    /// </summary>
    private void OnCircleModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.SwitchToCircleModeCommand.Execute(null);
        }
    }

    /// <summary>
    /// 宠物模式按钮点击
    /// </summary>
    private void OnPetModeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.SwitchToPetModeCommand.Execute(null);
        }
    }

    /// <summary>
    /// 切换显示模式
    /// </summary>
    private void SwitchDisplayMode(DisplayMode mode)
    {
        _currentMode = mode;

        // 切换可见性
        CardGrid.IsVisible = mode == DisplayMode.Card;
        CircleGrid.IsVisible = mode == DisplayMode.Circle;
        PetGrid.IsVisible = mode == DisplayMode.Pet;

        // 更新按钮高亮状态
        UpdateModeButtonStyles(mode);

        // 刷新显示内容
        RefreshDisplay();
    }

    /// <summary>
    /// 更新模式按钮样式，高亮当前选中的模式
    /// </summary>
    private void UpdateModeButtonStyles(DisplayMode mode)
    {
        // 重置所有按钮样式
        CardModeButton.Classes.Clear();
        CircleModeButton.Classes.Clear();
        PetModeButton.Classes.Clear();

        // 高亮当前选中模式按钮
        switch (mode)
        {
            case DisplayMode.Card:
                CardModeButton.Classes.Add("accent");
                break;
            case DisplayMode.Circle:
                CircleModeButton.Classes.Add("accent");
                break;
            case DisplayMode.Pet:
                PetModeButton.Classes.Add("accent");
                break;
        }
    }

    /// <summary>
    /// 刷新显示内容，根据当前模式填充对应控件
    /// </summary>
    private void RefreshDisplay()
    {
        if (DataContext is not ScoreDisplayViewModel vm) return;

        switch (_currentMode)
        {
            case DisplayMode.Card:
                RefreshCardGrid(vm);
                break;
            case DisplayMode.Circle:
                RefreshCircleGrid(vm);
                break;
            case DisplayMode.Pet:
                RefreshPetGrid(vm);
                break;
        }
    }

    /// <summary>
    /// 刷新卡片模式网格
    /// </summary>
    private void RefreshCardGrid(ScoreDisplayViewModel vm)
    {
        CardGrid.Children.Clear();
        foreach (var item in vm.Students)
        {
            var control = new StudentCardControl
            {
                DataContext = item
            };
            control.StudentClicked += OnStudentClicked;
            CardGrid.Children.Add(control);
        }
    }

    /// <summary>
    /// 刷新圆形模式网格
    /// </summary>
    private void RefreshCircleGrid(ScoreDisplayViewModel vm)
    {
        CircleGrid.Children.Clear();
        foreach (var item in vm.Students)
        {
            var control = new StudentCircleControl
            {
                DataContext = item
            };
            control.StudentClicked += OnStudentClicked;
            CircleGrid.Children.Add(control);
        }
    }

    /// <summary>
    /// 刷新宠物模式网格
    /// </summary>
    private void RefreshPetGrid(ScoreDisplayViewModel vm)
    {
        PetGrid.Children.Clear();
        foreach (var item in vm.Students)
        {
            var control = new PetDisplayControl
            {
                DataContext = item,
                PetLevel = vm.DisplaySettings.PetLevel,
                PetExperience = vm.DisplaySettings.PetExperience,
                PetStyle = vm.DisplaySettings.PetStyle
            };
            control.StudentClicked += OnStudentClicked;
            PetGrid.Children.Add(control);
        }
    }

    /// <summary>
    /// 学生控件点击事件处理，弹出快捷加减分对话框
    /// </summary>
    private async void OnStudentClicked(object? sender, Student student)
    {
        if (DataContext is not ScoreDisplayViewModel vm) return;

        var item = vm.Students.FirstOrDefault(s => s.Id == student.Id);
        if (item == null) return;

        // 创建快捷加减分对话框
        var dialog = new ContentDialog
        {
            Title = $"{student.Name} - 快捷加减分",
            PrimaryButtonText = "加分",
            SecondaryButtonText = "减分",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"当前积分: {student.Score:F1}",
                        FontSize = 16,
                        FontWeight = Avalonia.Media.FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = $"加减分值: {vm.QuickScoreValue}"
                    }
                }
            }
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await vm.QuickAddScoreCommand.ExecuteAsync(item);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await vm.QuickSubtractScoreCommand.ExecuteAsync(item);
        }
    }
}
