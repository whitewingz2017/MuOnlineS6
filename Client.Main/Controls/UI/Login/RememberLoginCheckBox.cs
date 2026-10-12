using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Login;

internal sealed class RememberLoginCheckBox : ButtonControl
{
    private readonly LabelControl _label;
    public bool Checked { get; set; }

    public RememberLoginCheckBox()
    {
        Interactive = true;
        AutoViewSize = false;
        Controls.Add(_label = new LabelControl
        {
            Text = "Remember login details", X = 34, Y = 8, FontSize = 14,
            TextColor = Color.White, Interactive = false
        });
    }

    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        base.Draw(gameTime);
        // Textureless SpriteControl buttons skip drawing their child controls.
        _label.Draw(gameTime);
        var sprite = GraphicsManager.Instance.Sprite;
        var pixel = GraphicsManager.Instance.Pixel;
        var r = DisplayRectangle;
        var box = new Rectangle(r.X + 5, r.Y + (r.Height - 22) / 2, 22, 22);
        sprite.Draw(pixel, box, Color.Goldenrod);
        sprite.Draw(pixel, new Rectangle(box.X + 2, box.Y + 2, 18, 18), new Color(12, 12, 16));
        if (Checked)
        {
            sprite.Draw(pixel, new Vector2(box.X + 4, box.Y + 10), null, Color.Goldenrod,
                MathHelper.PiOver4, Vector2.Zero, new Vector2(7, 3), SpriteEffects.None, 0);
            sprite.Draw(pixel, new Vector2(box.X + 9, box.Y + 15), null, Color.Goldenrod,
                -MathHelper.PiOver4, Vector2.Zero, new Vector2(13, 3), SpriteEffects.None, 0);
        }
    }
}
