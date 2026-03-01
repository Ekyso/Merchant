using Merchant.Misc;
using Merchant.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Pathfinding;
using StardewValley.TokenizableStrings;

namespace Merchant.Management;

public enum CxDialogueKind
{
    Haggle_Ask,
    Haggle_Compromise,
    Haggle_Overpriced,
    Haggle_Fail,
    Haggle_Success,
}

public sealed class CustomerActor : NPC
{
    internal static readonly Event BogusEvent = new();
    #region make
    private readonly Point entryPoint;
    internal readonly FriendEntry sourceFriend;
    internal readonly HaggleDialogue? haggleDialogue;
    internal bool HaggleEnabled = true;

    public CustomerActor(FriendEntry sourceFriend, Point entryPoint)
        : base(
            new AnimatedSprite(sourceFriend.Npc.Sprite.textureName.Value),
            Vector2.Zero,
            sourceFriend.Npc.speed,
            sourceFriend.Npc.Name
        )
    {
        this.modData.CopyFrom(sourceFriend.Npc.modData);
        this.sourceFriend = sourceFriend;
        this.entryPoint = entryPoint;
        forceOneTileWide.Value = true;
        followSchedule = false;
        EventActor = true;
        if (sourceFriend.CxData?.HaggleDialogue?.Count > 0)
        {
            haggleDialogue = Random.Shared.ChooseFrom(sourceFriend.CxData.HaggleDialogue.Values.ToList());
        }
        state = new(ActorState.Await, $"{nameof(ActorState)}[${sourceFriend.Npc.Name}]");
    }
    #endregion

    #region social
    internal static readonly NPC dummySpeaker = new(
        new AnimatedSprite("Characters\\Abigail", 0, 16, 16),
        Vector2.Zero,
        "",
        0,
        "???",
        Game1.staminaRect,
        eventActor: false
    )
    {
        portraitOverridden = true,
    };

    public Dialogue GetMerchantDialogue(NPC dummySpeaker, CxDialogueKind kind, params object[] substitutions)
    {
        dummySpeaker.Name = sourceFriend.Npc.Name;
        dummySpeaker.Portrait = sourceFriend.Npc.Portrait;
        dummySpeaker.displayName = sourceFriend.Npc.displayName;
        if (haggleDialogue != null)
        {
            string? dialogue = kind switch
            {
                CxDialogueKind.Haggle_Ask => haggleDialogue.Ask,
                CxDialogueKind.Haggle_Compromise => haggleDialogue.Compromise,
                CxDialogueKind.Haggle_Overpriced => haggleDialogue.Overpriced,
                CxDialogueKind.Haggle_Success => haggleDialogue.Success,
                CxDialogueKind.Haggle_Fail => haggleDialogue.Fail,
                _ => null,
            };
            if (dialogue != null)
            {
                return new Dialogue(
                    dummySpeaker,
                    string.Concat(AssetManager.Asset_Strings, ":", kind.ToString()),
                    string.Format(TokenParser.ParseText(dialogue) ?? dialogue, substitutions)
                );
            }
        }
        return new Dialogue(
            dummySpeaker,
            string.Concat(AssetManager.Asset_Strings, ":", kind.ToString()),
            AssetManager.LoadString(kind.ToString(), substitutions)
        );
    }

    public float GetFriendshipHaggleBase()
    {
        // TODO: custom haggle bonus
        if (sourceFriend.Fren.Points <= 1)
            return 0.15f;
        return 0.15f + MathF.Log10(sourceFriend.Fren.Points / 2000f) * 0.25f;
    }

