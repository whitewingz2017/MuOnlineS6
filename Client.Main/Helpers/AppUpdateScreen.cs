using System;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Hud;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Helpers;

// Shared display state: Android owns downloading/installing; the game draws it.
public static class AppUpdateScreen
{
    public static volatile bool Active;
    public static string Notes = "", Status = "", Error = "";
    public static long Downloaded, Total;
    public static double BytesPerSecond;
    public static Action Retry, Continue;
    private static Texture2D _background;
    public static void Show(string notes, long total)
    {
        Notes=notes ?? ""; Total=total; Downloaded=0; Error="";
        Status="Downloading update..."; Active=true;
    }
    public static void Fail(string message) { Error=message; Status="Update could not be completed"; }
    public static void Close() { Active=false; Retry=null; Continue=null; }
    public static void Draw()
    {
        if(!Active)return;
        _background ??= MuGame.Instance.Content.Load<Texture2D>("Background");
        var batch=GraphicsManager.Instance.Sprite;var pixel=GraphicsManager.Instance.Pixel;
        var window=UiScaler.WindowBounds;
        // This overlay runs after the scaled scene has been presented to the
        // native back buffer, so it must not inherit the scene render scale.
        var transform=Matrix.CreateScale(UiScaler.ScaleX,UiScaler.ScaleY,1f)*Matrix.CreateTranslation(UiScaler.Offset.X,UiScaler.Offset.Y,0f);
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,transform:transform);
        batch.Draw(_background,window,Color.White);
        int left=window.Left+48,width=window.Width-96,bottom=window.Bottom-48;
        var notes=new Rectangle(left,bottom-218,Math.Min(390,width),174);
        batch.Draw(pixel,notes,new Color(0,0,0,155));
        MobileHudLayout.Text(batch,"UPDATES",new Rectangle(notes.X+8,notes.Y+4,notes.Width-16,24),16,Color.Gold,false);
        // Wrap release notes and clip to the available panel; no hardcoded release claims.
        var font=GraphicsManager.Instance.Font;float scale=14f/font.LineSpacing;
        int y=notes.Y+32;
        foreach(string paragraph in Notes.Replace("\r","").Split('\n'))
        {
            string line="";
            foreach(string word in paragraph.Split(' '))
            {
                string candidate=line.Length==0?word:line+" "+word;
                if(font.MeasureString(candidate).X*scale>notes.Width-20 && line.Length>0)
                {
                    if(y+18>notes.Bottom)break;
                    batch.DrawString(font,line,new Vector2(notes.X+9,y),Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);y+=18;line=word;
                }else line=candidate;
            }
            if(y+18>notes.Bottom)break;
            batch.DrawString(font,line,new Vector2(notes.X+9,y),Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);y+=18;
        }
        MobileHudLayout.Text(batch,Status,new Rectangle(left,bottom-39,width,24),16,Color.White,false);
        var bar=new Rectangle(left,bottom-12,width,20);batch.Draw(pixel,bar,new Color(80,90,85));
        float progress=Total>0?Math.Clamp((float)Downloaded/Total,0,1):0;
        if(progress>0)batch.Draw(pixel,new Rectangle(bar.X+2,bar.Y+2,(int)((bar.Width-4)*progress),bar.Height-4),new Color(55,190,120));
        MobileHudLayout.Text(batch,$"{progress:P0}",bar,16,Color.White);
        MobileHudLayout.Text(batch,$"{Downloaded/1048576d:0.0} / {Total/1048576d:0.0} MB",new Rectangle(left,bottom+13,220,20),13,Color.LightGray,false);
        MobileHudLayout.Text(batch,$"{BytesPerSecond/1048576d:0.00} MB/s",new Rectangle(window.Center.X-90,bottom+13,180,20),13,Color.LightSkyBlue);
        if(Error.Length>0)
        {
            var panel=new Rectangle(window.Center.X-240,window.Center.Y-70,480,140);
            batch.Draw(pixel,panel,new Color(0,0,0,240));
            MobileHudLayout.Text(batch,Error,new Rectangle(panel.X+10,panel.Y+10,460,65),14,Color.White);
            Button("Retry",new Rectangle(panel.X+15,panel.Bottom-49,215,38),Retry);
            Button("Return to login",new Rectangle(panel.X+250,panel.Bottom-49,215,38),Continue);
        }
    }
    private static void Button(string text,Rectangle rect,Action action)
    {
        if(action==null)return;
        var game=MuGame.Instance;
        GraphicsManager.Instance.Sprite.Draw(GraphicsManager.Instance.Pixel,rect,new Color(30,65,65));
        MobileHudLayout.Text(GraphicsManager.Instance.Sprite,text,rect,15,Color.Gold);
        if(rect.Contains(game.UiMouseState.Position) && game.UiMouseState.LeftButton==ButtonState.Released && game.PrevUiMouseState.LeftButton==ButtonState.Pressed)
            action();
    }
}
