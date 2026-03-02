using System.Reflection;
using System.Reflection.Emit;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

internal static class DynamicMethods
{
    internal const string _currentMinigame = "_currentMinigame";
    internal static Action<IMinigame?> Set_Game1_currentMinigame = null!;
    internal const string _activeClickableMenu = "_activeClickableMenu";
    internal static Action<IClickableMenu?> Set_Game1_activeClickableMenu = null!;

    internal static void Make()
    {
        Set_Game1_currentMinigame = MakeStaticSetter<Game1, IMinigame>(
            _currentMinigame,
            nameof(Set_Game1_currentMinigame)
        );
        Set_Game1_activeClickableMenu = MakeStaticSetter<Game1, IClickableMenu>(
            _activeClickableMenu,
            nameof(Set_Game1_activeClickableMenu)
        );
    }

    private static Action<TRet?> MakeStaticSetter<THold, TRet>(string fieldName, string dynamicMethodName)
    {
        if (
            typeof(THold).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
            is not FieldInfo minigameField
        )
        {
            throw new NullReferenceException($"Failed to get '{fieldName}' field info");
        }
        DynamicMethod dm = new(dynamicMethodName, null, [typeof(TRet)]);
        ILGenerator gen = dm.GetILGenerator();
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Stsfld, minigameField);
        gen.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Action<TRet?>>();
    }
}
