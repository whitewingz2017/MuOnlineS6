using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Hud;

public sealed class BatteryStatusControl : UIControl
{
    private readonly LabelControl _label;
    private double _nextRefresh;
    private int _percent = -1;
    public BatteryStatusControl()
    {
        Interactive = false;
        AutoViewSize = false;
        ControlSize = ViewSize = new Point(100, 20);
        Align = Client.Main.Models.ControlAlign.Bottom | Client.Main.Models.ControlAlign.Left;
        Margin = new Margin { Bottom = 6, Left = 350 };
        _label = new LabelControl { X = 30, FontSize = 10, Text = "--%", TextColor = Color.White };
        Controls.Add(_label);
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (gameTime.TotalGameTime.TotalSeconds < _nextRefresh) return;
        _nextRefresh = gameTime.TotalGameTime.TotalSeconds + 15;
#if ANDROID
        var manager = Android.App.Application.Context.GetSystemService(Android.Content.Context.BatteryService) as Android.OS.BatteryManager;
        _percent = manager?.GetIntProperty((int)Android.OS.BatteryProperty.Capacity) ?? -1;
#endif
        _label.Text = _percent is >= 0 and <= 100 ? $"{_percent}%" : "--%";
        _label.TextColor = _percent is >= 0 and <= 20 ? Color.OrangeRed : Color.White;
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible) return;
        var sprite = GraphicsManager.Instance.Sprite;
        var pixel = GraphicsManager.Instance.Pixel;
        var r = DisplayRectangle;
        var color = _percent is >= 0 and <= 20 ? Color.OrangeRed : Color.LightGreen;
        sprite.Draw(pixel, new Rectangle(r.X, r.Y + 3, 23, 13), color);
        sprite.Draw(pixel, new Rectangle(r.X + 2, r.Y + 5, 19, 9), Color.Black);
        sprite.Draw(pixel, new Rectangle(r.X + 23, r.Y + 7, 3, 5), color);
        if (_percent is >= 0 and <= 100)
            sprite.Draw(pixel, new Rectangle(r.X + 3, r.Y + 6, (int)(17 * _percent / 100f), 7), color);
        base.Draw(gameTime);
    }
}
