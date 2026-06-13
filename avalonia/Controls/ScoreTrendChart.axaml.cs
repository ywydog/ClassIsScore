using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsScore.Models;

namespace ClassIsScore.Controls;

/// <summary>
/// 积分趋势折线图控件，使用 Canvas 绘制
/// </summary>
public partial class ScoreTrendChart : UserControl
{
    /// <summary>
    /// 数据源依赖属性
    /// </summary>
    public static readonly StyledProperty<IList<ScoreTrendPoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ScoreTrendChart, IList<ScoreTrendPoint>?>(nameof(ItemsSource));

    /// <summary>
    /// 强调色依赖属性
    /// </summary>
    public static readonly StyledProperty<Color> AccentColorProperty =
        AvaloniaProperty.Register<ScoreTrendChart, Color>(nameof(AccentColor), Color.Parse("#4CC2FF"));

    /// <summary>
    /// 数据源
    /// </summary>
    public IList<ScoreTrendPoint>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// 强调色
    /// </summary>
    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public ScoreTrendChart()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 当属性变更时重绘图表
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty ||
            change.Property == AccentColorProperty ||
            change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    /// <summary>
    /// 使用 DrawingContext 绘制折线图
    /// </summary>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var canvas = ChartCanvas;
        if (canvas == null) return;

        var width = Bounds.Width;
        var height = Bounds.Height;

        if (width <= 0 || height <= 0) return;

        // 绘制区域边距
        const double leftMargin = 50;
        const double rightMargin = 20;
        const double topMargin = 20;
        const double bottomMargin = 40;

        var chartWidth = width - leftMargin - rightMargin;
        var chartHeight = height - topMargin - bottomMargin;

        if (chartWidth <= 0 || chartHeight <= 0) return;

        var data = ItemsSource;
        if (data == null || data.Count == 0)
        {
            // 无数据时显示提示
            var noDataBrush = new SolidColorBrush(Color.Parse("#999999"));
            var formattedText = new FormattedText(
                "暂无数据",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                14,
                noDataBrush);
            context.DrawText(formattedText,
                new Point(width / 2 - formattedText.Width / 2, height / 2 - formattedText.Height / 2));
            return;
        }

        // 计算数据范围
        var minScore = data.Min(d => d.Score);
        var maxScore = data.Max(d => d.Score);

        // 留出上下余量
        var scoreRange = maxScore - minScore;
        if (scoreRange < 1) scoreRange = 1;
        var scorePadding = scoreRange * 0.1;
        var yMin = minScore - scorePadding;
        var yMax = maxScore + scorePadding;
        var yRange = yMax - yMin;
        if (yRange <= 0) yRange = 1;

        // 网格线和轴线的画笔
        var gridBrush = new SolidColorBrush(Color.Parse("#30FFFFFF"));
        var axisBrush = new SolidColorBrush(Color.Parse("#60FFFFFF"));
        var labelBrush = new SolidColorBrush(Color.Parse("#AAFFFFFF"));
        var accentBrush = new SolidColorBrush(AccentColor);
        var accentBrushSemi = new SolidColorBrush(Color.FromArgb(40, AccentColor.R, AccentColor.G, AccentColor.B));

        // 绘制背景网格（水平线）
        var gridLineCount = 5;
        for (var i = 0; i <= gridLineCount; i++)
        {
            var y = topMargin + chartHeight * i / gridLineCount;
            context.DrawLine(new Pen(gridBrush, 1),
                new Point(leftMargin, y),
                new Point(leftMargin + chartWidth, y));

            // Y轴标签
            var scoreValue = yMax - (yRange * i / gridLineCount);
            var yLabel = new FormattedText(
                scoreValue.ToString("F1"),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter"),
                11,
                labelBrush);
            context.DrawText(yLabel,
                new Point(leftMargin - yLabel.Width - 6, y - yLabel.Height / 2));
        }

        // 绘制坐标轴
        context.DrawLine(new Pen(axisBrush, 1.5),
            new Point(leftMargin, topMargin),
            new Point(leftMargin, topMargin + chartHeight));
        context.DrawLine(new Pen(axisBrush, 1.5),
            new Point(leftMargin, topMargin + chartHeight),
            new Point(leftMargin + chartWidth, topMargin + chartHeight));

        // 计算数据点位置
        var points = new List<Point>();
        for (var i = 0; i < data.Count; i++)
        {
            var x = leftMargin + chartWidth * i / Math.Max(data.Count - 1, 1);
            var y = topMargin + chartHeight * (1 - (data[i].Score - yMin) / yRange);
            points.Add(new Point(x, y));
        }

        // 绘制填充区域
        if (points.Count > 1)
        {
            var fillGeometry = new StreamGeometry();
            using (var ctx = fillGeometry.Open())
            {
                ctx.BeginFigure(points[0], false);
                for (var i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(points[i]);
                }
                // 闭合到底部
                ctx.LineTo(new Point(points[points.Count - 1].X, topMargin + chartHeight));
                ctx.LineTo(new Point(points[0].X, topMargin + chartHeight));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(accentBrushSemi, null, fillGeometry);
        }

        // 绘制折线
        if (points.Count > 1)
        {
            var lineGeometry = new StreamGeometry();
            using (var ctx = lineGeometry.Open())
            {
                ctx.BeginFigure(points[0], false);
                for (var i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(points[i]);
                }
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, new Pen(accentBrush, 2), lineGeometry);
        }

        // 绘制数据点和日期标签
        var maxLabels = Math.Min(data.Count, 10);
        var labelStep = Math.Max(1, data.Count / maxLabels);

        for (var i = 0; i < points.Count; i++)
        {
            // 数据点圆圈
            context.DrawEllipse(accentBrush, new Pen(new SolidColorBrush(Color.Parse("#202020")), 1.5),
                points[i], 4, 4);

            // X轴日期标签（间隔显示避免重叠）
            if (i % labelStep == 0 || i == points.Count - 1)
            {
                var dateText = data[i].Date.ToString("MM/dd");
                var xLabel = new FormattedText(
                    dateText,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Inter"),
                    11,
                    labelBrush);
                context.DrawText(xLabel,
                    new Point(points[i].X - xLabel.Width / 2, topMargin + chartHeight + 6));
            }
        }
    }
}
