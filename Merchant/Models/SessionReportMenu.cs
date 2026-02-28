using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Merchant.Models;

public sealed class SessionReportMenu : IClickableMenu
{
    private const int CELL_WIDTH = 300;
    private const int CELL_HEIGHT = 80;
    private const int ROWS = 8;
    private const int COLS = 4;

    private sealed class SoldRecordDisplay(
        SoldRecord record,
        Rectangle bounds,
        Item soldItem,
        Texture2D sprite,
        Rectangle mugshotSourceRect
    ) : ClickableComponent(bounds, soldItem)
    {
        private const int ICON_YOFFSET = (CELL_HEIGHT - 64) / 2;
        private const int TEXT_YOFFSET = (CELL_HEIGHT - 60) / 2;

        private readonly Rectangle shopBgRect = new(384, 396, 15, 15);

        private readonly string priceText = string.Concat(record.Price, "$");

        public void Draw(SpriteBatch b)
        {
            drawTextureBox(
                b,
                Game1.mouseCursors,
                shopBgRect,
                bounds.X,
                bounds.Y,
                CELL_WIDTH,
                CELL_HEIGHT,
                Color.White,
                scale: 4,
                drawShadow: false
            );
            int xOffset = bounds.X + 10;
            int y = bounds.Y;
            b.Draw(
                sprite,
                new Rectangle(
                    xOffset,
                    y + CELL_HEIGHT - mugshotSourceRect.Height * 4,
                    mugshotSourceRect.Width * 4,
                    mugshotSourceRect.Height * 4
                ),
                mugshotSourceRect,
                Color.White
            );
            xOffset += mugshotSourceRect.Width * 4 + 4;
            item.drawInMenu(b, new(xOffset, y + ICON_YOFFSET), 1f);
            xOffset += 64 + 4;
            SpriteText.drawString(b, priceText, xOffset, y + TEXT_YOFFSET);
        }

        internal void DrawToolTip(SpriteBatch b)
        {
            drawToolTip(b, item.getDescription(), item.DisplayName, item);
        }
    }

    private readonly List<SoldRecordDisplay> soldRecordDisplays = [];
    private SoldRecordDisplay? hoveredDisplay = null;

    public static SessionReportMenu Make(ShopkeepSessionLog sessionLog)
    {
        return new(sessionLog);
    }

    public SessionReportMenu(ShopkeepSessionLog sessionLog)
        : base(0, 0, CELL_WIDTH * COLS, CELL_HEIGHT * ROWS, true)
    {
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                int idx = col + row * COLS;
                // if (idx >= sessionLog.Sales.Count)
                //     break;

                int x = xPositionOnScreen + col * CELL_WIDTH;
                int y = yPositionOnScreen + row * CELL_HEIGHT;
                SoldRecord record = sessionLog.Sales[idx % sessionLog.Sales.Count];
                Item SoldItem = ItemRegistry.Create(record.ItemId);
                Texture2D Sprite;
                Rectangle MugshotSourceRect;
                if (ModEntry.FriendEntries.TryGetByName(record.Buyer, out NPC? npc))
                {
                    Sprite = npc.Sprite.Texture;
                    MugshotSourceRect = npc.getMugShotSourceRect();
                }
                else
                {
                    Sprite = Game1.content.Load<Texture2D>("Characters/Monsters/Skeleton");
                    MugshotSourceRect = new(0, 0, 16, 24);
                }
                int myID = 100 + idx;
                soldRecordDisplays.Add(
                    new(record, new(x, y, CELL_WIDTH, CELL_HEIGHT), SoldItem, Sprite, MugshotSourceRect)
                    {
                        myID = myID,
                        upNeighborID = row > 0 ? myID - COLS : ClickableComponent.ID_ignore,
                        upNeighborImmutable = true,
                        leftNeighborID = col > 0 ? myID - 1 : ClickableComponent.ID_ignore,
                        rightNeighborID = col < COLS - 1 ? myID + 1 : ClickableComponent.ID_ignore,
                        downNeighborID = row < COLS ? myID + COLS : ClickableComponent.ID_ignore,
                        downNeighborImmutable = true,
                    }
                );
            }
        }
        Recenter();
        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
        {
            populateClickableComponentList();
            snapToDefaultClickableComponent();
        }
    }

    private void Recenter()
    {
        Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(
            CELL_WIDTH * COLS,
            CELL_HEIGHT * ROWS
        );
        xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
        yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y;
        base.initialize(xPositionOnScreen, yPositionOnScreen, width, height, true);
        for (int i = 0; i < soldRecordDisplays.Count; i++)
        {
            SoldRecordDisplay record = soldRecordDisplays[i];
            record.bounds.X = xPositionOnScreen + i % COLS * CELL_WIDTH;
            record.bounds.Y = yPositionOnScreen + i / COLS * CELL_HEIGHT;
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        foreach (SoldRecordDisplay display in soldRecordDisplays)
        {
            if (display.containsPoint(x, y))
            {
                hoveredDisplay = display;
                return;
            }
        }
        hoveredDisplay = null;
    }

    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, xPositionOnScreen - 20, yPositionOnScreen - 20, width + 40, height + 40, Color.White);
        foreach (SoldRecordDisplay display in soldRecordDisplays)
        {
            display.Draw(b);
        }
        if (hoveredDisplay != null)
        {
            hoveredDisplay.DrawToolTip(b);
            ;
        }
        base.draw(b);
        drawMouse(b);
    }
}
