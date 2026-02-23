namespace Merchant.Models;

public sealed record SoldRecord(bool IsHaggle, string Buyer, string ItemId, int SoldPrice);

public sealed class ShopkeepSessionLog
{
    public uint Date; // days played
    public uint TimeOfDay; // time of day
    public List<SoldRecord> Sales = [];
}
