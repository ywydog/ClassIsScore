using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassIsScore.Models;

namespace ClassIsScore.Views;

/// <summary>
/// 悬浮窗代码逻辑
/// </summary>
public partial class FloatingWindow : Window
{
    private bool _isDragging;
    private bool _hasMoved;
    private Point _dragStartPoint;
    private PixelPoint _dragStartScreenPoint;
    private readonly FloatingWindowSettings _settings;

    /// <summary>
    /// 拖拽判定阈值（像素），超过此距离才视为拖拽
    /// </summary>
    private const double DragThreshold = 5.0;

    /// <summary>
    /// 悬浮窗位置变更事件（自定义，用于通知服务层保存位置）
    /// </summary>
    public event Action<double, double>? FloatingPositionChanged;

    /// <summary>
    /// 点击/轻触事件（用于打开主窗口）
    /// </summary>
    public event Action? FloatingClicked;

    public FloatingWindow() : this(new FloatingWindowSettings())
    {
    }

    public FloatingWindow(FloatingWindowSettings settings)
    {
        InitializeComponent();

        _settings = settings;

        // 应用设置
        ApplySettings(settings);
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

        // 应用外观设置
        ApplyAppearanceSettings(settings);
    }

    /// <summary>
    /// 应用外观相关设置
    /// </summary>
    private void ApplyAppearanceSettings(FloatingWindowSettings settings)
    {
        var buttonSize = Math.Clamp(settings.Size, 40, 80);
        var cornerRadius = buttonSize / 2.0;

        // 根据样式切换布局可见性
        var isClassic = settings.Style == FloatingWindowStyle.Classic;
        ClassicLayout.IsVisible = isClassic;
        PureIconLayout.IsVisible = !isClassic;

        if (isClassic)
        {
            ApplyClassicStyle(settings, buttonSize, cornerRadius);
        }
        else
        {
            ApplyPureIconStyle(settings, buttonSize, cornerRadius);
        }
    }

