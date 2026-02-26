using Merchant.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Merchant.Models;

public sealed class SessionReportMenu : IClickableMenu
{
    private readonly ShopkeepSessionLog sessionLog;

    public SessionReportMenu(ShopkeepSessionLog sessionLog)
        : base(0, 0, 600, 1200, true)
    {
        Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(
            width,
            base.height
        );
        xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X;
        yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y;

        this.sessionLog = sessionLog;
    }
}
