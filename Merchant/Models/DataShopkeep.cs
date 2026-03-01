using Microsoft.Xna.Framework;

namespace Merchant.Models;

public sealed class ShopkeepLocationData
{
    // Can use as shop
    public string? Condition { get; set; } = null;
    public string? CantBeShopReason { get; set; } = null;

    // Theme
    public List<ShopkeepThemeData> Themes { get; set; } = [];
}

public sealed class ShopkeepThemeData
{
    public string? Id
    {
        get =>
            field ??= string.Concat(
                Bonus.ToString(),
                ":",
                RequiredContextTags != null ? string.Join(',', RequiredContextTags) : "ANY"
            );
        set => field = value;
    } = null;
    public List<string>? RequiredContextTags { get; set; } = null;
    public float Bonus { get; set; } = 0f;
    public string? BonusBarTexture { get; set; } = null;
    public Rectangle BonusBarSourceRect { get; set; } = Rectangle.Empty;
}
