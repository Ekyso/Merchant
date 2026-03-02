using Merchant.Models;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Shops;

namespace Merchant.Misc;

internal static class AssetManager
{
    private const string Asset_TextureCraftables = $"{ModEntry.ModId}/craftables";
    internal const string Asset_Strings = $"{ModEntry.ModId}\\Strings";
    internal const string Asset_CustomerData = $"{ModEntry.ModId}/Customers";
    internal const string Asset_ShopkeepLocationData = $"{ModEntry.ModId}/ShopkeepLocations";
    internal const string CashRegisterId = $"{ModEntry.ModId}_CashRegister";
    internal const string CashRegisterQId = $"(BC){ModEntry.ModId}_CashRegister";
    internal const string ContextTag_CashRegister = $"{ModEntry.ModId}_cash_register";
    internal const string DoorbellCue = $"{ModEntry.ModId}_doorbell";

    private const AssetEditPriority ReallyEarly = AssetEditPriority.Early - 100;

    private static Dictionary<string, CustomerData>? customerData = null;

    public static CustomerData? GetCustomerData(string key)
    {
        customerData ??= Game1.content.Load<Dictionary<string, CustomerData>>(Asset_CustomerData);
        if (customerData.TryGetValue(key, out CustomerData? data))
            return data;
        return null;
    }

    private static Dictionary<string, ShopkeepLocationData>? shopkeepLocData = null;

    public static ShopkeepLocationData? GetShopkeepLocationData(string key)
    {
        shopkeepLocData ??= Game1.content.Load<Dictionary<string, ShopkeepLocationData>>(Asset_ShopkeepLocationData);
        if (shopkeepLocData.TryGetValue(key, out ShopkeepLocationData? data))
            return data;
        return null;
    }

    public static void Register()
    {
        ModEntry.help.Events.Content.AssetRequested += OnAssetRequested;
        ModEntry.help.Events.Content.AssetsInvalidated += OnAssetInvalidated;
    }

    internal static string LoadString(string key) => Game1.content.LoadString($"{Asset_Strings}:{key}");

    internal static string LoadString(string key, params object[] substitutions) =>
        Game1.content.LoadString($"{Asset_Strings}:{key}", substitutions);

    internal static string LoadStringReturnNullIfNotFound(string key, params object[] substitutions) =>
        Game1.content.LoadStringReturnNullIfNotFound($"{Asset_Strings}:{key}", substitutions);

    private static void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (
            e.NamesWithoutLocale.Any(name =>
                name.IsEquivalentTo(Asset_CustomerData) || name.IsEquivalentTo("Data/Characters")
            )
        )
        {
            customerData = null;
        }
        if (
            e.NamesWithoutLocale.Any(name =>
                name.IsEquivalentTo(Asset_ShopkeepLocationData) || name.IsEquivalentTo("Data/Buildings")
            )
        )
        {
            shopkeepLocData = null;
        }
    }

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
        else if (name.IsEquivalentTo("Data/Shops"))
        {
            e.Edit(Edit_Shops, AssetEditPriority.Default);
        }
        else if (name.IsEquivalentTo("Data/AudioChanges"))
        {
            e.Edit(Edit_AudioChanges, AssetEditPriority.Default);
        }
        else if (name.IsEquivalentTo(Asset_ShopkeepLocationData))
        {
            e.LoadFromModFile<Dictionary<string, ShopkeepLocationData>>(
                "assets/data_shopkeep_locations.json",
                AssetLoadPriority.Exclusive
            );
            e.Edit(Edit_ShopkeepLocations, ReallyEarly);
        }
        else if (name.IsEquivalentTo(Asset_CustomerData))
        {
            e.LoadFromModFile<Dictionary<string, CustomerData>>(
                "assets/data_customers.json",
                AssetLoadPriority.Exclusive
            );
            e.Edit(Edit_CustomerData, ReallyEarly);
        }
        else if (name.IsEquivalentTo(Asset_TextureCraftables))
        {
            e.LoadFromModFile<Texture2D>("assets/tx_craftables.png", AssetLoadPriority.Low);
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

    private static void Edit_ShopkeepLocations(IAssetData asset)
    {
        IDictionary<string, ShopkeepLocationData> data = asset.AsDictionary<string, ShopkeepLocationData>().Data;
        foreach ((string key, BuildingData buildingData) in Game1.buildingData)
        {
            if (buildingData.IndoorMap == null)
                continue;
            data.TryAdd(key, new());
        }
    }

    private static void Edit_CustomerData(IAssetData asset)
    {
        IDictionary<string, CustomerData> data = asset.AsDictionary<string, CustomerData>().Data;
        foreach ((string key, CharacterData charaData) in Game1.characterData)
        {
            if (GameStateQuery.IsImmutablyFalse(charaData.CanSocialize))
                continue;
            data.TryAdd(key, new());
        }
    }

    private static void Edit_AudioChanges(IAssetData asset)
    {
        IDictionary<string, AudioCueData> data = asset.AsDictionary<string, AudioCueData>().Data;
        data[DoorbellCue] = new()
        {
            Id = DoorbellCue,
            FilePaths =
            [
                Path.Combine(ModEntry.help.DirectoryPath, "assets", "sfx_doorbell01.ogg"),
                Path.Combine(ModEntry.help.DirectoryPath, "assets", "sfx_doorbell02.ogg"),
                Path.Combine(ModEntry.help.DirectoryPath, "assets", "sfx_doorbell03.ogg"),
                Path.Combine(ModEntry.help.DirectoryPath, "assets", "sfx_doorbell04.ogg"),
            ],
            Category = "Sound",
            StreamedVorbis = false,
            Looped = false,
            UseReverb = true,
        };
    }

    private static void Edit_Shops(IAssetData asset)
    {
        IDictionary<string, ShopData> data = asset.AsDictionary<string, ShopData>().Data;
        if (data.ContainsKey("Carpenter"))
            data["Carpenter"].Items.Add(new() { Id = CashRegisterQId, ItemId = CashRegisterQId });
    }

    public static void Edit_Machines(IAssetData asset)
    {
        IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
        data[CashRegisterQId] = new() { InteractMethod = GameDelegates.InteractMethod };
    }

    public static void Edit_BigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> data = asset.AsDictionary<string, BigCraftableData>().Data;
        data[CashRegisterId] = new()
        {
            Name = CashRegisterId,
            DisplayName = $"[LocalizedText {Asset_Strings}:CashRegister_Name]",
            Description = $"[LocalizedText {Asset_Strings}:CashRegister_Desc]",
            Price = 2500,
            Fragility = 0,
            CanBePlacedOutdoors = true,
            CanBePlacedIndoors = true,
            IsLamp = false,
            Texture = Asset_TextureCraftables,
            SpriteIndex = 0,
            ContextTags = [ContextTag_CashRegister],
            CustomFields = null,
        };
    }
}
