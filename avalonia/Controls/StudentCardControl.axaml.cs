using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsScore.Models;

namespace ClassIsScore.Controls;

/// <summary>
/// 学生卡片控件，显示头像、姓名和积分
/// </summary>
public partial class StudentCardControl : UserControl
{
    /// <summary>
    /// 点击学生卡片事件
    /// </summary>
    public event EventHandler<Student>? StudentClicked;

    public StudentCardControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 数据上下文变更时，更新头像显示
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateAvatarDisplay();
    }

    /// <summary>
    /// 根据是否有头像路径，切换显示头像图片或姓名首字
    /// </summary>
    private void UpdateAvatarDisplay()
    {
        if (DataContext is Student student)
        {
            if (!string.IsNullOrWhiteSpace(student.Avatar))
            {
                AvatarImage.IsVisible = true;
                InitialText.IsVisible = false;
            }
            else
            {
                AvatarImage.IsVisible = false;
                InitialText.IsVisible = true;
            }
        }
    }

    /// <summary>
    /// 卡片点击事件处理
    /// </summary>
    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is Student student)
        {
            StudentClicked?.Invoke(this, student);
        }
    }
}
