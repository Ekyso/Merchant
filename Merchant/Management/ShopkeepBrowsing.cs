using System.Diagnostics.CodeAnalysis;
using System.Text;
using Merchant.Misc;
using Merchant.Models;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace Merchant.Management;

public record ForSaleTarget(Item Thing, Furniture Table, List<(Point, int)> BrowseAround, bool FromHeldChest)
{
    public CustomerActor? HeldBy { get; set; } = null;
    public SoldRecord? Sold
    {
        get => field;
        set
        {
            if (field != null)
                return;
            field = value;
            HeldBy = null;
            if (FromHeldChest)
            {
                if (Table.heldObject.Value is Chest chest)
                {
                    for (int i = 0; i < chest.Items.Count; i++)
                    {
                        Item item = chest.Items[i];
                        if (item != null)
                        {
                            item.onDetachedFromParent();
                            if (item is SObject obj)
                                obj.performRemoveAction();
                            chest.Items[i] = null;
                        }
                    }
                }
            }
            else
            {
                if (Table.heldObject.Value is SObject obj)
                {
                    obj.onDetachedFromParent();
                    obj.performRemoveAction();
                    Table.heldObject.Value = null;
                }
            }
        }
    }
}

public sealed record ShopBonusStats(int StandingDecorCount, int TableCount, int FloorDecorCount, int MapTileCount)
{
    public readonly float StandingDecorBonusRaw = (float)StandingDecorCount / TableCount;
    public readonly float FloorCoverageBonusRaw = FloorDecorCount / (float)MapTileCount;
    public readonly float TotalBonus =
        MathF.Min(0.5f, 1.0f * (StandingDecorCount / (float)TableCount))
        + MathF.Min(0.5f, FloorDecorCount / (float)MapTileCount);

    public string FormatSummary()
    {
        StringBuilder sb = new();
        sb.Append(I18n.Bonus_Title());
        sb.Append("  ^");
        sb.Append("----------------------------------------");
        sb.Append("  ^");
        sb.Append(I18n.Bonus_Decor());
        sb.Append("  ^  ");
        sb.Append(
            I18n.Bonus_Decor_Values(
                StandingDecorCount,
                TableCount,
                $"{StandingDecorBonusRaw:P2}",
                StandingDecorBonusRaw >= 1f ? I18n.Bonus_Capped() : ""
            )
        );
        sb.Append("  ^");
        sb.Append(I18n.Bonus_RugFloor());
        sb.Append("  ^  ");
        sb.Append(
            I18n.Bonus_RugFloor_Values(
                FloorDecorCount,
                MapTileCount,
                $"{FloorCoverageBonusRaw:P2}",
                StandingDecorBonusRaw >= 0.5f ? I18n.Bonus_Capped() : ""
            )
        );
        sb.Append("  ^");
        sb.Append(I18n.Bonus_Total($"{TotalBonus:P2}"));
        return sb.ToString();
    }
}

