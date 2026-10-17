using System;
using Client.Main.Content;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Helpers;

/// <summary>MU's digit, Miss and separator artwork, loaded once before combat.</summary>
public static class CombatTextAtlas
{
    private static Texture2D _digits, _miss, _separator;
    private static readonly Rectangle[] DigitSources =
    {
        new(5,0,35,64), new(55,0,23,64), new(97,0,36,64), new(144,0,35,64),
        new(189,0,37,64), new(237,0,35,64), new(284,0,35,64), new(330,0,35,64),
        new(377,0,35,64), new(423,0,35,64), new(469,0,37,64)
    };

    public static async Task LoadAsync()
    {
        if (_digits is { IsDisposed: false }) return;
        var textures = await Task.WhenAll(
            TextureLoader.Instance.PrepareAndGetTexture("Interface/Font_Damage.OZT"),
            TextureLoader.Instance.PrepareAndGetTexture("Interface/font_miss.OZT"),
            TextureLoader.Instance.PrepareAndGetTexture("Interface/FontSC.OZT"));
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        MuGame.ScheduleOnMainThread(() =>
        {
            try
            {
                // OZT is straight alpha; keep the shared loader textures untouched.
                _digits = Premultiply(textures[0]);
                _miss = Premultiply(textures[1]);
                _separator = Premultiply(textures[2]);
                ready.SetResult(true);
            }
            catch (Exception error) { ready.SetException(error); }
        });
        await ready.Task;
    }

    private static Texture2D Premultiply(Texture2D source)
    {
        if (source == null) return null;
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.FromNonPremultiplied(pixels[i].ToVector4());
        var result = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
        result.SetData(pixels);
        return result;
    }

    public static bool TryDraw(SpriteBatch batch, string text, Vector2 center, Color color, float height)
    {
        if (_digits == null || _miss == null || _separator == null) return false;
        float scale = height / 64f;
        if (text.Equals("Miss", StringComparison.OrdinalIgnoreCase))
        {
            DrawGlyph(batch, _miss, new Rectangle(0,0,128,64), center - new Vector2(64,32)*scale, color, scale);
            return true;
        }
        float width = 0;
        foreach (char ch in text)
        {
            int index = ch is >= '0' and <= '9' ? ch-'0' : ch=='M' ? 10 : -1;
            if (index >= 0) width += DigitSources[index].Width + 4;
            else if (ch is ',' or '.') width += 16;
            else if (ch is not ('!' or '*' or ' ')) return false;
        }
        Vector2 position = center - new Vector2(width*.5f,32)*scale;
        foreach (char ch in text)
        {
            int index = ch is >= '0' and <= '9' ? ch-'0' : ch=='M' ? 10 : -1;
            if (index >= 0)
            {
                DrawGlyph(batch,_digits,DigitSources[index],position,color,scale);
                position.X += (DigitSources[index].Width+4)*scale;
            }
            else if (ch is ',' or '.')
            {
                DrawGlyph(batch,_separator,new Rectangle(0,0,16,64),position,color,scale);
                position.X += 16*scale;
            }
        }
        return true;
    }

    private static void DrawGlyph(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 position, Color color, float scale)
    {
        position = new Vector2(MathF.Round(position.X),MathF.Round(position.Y));
        batch.Draw(texture,position+Vector2.One,source,Color.Black*(color.A/255f),0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.Draw(texture,position,source,color,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
}
