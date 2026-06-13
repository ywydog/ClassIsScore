using System;
using System.Threading.Tasks;
using ClassIsScore.Models;
using ClassIsScore.Services;
using ClassIsScore.Services.Abstractions;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Controls;

/// <summary>
/// 管理员验证对话框代码逻辑
/// </summary>
public partial class AdminVerifyDialog : ContentDialog
{
    private readonly IAdminService? _adminService;
    private readonly ILogger? _logger;

    /// <summary>
    /// 验证是否通过
    /// </summary>
    public bool IsVerified { get; private set; }

    public AdminVerifyDialog()
    {
        InitializeComponent();
        _adminService = AppHost.Instance?.GetService<IAdminService>();
        _logger = AppHost.Instance?.TryGetService<ILogger<AdminVerifyDialog>>();

        SetupVerifyMethod();
    }

    /// <summary>
    /// 带服务的构造函数
    /// </summary>
    public AdminVerifyDialog(IAdminService adminService, ILogger<AdminVerifyDialog> logger) : this()
    {
        _adminService = adminService;
        _logger = logger;
        SetupVerifyMethod();
    }

    /// <summary>
    /// 根据当前验证方式设置对话框显示内容
    /// </summary>
    private void SetupVerifyMethod()
    {
        if (_adminService == null) return;

        var settings = _adminService.Settings;
        switch (settings.VerificationMethod)
        {
            case VerificationMethod.Password:
                PasswordBox.IsVisible = true;
                UsbVerifyPanel.IsVisible = false;
                FaceVerifyPanel.IsVisible = false;
                break;

            case VerificationMethod.Usb:
                PasswordBox.IsVisible = false;
                UsbVerifyPanel.IsVisible = true;
                FaceVerifyPanel.IsVisible = false;
                break;

            case VerificationMethod.Face:
                PasswordBox.IsVisible = false;
                UsbVerifyPanel.IsVisible = false;
                FaceVerifyPanel.IsVisible = true;
                break;
        }
    }

    /// <summary>
    /// 主按钮点击事件处理
    /// </summary>
    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 获取延迟，用于异步验证
        var deferral = args.GetDeferral();

        try
        {
            if (_adminService == null)
            {
                ShowError("管理员验证服务不可用");
                args.Cancel = true;
                return;
            }

            var settings = _adminService.Settings;
            bool result;

            switch (settings.VerificationMethod)
            {
                case VerificationMethod.Password:
                    var password = PasswordBox.Text ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        ShowError("请输入密码");
                        args.Cancel = true;
                        return;
                    }
                    result = await _adminService.VerifyPasswordAsync(password);
                    break;

                case VerificationMethod.Usb:
                    result = await _adminService.VerifyUsbAsync();
                    break;

                case VerificationMethod.Face:
                    try
                    {
                        result = await _adminService.VerifyFaceAsync();
                    }
                    catch (NotImplementedException)
                    {
                        ShowError("人脸验证功能尚未实现");
                        args.Cancel = true;
                        return;
                    }
                    break;

                default:
                    ShowError("未知的验证方式");
                    args.Cancel = true;
                    return;
            }

            if (result)
            {
                IsVerified = true;
                _logger?.LogInformation("管理员验证通过");
            }
            else
            {
                IsVerified = false;
                ShowError("验证失败，请重试");
                args.Cancel = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "管理员验证过程出错");
            ShowError($"验证出错: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    /// <summary>
    /// 显示错误提示
    /// </summary>
    /// <param name="message">错误消息</param>
    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
