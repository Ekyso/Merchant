global using SObject = StardewValley.Object;
using System.Diagnostics;
using Merchant.Management;
using Merchant.Misc;
using StardewModdingAPI;
using StardewValley;

namespace Merchant;

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    public const string ModId = "mushymato.Merchant";
    private static IMonitor? mon;
    internal static IModHelper help = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;
        help = helper;

        AssetManager.Register();

#if DEBUG
        DebugEntry(helper);
#endif
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }

    /// <summary>SMAPI static monitor Log wrapper, debug only</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    [Conditional("DEBUG")]
    internal static void LogDebug(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    public static bool InteractShowMerchantMenu(StardewValley.Object machine, GameLocation location, Farmer player)
    {
        return ShopkeepGame.StartMinigame(help, Game1.currentLocation, Game1.player) != null;
    }

#if DEBUG
    private void DebugEntry(IModHelper helper)
    {
        helper.ConsoleCommands.Add("merchant-tst", "Begin merchant minigame here", TestMerchantGame);
    }

    private void TestMerchantGame(string arg1, string[] arg2)
    {
        if (Game1.currentMinigame is ShopkeepGame)
        {
            Game1.currentMinigame.unload();
            Game1.currentMinigame = null;
        }
        else
        {
            ShopkeepGame.StartMinigame(Helper, Game1.currentLocation, Game1.player);
        }
    }
#endif
}
