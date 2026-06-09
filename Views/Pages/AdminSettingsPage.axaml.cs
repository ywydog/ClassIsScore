using System;
using Avalonia.Controls;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using ClassIsScore.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Views.Pages;

/// <summary>
/// 管理员设置页面代码逻辑
/// </summary>
public partial class AdminSettingsPage : UserControl
{
    public AdminSettingsPage()
    {
        InitializeComponent();

        // 通过 AppHost 获取服务并创建 ViewModel
        var adminService = AppHost.Instance?.GetService<IAdminService>();
        var logger = AppHost.Instance?.GetService<ILogger<AdminSettingsViewModel>>();

        if (adminService != null && logger != null)
        {
            DataContext = new AdminSettingsViewModel(adminService, logger);
        }
    }

    /// <summary>
    /// 带 ViewModel 的构造函数（用于依赖注入）
    /// </summary>
    public AdminSettingsPage(AdminSettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
