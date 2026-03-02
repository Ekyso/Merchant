using Merchant.Misc;
using Merchant.Models;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewValley.TokenizableStrings;

namespace Merchant.Management;

public sealed class CustomerActor : NPC
{
    internal static readonly Event BogusEvent = new();
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
        this.modData.CopyFrom(sourceFriend.Npc.modData);
        this.sourceFriend = sourceFriend;
        this.entryPoint = entryPoint;
        forceOneTileWide.Value = true;
        followSchedule = false;
        EventActor = true;
        collidesWithOtherCharacters.Value = false;
        state = new(ActorState.Await, $"{nameof(ActorState)}[{sourceFriend.Npc.Name}]");
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

    public Dialogue GetHaggleDialogue(NPC dummySpeaker, CustomerDialogueKind kind, params object[] substitutions)
    {
        dummySpeaker.Name = sourceFriend.Npc.Name;
        dummySpeaker.Portrait = sourceFriend.Npc.Portrait;
        dummySpeaker.displayName = sourceFriend.Npc.displayName;
        if (sourceFriend.CxData?.TryGetDialogueText(kind, out string? dialogueText) ?? false)
        {
            return new Dialogue(
                dummySpeaker,
                string.Concat(AssetManager.Asset_Strings, ":", kind.ToString()),
                string.Format(TokenParser.ParseText(dialogueText) ?? dialogueText, substitutions)
            );
        }
        return new Dialogue(
            dummySpeaker,
            string.Concat(AssetManager.Asset_Strings, ":", kind.ToString()),
            AssetManager.LoadString(kind.ToString(), substitutions)
        );
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
            if (value == null)
                ForSaleBrowsing = null;
        }
    }
    public (Point, int)? ForSaleBrowsing;

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
                int giftTaste = sourceFriend.GetGiftTasteForSaleItem(forSale);
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
            ForSaleBrowsing = Random.Shared.ChooseFrom(ForSale.BrowseAround);
            state.Current = ActorState.Move;
            controller = new PathFindController(
                this,
                currentLocation,
                ForSaleBrowsing.Value.Item1,
                ForSaleBrowsing.Value.Item2,
                ReachedForSaleItem
            )
            {
                nonDestructivePathing = true,
            };
        }
    }

    private void ReachedForSaleItem(Character c, GameLocation location)
    {
        state.Current = ActorState.Considering;
        state.SetNext(ActorState.Decide, Random.Shared.NextSingle() * 750, DecideBuy);
    }

    private void DecideBuy()
    {
        if (ForSale != null)
        {
            int giftTaste = sourceFriend.GetGiftTasteForSaleItem(ForSale);
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
        controller = new PathFindController(this, currentLocation, entryPoint, -1, LeftTheShop)
        {
            nonDestructivePathing = true,
        };
    }

    private void LeftTheShop(Character c, GameLocation location)
    {
        ModEntry.Log($"LeftTheShop {c.displayName}");
        ForSale = null;
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
        else if (state.Current == ActorState.Move && controller == null)
        {
            ModEntry.Log($"Actor '{Name}' stuck in Move, do force unstuck.", LogLevel.Warn);
            if (ForSale != null && ForSaleBrowsing != null && TilePoint != ForSaleBrowsing.Value.Item1)
            {
                state.Current = ActorState.Considering;
                setTilePosition(ForSaleBrowsing.Value.Item1);
                faceDirection(ForSaleBrowsing.Value.Item2);
                ReachedForSaleItem(this, currentLocation);
            }
            else
            {
                LeavingTheShop();
            }
        }
    }

    public void UpdateDuringReporting(GameTime time, GameLocation location)
    {
        IClickableMenu menu = Game1.activeClickableMenu;
        DynamicMethods.Set_Game1_activeClickableMenu(null);
        LeavingTheShop();
        update(time, location);
        DynamicMethods.Set_Game1_activeClickableMenu(menu);
    }
    #endregion
}
