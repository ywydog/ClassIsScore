using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsScore.Models;
using ClassIsScore.ViewModels;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 学生管理页面代码逻辑
/// </summary>
public partial class StudentManagementPage : UserControl
{
    public StudentManagementPage()
    {
        InitializeComponent();

        if (DataContext is StudentManagementViewModel vm)
        {
            PopulatePetTypeComboBox(vm);
        }

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 对话框入场动画触发
    /// </summary>
    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 当DataContext的IsEditDialogOpen变为true时，触发对话框入场动画
        if (change.Property == DataContextProperty && change.NewValue is StudentManagementViewModel vm)
        {
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StudentManagementViewModel.IsEditDialogOpen) && vm.IsEditDialogOpen)
                {
                    // 延迟一帧后设置目标值，触发Transitions
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var maskBorder = this.FindControl<Border>("EditDialogBorder");
                        if (maskBorder != null)
                        {
                            maskBorder.Opacity = 1;
                            maskBorder.RenderTransform = new Avalonia.Media.ScaleTransform(1, 1);
                        }
                    });
                }
            };
        }
    }

    /// <summary>
    /// 数据上下文变更时填充宠物类型下拉框
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StudentManagementViewModel vm)
        {
            PopulatePetTypeComboBox(vm);
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>
    /// ViewModel属性变更处理
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is StudentManagementViewModel vm)
        {
            if (e.PropertyName == nameof(StudentManagementViewModel.IsEditDialogOpen) && vm.IsEditDialogOpen)
            {
                PopulatePetTypeComboBox(vm);

                // 设置当前选中的宠物类型
                var petType = vm.EditingPetType;
                if (string.IsNullOrEmpty(petType))
                {
                    PetTypeComboBox.SelectedIndex = 0;
                }
                else
                {
                    for (int i = 1; i < PetTypeComboBox.Items.Count; i++)
                    {
                        if (PetTypeComboBox.Items.ElementAt(i) is ComboBoxItem item && item.Tag as string == petType)
                        {
                            PetTypeComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 填充宠物类型下拉框
    /// </summary>
    private void PopulatePetTypeComboBox(StudentManagementViewModel vm)
    {
        PetTypeComboBox.Items.Clear();

        // 未领养选项
        PetTypeComboBox.Items.Add(new ComboBoxItem { Content = "未领养", Tag = "" });

        // 普通宠物
        foreach (var pet in vm.PetTypes.Where(p => p.Category == PetCategory.Normal))
        {
            PetTypeComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"{pet.Emoji} {pet.Name}",
                Tag = pet.Id
            });
        }

        // 神兽
        foreach (var pet in vm.PetTypes.Where(p => p.Category == PetCategory.Mythical))
        {
            PetTypeComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"{pet.Emoji} {pet.Name}",
                Tag = pet.Id
            });
        }

        PetTypeComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// 页面加载完成时，自动加载学生数据
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StudentManagementViewModel vm)
        {
            vm.LoadStudentsCommand.Execute(null);
            vm.Students.CollectionChanged += (_, _) => UpdateStatusText();
        }
    }

    /// <summary>
    /// 更新底部状态栏文本
    /// </summary>
    private void UpdateStatusText()
    {
        if (DataContext is StudentManagementViewModel vm)
        {
            StatusText.Text = $"共 {vm.Students.Count} 名学生";
        }
    }

    /// <summary>
    /// 导入Excel按钮点击事件，打开文件选择对话框
    /// </summary>
    private async void OnImportExcelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentManagementViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "选择Excel文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Excel文件") { Patterns = new[] { "*.xlsx", "*.xls" } },
                new Avalonia.Platform.Storage.FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            await vm.ImportExcelCommand.ExecuteAsync(files[0].Path.LocalPath);
        }
    }

    /// <summary>
    /// 导入CSV按钮点击事件，打开文件选择对话框
    /// </summary>
    private async void OnImportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentManagementViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "选择CSV文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("CSV文件") { Patterns = new[] { "*.csv" } },
                new Avalonia.Platform.Storage.FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            await vm.ImportCsvCommand.ExecuteAsync(files[0].Path.LocalPath);
        }
    }

    /// <summary>
    /// 学生列表双击事件，编辑选中学生
    /// </summary>
    private void OnStudentDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is StudentManagementViewModel vm && vm.SelectedStudent != null)
        {
            vm.EditStudentCommand.Execute(vm.SelectedStudent);
        }
    }

    /// <summary>
    /// 编辑对话框取消按钮
    /// </summary>
    private void OnEditDialogCancel(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StudentManagementViewModel vm)
        {
            vm.IsEditDialogOpen = false;
        }
    }

    /// <summary>
    /// 宠物类型选择变更事件处理
    /// </summary>
    private void OnPetTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is StudentManagementViewModel vm && PetTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            var petTypeId = item.Tag as string;
            vm.EditingPetType = string.IsNullOrEmpty(petTypeId) ? null : petTypeId;
        }
    }
}
