using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace ClassIsScore.Helpers;

/// <summary>
/// 无焦点辅助工具，用于设置窗口不获取焦点
/// 通过P/Invoke在Windows平台注入WS_EX_NOACTIVATE样式实现真正的无焦点窗口
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

        // Windows平台：通过P/Invoke设置WS_EX_NOACTIVATE
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var handle = window.TryGetPlatformHandle();
                if (handle != null)
                {
                    var hwnd = handle.Handle;
                    var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
                }
            }
            catch
            {
                // P/Invoke失败时降级为仅ShowActivated=false
            }
        }
    }

    // Windows P/Invoke 声明

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);
    }

    private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
    }
}
