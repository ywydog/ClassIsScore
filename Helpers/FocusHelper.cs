using Avalonia.Controls;

namespace ClassIsScore.Helpers;

/// <summary>
/// 无焦点辅助工具，用于设置窗口不获取焦点
/// </summary>
public static class FocusHelper
{
    /// <summary>
    /// 设置窗口为无焦点模式
    /// </summary>
    /// <param name="window">目标窗口</param>
    public static void SetNoFocus(Window window)
    {
        window.ShowActivated = false;
    }
}
