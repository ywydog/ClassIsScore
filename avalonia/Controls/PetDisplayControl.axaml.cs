using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassIsScore.Models;
using ClassIsScore.ViewModels;

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

    public PetDisplayControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 数据上下文变更时更新显示
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateDisplay();
    }

    /// <summary>
    /// 根据绑定数据更新所有显示元素
    /// </summary>
    private void UpdateDisplay()
    {
        if (DataContext is not StudentDisplayItem item) return;

        var petLevel = item.PetLevel;
        var hasPet = item.HasPet;
        var isGraduated = item.IsGraduated;

        // 更新宠物图片
        UpdatePetImage(item.PetType, petLevel, hasPet);

        // 更新宠物图片区域背景（等级渐变色）
        var gradient = PetSystem.GetLevelGradient(petLevel);
        PetImageBorder.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse(gradient.Start), 0),
                new GradientStop(Color.Parse(gradient.End), 1)
            }
        };

        // 未领养时灰色背景
        if (!hasPet)
        {
            PetImageBorder.Background = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
        }

        // 更新等级徽章
        LevelBadgeText.Text = $"Lv.{petLevel}";
        var borderColor = PetSystem.GetLevelBorderColor(petLevel);
        LevelBadge.Background = new SolidColorBrush(Color.Parse(borderColor));

        // 更新卡片边框颜色
        CardBorder.BorderBrush = new SolidColorBrush(Color.Parse(borderColor));

        // 更新毕业标记
        GraduatedMark.IsVisible = isGraduated;

        // 更新经验进度条
        var progress = item.LevelProgress;
        var barWidth = 140.0;
        if (progress.IsMaxLevel)
        {
            ExperienceBar.Width = barWidth;
        }
        else
        {
            var ratio = Math.Clamp(progress.Percentage / 100.0, 0, 1);
            ExperienceBar.Width = barWidth * ratio;
        }

        // 更新经验条渐变色
        ExperienceBar.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse(gradient.Start), 0),
                new GradientStop(Color.Parse(gradient.End), 1)
            }
        };

        // 更新经验文本
        ExpText.Text = progress.IsMaxLevel
            ? "MAX"
            : $"{progress.Current}/{progress.Required}";

        // 更新宠物名称
        PetNameText.Text = hasPet ? item.PetName : "未领养";

        // 更新等级称号
        LevelTitleText.Text = hasPet ? item.LevelTitle : "";
    }

    /// <summary>
    /// 更新宠物图片，根据宠物类型和等级加载对应图片
    /// </summary>
    private void UpdatePetImage(string? petType, int level, bool hasPet)
    {
        try
        {
            var imagePath = hasPet
                ? PetSystem.GetPetImagePath(petType, level)
                : PetSystem.GetDefaultPetImagePath();

            if (!string.IsNullOrEmpty(imagePath))
            {
                var uri = new Uri(imagePath);
                PetImage.Source = new Bitmap(AssetLoader.Open(uri));
            }
        }
        catch (Exception)
        {
            // 图片加载失败时使用默认图片
            try
            {
                var defaultUri = new Uri(PetSystem.GetDefaultPetImagePath());
                PetImage.Source = new Bitmap(AssetLoader.Open(defaultUri));
            }
            catch
            {
                PetImage.Source = null;
            }
        }

        // 未领养时降低透明度
        PetImage.Opacity = hasPet ? 1.0 : 0.3;
    }

    /// <summary>
    /// 宠物控件点击事件处理
    /// </summary>
    private void OnPetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is StudentDisplayItem item)
        {
            StudentClicked?.Invoke(this, item.Student);
        }
    }
}
