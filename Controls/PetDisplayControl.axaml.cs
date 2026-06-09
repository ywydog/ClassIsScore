using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ClassIsScore.Models;
using FluentAvalonia.UI.Controls;

namespace ClassIsScore.Controls;

/// <summary>
/// 宠物模式控件，以宠物形象显示学生积分信息
/// </summary>
public partial class PetDisplayControl : UserControl
{
    /// <summary>
    /// 点击宠物控件事件
    /// </summary>
    public event EventHandler<Student>? StudentClicked;

    /// <summary>
    /// 宠物等级依赖属性
    /// </summary>
    public static readonly StyledProperty<int> PetLevelProperty =
        AvaloniaProperty.Register<PetDisplayControl, int>(nameof(PetLevel), 1);

    /// <summary>
    /// 宠物等级
    /// </summary>
    public int PetLevel
    {
        get => GetValue(PetLevelProperty);
        set => SetValue(PetLevelProperty, value);
    }

    /// <summary>
    /// 宠物经验值依赖属性
    /// </summary>
    public static readonly StyledProperty<double> PetExperienceProperty =
        AvaloniaProperty.Register<PetDisplayControl, double>(nameof(PetExperience));

    /// <summary>
    /// 宠物经验值
    /// </summary>
    public double PetExperience
    {
        get => GetValue(PetExperienceProperty);
        set => SetValue(PetExperienceProperty, value);
    }

    /// <summary>
    /// 宠物样式标识依赖属性（预留接口）
    /// </summary>
    public static readonly StyledProperty<string?> PetStyleProperty =
        AvaloniaProperty.Register<PetDisplayControl, string?>(nameof(PetStyle));

    /// <summary>
    /// 宠物样式标识
    /// </summary>
    public string? PetStyle
    {
        get => GetValue(PetStyleProperty);
        set => SetValue(PetStyleProperty, value);
    }

    public PetDisplayControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        PetLevelProperty.Changed.AddClassHandler<PetDisplayControl>(OnPetLevelChanged);
        PetExperienceProperty.Changed.AddClassHandler<PetDisplayControl>(OnPetExperienceChanged);
    }

    /// <summary>
    /// 数据上下文变更时更新显示
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateLevelStars();
        UpdateExperienceBar();
        UpdatePetEmoji();
    }

    /// <summary>
    /// 宠物等级变更时更新星星显示
    /// </summary>
    private void OnPetLevelChanged(PetDisplayControl control, AvaloniaPropertyChangedEventArgs e)
    {
        control.UpdateLevelStars();
    }

    /// <summary>
    /// 宠物经验值变更时更新经验条
    /// </summary>
    private void OnPetExperienceChanged(PetDisplayControl control, AvaloniaPropertyChangedEventArgs e)
    {
        control.UpdateExperienceBar();
    }

    /// <summary>
    /// 更新等级星星图标显示
    /// </summary>
    private void UpdateLevelStars()
    {
        if (LevelStarsPanel == null) return;

        LevelStarsPanel.Children.Clear();
        var level = Math.Min(PetLevel, 5); // 最多显示5颗星
        for (int i = 0; i < level; i++)
        {
            var icon = new SymbolIcon
            {
                Symbol = Symbol.StarFilled,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07))
            };
            LevelStarsPanel.Children.Add(icon);
        }
    }

    /// <summary>
    /// 更新经验条宽度
    /// </summary>
    private void UpdateExperienceBar()
    {
        if (ExperienceBar == null) return;

        // 经验值范围 0~100，映射到经验条宽度（总宽120）
        var ratio = Math.Clamp(PetExperience / 100.0, 0, 1);
        ExperienceBar.Width = 120 * ratio;
    }

    /// <summary>
    /// 根据宠物样式更新宠物 emoji（预留接口）
    /// </summary>
    private void UpdatePetEmoji()
    {
        if (PetEmoji == null) return;

        // 根据宠物样式标识选择不同 emoji，预留自定义接口
        PetEmoji.Text = PetStyle switch
        {
            "cat" => "🐱",
            "dog" => "🐶",
            "rabbit" => "🐰",
            "panda" => "🐼",
            "fox" => "🦊",
            _ => "🐱" // 默认猫咪
        };
    }

    /// <summary>
    /// 宠物控件点击事件处理
    /// </summary>
    private void OnPetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is Student student)
        {
            StudentClicked?.Invoke(this, student);
        }
    }
}
