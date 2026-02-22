using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;

namespace Merchant.Misc;

internal static class AssetManager
{
    private const string Asset_TextureCashregister = $"{ModEntry.ModId}/cashregister";
    private const string Asset_Strings = $"{ModEntry.ModId}\\Strings";
    internal const string CashRegisterId = $"{ModEntry.ModId}_CashRegister";
    internal const string CashRegisterQId = $"(BC){ModEntry.ModId}_CashRegister";

    public static void Register()
    {
        ModEntry.help.Events.Content.AssetRequested += OnAssetRequested;
    }

    internal static string LoadString(string key) => Game1.content.LoadString($"{Asset_Strings}:{key}");

    internal static string LoadString(string key, params object[] substitutions) =>
        Game1.content.LoadString($"{Asset_Strings}:{key}", substitutions);

    internal static string LoadStringReturnNullIfNotFound(string key, params object[] substitutions) =>
        Game1.content.LoadStringReturnNullIfNotFound($"{Asset_Strings}:{key}", substitutions);

    public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        IAssetName name = e.NameWithoutLocale;
        if (name.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(Edit_BigCraftables, AssetEditPriority.Default);
        }
        else if (name.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(Edit_Machines, AssetEditPriority.Default);
        }
        else if (name.IsEquivalentTo(Asset_TextureCashregister))
        {
            e.LoadFromModFile<Texture2D>("assets/cashregister.png", AssetLoadPriority.Low);
        }
        else if (name.IsEquivalentTo(Asset_Strings))
        {
            string stringsAsset = Path.Combine("i18n", e.Name.LanguageCode.ToString() ?? "default", "strings.json");
            if (File.Exists(Path.Combine(ModEntry.help.DirectoryPath, stringsAsset)))
            {
                e.LoadFromModFile<Dictionary<string, string>>(stringsAsset, AssetLoadPriority.Exclusive);
            }
            else
            {
                e.LoadFromModFile<Dictionary<string, string>>("i18n/default/strings.json", AssetLoadPriority.Exclusive);
            }
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
            DisplayName = $"[LocalizedText {Asset_Strings}:bc.cash-register.name]",
            Description = $"[LocalizedText {Asset_Strings}:bc.cash-register.desc]",
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