    public float GetHaggleBaseTargetPointer(ForSaleTarget forSale)
    {
        float haggleBaseTarget = GetFriendshipHaggleBase();
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
            case gift_taste_dislike:
                haggleBaseTarget -= 0.1f;
                break;
        }
        return Math.Max(0f, haggleBaseTarget + 0.2f * Random.Shared.NextSingle());
    }

    public float GetHaggleTargetOverRange()
    {
        // TODO: custom target over range bonus
        // 0.05f per heart, up to 0.25f
        if (sourceFriend.Fren.Points >= 1250)
            return 0.25f;
        return 0.05f * (sourceFriend.Fren.Points / 250f);
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

    private readonly StateManager<ActorState> state;

    private readonly float chanceToBuy = 0.2f + 0.3f * Random.Shared.NextSingle();
    private int browsedCount = 0;
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
                    LeavingTheShop();
                return;
            }
            if (availableForSale.Count == 0)
            {
                return;
            }

            List<ForSaleTarget>[] rankedForSaleTargets =
            [
                [], // love
                [], // like
                [], // neutral
                [], // dislike
                [], // hated
            ];
            foreach (ForSaleTarget forSale in availableForSale)
            {
                int giftTaste = GetGiftTasteForSaleItem(forSale);
                int seq = giftTaste switch
                {
                    gift_taste_love => 0,
                    gift_taste_stardroptea => 0,
                    gift_taste_like => 1,
                    gift_taste_neutral => 2,
                    gift_taste_dislike => 3,
                    _ => 4,
                };
                rankedForSaleTargets[seq].Add(forSale);
            }
            ForSaleTarget? nextForSale = null;
            foreach (List<ForSaleTarget> giftTasteForSale in rankedForSaleTargets)
            {
                if (giftTasteForSale.Count > 0)
                {
                    nextForSale = Random.Shared.ChooseFrom(giftTasteForSale);
                    break;
                }
            }

            if (nextForSale == null)
            {
                if (availableForSaleHeld == null)
                {
                    LeavingTheShop();
                }
                return;
            }

            browsedCount++;
            if (nextForSale == ForSale)
            {
                return;
            }
            ForSale = nextForSale;

            state.Current = ActorState.Move;
            (Point endPoint, int facing) = Random.Shared.ChooseFrom(ForSale.BrowseAround);
            controller = new PathFindController(this, currentLocation, endPoint, facing, ReachedForSaleItem);
        }
    }

    private void ReachedForSaleItem(Character c, GameLocation location)
    {
        state.Current = ActorState.Considering;
        state.SetNext(ActorState.Decide, Random.Shared.NextSingle() * 750, DecideBuy);
    }

    private void DecideBuy()
    {
        if (HaggleEnabled && ForSale != null)
        {
            int giftTaste = GetGiftTasteForSaleItem(ForSale);
            if (giftTaste == gift_taste_hate)
            {
                doEmote(angryEmote);
                state.SetNext(ActorState.Leaving, 500, LeavingTheShop);
                return;
            }
            float bonusChanceToBuy = giftTaste == gift_taste_love ? 0.2f : 0f;
            if (Random.Shared.NextSingle() < chanceToBuy + bonusChanceToBuy + browsedCount * 0.1f)
            {
                doEmote(giftTaste == gift_taste_love ? heartEmote : exclamationEmote);
                state.SetNext(ActorState.Buy, 500);
                return;
            }
        }

        ForSale = null;
        state.SetNext(ActorState.Await, Random.Shared.NextSingle() * 750);
    }

    internal void LeavingTheShop()
    {
        if (IsLeavingOrFinished)
            return;
        ForSale = null;
        state.Current = ActorState.Leaving;
        controller = new PathFindController(this, currentLocation, entryPoint, -1, LeftTheShop);
    }

    private void LeftTheShop(Character c, GameLocation location)
    {
        ModEntry.Log($"LeftTheShop {c.displayName}");
        ForSale = null;
        cachedGiftTastes.Clear();
        state.SetAndLock(ActorState.Finished);
        Position = Vector2.Zero;
        IsInvisible = true;
    }

    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        // controller updates from vanilla
        if (!Game1.IsMasterGame)
        {
            if (controller == null && !freezeMotion)
            {
                updateMovement(location, time);
            }
            if (controller != null && !freezeMotion && controller.update(time))
            {
                controller = null;
            }
        }
        state.Update(time);
        if (state.Current == ActorState.Leaving && TilePoint == entryPoint)
        {
            LeftTheShop(this, location);
        }
    }
    #endregion
}