public sealed record ShopkeepBrowsing(
    GameLocation Location,
    Farmer Player,
    Point EntryPoint,
    List<Point> ReachableTiles,
    List<CustomerActor> CustomerActors,
    List<ForSaleTarget> ForSaleTargets,
    ShopBonusStats ShopBonus
)
{
    #region make
    public static bool TryMake(
        GameLocation location,
        Farmer player,
        [NotNullWhen(true)] out ShopkeepBrowsing? browsing,
        [NotNullWhen(false)] out string? failReason
    )
    {
        browsing = null;
        failReason = null;
        // location
        if (location is FarmHouse)
        {
            failReason = I18n.FailReason_IsFarmHouse();
            return false;
        }
        if (location.ParentBuilding == null)
        {
            failReason = I18n.FailReason_NotFarmBuilding();
            return false;
        }
        if (location.Map == null)
        {
            failReason = I18n.FailReason_InvalidMap();
            return false;
        }
        int mapTileCount = location.Map.DisplayWidth / 64 * (location.Map.DisplayHeight / 64);
        if (mapTileCount == 0)
        {
            failReason = I18n.FailReason_InvalidMap();
            return false;
        }
        // tile accessibility
        if (location.warps.Count < 1)
        {
            failReason = I18n.FailReason_NoWarpsIn();
            return false;
        }
        Warp firstWarp = location.warps[0];
        Point entryPoint = new(firstWarp.X, firstWarp.Y - 1);
        List<Point> reachableTiles = Topology.TileStandableBFS(location, entryPoint);
        if (!reachableTiles.Any())
        {
            failReason = I18n.FailReason_NoReachable();
            return false;
        }

        // shop layout and for sale items
        int floorDecorCount = 0;
        int standingDecorCount = 0;
        List<ForSaleTarget> forSaleTables = [];
        foreach (Furniture furniture in location.furniture)
        {
            if (furniture.IsTable() && furniture.heldObject.Value != null)
            {
                AddForSaleTable(player, reachableTiles, forSaleTables, furniture);
            }
            else if (furniture.furniture_type.Value == 12)
            {
                floorDecorCount += furniture.getTilesHigh() * furniture.getTilesWide();
            }
            else
            {
                standingDecorCount++;
            }
        }
        if (forSaleTables.Count == 0)
        {
            failReason = I18n.FailReason_NoItemsForSale();
            return false;
        }

        floorDecorCount += location.terrainFeatures.Count();

        // customers
        List<CustomerActor> customerActors = [];
        foreach (FriendEntry sourceFriend in NPCLookup.PickCustomerNPCs(player, forSaleTables.Count))
        {
            customerActors.Add(new CustomerActor(sourceFriend, entryPoint));
        }

        ShopBonusStats bonusStats = new(standingDecorCount, forSaleTables.Count, floorDecorCount, mapTileCount);

        browsing = new(location, player, entryPoint, reachableTiles, customerActors, forSaleTables, bonusStats);
        return true;
    }

    public static void AddForSaleTable(
        Farmer player,
        List<Point> reachableTiles,
        List<ForSaleTarget> forSaleTables,
        Furniture furniture
    )
    {
        List<(Point, int)> browseAround = FormBrowseAround(furniture, reachableTiles).ToList();
        if (!browseAround.Any())
            return;

        if (furniture.heldObject.Value is Chest chest)
        {
            // FF branch
            foreach (Item item in chest.Items)
            {
                if (item != null && item.sellToStorePrice(player.UniqueMultiplayerID) > 0)
                {
                    forSaleTables.Add(new(item, furniture, browseAround, true));
                }
            }
        }
        else if (furniture.heldObject.Value.sellToStorePrice(player.UniqueMultiplayerID) > 0)
        {
            forSaleTables.Add(new(furniture.heldObject.Value, furniture, browseAround, false));
        }
    }

    private static IEnumerable<(Point, int)> FormBrowseAround(Furniture furniture, List<Point> reachable)
    {
        Rectangle boundingBox = new(
            (int)furniture.TileLocation.X,
            (int)furniture.TileLocation.Y,
            furniture.getTilesWide(),
            furniture.getTilesHigh()
        );
        Point pnt;

        for (int i = 0; i < boundingBox.Width; i++)
        {
            int x = boundingBox.Left + i;
            // X
            // .
            pnt = new(x, boundingBox.Bottom);
            if (reachable.Contains(pnt))
                yield return new(pnt, 0);
            // .
            // X
            pnt = new(x, boundingBox.Top - 1);
            if (reachable.Contains(pnt))
                yield return new(pnt, 2);
        }
        for (int i = 0; i < boundingBox.Height; i++)
        {
            int y = boundingBox.Top + i;
            // .X
            pnt = new(boundingBox.Left - 1, y);
            if (reachable.Contains(pnt))
                yield return new(pnt, 1);
            // X.
            pnt = new(boundingBox.Right, y);
            if (reachable.Contains(pnt))
                yield return new(pnt, 3);
        }
    }
    #endregion

    #region browsing loop
    internal enum BrowsingState
    {
        Waiting,
        NewCustomer,
        Finished,
    }

    private readonly StateManager<BrowsingState> state = new(BrowsingState.NewCustomer);
    private const int newCustomerCDMin = 2000;
    private const int newCustomerCDMax = 4000;

    internal bool AboutToFinish => state.Next == BrowsingState.Finished;

    private readonly Queue<CustomerActor> waitingActors = ShuffleWaitingActors(CustomerActors);
    private readonly List<CustomerActor> dispatchedActors = [];

    public static Queue<CustomerActor> ShuffleWaitingActors(List<CustomerActor> customerActors)
    {
        customerActors = customerActors.ToList();
        Random.Shared.ShuffleInPlace(customerActors);
        return new(customerActors);
    }

    public bool Update(GameTime time, ref ShopkeepHaggle? haggling)
    {
        state.Update(time);
        if (state.Current == BrowsingState.Finished)
        {
            return true;
        }

        if (waitingActors.Count == 0 && dispatchedActors.All(actor => actor.IsLeavingOrFinished))
        {
            ModEntry.Log("Browsing finished reason: all actors are leaving");
            state.Current = BrowsingState.Finished;
            return true;
        }

        List<ForSaleTarget>? availableForSale = null;
        List<ForSaleTarget>? availableForSaleHeld = null;
        foreach (ForSaleTarget forSale in ForSaleTargets)
        {
            if (forSale.Sold == null)
            {
                if (forSale.HeldBy == null)
                {
                    availableForSale ??= [];
                    availableForSale.Add(forSale);
                }
                else
                {
                    availableForSaleHeld ??= [];
                    availableForSaleHeld.Add(forSale);
                }
            }
        }

        if (availableForSale == null && availableForSaleHeld == null)
        {
            ModEntry.Log("Browsing finished reason: all items have been sold");
            state.Current = BrowsingState.Finished;
            return true;
        }

        if (state.Current == BrowsingState.NewCustomer)
        {
            state.Current = BrowsingState.Waiting;
            AddNewCustomer();
            if (waitingActors.Any())
            {
                state.SetNext(BrowsingState.NewCustomer, Random.Shared.Next(newCustomerCDMin, newCustomerCDMax));
            }
        }

        foreach (CustomerActor actor in dispatchedActors)
        {
            actor.UpdateBuyTarget(availableForSale, availableForSaleHeld, out ForSaleTarget? hagglingForSaleTarget);
            if (haggling == null && hagglingForSaleTarget != null)
            {
                haggling = ShopkeepHaggle.Make(Player, actor, hagglingForSaleTarget, ShopBonus.TotalBonus);
            }
        }

        return false;
    }

    private void AddNewCustomer()
    {
        if (!waitingActors.TryDequeue(out CustomerActor? nextActor))
        {
            return;
        }
        ModEntry.Log($"AddNewCustomer: {nextActor.Name}");
        dispatchedActors.Add(nextActor);
        nextActor.currentLocation = Location;
        nextActor.setTileLocation(EntryPoint.ToVector2());
        Location.characters.Add(nextActor);
        Game1.playSound(AssetManager.DoorbellCue, 1100 + (int)(300 * Random.Shared.NextSingle()));
    }

    internal void FinalizeAndCleanup()
    {
        List<SoldRecord> sales = [];
        ModEntry.Log("===== SOLD =====", LogLevel.Info);

        ulong totalEarnings = 0;
        foreach (ForSaleTarget forSale in ForSaleTargets)
        {
            if (forSale.Sold != null)
            {
                sales.Add(forSale.Sold);
                totalEarnings += forSale.Sold.Price;
                Item thing = forSale.Thing;
                Game1.stats.ItemsShipped += (uint)thing.Stack;
                if (thing.Category == -75 || thing.Category == -79)
                {
                    Game1.stats.CropsShipped += (uint)thing.Stack;
                }
                if (thing is SObject obj && obj.countsForShippedCollection())
                {
                    Player.shippedBasic(obj.ItemId, obj.Stack);
                }
                ModEntry.Log($"- {forSale.Thing.DisplayName} ({forSale.Sold})", LogLevel.Info);
            }
        }
        Player.Money = Player.Money + (int)totalEarnings;
        Game1.dayTimeMoneyBox.gotGoldCoin((int)totalEarnings);

        ModEntry.ProgressData!.SaveShopkeepSession(sales, false, totalEarnings);

        waitingActors.Clear();
        dispatchedActors.Clear();
        Location.characters.RemoveWhere(actor => actor is CustomerActor);
    }
    #endregion
}
