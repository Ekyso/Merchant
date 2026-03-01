namespace Merchant.Models;

public sealed class ShopkeepLocationData
{
    // Can use as shop
    public string? Condition { get; set; } = null;
    public string? CantBeShopReason { get; set; } = null;

    // Theme
    public List<ShopkeepThemeBoostData> ThemedBoosts { get; set; } = [];
}

public sealed class ShopkeepThemeBoostData
{
    public string? Id
    {
        get =>
            field ??= string.Concat(
                Value.ToString(),
                ":",
                RequiredContextTags != null ? string.Join(',', RequiredContextTags) : "ANY"
            );
        set => field = value;
    } = null;
    public List<string>? RequiredContextTags { get; set; } = null;
    public float Value
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 0.5f);
    } = 0f;
}
