using Microsoft.Xna.Framework;
using StardewValley;

namespace Merchant.Misc;

public record FriendEntry(NPC Npc, Friendship Fren, int MaxHeartCount)
{
    public const int OneHeart = 250;
    public float FrenPercent => Fren.Points / (float)(OneHeart * MaxHeartCount);
    public bool IsMaxedHeart => Fren.Points == OneHeart * MaxHeartCount;
}

internal static class NPCLookup
{
    private static List<FriendEntry>? sorted = null;
    private static int bisect = 0;

    internal static void Clear() => sorted = null;

    internal static IEnumerable<FriendEntry> PickNRandomNPCs(Farmer player, int count = 10, int startIdx = 0)
    {
        sorted ??= PopulateSortedNPCList(player);
        int validCount = sorted.Count - startIdx;
        if (validCount == 0)
            yield break;
        List<int> ranges = Enumerable.Range(startIdx, validCount).ToList();
        Random.Shared.ShuffleInPlace(ranges);
        for (int i = 0; i < Math.Min(ranges.Count, count); i++)
        {
            yield return sorted[ranges[i]];
        }
    }

    internal static IEnumerable<FriendEntry> PickCustomerNPCs(Farmer player, int maxCount)
    {
        foreach (FriendEntry npc in PickNRandomNPCs(player, 4, bisect))
        {
            maxCount--;
            yield return npc;
            if (maxCount == 0)
                yield break;
        }
        foreach (FriendEntry npc in PickNRandomNPCs(player, 8, 0))
        {
            maxCount--;
            yield return npc;
            if (maxCount == 0)
                yield break;
        }
    }

    private static List<FriendEntry> PopulateSortedNPCList(Farmer player)
    {
        List<FriendEntry> newSortedList = [];
        Utility.ForEachVillager(npc =>
        {
            if (npc.CanSocialize && player.friendshipData.TryGetValue(npc.Name, out Friendship friendship))
                newSortedList.Add(new(npc, friendship, Utility.GetMaximumHeartsForCharacter(npc)));
            return true;
        });
        newSortedList.Sort(
            (npcA, npcB) =>
            {
                if (npcA.Fren.Points == npcB.Fren.Points)
                    return 0;
                return npcA.FrenPercent.CompareTo(npcB.FrenPercent);
            }
        );
        bisect = newSortedList.Count;
        for (int i = 0; i < newSortedList.Count; i++)
        {
            if (newSortedList[i].IsMaxedHeart)
            {
                bisect = i;
                break;
            }
        }
        return newSortedList;
    }
}
