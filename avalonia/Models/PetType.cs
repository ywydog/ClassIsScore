namespace ClassIsScore.Models;

/// <summary>
/// 宠物分类
/// </summary>
public enum PetCategory
{
    /// <summary>普通</summary>
    Normal,
    /// <summary>神兽</summary>
    Mythical
}

/// <summary>
/// 宠物类型定义
/// </summary>
public class PetTypeInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PetCategory Category { get; set; }
    public string Emoji { get; set; } = "🐱";
    public string Description { get; set; } = string.Empty;
}