    /// <summary>
    /// 应用经典模式样式
    /// </summary>
    private void ApplyClassicStyle(FloatingWindowSettings settings, double buttonSize, double cornerRadius)
    {
        var iconSize = buttonSize * 0.58;

        // 更新窗口大小（按钮大小 + 上下边距 + 标签空间）
        var windowWidth = buttonSize + 16;
        var windowHeight = settings.ShowLabel ? buttonSize + 24 : buttonSize + 12;
        Width = windowWidth;
        Height = windowHeight;

        // 更新边框大小
        FloatingBorder.Width = buttonSize;
        FloatingBorder.Height = buttonSize;
        FloatingBorder.CornerRadius = new CornerRadius(cornerRadius);

        // 更新图标大小
        AppLogoImage.Width = iconSize;
        AppLogoImage.Height = iconSize;

        // 加载自定义图标
        LoadImageSource(AppLogoImage, settings.CustomIconPath);

        // 更新显示文本
        DisplayTextLabel.Text = settings.DisplayText;
        DisplayTextLabel.IsVisible = settings.ShowLabel;

        // 更新主题色
        if (!string.IsNullOrEmpty(settings.AccentColor))
        {
            try
            {
                var color = Avalonia.Media.Color.Parse(settings.AccentColor);
                FloatingBorder.Background = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.FromArgb(0xCC, color.R, color.G, color.B));
            }
            catch
            {
                FloatingBorder.Background = Avalonia.Media.Brush.Parse("#CC4CC2FF");
            }
        }
        else
        {
            FloatingBorder.Background = Avalonia.Media.Brush.Parse("#CC4CC2FF");
        }
    }

    /// <summary>
    /// 应用纯图标模式样式
    /// </summary>
    private void ApplyPureIconStyle(FloatingWindowSettings settings, double buttonSize, double cornerRadius)
    {
        // 窗口大小 = 图片大小
        Width = buttonSize;
        Height = buttonSize;

        // 更新纯图标布局大小
        PureIconLayout.Width = buttonSize;
        PureIconLayout.Height = buttonSize;
        PureIconLayout.CornerRadius = new CornerRadius(cornerRadius);

        // 加载自定义图标
        LoadImageSource(PureIconImage, settings.CustomIconPath);
    }

    /// <summary>
    /// 加载图片源（自定义路径或默认 AppLogo）
    /// </summary>
    private void LoadImageSource(Image imageControl, string? customIconPath)
    {
        if (!string.IsNullOrEmpty(customIconPath))
        {
            try
            {
                // 尝试加载自定义图片
                if (System.IO.File.Exists(customIconPath))
                {
                    using var stream = System.IO.File.OpenRead(customIconPath);
                    imageControl.Source = new Bitmap(stream);
                    return;
                }
            }
            catch
            {
                // 自定义图片加载失败，回退到默认
            }
        }

        // 使用默认 AppLogo
        using var defaultStream = AssetLoader.Open(new Uri("avares://ClassIsScore/Assets/AppLogo.png"));
        imageControl.Source = new Bitmap(defaultStream);
    }

    /// <summary>
    /// 从设置更新悬浮窗外观（无需重建窗口）
    /// </summary>
    public void UpdateFromSettings(FloatingWindowSettings settings)
    {
        // 更新透明度和无焦点
        Opacity = settings.Opacity;
        ShowActivated = !settings.IsNoFocus;

        // 更新外观
        ApplyAppearanceSettings(settings);
    }

    /// <summary>
    /// 通知积分变动，触发脉冲动画
    /// </summary>
    public void NotifyScoreChange()
    {
        // 仅经典模式支持脉冲动画
        if (_settings.Style != FloatingWindowStyle.Classic) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                FloatingBorder.Classes.Remove("pulse");
                await System.Threading.Tasks.Task.Delay(16);
                FloatingBorder.Classes.Add("pulse");
                await System.Threading.Tasks.Task.Delay(600);
                FloatingBorder.Classes.Remove("pulse");
            }
            catch
            {
                // 忽略动画异常
            }
        });
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
    /// 指针按下事件 - 记录起始位置
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        // 支持鼠标左键和触摸按下
        if (pointerPoint.Properties.IsLeftButtonPressed || e.Pointer.Type == PointerType.Touch)
        {
            _isDragging = true;
            _hasMoved = false;
            _dragStartPoint = e.GetPosition(this);
            _dragStartScreenPoint = Position;
            e.Handled = true;
        }
    }

    /// <summary>
    /// 指针移动事件 - 拖拽移动窗口（带阈值判定）
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var pointerPoint = e.GetCurrentPoint(this);
        // 支持鼠标左键和触摸
        if (!pointerPoint.Properties.IsLeftButtonPressed && e.Pointer.Type != PointerType.Touch)
        {
            _isDragging = false;
            return;
        }

        var currentPoint = e.GetPosition(this);
        var offset = currentPoint - _dragStartPoint;

        // 判断是否超过拖拽阈值
        if (!_hasMoved)
        {
            var distance = Math.Sqrt(offset.X * offset.X + offset.Y * offset.Y);
            if (distance < DragThreshold)
            {
                return;
            }
            _hasMoved = true;
        }

        // 移动窗口
        var newPosition = new PixelPoint(
            _dragStartScreenPoint.X + (int)offset.X,
            _dragStartScreenPoint.Y + (int)offset.Y);
        Position = newPosition;

        e.Handled = true;
    }

    /// <summary>
    /// 指针释放事件 - 结束拖拽或触发点击
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            if (_hasMoved)
            {
                // 拖拽移动完成，通知位置变更
                FloatingPositionChanged?.Invoke(Position.X, Position.Y);
            }
            else
            {
                // 未超过阈值，视为点击/轻触
                FloatingClicked?.Invoke();
            }

            e.Handled = true;
        }
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
