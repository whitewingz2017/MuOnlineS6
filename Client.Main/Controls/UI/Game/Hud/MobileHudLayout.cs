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
        var font=GraphicsManager.Instance.Font;if(font==null)return;
        float scale=size/font.LineSpacing;
        var measure=font.MeasureString(value)*scale;
        if(measure.X>bounds.Width-6)scale*=Math.Max(0,(bounds.Width-6)/measure.X);
        var position=new Vector2(center?bounds.Center.X-font.MeasureString(value).X*scale/2:bounds.X+4,bounds.Center.Y-font.LineSpacing*scale/2);
        batch.DrawString(font,value,position+Vector2.One,Color.Black,0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.DrawString(font,value,position,color,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
}
