using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Hud;

public sealed class MobileUpdateControl : UIControl
{
    private Texture2D _icon;
    public MobileUpdateControl()
    {AutoViewSize=false;Interactive=true;BackgroundColor=Color.Transparent;Visible=false;}

    public override void Update(GameTime time)
    {
        Visible=UiThemeManager.IsMobileInterfaceActive && AppUpdateNotice.Current!=null && !AppUpdateScreen.Active;
        if(!Visible)return;
        X=MobilePlayerPanel.HelperStartBounds.Right+10;Y=UiScaler.WindowBounds.Top+6;
        ControlSize=ViewSize=new Point(56,56);
        base.Update(time);
    }

    public override bool OnClick()
    {
        if(AppUpdateNotice.Current is not { } offer)return true;
        Scene?.FocusControl?.OnBlur();if(Scene!=null)Scene.FocusControl=null;
        var dialog=new MobileUpdateDetailsDialog(offer);
        dialog.ShowDialog();dialog.BringToFront();
        return true;
    }

    public override void Draw(GameTime time)
    {
        if(!Visible)return;
        if(_icon==null || _icon.IsDisposed)
        {using var input=MobilePlayerPanel.OpenTexture("updates");_icon=Texture2D.FromStream(GraphicsDevice,input);}
        float pulse=.5f+.5f*MathF.Sin((float)time.TotalGameTime.TotalSeconds*3f);
        var rect=DisplayRectangle;var glow=rect;glow.Inflate(4+(int)(pulse*3),4+(int)(pulse*3));
        var batch=GraphicsManager.Instance.Sprite;
        // Additive blending lets the icon's black background disappear into the scene.
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.Additive,SamplerState.LinearClamp,DepthStencilState.None,transform:UiScaler.SpriteTransform);
        batch.Draw(_icon,glow,Color.White*(.16f+.18f*pulse));
        batch.Draw(_icon,rect,Color.White*(.8f+.2f*pulse));
    }

    public override void Dispose(){_icon?.Dispose();base.Dispose();}
}

internal sealed class MobileUpdateDetailsDialog : DialogControl
{
    private readonly AppUpdateOffer _offer;
    private readonly List<string> _lines=new();
    private Rectangle _notes,_update,_later;
    private int _firstLine,_lastPointerY;
    private bool _dragging;
    private Point? _pressOrigin;
    private const int LineHeight=25;

    internal MobileUpdateDetailsDialog(AppUpdateOffer offer)
    {
        _offer=offer;AutoViewSize=false;Interactive=true;
        int width=Math.Min(560,UiScaler.WindowBounds.Width-32),height=Math.Min(440,UiScaler.WindowBounds.Height-40);
        ControlSize=ViewSize=new Point(width,height);
        X=UiScaler.WindowBounds.Center.X-width/2;Y=UiScaler.WindowBounds.Center.Y-height/2;Align=0;
        _notes=new Rectangle(18,112,width-36,height-186);
        _update=new Rectangle(18,height-55,(width-48)/2,38);
        _later=new Rectangle(_update.Right+12,height-55,(width-48)/2,38);
        var font=GraphicsManager.GetUiFont(16,out float scale);
        foreach(string paragraph in (offer.Notes??"").Replace("\r","").Split('\n'))
        {
            string line="";
            foreach(string word in paragraph.Split(' '))
            {
                string next=line.Length==0?word:line+" "+word;
                if(line.Length>0 && font.MeasureString(next).X*scale>_notes.Width-12)
                {_lines.Add(line);line=word;}
                else line=next;
            }
            _lines.Add(line);
        }
        if(_lines.Count==1 && string.IsNullOrWhiteSpace(_lines[0]))_lines[0]="No release notes provided.";
    }

    private Rectangle Screen(Rectangle rect)=>new(DisplayRectangle.X+rect.X,DisplayRectangle.Y+rect.Y,rect.Width,rect.Height);
    private int VisibleLines=>Math.Max(1,_notes.Height/LineHeight);
    private void Scroll(int lines)=>_firstLine=Math.Clamp(_firstLine+lines,0,Math.Max(0,_lines.Count-VisibleLines));
    public override bool ProcessMouseScroll(int delta){Scroll(delta>0?-2:2);return true;}

    public override void Update(GameTime time)
    {
        var mouse=MuGame.Instance.UiMouseState;var previous=MuGame.Instance.PrevUiMouseState;
        if(mouse.LeftButton==ButtonState.Pressed && previous.LeftButton==ButtonState.Released)_pressOrigin=mouse.Position;
        base.Update(time);
        if(mouse.LeftButton==ButtonState.Pressed && previous.LeftButton==ButtonState.Released && Screen(_notes).Contains(mouse.Position))
        {_dragging=true;_lastPointerY=mouse.Y;}
        if(_dragging && mouse.LeftButton==ButtonState.Pressed)
        {
            int lines=(_lastPointerY-mouse.Y)/LineHeight;
            if(lines!=0){Scroll(lines);_lastPointerY=mouse.Y;}
        }
        if(mouse.LeftButton==ButtonState.Released)_dragging=false;
    }

    public override bool OnClick()
    {
        var point=MuGame.Instance.UiMouseState.Position;
        var origin=_pressOrigin;_pressOrigin=null;
        if(!origin.HasValue)return true;
        if(Screen(_update).Contains(point) && Screen(_update).Contains(origin.Value))
        {
            Close();
            if(AppUpdateNotice.Current is { } current && current.Version==_offer.Version)
                current.UpdateNow?.Invoke();
        }
        else if(Screen(_later).Contains(point) && Screen(_later).Contains(origin.Value))Close();
        return true;
    }

    public override void Draw(GameTime time)
    {
        if(!Visible)return;
        var batch=GraphicsManager.Instance.Sprite;var pixel=GraphicsManager.Instance.Pixel;
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,transform:UiScaler.SpriteTransform);
        batch.Draw(pixel,DisplayRectangle,new Color(18,24,29,248));
        MobileHudLayout.Text(batch,$"Update available: {_offer.Version}",Screen(new Rectangle(12,14,ControlSize.X-24,32)),20,Color.Gold);
        MobileHudLayout.Text(batch,$"Download: {_offer.FileSize/1048576d:0.0} MB",Screen(new Rectangle(18,51,ControlSize.X-36,25)),14,Color.LightGray,false);
        MobileHudLayout.Text(batch,"Updating will close your current game session.",Screen(new Rectangle(18,78,ControlSize.X-36,25)),13,Color.LightGray,false);
        for(int row=0;row<VisibleLines && row+_firstLine<_lines.Count;row++)
            MobileHudLayout.Text(batch,_lines[row+_firstLine],Screen(new Rectangle(_notes.X,_notes.Y+row*LineHeight,_notes.Width,LineHeight)),16,Color.White,false);
        if(_lines.Count>VisibleLines)
            MobileHudLayout.Text(batch,"Swipe to read more",Screen(new Rectangle(18,ControlSize.Y-84,ControlSize.X-36,23)),12,Color.LightGray);
        batch.Draw(pixel,Screen(_update),new Color(25,83,48));
        batch.Draw(pixel,Screen(_later),new Color(39,48,56));
        MobileHudLayout.Text(batch,"Update now",Screen(_update),16,Color.Gold);
        MobileHudLayout.Text(batch,"Later",Screen(_later),16,Color.White);
    }
}
