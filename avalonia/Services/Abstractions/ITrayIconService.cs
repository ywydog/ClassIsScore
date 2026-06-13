namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 系统托盘图标服务接口
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    void Initialize();

    /// <summary>
    /// 显示气泡通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    void ShowNotification(string title, string message);

    /// <summary>
    /// 托盘图标是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// 托盘图标是否已初始化
    /// </summary>
    bool IsInitialized { get; }
}
