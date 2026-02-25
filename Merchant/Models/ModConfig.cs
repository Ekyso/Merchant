using Merchant.ModIntegration;
using StardewModdingAPI;

namespace Merchant.Models;

public sealed class ModConfig
{
    public bool EnableAutoRestock { get; set; } = true;

    private void Reset()
    {
        EnableAutoRestock = true;
    }

    private void Save()
    {
        ModEntry.help.WriteConfig(this);
    }

    public void Register(IManifest mod, IGenericModConfigMenuApi gmcm)
    {
        gmcm.Register(mod, Reset, Save);
        gmcm.AddBoolOption(
            mod,
            () => EnableAutoRestock,
            (value) => EnableAutoRestock = value,
            I18n.Config_AutoRestock_Name,
            I18n.Config_AutoRestock_Desc
        );
    }
}
