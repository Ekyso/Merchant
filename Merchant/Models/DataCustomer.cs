using System.Diagnostics.CodeAnalysis;
using StardewValley.Extensions;

namespace Merchant.Models;

public enum CustomerDialogueKind
{
    Haggle_Ask = 0,
    Haggle_Compromise = 1,
    Haggle_Overpriced = 2,
    Haggle_Success = 3,
    Haggle_Fail = 4,
}

public sealed class CustomerData
{
    // Will Shop
    public string? Condition { get; set; } = null;
    public float Chance { get; set; } = 1.0f;

    // Haggle Dialogue
    public Dictionary<string, HaggleDialogue> Dialogue = [];

    private List<string>[]? MergedHaggleDialogue
    {
        get
        {
            if (field != null || Dialogue == null)
                return field;
            field =
            [
                [],
                [],
                [],
                [],
                [],
            ];
            foreach (HaggleDialogue haggleDialogue in Dialogue.Values)
            {
                if (haggleDialogue.Ask != null)
                    field[(int)CustomerDialogueKind.Haggle_Ask].Add(haggleDialogue.Ask);
                if (haggleDialogue.Compromise != null)
                    field[(int)CustomerDialogueKind.Haggle_Compromise].Add(haggleDialogue.Compromise);
                if (haggleDialogue.Overpriced != null)
                    field[(int)CustomerDialogueKind.Haggle_Overpriced].Add(haggleDialogue.Overpriced);
                if (haggleDialogue.Success != null)
                    field[(int)CustomerDialogueKind.Haggle_Success].Add(haggleDialogue.Success);
                if (haggleDialogue.Fail != null)
                    field[(int)CustomerDialogueKind.Haggle_Fail].Add(haggleDialogue.Fail);
            }
            return field;
        }
    } = null;

    internal bool TryGetCustomerDialogue(CustomerDialogueKind kind, [NotNullWhen(true)] out string? dialogueText)
    {
        dialogueText = null;
        if (MergedHaggleDialogue == null || (int)kind >= MergedHaggleDialogue.Length)
            return false;
        dialogueText = Random.Shared.ChooseFrom(MergedHaggleDialogue[(int)kind]);
        return dialogueText != null;
    }
}

public sealed class HaggleDialogue
{
    public string? Ask { get; set; } = null;
    public string? Compromise { get; set; } = null;
    public string? Overpriced { get; set; } = null;
    public string? Success { get; set; } = null;
    public string? Fail { get; set; } = null;
}
