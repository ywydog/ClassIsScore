using System.Linq;

namespace ClassIsScore.Models;

/// <summary>
/// 宠物系统配置和计算逻辑
/// </summary>
public static class PetSystem
{
    /// <summary>
    /// 等级经验阈值（递增设计）
    /// Lv1→2需要40经验, Lv2→3需要60, ... Lv7→8需要160
    /// </summary>
    public static readonly int[] LevelThresholds = { 40, 60, 80, 100, 120, 140, 160 };

    /// <summary>
    /// 最大等级
    /// </summary>
    public const int MaxLevel = 8;

    /// <summary>
    /// 所有可用宠物类型
    /// </summary>
    public static readonly PetTypeInfo[] AllPetTypes = {
        // 普通动物
        new() { Id = "cat", Name = "猫咪", Category = PetCategory.Normal, Emoji = "🐱", Description = "温顺可爱的小猫" },
        new() { Id = "orange-cat", Name = "橘猫", Category = PetCategory.Normal, Emoji = "🍊", Description = "十只橘猫九只胖" },
        new() { Id = "ragdoll", Name = "布偶猫", Category = PetCategory.Normal, Emoji = "🧸", Description = "优雅的猫中仙女" },
        new() { Id = "dog", Name = "小狗", Category = PetCategory.Normal, Emoji = "🐶", Description = "忠诚的好伙伴" },
        new() { Id = "shiba", Name = "柴犬", Category = PetCategory.Normal, Emoji = "🐕", Description = "微笑天使" },
        new() { Id = "corgi", Name = "柯基", Category = PetCategory.Normal, Emoji = "🦊", Description = "短腿小电臀" },
        new() { Id = "golden", Name = "金毛", Category = PetCategory.Normal, Emoji = "🦮", Description = "温暖的大暖男" },
        new() { Id = "husky", Name = "哈士奇", Category = PetCategory.Normal, Emoji = "🐺", Description = "拆家小能手" },
        new() { Id = "rabbit", Name = "兔子", Category = PetCategory.Normal, Emoji = "🐰", Description = "软萌小兔子" },
        new() { Id = "hamster", Name = "仓鼠", Category = PetCategory.Normal, Emoji = "🐹", Description = "腮帮子鼓鼓的" },
        new() { Id = "duck", Name = "柯尔鸭", Category = PetCategory.Normal, Emoji = "🦆", Description = "嘎嘎嘎" },
        new() { Id = "panda", Name = "熊猫", Category = PetCategory.Normal, Emoji = "🐼", Description = "国宝级萌物" },
        new() { Id = "red-panda", Name = "小熊猫", Category = PetCategory.Normal, Emoji = "🦝", Description = "不是小浣熊" },
        new() { Id = "alpaca", Name = "羊驼", Category = PetCategory.Normal, Emoji = "🦙", Description = "会吐口水的萌物" },
        new() { Id = "fox", Name = "狐狸", Category = PetCategory.Normal, Emoji = "🦊", Description = "聪明的小狐狸" },
        // 神兽
        new() { Id = "white-tiger", Name = "白虎", Category = PetCategory.Mythical, Emoji = "🐯", Description = "传说中的神兽" },
        new() { Id = "unicorn", Name = "独角兽", Category = PetCategory.Mythical, Emoji = "🦄", Description = "纯洁的象征" },
        new() { Id = "dragon", Name = "青龙", Category = PetCategory.Mythical, Emoji = "🐉", Description = "东方神龙" },
        new() { Id = "phoenix", Name = "朱雀", Category = PetCategory.Mythical, Emoji = "🔥", Description = "浴火重生" },
        new() { Id = "pixiu", Name = "貔貅", Category = PetCategory.Mythical, Emoji = "🦁", Description = "招财进宝" },
    };

    /// <summary>
    /// 根据经验值计算等级
    /// </summary>
    public static int CalculateLevel(double exp)
    {
        int level = 1;
        double total = 0;
        foreach (var threshold in LevelThresholds)
        {
            total += threshold;
            if (exp >= total) level++;
            else break;
        }
        return System.Math.Min(level, MaxLevel);
    }

    /// <summary>
    /// 获取当前等级进度信息
    /// </summary>
    public static LevelProgress GetLevelProgress(double exp)
    {
        if (exp <= 0)
            return new LevelProgress { Current = 0, Required = LevelThresholds[0], Percentage = 0, IsMaxLevel = false };

        double total = 0;
        for (int i = 0; i < LevelThresholds.Length; i++)
        {
            var levelTotal = total + LevelThresholds[i];
            if (exp < levelTotal)
            {
                var current = exp - total;
                return new LevelProgress
                {
                    Current = (int)current,
                    Required = LevelThresholds[i],
                    Percentage = (int)(current / LevelThresholds[i] * 100),
                    IsMaxLevel = false
                };
            }
            total = levelTotal;
        }

        var maxExp = LevelThresholds.Sum();
        return new LevelProgress { Current = (int)exp, Required = maxExp, Percentage = 100, IsMaxLevel = true };
    }

    /// <summary>
    /// 获取等级对应的边框颜色
    /// </summary>
    public static string GetLevelBorderColor(int level) => level switch
    {
        1 => "#FFE0E0E0", // 浅灰
        2 => "#FFB0B0B0", // 灰
        3 => "#FF4488FF", // 蓝
        4 => "#FF00C8FF", // 青
        5 => "#FF8844FF", // 紫
        6 => "#FFFF4488", // 粉
        7 => "#FFFF2244", // 红
        8 => "#FFFFCC00", // 金
        _ => "#FFE0E0E0"
    };

    /// <summary>
    /// 获取等级对应的称号
    /// </summary>
    public static string GetLevelTitle(int level) => level switch
    {
        >= 8 => "已毕业",
        >= 7 => "史诗级",
        >= 5 => "稀有",
        >= 3 => "优秀",
        _ => "成长中"
    };

    /// <summary>
    /// 获取等级对应的背景渐变色
    /// </summary>
    public static (string Start, string End) GetLevelGradient(int level) => level switch
    {
        >= 8 => ("#FFD700", "#FF8C00"), // 金色
        >= 7 => ("#FF4488", "#FF2244"), // 红粉
        >= 5 => ("#8844FF", "#4488FF"), // 紫蓝
        >= 3 => ("#4488FF", "#00C8FF"), // 蓝青
        _ => ("#AAAAAA", "#888888")     // 灰色
    };

    /// <summary>
    /// 获取宠物类型信息
    /// </summary>
    public static PetTypeInfo? GetPetTypeInfo(string petTypeId)
        => AllPetTypes.FirstOrDefault(p => p.Id == petTypeId);

    /// <summary>
    /// 获取宠物emoji
    /// </summary>
    public static string GetPetEmoji(string? petTypeId)
        => GetPetTypeInfo(petTypeId ?? "")?.Emoji ?? "❓";
}

/// <summary>
/// 等级进度信息
/// </summary>
public class LevelProgress
{
    public int Current { get; set; }
    public int Required { get; set; }
    public int Percentage { get; set; }
    public bool IsMaxLevel { get; set; }
}
