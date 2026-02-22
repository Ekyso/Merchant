using Merchant.Misc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Merchant.Management;

public sealed class CustomerActor : NPC
{
    internal static CustomerActor? Make(GameLocation location, string npcName)
    {
        if (Game1.getCharacterFromName(npcName) is not NPC sourceNPC)
        {
            return null;
        }
        CustomerActor customerActor = new(
            new AnimatedSprite(sourceNPC.Sprite.textureName.Value),
            new Vector2(0, 0),
            location.NameOrUniqueName,
            sourceNPC.FacingDirection,
            sourceNPC.Name,
            sourceNPC.Portrait,
            eventActor: true
        );
        customerActor.NetFields.CopyFrom(sourceNPC.NetFields);
        return customerActor;
    }

    public CustomerActor(
        AnimatedSprite sprite,
        Vector2 position,
        string defaultMap,
        int facingDir,
        string name,
        Texture2D portrait,
        bool eventActor
    )
        : base(sprite, position, defaultMap, facingDir, name, portrait, eventActor)
    {
        forceOneTileWide.Value = true;
    }

    public Dialogue GetMerchantDialogue(string key, params object[] substitutions)
    {
        string merchantKey = $"{ModEntry.ModId}_{key}";
        if (TryGetDialogue(merchantKey, substitutions) is Dialogue dialogue)
            return dialogue;
        return new Dialogue(
            this,
            string.Concat(AssetManager.Asset_Strings, ":", key),
            AssetManager.LoadString(key, substitutions)
        );
    }
}
