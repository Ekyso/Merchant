using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;

namespace Merchant.Misc;

internal static class AssetManager
{
    private const string Asset_TextureCashregister = $"{ModEntry.ModId}/cashregister";
    internal const string CashRegisterId = $"{ModEntry.ModId}_CashRegister";
    internal const string CashRegisterQId = $"(BC){ModEntry.ModId}_CashRegister";

    public static void Register()
    {
        ModEntry.help.Events.Content.AssetRequested += OnAssetRequested;
    }

    public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(Edit_BigCraftables, AssetEditPriority.Default);
            return;
        }
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(Edit_Machines, AssetEditPriority.Default);
            return;
        }
        if (e.NameWithoutLocale.IsEquivalentTo(Asset_TextureCashregister))
        {
            e.LoadFromModFile<Texture2D>("assets/cashregister.png", AssetLoadPriority.Low);
            return;
        }
    }

    public static void Edit_Machines(IAssetData asset)
    {
        IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
        data[CashRegisterQId] = new() { InteractMethod = "Merchant.ModEntry, Merchant: InteractShowMerchantMenu" };
    }

    public static void Edit_BigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
        data[CashRegisterId] = new()
        {
            Name = CashRegisterId,
            DisplayName = I18n.Bc_CashRegister_Name(),
            Description = I18n.Bc_CashRegister_Desc(),
            Price = 5000,
            Fragility = 0,
            CanBePlacedOutdoors = true,
            CanBePlacedIndoors = true,
            IsLamp = true,
            Texture = Asset_TextureCashregister,
            SpriteIndex = 0,
            ContextTags = [ModEntry.ModId],
            CustomFields = null,
        };
    }
}
