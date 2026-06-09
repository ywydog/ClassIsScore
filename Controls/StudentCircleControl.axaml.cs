using System;
using Avalonia.Controls;
using Avalonia.Input;
using ClassIsScore.Models;

namespace ClassIsScore.Controls;

/// <summary>
/// 学生圆形控件，以圆形头像方式显示学生信息
/// </summary>
public partial class StudentCircleControl : UserControl
{
    /// <summary>
    /// 点击学生圆形控件事件
    /// </summary>
    public event EventHandler<Student>? StudentClicked;

    public StudentCircleControl()
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
    /// 圆形控件点击事件处理
    /// </summary>
    private void OnCirclePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is Student student)
        {
            StudentClicked?.Invoke(this, student);
        }
    }
}
