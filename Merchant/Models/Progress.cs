namespace Merchant.Models;

public sealed record PurchaseRecord(bool IsHaggle, string Buyer, string ItemId, int BasePrice, double Multiplier);

public sealed class ShopkeepSessionLog
{
    public uint Date; // days played
    public uint TimeOfDay; // time of day
    public List<PurchaseRecord> Sales = [];
}
