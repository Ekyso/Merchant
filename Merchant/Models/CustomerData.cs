namespace Merchant.Models;

public sealed class CustomerData
{
    // Will Shop
    public string? Condition { get; set; } = null;
    public float Chance { get; set; } = 1.0f;

    // Dialogue
    public string? Haggle_Ask { get; set; } = null;
    public string? Haggle_Compromise { get; set; } = null;
    public string? Haggle_Overpriced { get; set; } = null;
    public string? Haggle_Fail { get; set; } = null;
    public string? Haggle_Success { get; set; } = null;
}
