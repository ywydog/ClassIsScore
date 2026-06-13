using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        else if (e.PropertyName == nameof(ScoreDisplayViewModel.ShowPetSelection))
        {
            if (DataContext is ScoreDisplayViewModel vm)
            {
                PetSelectionOverlay.IsVisible = vm.ShowPetSelection;
                if (vm.ShowPetSelection)
                {
                    PopulatePetSelectionPanel(vm);
                }
            }
        }
        else if (e.PropertyName == nameof(ScoreDisplayViewModel.IsMultiSelectMode))
        {
            UpdateMultiSelectButtonStyle();
            RefreshDisplay();
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
    /// 多选按钮点击
    /// </summary>
    private void OnMultiSelectClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.ToggleMultiSelectCommand.Execute(null);
        }
    }

    /// <summary>
    /// 更新多选按钮样式
    /// </summary>
    private void UpdateMultiSelectButtonStyle()
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            MultiSelectButton.Classes.Clear();
            if (vm.IsMultiSelectMode)
            {
                MultiSelectButton.Classes.Add("accent");
            }
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
        // 重置所有按钮：移除accent，确保保留ModeButton
        CardModeButton.Classes.Remove("accent");
        CircleModeButton.Classes.Remove("accent");
        PetModeButton.Classes.Remove("accent");

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

            // 多选模式下设置选中视觉状态
            if (vm.IsMultiSelectMode && item.IsSelected)
            {
                ApplySelectionHighlight(control);
            }
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

            // 多选模式下设置选中视觉状态
            if (vm.IsMultiSelectMode && item.IsSelected)
            {
                ApplySelectionHighlight(control);
            }
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
                DataContext = item
            };
            control.StudentClicked += OnStudentClicked;
            PetGrid.Children.Add(control);

            // 多选模式下设置选中视觉状态
            if (vm.IsMultiSelectMode && item.IsSelected)
            {
                ApplySelectionHighlight(control);
            }
        }
    }

    /// <summary>
    /// 为控件应用选中高亮样式
    /// </summary>
    private void ApplySelectionHighlight(UserControl control)
    {
        control.Opacity = 0.6;
    }

    /// <summary>
    /// 移除控件选中高亮样式
    /// </summary>
    private void RemoveSelectionHighlight(UserControl control)
    {
        control.Opacity = 1.0;
    }

    /// <summary>
    /// 学生控件点击事件处理
    /// </summary>
    private async void OnStudentClicked(object? sender, Student student)
    {
        if (DataContext is not ScoreDisplayViewModel vm) return;

        var item = vm.Students.FirstOrDefault(s => s.Id == student.Id);
        if (item == null) return;

        // 多选模式下，切换选中状态
        if (vm.IsMultiSelectMode)
        {
            vm.ToggleStudentSelection(item);

            // 更新视觉状态
            if (sender is UserControl control)
            {
                if (item.IsSelected)
                {
                    ApplySelectionHighlight(control);
                }
                else
                {
                    RemoveSelectionHighlight(control);
                }
            }
            return;
        }

        // 宠物模式下，未领养宠物时弹出宠物选择对话框
        if (_currentMode == DisplayMode.Pet && !item.HasPet)
        {
            vm.PetSelectingStudent = item;
            PetSelectionTitle.Text = $"为 {student.Name} 选择宠物";
            vm.ShowPetSelection = true;
            return;
        }

        // 创建快捷加减分对话框
        var viewProfileButton = new Button
        {
            Content = "查看详情",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Padding = new Thickness(12, 4),
            Tag = student.Id
        };
        viewProfileButton.Click += OnViewProfileFromDialog;

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
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = $"加减分值: {vm.QuickScoreValue}"
                    },
                    viewProfileButton
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

    /// <summary>
    /// 填充宠物选择面板
    /// </summary>
    private void PopulatePetSelectionPanel(ScoreDisplayViewModel vm)
    {
        NormalPetPanel.Children.Clear();
        MythicalPetPanel.Children.Clear();

        foreach (var petType in vm.PetTypes)
        {
            var button = new Button
            {
                Margin = new Avalonia.Thickness(4),
                Padding = new Avalonia.Thickness(8, 4),
                Tag = petType.Id,
                Content = new StackPanel
                {
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = petType.Emoji,
                            FontSize = 24,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = petType.Name,
                            FontSize = 11,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };

            button.Click += OnPetTypeSelected;

            if (petType.Category == PetCategory.Normal)
            {
                NormalPetPanel.Children.Add(button);
            }
            else
            {
                MythicalPetPanel.Children.Add(button);
            }
        }
    }

    /// <summary>
    /// 宠物类型选择事件处理
    /// </summary>
    private async void OnPetTypeSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string petTypeId)
        {
            if (DataContext is ScoreDisplayViewModel vm)
            {
                await vm.SelectPetCommand.ExecuteAsync(petTypeId);
            }
        }
    }

    /// <summary>
    /// 宠物选择对话框取消按钮
    /// </summary>
    private void OnPetSelectionCancel(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScoreDisplayViewModel vm)
        {
            vm.ShowPetSelection = false;
        }
    }

    /// <summary>
    /// 对话框中"查看详情"按钮点击事件
    /// </summary>
    private async void OnViewProfileFromDialog(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid studentId)
        {
            await ShowStudentProfileAsync(studentId);
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
}
