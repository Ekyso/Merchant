using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Merchant.Management;

public sealed record ShopkeepHaggle(NPC Buyer, Item ForSale, float MinMult, float MaxMult, int MaxCount)
{
    public enum PointerState
    {
        Begin,
        Increase,
        Decrease,
        DoneSuccess,
        DoneFailed,
    }

    private const double timeoutMS = 1500.0;
    private TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMS);
    public PointerState state = PointerState.Begin;
    public bool IsReadyToStart => !IsPointerMoving && Game1.activeClickableMenu is DialogueBox { transitioning: false };
    public bool IsPointerMoving => state == PointerState.Increase || state == PointerState.Decrease;
    public bool IsDone => state == PointerState.DoneSuccess || state == PointerState.DoneFailed;
    private float pointer = 0;

    public int Count { get; private set; } = 0;
    public float TargetPointer { get; private set; } = Random.Shared.NextSingle();
    public float PickedMult { get; private set; } = -1;

    private const string haggleDialogueKey = $"{ModEntry.ModId}_haggle";
    private const int haggleBarWidth = 1200;
    private const int haggleBarHeight = 80;
    private Rectangle haggleBarBounds = Rectangle.Empty;
    private Rectangle targetPointerBounds = Rectangle.Empty;

    public void Initialize()
    {
        Dialogue dialogue = new(Buyer, haggleDialogueKey, $"TODO: I want to buy {ForSale.DisplayName}");
        SetNextDialogue(dialogue);
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        Vector2 position = Utility.getTopLeftPositionForCenteringOnScreen(haggleBarWidth, haggleBarHeight, 0, 0);
        haggleBarBounds = new(
            (int)position.X,
            (int)MathF.Min(position.Y, Game1.viewport.Height - 600),
            haggleBarWidth,
            haggleBarHeight
        );
        CalculateTargetPointerBounds();
    }

    private void CalculateTargetPointerBounds()
    {
        float targetPosMin = Utility.Lerp(haggleBarBounds.Left, haggleBarBounds.Right, TargetPointer - 0.05f);
        float targetPosMax = Utility.Lerp(haggleBarBounds.Left, haggleBarBounds.Right, TargetPointer + 0.05f);
        targetPointerBounds = new(
            (int)targetPosMin,
            haggleBarBounds.Y,
            (int)(targetPosMax - targetPosMin),
            haggleBarHeight
        );
    }

    private void SetNextDialogue(Dialogue dialogue, bool transitioning = true)
    {
        Game1.activeClickableMenu = new DialogueBox(dialogue) { showTyping = false, transitioning = transitioning };
    }

    public bool BeginHaggleRound()
    {
        if (state == PointerState.DoneSuccess || state == PointerState.DoneFailed)
        {
            Game1.exitActiveMenu();
            return false;
        }
        pointer = 0f;
        state = PointerState.Increase;
        timeout = TimeSpan.FromMilliseconds(timeoutMS);
        Count++;
        return true;
    }

    public void Update(GameTime time)
    {
        if (state == PointerState.Begin || state == PointerState.DoneSuccess || state == PointerState.DoneFailed)
        {
            return;
        }

        timeout -= time.ElapsedGameTime;
        if (timeout <= TimeSpan.Zero)
        {
            if (state == PointerState.Decrease)
            {
                state = Count >= MaxCount ? PointerState.DoneFailed : PointerState.Begin;
            }
            else
            {
                state = PointerState.Decrease;
                timeout = TimeSpan.FromMilliseconds(timeoutMS);
            }
        }
        else
        {
            if (state == PointerState.Increase)
            {
                pointer = (float)(1.0 - (timeout.TotalMilliseconds / timeoutMS));
            }
            else
            {
                pointer = (float)(timeout.TotalMilliseconds / timeoutMS);
            }
        }
    }

    public void Pick()
    {
        if (pointer <= TargetPointer)
        {
            PickedMult = Utility.Lerp(MinMult, MaxMult, pointer);
            state = PointerState.DoneSuccess;
        }
        else if (Count >= MaxCount)
        {
            state = PointerState.DoneFailed;
        }
        else
        {
            TargetPointer = Utility.Lerp(TargetPointer, pointer, Random.Shared.NextSingle());
            CalculateTargetPointerBounds();
            state = PointerState.Begin;
        }
    }

    private static readonly Vector2 HaggleDrawPos = new(12, 12 + 64);

    public void Draw(SpriteBatch b)
    {
#if DEBUG
        b.Draw(Game1.staminaRect, new Rectangle(0, 64, Game1.viewport.Width, 64), Color.Black * 0.5f);
        b.DrawString(
            Game1.dialogueFont,
            $"Haggle {Count} {state}: Pick {Utility.Lerp(MinMult, MaxMult, pointer):0.00} ({MinMult:0.00} - {MaxMult:0.00}, Target {TargetPointer:0.00})",
            HaggleDrawPos,
            Color.White
        );
#endif

        IClickableMenu.drawTextureBox(
            b,
            haggleBarBounds.X,
            haggleBarBounds.Y,
            haggleBarBounds.Width,
            haggleBarBounds.Height,
            Color.White
        );
        Utility.DrawSquare(b, targetPointerBounds, 0, backgroundColor: Color.Blue * 0.7f);

        float pointerPos = Utility.Lerp(haggleBarBounds.Left, haggleBarBounds.Right, pointer);
        Utility.DrawSquare(
            b,
            new((int)(pointerPos - (haggleBarHeight / 2)), targetPointerBounds.Y, haggleBarHeight, haggleBarHeight),
            0,
            backgroundColor: Color.Green * 0.7f
        );
    }
}
