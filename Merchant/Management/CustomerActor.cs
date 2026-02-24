using Merchant.Misc;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Pathfinding;

namespace Merchant.Management;

public sealed class CustomerActor : NPC
{
    #region make
    private readonly Point entryPoint;
    internal readonly FriendEntry sourceFriend;

    public CustomerActor(FriendEntry sourceFriend, Point entryPoint)
        : base(
            new AnimatedSprite(sourceFriend.Npc.Sprite.textureName.Value),
            Vector2.Zero,
            sourceFriend.Npc.speed,
            sourceFriend.Npc.Name
        )
    {
        NetFields.CopyFrom(sourceFriend.Npc.NetFields);
        this.sourceFriend = sourceFriend;
        this.entryPoint = entryPoint;
        forceOneTileWide.Value = true;
        followSchedule = false;
        EventActor = true;
    }
    #endregion

    #region social
    public Dialogue GetMerchantDialogue(NPC dummySpeaker, string key, params object[] substitutions)
    {
        dummySpeaker.Portrait = sourceFriend.Npc.Portrait;
        dummySpeaker.displayName = sourceFriend.Npc.displayName;
        return new Dialogue(
            dummySpeaker,
            string.Concat(AssetManager.Asset_Strings, ":", key),
            AssetManager.LoadString(key, substitutions)
        );
    }

    public float GetFriendshipHaggleBonus()
    {
        // TODO: custom haggle bonus
        if (sourceFriend.Fren.Points <= 1)
            return 0.15f;
        return 0.15f + MathF.Log10(sourceFriend.Fren.Points / 2000f) * 0.25f;
    }

    public float GetHaggleBaseTargetPointer(ForSaleTarget forSale)
    {
        float haggleBaseTarget = GetFriendshipHaggleBonus();
        int giftTaste = GetGiftTasteForSaleItem(forSale);
        switch (giftTaste)
        {
            case gift_taste_stardroptea:
            case gift_taste_love:
                haggleBaseTarget += 0.2f;
                break;
            case gift_taste_like:
                haggleBaseTarget += 0.1f;
                break;
        }
        return haggleBaseTarget + 0.3f * Random.Shared.NextSingle();
    }

    public float GetHaggleTargetOverRange()
    {
        // TODO: custom target over range bonus
        if (sourceFriend.Fren == null)
            return 0.25f;
        // 0.05f per heart, up to 0.25f
        if (sourceFriend.Fren.Points >= 1250)
            return 0.5f;
        return 0.25f + 0.05f * sourceFriend.Fren.Points / 250f;
    }

    private readonly Dictionary<ForSaleTarget, int> cachedGiftTastes = [];

    private int GetGiftTasteForSaleItem(ForSaleTarget forSale)
    {
        if (!cachedGiftTastes.TryGetValue(forSale, out int giftTaste))
        {
            giftTaste = sourceFriend.Npc.getGiftTasteForThisItem(forSale.Thing);
            cachedGiftTastes[forSale] = giftTaste;
        }
        return giftTaste;
    }
    #endregion

    #region browsing
    internal enum ActorState
    {
        Await,
        Move,
        Considering,
        Decide,
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

    public bool IsLeavingOrFinished => state.Current == ActorState.Leaving || state.Current == ActorState.Finished;

    public void UpdateBuyTarget(
        List<ForSaleTarget>? availableForSale,
        List<ForSaleTarget>? availableForSaleHeld,
        out ForSaleTarget? hagglingForSaleTarget
    )
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
            if (availableForSale == null)
            {
                if (availableForSaleHeld == null)
                {
                    LeaveTheShop();
                }
                return;
            }
            if (availableForSale.Count == 0)
            {
                return;
            }
            state.Current = ActorState.Move;
            List<ForSaleTarget> likedForSaleTargets = availableForSale.Where(ForSaleNotHated).ToList();
            if (likedForSaleTargets.Count == 0)
            {
                if (availableForSaleHeld == null)
                {
                    LeaveTheShop();
                    return;
                }
                else
                {
                    ForSale = Random.Shared.ChooseFrom(availableForSale);
                }
            }
            else
            {
                ForSale = Random.Shared.ChooseFrom(likedForSaleTargets);
            }

            (Point endPoint, int facing) = Random.Shared.ChooseFrom(ForSale.BrowseAround);
            browsedCount++;
            controller = new PathFindController(this, currentLocation, endPoint, facing, ReachedForSaleItem);
        }
    }

    private bool ForSaleNotHated(ForSaleTarget forSale)
    {
        int giftTaste = GetGiftTasteForSaleItem(forSale);
        return giftTaste != gift_taste_dislike && giftTaste != gift_taste_hate;
    }

    private void FinishedBuying(Character c, GameLocation location)
    {
        ModEntry.Log($"FinishedBuying {c.displayName}");
        ForSale = null;
        cachedGiftTastes.Clear();
        state.Current = ActorState.Finished;
        location.characters.Remove(this);
    }

    private void ReachedForSaleItem(Character c, GameLocation location)
    {
        state.Current = ActorState.Considering;
        state.SetNext(ActorState.Decide, Random.Shared.NextSingle() * 1000, DecideBuy);
    }

    private void DecideBuy(ActorState oldState, ActorState newState)
    {
        if (ForSale != null)
        {
            int giftTaste = GetGiftTasteForSaleItem(ForSale);
            if (
                giftTaste != gift_taste_dislike
                && giftTaste != gift_taste_hate
                && Random.Shared.NextSingle() < 0.3f + browsedCount * 0.1f
            )
            {
                doEmote(giftTaste == gift_taste_love ? 20 : 32);
                state.SetNext(ActorState.Buy, 500);
                return;
            }
        }

        ForSale = null;
        state.SetNext(browsedCount >= maxBrowsedCount ? ActorState.Leaving : ActorState.Await, 500);
    }

    internal void LeaveTheShop()
    {
        ForSale = null;
        state.Current = ActorState.Leaving;
        controller = new PathFindController(this, currentLocation, entryPoint, -1, FinishedBuying);
    }

    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        state.Update(time);
        if (state.Current == ActorState.Leaving && TilePoint == entryPoint)
        {
            FinishedBuying(this, location);
        }
    }
    #endregion
}
