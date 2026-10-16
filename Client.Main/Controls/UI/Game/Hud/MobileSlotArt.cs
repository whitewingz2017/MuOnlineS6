using Microsoft.Xna.Framework.Graphics;
using Client.Main.Controllers;
namespace Client.Main.Controls.UI.Game.Hud;
internal static class MobileSlotArt
{
    private static Texture2D _plus;
    internal static Texture2D Plus
    {
        get
        {
            if (_plus == null || _plus.IsDisposed || _plus.GraphicsDevice != GraphicsManager.Instance.GraphicsDevice)
            {
                using var stream = MobilePlayerPanel.OpenTexture("plus_button");
                _plus = Texture2D.FromStream(MuGame.Instance.GraphicsDevice, stream);
            }
            return _plus;
        }
    }
}
