using Merchant.Misc;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace Merchant.Management;

public record ForSaleTarget(Item ForSale, Furniture Table, IReadOnlyList<(Point, int)> BrowseAround);

public sealed record ShopkeepBrowsing(
    GameLocation Location,
    Farmer Player,
    Warp FirstWarp,
    List<Point> ReachableTiles,
    List<CustomerActor> CustomerActors,
    List<ForSaleTarget> ForSaleTables
)
{
    public static ShopkeepBrowsing? Make(GameLocation location, Farmer player, List<string> customerNPCs)
    {
        if (location.warps.Count < 1)
        {
            ModEntry.Log($"Location {location.NameOrUniqueName} has no warps in", LogLevel.Error);
            return null;
        }
        Warp firstWarp = location.warps[0];
        List<Point> reachableTiles = Topology.TileStandableBFS(location, new(firstWarp.X, firstWarp.Y - 1));
        if (!reachableTiles.Any())
        {
            ModEntry.Log($"Location {location.NameOrUniqueName} has no reachable tiles", LogLevel.Error);
            return null;
        }

        List<CustomerActor> customerActors = [];
        foreach (string npcName in customerNPCs)
        {
            if (CustomerActor.Make(location, npcName) is CustomerActor customer)
            {
                ModEntry.LogDebug($"Customer: {npcName}");
                customerActors.Add(customer);
            }
        }

        List<ForSaleTarget> forSaleTables = [];
        foreach (Furniture furniture in location.furniture)
        {
            if (furniture.heldObject.Value != null)
            {
                IReadOnlyList<(Point, int)> browseAround = FormBrowseAround(furniture, reachableTiles).ToList();
                if (!browseAround.Any())
                    continue;

                if (furniture.heldObject.Value is Chest chest)
                {
                    foreach (Item item in chest.Items)
                    {
                        if (item != null && item.sellToStorePrice(player.UniqueMultiplayerID) > 0)
                        {
                            forSaleTables.Add(new(item, furniture, browseAround));
                        }
                    }
                }
                else if (furniture.heldObject.Value.sellToStorePrice(player.UniqueMultiplayerID) > 0)
                {
                    ModEntry.LogDebug(
                        $"ForSale: {furniture.heldObject.Value.DisplayName} ({string.Join(',', browseAround)})"
                    );
                    forSaleTables.Add(new(furniture.heldObject.Value, furniture, browseAround));
                }
            }
        }

        return new(location, player, firstWarp, reachableTiles, customerActors, forSaleTables);
    }

    internal bool TryGetHaggle(out ShopkeepHaggle haggling)
    {
        haggling = ShopkeepHaggle.Make(Player, CustomerActors[0], ItemRegistry.Create("(O)Book_Void"));
        return true;
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
            pnt = new(x, boundingBox.Bottom + 1);
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
            pnt = new(boundingBox.Right + 1, y);
            if (reachable.Contains(pnt))
                yield return new(pnt, 3);
        }
    }
}
