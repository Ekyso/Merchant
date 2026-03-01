namespace Merchant.Models;

public sealed class CustomerData
{
    // Will Shop
    public string? Condition { get; set; } = null;
    public float Chance { get; set; } = 1.0f;

    // Haggle Dialogue
    public Dictionary<string, HaggleDialogue> HaggleDialogue = [];
}

public class HaggleDialogue
{
    public string? Ask { get; set; } = null;
    public string? Compromise { get; set; } = null;
    public string? Overpriced { get; set; } = null;
    public string? Success { get; set; } = null;
    public string? Fail { get; set; } = null;
}
