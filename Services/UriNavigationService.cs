using System;
using System.Collections.Generic;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// URI 导航服务实现，支持通过 URI 协议在应用内进行页面导航
/// </summary>
public class UriNavigationService : IUriNavigationService
{
    private readonly ILogger<UriNavigationService> _logger;
    private readonly Dictionary<string, Action<UriNavigationEventArgs>> _appHandlers = new();

    public UriNavigationService(ILogger<UriNavigationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 导航到指定 URI
    /// </summary>
    /// <param name="uri">目标 URI</param>
    public void Navigate(Uri uri)
    {
        _logger.LogInformation("导航到: {Uri}", uri);

        if (uri.Scheme != IUriNavigationService.UriScheme)
        {
            // 非 classisscore 协议，尝试使用系统默认浏览器打开
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.ToString())
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "无法打开外部链接: {Uri}", uri);
            }
            return;
        }

        // 处理应用内部导航
        var path = uri.Host + uri.AbsolutePath;
        if (_appHandlers.TryGetValue(path, out var handler))
        {
            handler.Invoke(new UriNavigationEventArgs { Uri = uri });
        }
        else
        {
            _logger.LogWarning("未注册的导航路径: {Path}", path);
        }
    }

    /// <summary>
    /// 导航到指定 URI，但在抛出异常时自动捕获
    /// </summary>
    /// <param name="uri">目标 URI</param>
    /// <param name="exception">导航时产生的异常（如有）</param>
    public void NavigateWrapped(Uri uri, out Exception? exception)
    {
        exception = null;
        try
        {
            Navigate(uri);
        }
        catch (Exception ex)
        {
            exception = ex;
            _logger.LogError(ex, "导航到 {Uri} 时发生错误", uri);
        }
    }

    /// <summary>
    /// 注册应用导航路径处理程序
    /// </summary>
    /// <param name="path">导航路径</param>
    /// <param name="onNavigated">导航回调</param>
    public void HandleAppNavigation(string path, Action<UriNavigationEventArgs> onNavigated)
    {
        _appHandlers[path] = onNavigated;
        _logger.LogDebug("注册应用导航路径: {Path}", path);
    }
}
