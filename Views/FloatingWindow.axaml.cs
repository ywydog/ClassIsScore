using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ClassIsScore.Models;

namespace ClassIsScore.Views;

/// <summary>
/// 悬浮窗代码逻辑
/// </summary>
public partial class FloatingWindow : Window
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private readonly FloatingWindowSettings _settings;

    /// <summary>
    /// 悬浮窗位置变更事件（自定义，用于通知服务层保存位置）
    /// </summary>
    public event Action<double, double>? FloatingPositionChanged;

    public FloatingWindow() : this(new FloatingWindowSettings())
    {
    }

    public FloatingWindow(FloatingWindowSettings settings)
    {
        InitializeComponent();

        _settings = settings;

        // 应用设置
        ApplySettings(settings);

        // 绑定点击事件（打开主窗口积分管理页面）
        FloatingBorder.PointerPressed += OnBorderPointerPressed;
    }

    /// <summary>
    /// 应用悬浮窗设置
    /// </summary>
    private void ApplySettings(FloatingWindowSettings settings)
    {
        // 设置透明度
        Opacity = settings.Opacity;

        // 设置无焦点模式
        ShowActivated = !settings.IsNoFocus;

        // 设置初始位置
        if (settings.PositionX > 0 || settings.PositionY > 0)
        {
            Position = new PixelPoint((int)settings.PositionX, (int)settings.PositionY);
        }
        else
        {
            // 默认位置：屏幕右上角
            var screen = Screens.Primary;
            if (screen != null)
            {
                Position = new PixelPoint(
                    screen.WorkingArea.Width - 80,
                    screen.WorkingArea.Height / 3);
            }
        }
    }

    /// <summary>
    /// 更新悬浮窗位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public void UpdatePosition(double x, double y)
    {
        Position = new PixelPoint((int)x, (int)y);
    }

    /// <summary>
    /// 鼠标按下事件 - 开始拖拽
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标移动事件 - 拖拽移动窗口
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;

            // 移动窗口
            var newPosition = new PixelPoint(
                Position.X + (int)offset.X,
                Position.Y + (int)offset.Y);
            Position = newPosition;

            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标释放事件 - 结束拖拽
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            // 通知位置变更
            FloatingPositionChanged?.Invoke(Position.X, Position.Y);

            e.Handled = true;
        }
    }

    /// <summary>
    /// 悬浮窗边框点击事件 - 打开积分管理页面
    /// </summary>
    private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 仅在非拖拽状态下处理点击（短按即释放时触发）
        // 通过 PointerReleased 判断是否为点击
    }

    /// <summary>
    /// 窗口关闭时清理
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 保存最终位置
        FloatingPositionChanged?.Invoke(Position.X, Position.Y);
    }
}
