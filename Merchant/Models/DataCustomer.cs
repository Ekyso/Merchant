using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
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
    public Dictionary<string, CustomerDialogue> Dialogue = [];

    private List<string>[]? mergedDialogues = null;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        // merge
        mergedDialogues =
        [
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
        ];
        foreach (CustomerDialogue dialogue in Dialogue.Values)
        {
            if (dialogue.Haggle_Ask != null)
                mergedDialogues[(int)CustomerDialogueKind.Haggle_Ask].Add(dialogue.Haggle_Ask);
            if (dialogue.Haggle_Compromise != null)
                mergedDialogues[(int)CustomerDialogueKind.Haggle_Compromise].Add(dialogue.Haggle_Compromise);
            if (dialogue.Haggle_Overpriced != null)
                mergedDialogues[(int)CustomerDialogueKind.Haggle_Overpriced].Add(dialogue.Haggle_Overpriced);
            if (dialogue.Haggle_Success != null)
                mergedDialogues[(int)CustomerDialogueKind.Haggle_Success].Add(dialogue.Haggle_Success);
            if (dialogue.Haggle_Fail != null)
                mergedDialogues[(int)CustomerDialogueKind.Haggle_Fail].Add(dialogue.Haggle_Fail);
        }
    }

    internal bool TryGetDialogueText(CustomerDialogueKind kind, [NotNullWhen(true)] out string? dialogueText)
    {
        dialogueText = null;
        if (mergedDialogues == null || (int)kind >= mergedDialogues.Length)
            return false;
        dialogueText = Random.Shared.ChooseFrom(mergedDialogues[(int)kind]);
        return dialogueText != null;
    }
}

public sealed class CustomerDialogue
{
    public string? Haggle_Ask { get; set; } = null;
    public string? Haggle_Compromise { get; set; } = null;
    public string? Haggle_Overpriced { get; set; } = null;
    public string? Haggle_Success { get; set; } = null;
    public string? Haggle_Fail { get; set; } = null;
}
