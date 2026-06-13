using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using ClassIsScore.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsScore.ViewModels;

/// <summary>
/// 关于页面 ViewModel
/// </summary>
public partial class AboutViewModel : ObservableObject
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [ObservableProperty]
    private string _appName = "ClassIsScore";

    /// <summary>
    /// 应用版本号
    /// </summary>
    [ObservableProperty]
    private string _appVersion = string.Empty;

    /// <summary>
    /// 版权信息
    /// </summary>
    [ObservableProperty]
    private string _copyright = "Copyright (c) 2024-2026 ClassIsScore Contributors";

    /// <summary>
    /// 应用描述
    /// </summary>
    [ObservableProperty]
    private string _description = "教室大屏多功能积分管理软件";

    /// <summary>
    /// 许可证类型
    /// </summary>
    [ObservableProperty]
    private string _license = "MIT License";

    /// <summary>
    /// 第三方库列表
    /// </summary>
    public ObservableCollection<ThirdPartyLibInfo> ThirdPartyLibs { get; } = [];

    /// <summary>
    /// 项目链接列表
    /// </summary>
    public ObservableCollection<ProjectLink> ProjectLinks { get; } = [];

    /// <summary>
    /// 诊断信息
    /// </summary>
    [ObservableProperty]
    private string _diagnosticInfo = string.Empty;

    /// <summary>
    /// MIT 许可证全文
    /// </summary>
    [ObservableProperty]
    private string _licenseText = string.Empty;

    /// <summary>
    /// 是否显示许可证全文
    /// </summary>
    [ObservableProperty]
    private bool _isLicenseExpanded;

    public AboutViewModel()
    {
        LoadAppVersion();
        LoadThirdPartyLibs();
        LoadProjectLinks();
        LoadDiagnosticInfo();
        LoadLicenseText();
    }

    /// <summary>
    /// 加载应用版本号
    /// </summary>
    private void LoadAppVersion()
    {
        AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "未知";
    }

    /// <summary>
    /// 加载第三方库列表
    /// </summary>
    private void LoadThirdPartyLibs()
    {
        ThirdPartyLibs.Clear();
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "Avalonia",
            Version = "11.3.13",
            License = "MIT",
            Url = "https://github.com/AvaloniaUI/Avalonia",
            Description = "跨平台 .NET UI 框架"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "FluentAvalonia",
            Version = "2.4.1",
            License = "MIT",
            Url = "https://github.com/amwx/FluentAvalonia",
            Description = "Avalonia 的 Fluent Design 控件库"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "CommunityToolkit.Mvvm",
            Version = "8.2.1",
            License = "MIT",
            Url = "https://github.com/CommunityToolkit/dotnet",
            Description = "MVVM 工具包，提供数据绑定和命令支持"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "Microsoft.Extensions.Hosting",
            Version = "8.0.1",
            License = "MIT",
            Url = "https://github.com/dotnet/runtime",
            Description = ".NET 通用主机，管理应用生命周期和依赖注入"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "ClosedXML",
            Version = "0.104.2",
            License = "MIT",
            Url = "https://github.com/ClosedXML/ClosedXML",
            Description = "Excel 文件操作库"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "SharpZipLib",
            Version = "1.4.2",
            License = "MIT",
            Url = "https://github.com/icsharpcode/SharpZipLib",
            Description = "压缩和解压缩库"
        });
        ThirdPartyLibs.Add(new ThirdPartyLibInfo
        {
            Name = "System.Text.Json",
            Version = "8.0.5",
            License = "MIT",
            Url = "https://github.com/dotnet/runtime",
            Description = "高性能 JSON 序列化和反序列化库"
        });
    }

    /// <summary>
    /// 加载项目链接列表
    /// </summary>
    private void LoadProjectLinks()
    {
        ProjectLinks.Clear();
        ProjectLinks.Add(new ProjectLink
        {
            Title = "GitHub 仓库",
            Url = "https://github.com/ywydog/ClassIsScore",
            IconGlyph = "\uE943" // Link icon
        });
        ProjectLinks.Add(new ProjectLink
        {
            Title = "问题反馈",
            Url = "https://github.com/ywydog/ClassIsScore/issues",
            IconGlyph = "\uE9D9" // Bug icon
        });
        ProjectLinks.Add(new ProjectLink
        {
            Title = "开发文档",
            Url = "https://github.com/ywydog/ClassIsScore/blob/main/docs/DEVELOPMENT.md",
            IconGlyph = "\uE8F1" // Document icon
        });
        ProjectLinks.Add(new ProjectLink
        {
            Title = "URI 调用文档",
            Url = "https://github.com/ywydog/ClassIsScore/blob/main/docs/URI.md",
            IconGlyph = "\uE71B" // Remote icon
        });
    }

    /// <summary>
    /// 加载诊断信息
    /// </summary>
    private void LoadDiagnosticInfo()
    {
        var version = AppVersion;
        var osDescription = RuntimeInformation.OSDescription;
        var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
        var frameworkDescription = RuntimeInformation.FrameworkDescription;
        var architecture = RuntimeInformation.ProcessArchitecture.ToString();
        var machineName = Environment.MachineName;

        DiagnosticInfo = $"应用版本: ClassIsScore v{version}\n" +
                         $"操作系统: {osDescription}\n" +
                         $"运行时: {frameworkDescription}\n" +
                         $"运行时标识: {runtimeIdentifier}\n" +
                         $"处理器架构: {architecture}\n" +
                         $"计算机名: {machineName}";
    }

    /// <summary>
    /// 加载 MIT 许可证全文
    /// </summary>
    private void LoadLicenseText()
    {
        LicenseText = @"MIT License

Copyright (c) 2024-2026 ClassIsScore Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.";
    }

    /// <summary>
    /// 打开链接命令
    /// </summary>
    [RelayCommand]
    private void OpenLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch
        {
            // 忽略打开链接失败
        }
    }

    /// <summary>
    /// 复制诊断信息命令
    /// </summary>
    [RelayCommand]
    private async Task CopyDiagnosticInfoAsync()
    {
        try
        {
            var app = Avalonia.Application.Current;
            if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(DiagnosticInfo);
                }
            }
        }
        catch
        {
            // 忽略复制失败
        }
    }

    /// <summary>
    /// 显示/隐藏许可证全文命令
    /// </summary>
    [RelayCommand]
    private void ShowLicense()
    {
        IsLicenseExpanded = !IsLicenseExpanded;
    }
}
