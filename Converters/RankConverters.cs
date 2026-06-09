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
