using Merchant.Misc;
using Merchant.Models;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace Merchant.Management;

public record ForSaleTarget(Item Thing, Furniture Table, List<(Point, int)> BrowseAround, bool FromHeldChest)
{
    public bool IsAvailable => HeldBy == null && Sold == null;
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

public sealed record ShopkeepBrowsing(
    GameLocation Location,
    Farmer Player,
    Point EntryPoint,
    List<Point> ReachableTiles,
    List<CustomerActor> CustomerActors,
    List<ForSaleTarget> ForSaleTargets,
    float ShopDecorBonus
)
{
    #region make
    public static ShopkeepBrowsing? Make(GameLocation location, Farmer player, List<string> customerNPCs)
    {
        // tile accessibility
        if (location.warps.Count < 1)
        {
            ModEntry.Log($"Location {location.NameOrUniqueName} has no warps in", LogLevel.Error);
            return null;
        }
        Warp firstWarp = location.warps[0];
        Point entryPoint = new(firstWarp.X, firstWarp.Y - 1);
        List<Point> reachableTiles = Topology.TileStandableBFS(location, entryPoint);
        if (!reachableTiles.Any())
        {
            ModEntry.Log($"Location {location.NameOrUniqueName} has no reachable tiles", LogLevel.Error);
            return null;
        }

        // customers
        List<CustomerActor> customerActors = [];
        foreach (string npcName in customerNPCs)
        {
            if (CustomerActor.Make(location, player, entryPoint, npcName) is CustomerActor customer)
            {
                ModEntry.LogDebug($"Customer: {npcName}");
                customerActors.Add(customer);
            }
        }

        // shop layout and for sale items
        int tableCount = 0;
        int floorDecorCount = 0;
        int standingDecorCount = 0;
        List<ForSaleTarget> forSaleTables = [];
        foreach (Furniture furniture in location.furniture)
        {
            if (furniture.heldObject.Value != null)
            {
                tableCount++;

                List<(Point, int)> browseAround = FormBrowseAround(furniture, reachableTiles).ToList();
                if (!browseAround.Any())
                    continue;

                if (furniture.heldObject.Value is Chest chest)
                {
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
                    ModEntry.LogDebug(
                        $"ForSale: {furniture.heldObject.Value.DisplayName} ({string.Join(',', browseAround)})"
                    );
                    forSaleTables.Add(new(furniture.heldObject.Value, furniture, browseAround, false));
                }
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
        int objCount = location.objects.Count();
        standingDecorCount += location.objects.Count();
        floorDecorCount += location.terrainFeatures.Count();

        float shopDecorBonus = 0;
        if (location.furniture.Count > 0)
        {
            float decorBonus = (float)standingDecorCount / (location.furniture.Count + objCount);
            float rugBonus =
                (floorDecorCount * 0.5f) / ((location.Map.DisplayWidth / 64) * (location.Map.DisplayHeight / 64));
            ModEntry.LogDebug(
                $"DecorBonus: {standingDecorCount} / {location.furniture.Count + objCount} = {decorBonus}"
            );
            ModEntry.LogDebug(
                $"RugBonus: {floorDecorCount} / {(location.Map.DisplayWidth / 64) * (location.Map.DisplayHeight / 64)} {rugBonus}"
            );
            shopDecorBonus = Math.Min(decorBonus, 0.6f) + Math.Min(rugBonus, 0.4f);
        }

        return new(location, player, entryPoint, reachableTiles, customerActors, forSaleTables, shopDecorBonus);
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

    private readonly Queue<CustomerActor> waitingActors = ShuffleWaitingActors(CustomerActors);
    private readonly List<CustomerActor> dispatchedActors = [];

    public static Queue<CustomerActor> ShuffleWaitingActors(List<CustomerActor> customerActors)
    {
        customerActors = customerActors.ToList();
        int n = customerActors.Count;
        while (n > 1)
        {
            // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
            n--;
            int k = Random.Shared.Next(n + 1);
            (customerActors[n], customerActors[k]) = (customerActors[k], customerActors[n]);
        }
        return new(customerActors);
    }

    public bool Update(GameTime time, ref ShopkeepHaggle? haggling)
    {
        state.Update(time);
        if (state.Current == BrowsingState.Finished)
        {
            return true;
        }

        if (waitingActors.Count == 0 && dispatchedActors.All(actor => actor.IsFinished))
        {
            state.Current = BrowsingState.Finished;
            return true;
        }

        if (state.Current == BrowsingState.NewCustomer)
        {
            state.Current = BrowsingState.Waiting;
            if (AddNewCustomer())
            {
                state.SetNext(BrowsingState.NewCustomer, Random.Shared.Next(newCustomerCDMin, newCustomerCDMax));
            }
            else if (state.Next != BrowsingState.Finished)
            {
                state.SetNext(BrowsingState.Finished, 30000);
                return false;
            }
        }

        List<ForSaleTarget> forSaleTargetsFiltered = ForSaleTargets.Where(forSale => forSale.IsAvailable).ToList();
        if (forSaleTargetsFiltered.Count > 0)
        {
            foreach (CustomerActor actor in dispatchedActors)
            {
                actor.UpdateBuyTarget(forSaleTargetsFiltered, out ForSaleTarget? hagglingForSaleTarget);
                if (haggling == null && hagglingForSaleTarget != null)
                {
                    haggling = ShopkeepHaggle.Make(Player, actor, hagglingForSaleTarget, ShopDecorBonus);
                }
            }
        }

        return false;
    }

    public void Cleanup()
    {
        waitingActors.Clear();
        dispatchedActors.Clear();
        Location.characters.RemoveWhere(actor => actor is CustomerActor);
        state.Current = BrowsingState.Finished;
    }

    private bool AddNewCustomer()
    {
        if (!waitingActors.TryDequeue(out CustomerActor? nextActor))
        {
            return false;
        }
        nextActor.currentLocation = Location;
        nextActor.setTileLocation(EntryPoint.ToVector2());
        ModEntry.LogDebug($"AddNewCustomer {nextActor.Name}");
        Location.characters.Add(nextActor);
        dispatchedActors.Add(nextActor);
        return true;
    }

    internal void DebugSummary()
    {
        ModEntry.Log("===== SOLD =====", LogLevel.Info);
        foreach (ForSaleTarget forSale in ForSaleTargets)
        {
            if (forSale.Sold != null)
                ModEntry.Log($"- {forSale.Thing.DisplayName} ({forSale.Sold})", LogLevel.Info);
        }
        ModEntry.Log("===== REMAINING =====", LogLevel.Info);
        foreach (ForSaleTarget forSale in ForSaleTargets)
        {
            if (forSale.Sold == null)
                ModEntry.Log($"- {forSale.Thing.DisplayName} ({forSale.Thing.QualifiedItemId})", LogLevel.Info);
        }
    }
    #endregion
}
