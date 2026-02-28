using System.Diagnostics.CodeAnalysis;
using Merchant.Misc;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace Merchant.Management;

public interface ITableShim
{
    bool TryGetForSaleTargets(
        Furniture table,
        Farmer player,
        List<Point> reachableTiles,
        [NotNullWhen(true)] out List<ForSaleTarget?>? forSaleTargets
    );
    bool TryRemoveItemFromTable(Furniture table, Item item);
    bool TryPlaceItemOnTable(Furniture table, ref Item? item);
    bool HasSpaceForItems(Furniture table);
}

public sealed class TableShimVanilla : ITableShim
{
    public bool TryGetForSaleTargets(
        Furniture table,
        Farmer player,
        List<Point> reachableTiles,
        [NotNullWhen(true)] out List<ForSaleTarget?>? forSaleTargets
    )
    {
        forSaleTargets = null;
        if (!table.IsTable() || table.heldObject.Value == null)
        {
            return false;
        }
        Rectangle boundingBox = new(
            (int)table.TileLocation.X,
            (int)table.TileLocation.Y,
            table.getTilesWide(),
            table.getTilesHigh()
        );
        List<(Point, int)> browseAround = Topology.FormBrowseAround(boundingBox, reachableTiles);
        bool unreachable = !browseAround.Any();

        if (ForSaleTarget.CanOfferForSale(table.heldObject.Value, player))
        {
            if (unreachable)
            {
                forSaleTargets = [null];
                return true;
            }
            else
            {
                forSaleTargets = [new(table.heldObject.Value, table, browseAround)];
                return true;
            }
        }
        return false;
    }

    public bool HasSpaceForItems(Furniture table)
    {
        return table.IsTable() && table.heldObject.Value == null;
    }

    public bool TryRemoveItemFromTable(Furniture table, Item item)
    {
        if (item is SObject obj && table.heldObject.Value == item)
        {
            obj.onDetachedFromParent();
            obj.performRemoveAction();
            table.heldObject.Value = null;
            return true;
        }
        return false;
    }

    public bool TryPlaceItemOnTable(Furniture table, ref Item? item)
    {
        if (table.performObjectDropInAction(item, true, null))
        {
            table.performObjectDropInAction(item, false, null);
            item = item?.ConsumeStack(1);
            return true;
        }
        return false;
    }
}
