using StardewValley;

namespace Merchant.Models;

public sealed record SoldRecord(string Buyer, string ItemId, uint Price);

public sealed class ShopkeepSessionLog
{
    public bool IsAutoShopkeep;
    public int Date; // days played
    public List<SoldRecord> Sales = [];
}

public sealed class MerchantProgressData
{
    private string key = "merchant";
    internal ulong TotalEarnings = 0;
    internal uint TotalItemsSold = 0;
    public List<ShopkeepSessionLog> Logs = [];

    private void Validate()
    {
        foreach (ShopkeepSessionLog log in Logs)
        {
            ulong totalEarnings = 0;
            foreach (SoldRecord sale in log.Sales)
            {
                totalEarnings += sale.Price;
            }
            if (!log.IsAutoShopkeep)
                TotalEarnings += totalEarnings;
        }
    }

    public static MerchantProgressData Read()
    {
        string key = $"progress-{Game1.uniqueIDForThisGame}-{Game1.player.UniqueMultiplayerID}";
        ModEntry.Log($"Read progress data '{key}'");
        MerchantProgressData saveData = ModEntry.help.Data.ReadGlobalData<MerchantProgressData>(key) ?? new();
        saveData.key = key;
        saveData.Validate();
        return saveData;
    }

    public void Write()
    {
        ModEntry.Log($"Wrote progress data '{key}'");
        ModEntry.help.Data.WriteGlobalData(key, this);
    }

    public void SaveShopkeepSession(List<SoldRecord> sales, bool isAutoShopkeep, ulong totalEarnings)
    {
        ShopkeepSessionLog newLog = new()
        {
            IsAutoShopkeep = isAutoShopkeep,
            Date = Game1.Date.TotalDays,
            Sales = sales,
        };

        if (isAutoShopkeep)
            TotalEarnings += totalEarnings;

        Logs.Add(newLog);
    }
}
