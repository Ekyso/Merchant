using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;

namespace Merchant.Misc;

public static class Topology
{
    public static List<(Point, int)> FormBrowseAround(Rectangle boundingBox, List<Point> reachable)
    {
        List<(Point, int)> browseAround = [];
        Point pnt;
        for (int i = 0; i < boundingBox.Width; i++)
        {
            int x = boundingBox.Left + i;
            // X
            // .
            pnt = new(x, boundingBox.Bottom);
            if (reachable.Contains(pnt))
                browseAround.Add(new(pnt, 0));
            // .
            // X
            pnt = new(x, boundingBox.Top - 1);
            if (reachable.Contains(pnt))
                browseAround.Add(new(pnt, 2));
        }
        for (int i = 0; i < boundingBox.Height; i++)
        {
            int y = boundingBox.Top + i;
            // .X
            pnt = new(boundingBox.Left - 1, y);
            if (reachable.Contains(pnt))
                browseAround.Add(new(pnt, 1));
            // X.
            pnt = new(boundingBox.Right, y);
            if (reachable.Contains(pnt))
                browseAround.Add(new(pnt, 3));
        }
        return browseAround;
    }

    private static IEnumerable<Point> SurroundingTiles(Point nextPoint, int maxX, int maxY)
    {
        if (nextPoint.X > 0)
            yield return new(nextPoint.X - 1, nextPoint.Y);
        if (nextPoint.Y > 0)
            yield return new(nextPoint.X, nextPoint.Y - 1);
        if (nextPoint.X < maxX - 1)
            yield return new(nextPoint.X + 1, nextPoint.Y);
        if (nextPoint.Y < maxY - 1)
            yield return new(nextPoint.X, nextPoint.Y + 1);
    }

    internal static bool IsTileStandable(
        GameLocation location,
        Point tile,
        CollisionMask collisionMask = ~(CollisionMask.Characters | CollisionMask.Farmers)
    )
    {
        return IsTilePassable(location, tile)
            && !IsWarp(location, tile)
            && !location.IsTileBlockedBy(
                tile.ToVector2(),
                collisionMask: collisionMask,
                ignorePassables: CollisionMask.All
            )
            && (location is not DecoratableLocation decoLoc || !decoLoc.isTileOnWall(tile.X, tile.Y));
    }

    /// <summary>Get whether players can walk on a map tile.</summary>
    /// <param name="location">The location to check.</param>
    /// <param name="tile">The tile position.</param>
    /// <remarks>This is derived from <see cref="GameLocation.isTilePassable(Vector2)" />, but also checks tile properties in addition to tile index properties to match the actual game behavior.</remarks>
    /// <remarks>Originally written for DataLayers</remarks>
    private static bool IsTilePassable(GameLocation location, Point tile)
    {
        // passable if Buildings layer has 'Passable' property
        xTile.Tiles.Tile? buildingTile = location.map.RequireLayer("Buildings").Tiles[(int)tile.X, (int)tile.Y];
        if (buildingTile?.Properties.ContainsKey("Passable") is true)
            return true;

        // non-passable if Back layer has 'Passable' or 'NPCBarrier' property
        xTile.Tiles.Tile? backTile = location.map.RequireLayer("Back").Tiles[(int)tile.X, (int)tile.Y];
        if (backTile?.Properties.ContainsKey("Passable") is true)
            return false;
        if (backTile?.Properties.ContainsKey("NPCBarrier") is true)
            return false;

        // else check tile indexes
        return location.isTilePassable(tile.ToVector2());
    }

    private static bool IsWarp(GameLocation location, Point tile)
    {
        if (location.warps.Any(warp => warp.X == tile.X && warp.Y == tile.Y))
        {
            return true;
        }
        if (location.doors.ContainsKey(tile))
        {
            return true;
        }
        if (location.doesTileHaveProperty(tile.X, tile.Y, "TouchAction", "Back") is string touchAction)
        {
            return touchAction == "Warp" || touchAction == "MagicWarp";
        }
        return false;
    }

    internal static List<Point> TileStandableBFS(
        GameLocation location,
        Point startingTile,
        CollisionMask collisionMask = ~CollisionMask.Characters
    )
    {
        int maxX = location.Map.DisplayWidth / 64;
        int maxY = location.Map.DisplayHeight / 64;
        Dictionary<Point, bool> tileStandableState = [];
        tileStandableState[startingTile] = IsTileStandable(location, startingTile, collisionMask);
        Queue<(Point, int)> tileQueue = [];
        tileQueue.Enqueue(new(startingTile, 0));
        int standableCnt = 1;
        while (tileQueue.Count > 0)
        {
            (Point, int) next = tileQueue.Dequeue();
            Point nextPoint = next.Item1;
            int depth = next.Item2 + 1;
            foreach (Point neighbour in SurroundingTiles(nextPoint, maxX, maxY))
            {
                if (!tileStandableState.ContainsKey(neighbour))
                {
                    bool standable = IsTileStandable(location, neighbour, collisionMask);
                    tileStandableState[neighbour] = standable;
                    if (standable)
                    {
                        standableCnt++;
                        tileQueue.Enqueue(new(neighbour, depth));
                    }
                }
            }
        }
        return tileStandableState.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
    }
}
