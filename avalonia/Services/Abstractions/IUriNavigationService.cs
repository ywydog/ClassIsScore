using System;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// URI 导航服务接口，用于在应用内部通过 URI 进行页面导航
/// </summary>
public interface IUriNavigationService
{
    /// <summary>
    /// 应用内部 URI 协议名
    /// </summary>
    public static string UriScheme { get; } = "classisscore";

    /// <summary>
    /// 应用导航主机名
    /// </summary>
    public static string UriDomainApp { get; } = "app";

    /// <summary>
    /// 导航到指定 URI
    /// </summary>
    /// <param name="uri">目标 URI</param>
    void Navigate(Uri uri);

    /// <summary>
    /// 导航到指定 URI，但在抛出异常时自动捕获
    /// </summary>
    /// <param name="uri">目标 URI</param>
    /// <param name="exception">导航时产生的异常（如有）</param>
    void NavigateWrapped(Uri uri, out Exception? exception);

    /// <summary>
    /// 注册应用导航路径处理程序
    /// </summary>
    /// <param name="path">导航路径</param>
    /// <param name="onNavigated">导航回调</param>
    void HandleAppNavigation(string path, Action<UriNavigationEventArgs> onNavigated);
}

/// <summary>
/// URI 导航事件参数
/// </summary>
public class UriNavigationEventArgs : EventArgs
{
    /// <summary>
    /// 导航目标 URI
    /// </summary>
    public Uri Uri { get; init; } = new Uri("about:blank");
}
