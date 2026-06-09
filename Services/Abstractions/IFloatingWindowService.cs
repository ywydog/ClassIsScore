using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 悬浮窗服务接口
/// </summary>
public interface IFloatingWindowService
{
    /// <summary>
    /// 显示悬浮窗
    /// </summary>
    void Show();

    /// <summary>
    /// 隐藏悬浮窗
    /// </summary>
    void Hide();

    /// <summary>
    /// 切换悬浮窗显示状态
    /// </summary>
    void Toggle();

    /// <summary>
    /// 悬浮窗是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// 获取当前悬浮窗设置
    /// </summary>
    FloatingWindowSettings Settings { get; }

    /// <summary>
    /// 更新悬浮窗位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    void UpdatePosition(double x, double y);

    /// <summary>
    /// 通知积分变动，触发悬浮窗脉冲动画
    /// </summary>
    void NotifyScoreChange();
}
