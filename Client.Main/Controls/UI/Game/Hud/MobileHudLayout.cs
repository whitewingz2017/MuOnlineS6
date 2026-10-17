using System;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Controls.UI.Game.Hud;

public static class MobileHudLayout
{
    public static int KeyboardInsetPixels { get; set; }
    public static int ChatWidth => Math.Clamp((int)(UiScaler.WindowBounds.Width*.44f),280,560);
    public static int ChatX => UiScaler.WindowBounds.Center.X-ChatWidth/2;
    public static int ChatBottom => UiScaler.WindowBounds.Bottom-14-(int)(KeyboardInsetPixels*UiScaler.InverseScaleY);
    public static void Text(SpriteBatch batch,string value,Rectangle bounds,float size,Color color,bool center=true)
    {
        var font=GraphicsManager.GetUiFont(size,out float scale);if(font==null)return;
        // Font atlas line heights can exceed a short button's UI height at high DPI.
        // Keep this division fractional or its scale truncates to zero.
        scale=Math.Min(scale,Math.Max(0,(bounds.Height-2)/(float)font.LineSpacing));
        var measure=font.MeasureString(value)*scale;
        if(measure.X>bounds.Width-6)scale*=Math.Max(0,(bounds.Width-6)/measure.X);
        var position=new Vector2(center?bounds.Center.X-font.MeasureString(value).X*scale/2:bounds.X+4,bounds.Center.Y-font.LineSpacing*scale/2);
        Client.Main.Helpers.OutlinedTextRenderer.Draw(batch,font,value,position,color,scale);
    }
}
