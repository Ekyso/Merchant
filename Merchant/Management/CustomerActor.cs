using Merchant.Misc;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Pathfinding;

namespace Merchant.Management;

public sealed class CustomerActor : NPC
{
    #region make
    public static CustomerActor? Make(GameLocation location, Farmer farmer, Point entryPoint, string npcName)
    {
        if (Game1.getCharacterFromName(npcName) is not NPC sourceNPC)
        {
            return null;
        }
        CustomerActor customerActor = new(sourceNPC, location, farmer, entryPoint);
        customerActor.NetFields.CopyFrom(sourceNPC.NetFields);
        return customerActor;
    }

    private readonly Friendship? friendship;
    private readonly Point entryPoint;

    public CustomerActor(NPC sourceNPC, GameLocation location, Farmer player, Point entryPoint)
        : base(
            new AnimatedSprite(sourceNPC.Sprite.textureName.Value),
            Vector2.Zero,
            location.NameOrUniqueName,
            sourceNPC.FacingDirection,
            sourceNPC.Name,
            sourceNPC.Portrait,
            true
        )
    {
        this.entryPoint = entryPoint;
        forceOneTileWide.Value = true;
        followSchedule = false;
        if (!player.friendshipData.TryGetValue(sourceNPC.Name, out friendship))
        {
            friendship = null;
        }
    }
    #endregion

    #region social
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

    public float GetFriendshipHaggleBonus()
    {
        if (friendship == null || friendship.Points <= 1)
            return 0;

        return MathF.Log10(friendship.Points / 2500f) * 0.2f;
    }

    public int GetFriendshipHaggleMaxCount()
    {
        if (friendship == null || friendship.Points <= Utility.GetMaximumHeartsForCharacter(this))
            return 3;
        return 5;
    }
    #endregion

    #region browsing
    internal enum ActorState
    {
        Await,
        Move,
        Check,
        Buy,
        Leaving,
        Finished,
    }

    private readonly StateManager<ActorState> state = new(ActorState.Await);

    private int browsedCount = 0;
    private const int maxBrowsedCount = 5;
    public ForSaleTarget? ForSale
    {
        get => field;
        set
        {
            field?.HeldBy = null;
            value?.HeldBy = this;
            field = value;
        }
    }

    public bool IsFinished => state.Current == ActorState.Finished;

    public void UpdateBuyTarget(List<ForSaleTarget> forSaleTargets, out ForSaleTarget? hagglingForSaleTarget)
    {
        hagglingForSaleTarget = null;
        if (state.Current == ActorState.Buy)
        {
            if (ForSale == null)
            {
                state.Current = ActorState.Await;
            }
            else
            {
                hagglingForSaleTarget = ForSale;
            }
        }
        if (state.Current == ActorState.Await)
        {
            state.Current = ActorState.Move;
            ForSale = Random.Shared.ChooseFrom(forSaleTargets);
            (Point endPoint, int facing) = Random.Shared.ChooseFrom(ForSale.BrowseAround);
            controller = new PathFindController(this, currentLocation, endPoint, facing, ReachedForSaleItem);
        }
    }

    private void FinishedBuying(Character c, GameLocation location)
    {
        ForSale = null;
        state.Current = ActorState.Finished;
        location.characters.Remove(this);
    }

    private void ReachedForSaleItem(Character c, GameLocation location)
    {
        ModEntry.LogDebug($"ReachedForSaleItem {Name}");
        state.Current = ActorState.Check;
        browsedCount++;
        if (Random.Shared.NextSingle() < 0.3f + browsedCount * 0.1f)
        {
            doEmote(16);
            state.SetNext(ActorState.Buy, 1000);
        }
        else
        {
            ForSale = null;
            state.SetNext(browsedCount >= maxBrowsedCount ? ActorState.Leaving : ActorState.Await, 1000);
        }
    }

    internal void DoneHaggling()
    {
        ForSale = null;
        state.Current = ActorState.Leaving;
        controller = new PathFindController(this, currentLocation, entryPoint, -1, FinishedBuying);
    }

    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        state.Update(time);
        if (state.Current == ActorState.Leaving && TilePoint == entryPoint && !IsFinished)
        {
            FinishedBuying(this, location);
        }
    }
    #endregion
}
