using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using ClassIsScore.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 设置页面代码逻辑
/// </summary>
public partial class SettingsPage : UserControl
{
    private Timer? _statusTimer;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>
    /// 页面加载完成后播放入场动画
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var cards = this.FindControl<TabControl>("SettingsTabControl");
        if (cards == null) return;

        // 为当前可见Tab的卡片播放入场动画
        PlayEntranceAnimation(cards);
    }

    /// <summary>
    /// Tab 切换时播放入场动画
    /// </summary>
    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            PlayEntranceAnimation(tabControl);
        }
    }

    /// <summary>
    /// 为当前Tab的设置卡片播放入场动画（依次淡入+上滑）
    /// </summary>
    private static void PlayEntranceAnimation(TabControl tabControl)
    {
        if (tabControl.SelectedItem is not TabItem selectedTab) return;
        if (selectedTab.Content is not ScrollViewer sv) return;
        if (sv.Content is not StackPanel panel) return;

        var index = 0;
        foreach (var child in panel.Children)
        {
            if (child is Border border)
            {
                // 初始状态：透明+下移12px
                border.Opacity = 0;
                border.RenderTransform = new TranslateTransform(0, 12);

                var delay = index * 30;

                // 淡入动画
                var opacityAnim = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(250),
                    Delay = TimeSpan.FromMilliseconds(delay),
                    Easing = new Avalonia.Animation.Easings.CubicEaseOut(),
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters = { new Setter(OpacityProperty, 0d) }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters = { new Setter(OpacityProperty, 1d) }
                        }
                    }
                };

                // 上滑动画
                var slideAnim = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    Delay = TimeSpan.FromMilliseconds(delay),
                    Easing = new Avalonia.Animation.Easings.CubicEaseOut(),
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters = { new Setter(TranslateTransform.YProperty, 12d) }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters = { new Setter(TranslateTransform.YProperty, 0d) }
                        }
                    }
                };

                opacityAnim.RunAsync(border);
                slideAnim.RunAsync(border);
                index++;
            }
        }
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
        viewModel.ImportThemeRequested += OnImportThemeRequested;
        viewModel.DeleteThemeRequested += OnDeleteThemeRequested;

        // 订阅状态消息变更，自动3秒后清除
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage))
            {
                _statusTimer?.Stop();
                _statusTimer?.Dispose();
                _statusTimer = new Timer(3000) { AutoReset = false };
                _statusTimer.Elapsed += (_, _) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (DataContext is SettingsViewModel vm)
                            vm.StatusMessage = string.Empty;
                    });
                };
                _statusTimer.Start();
            }
        };
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
    /// 悬浮窗主题色按钮点击事件
    /// </summary>
    private void OnFloatingAccentColorClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string colorHex)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.FloatingWindowAccentColor = colorHex;
            }
        }
    }

    /// <summary>
    /// 插件启用/禁用切换事件
    /// </summary>
    private void OnPluginToggled(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.Tag is string pluginId)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.TogglePluginCommand.Execute((pluginId, toggle.IsChecked == true));
            }
        }
    }

    /// <summary>
    /// 主题启用/禁用切换事件
    /// </summary>
    private void OnThemeToggled(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.Tag is string themeId)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.ToggleThemeCommand.Execute((themeId, toggle.IsChecked == true));
            }
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

    /// <summary>
    /// 处理导入主题文件选择请求
    /// </summary>
    private async Task<string?> OnImportThemeRequested()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var storageProvider = topLevel.StorageProvider;

        var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入自定义主题",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("ClassIsScore 主题包")
                {
                    Patterns = ["*.cisui"]
                }
            ]
        });

        return results.Count > 0 ? results[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// 删除主题按钮点击事件
    /// </summary>
    private async void OnDeleteThemeClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string themeId) return;

        // 确认对话框
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window window)
        {
            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = "确定要删除此主题吗？此操作不可撤销。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync(window);
            if (result == ContentDialogResult.Primary && DataContext is SettingsViewModel vm)
            {
                await vm.DeleteThemeAsync(themeId);
            }
        }
        else if (DataContext is SettingsViewModel vm)
        {
            await vm.DeleteThemeAsync(themeId);
        }
    }

    /// <summary>
    /// 处理删除主题请求
    /// </summary>
    private async Task OnDeleteThemeRequested(string themeId)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.DeleteThemeAsync(themeId);
        }
    }
}
