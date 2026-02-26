using System.Text;

namespace Merchant.Models;

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
