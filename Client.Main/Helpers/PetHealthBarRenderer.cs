#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Controls.UI;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Helpers;

public static class PetHealthBarRenderer
{
    private static Texture2D? _fill;
    public static async Task LoadAsync() => _fill = await UiThemeManager.LoadThemeTextureAsync("Interface/newui_Pet_HpBar.OZJ");
    public static void Draw(SpriteBatch batch, Rectangle bounds, float? fraction, float depth = 0)
    {
        var pixel=GraphicsManager.Instance.Pixel;
        batch.Draw(pixel,bounds,null,new Color(123,104,65),0,Vector2.Zero,SpriteEffects.None,depth);
        var inside=new Rectangle(bounds.X+1,bounds.Y+1,Math.Max(0,bounds.Width-2),Math.Max(1,bounds.Height-2));
        batch.Draw(pixel,inside,null,new Color(24,9,9),0,Vector2.Zero,SpriteEffects.None,depth);
        // Unknown scope health is a dim preview, not a zero-health claim.
        float value=Math.Clamp(fraction ?? 1f,0,1);
        int width=(int)(inside.Width*value);
        if(width<=0)return;
        var texture=_fill is {IsDisposed:false}?_fill:pixel;
        var source=new Rectangle(0,0,Math.Max(1,(int)(texture.Width*value)),texture.Height);
        batch.Draw(texture,new Rectangle(inside.X,inside.Y,width,inside.Height),source,
            (texture==pixel?new Color(220,35,25):Color.White)*(fraction.HasValue?1f:.85f),0,Vector2.Zero,SpriteEffects.None,depth);
    }
}
