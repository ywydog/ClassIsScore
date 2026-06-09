namespace ClassIsScore.Models;

/// <summary>
/// 排行榜条目模型
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 姓名（个人）或小组名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 积分
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 是否为小组排行条目
    /// </summary>
    public bool IsGroup { get; set; }
}
