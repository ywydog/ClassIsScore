using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClassIsScore.Converters;

/// <summary>
/// 排名转奖牌背景色转换器（前三名显示金银铜色，其余透明）
/// </summary>
public class RankToMedalBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rank)
        {
            return rank switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),    // 金牌
                2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)),  // 银牌
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),   // 铜牌
                _ => Brushes.Transparent
            };
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 排名是否为前三名转换器（用于控制奖牌可见性）
/// </summary>
public class RankToMedalVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rank)
        {
            return rank <= 3;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 排名是否超过前三名转换器（用于控制普通排名文字可见性）
/// </summary>
public class RankToNormalVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rank)
        {
            return rank > 3;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 积分变动值转颜色转换器（正数绿色，负数红色，零默认色）
/// </summary>
public class ScoreChangeColorConverter : IValueConverter
{
    public static readonly ScoreChangeColorConverter Instance = new();

    private static readonly SolidColorBrush PositiveBrush = new(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush NegativeBrush = new(Color.FromRgb(0xFF, 0x57, 0x22));
    private static readonly SolidColorBrush ZeroBrush = new(Color.FromRgb(0xAA, 0xAA, 0xAA));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double change)
        {
            if (change > 0) return PositiveBrush;
            if (change < 0) return NegativeBrush;
            return ZeroBrush;
        }
        return ZeroBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
