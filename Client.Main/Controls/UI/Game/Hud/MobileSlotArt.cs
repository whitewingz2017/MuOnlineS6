using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Controllers;
namespace Client.Main.Controls.UI.Game.Hud;
internal static class MobileSlotArt
{
    private static Texture2D _plus;
    private static Texture2D _occupiedCircle;
    internal static void DrawOccupiedCircle(SpriteBatch batch,Rectangle slot)
    {
        if(_occupiedCircle==null || _occupiedCircle.IsDisposed || _occupiedCircle.GraphicsDevice!=batch.GraphicsDevice)
        {
            const int size=128;
            var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=x+.5f-size*.5f,dy=y+.5f-size*.5f;
                float coverage=Math.Clamp(size*.5f-MathF.Sqrt(dx*dx+dy*dy),0,1);
                colors[y*size+x]=new Color(0,0,0,(int)(128*coverage));
            }
            _occupiedCircle=new Texture2D(batch.GraphicsDevice,size,size);_occupiedCircle.SetData(colors);
        }
        int diameter=Math.Max(slot.Width,slot.Height);
        batch.Draw(_occupiedCircle,new Rectangle(slot.Center.X-diameter/2,slot.Center.Y-diameter/2,diameter,diameter),Color.White);
    }
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
