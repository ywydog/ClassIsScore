using System;

namespace ClassIsScore.Models;

/// <summary>
/// 积分趋势数据点，表示某日的累计积分和当日变动
/// </summary>
public class ScoreTrendPoint
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 截至当日的累计积分
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 当日积分变动值
    /// </summary>
    public double Change { get; set; }
}
