namespace Merchant.Misc;

public static class Ease
{
    // https://easings.net/
    public static float InQuad(float progValue, float baseValue = 0)
    {
        return baseValue + progValue * progValue;
    }
}
