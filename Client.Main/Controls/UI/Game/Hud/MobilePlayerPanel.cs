#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace Client.Main.Controls.UI.Game.Hud;

public sealed class MobilePlayerPanel : UIControl
{
    // Mobile button sizes and positions, in virtual UI pixels before PanelScale.
    public const int HelperButtonSize = 40;
    public const int HelperButtonGap = 6;
    public const int HelperButtonsX = 347;
    public const int HelperButtonsY = 6;
    public const int AttackModeButtonWidth = 112;
    public const int AttackModeButtonHeight = 36;
    public const int AttackModeButtonX = 2;
    public const int AttackModeButtonY = 122;
    private readonly CharacterState _state;
    private readonly Func<PlayerObject?> _player;
    private readonly Func<bool> _pk;
    private readonly Action _toggle;
    private readonly bool _preview;
    private readonly Dictionary<string,Texture2D> _textures=new();
    private RenderTarget2D? _portrait;
    private BasicEffect? _portraitEffect;
    private double _nextPortrait;
    private bool _pressHandled;
    private bool _modeListOpen;
    public static float PanelScale => Math.Clamp(UiScaler.WindowBounds.Width/900f,.65f,1f);
    public static int PanelHeight => (int)MathF.Round((AttackModeButtonY+AttackModeButtonHeight+8)*PanelScale);
    public MobilePlayerPanel(CharacterState state,Func<PlayerObject?> player,Func<bool> pk,Action toggle,bool preview=false)
    {
        _state=state;_player=player;_pk=pk;_toggle=toggle;_preview=preview;
        AutoViewSize=false;Interactive=true;BackgroundColor=Color.Transparent;BorderThickness=0;
    }
    private Rectangle Rect(int x,int y,int w,int h)=>new(X+(int)(x*PanelScale),Y+(int)(y*PanelScale),Math.Max(1,(int)(w*PanelScale)),Math.Max(1,(int)(h*PanelScale)));
    public Rectangle ModeBounds=>Rect(AttackModeButtonX,AttackModeButtonY,AttackModeButtonWidth,AttackModeButtonHeight);
    public static Rectangle HelperSettingsBounds => ScreenRect(HelperButtonsX,HelperButtonsY,HelperButtonSize,HelperButtonSize);
    public static Rectangle HelperStartBounds => ScreenRect(HelperButtonsX+HelperButtonSize+HelperButtonGap,HelperButtonsY,HelperButtonSize,HelperButtonSize);
    public static Point BuffAnchor => ScreenRect(114,139,1,1).Location;
    // Reserve the expanded mode list as well, so opening it does not cover shortcuts.
    public static Rectangle LayoutBounds => ScreenRect(0,0,Math.Max(399,HelperButtonsX+HelperButtonSize*2+HelperButtonGap+4),AttackModeButtonY+3*(AttackModeButtonHeight+4)+4);
    private static Rectangle ScreenRect(int x,int y,int width,int height) => new(
        UiScaler.WindowBounds.Left+4+(int)(x*PanelScale),UiScaler.WindowBounds.Top+4+(int)(y*PanelScale),
        Math.Max(1,(int)(width*PanelScale)),Math.Max(1,(int)(height*PanelScale)));
    private Rectangle ModeOptionBounds(int option)=>Rect(AttackModeButtonX,AttackModeButtonY+(option+1)*(AttackModeButtonHeight+4),AttackModeButtonWidth,AttackModeButtonHeight);
    public override void Update(GameTime time)
    {
        Visible=_preview || UiThemeManager.IsMobileInterfaceActive;
        if(!Visible)return;
        X=UiScaler.WindowBounds.Left+4;Y=UiScaler.WindowBounds.Top+4;
        ControlSize=ViewSize=new Point((int)(Math.Max(399,HelperButtonsX+HelperButtonSize*2+HelperButtonGap+4)*PanelScale),_modeListOpen?(int)((AttackModeButtonY+3*(AttackModeButtonHeight+4)+4)*PanelScale):PanelHeight);
        if(_textures.Count==0)
            foreach(string name in new[]{"char_profile","nameplate_top","bar_empty_1","bar_empty_2","bar_empty_3","bar_empty_4","bar_hp_fill","bar_mp_fill","bar_sd_fill","bar_ag_fill","normal_button_large","pk_button_active"})
            {
                using var stream=OpenTexture(name);
                _textures[name]=Texture2D.FromStream(GraphicsDevice,stream);
            }
        if(_portrait==null || _portrait.IsDisposed || _portrait.GraphicsDevice!=GraphicsDevice)
        {
            _portrait?.Dispose();_portraitEffect?.Dispose();
            _portrait=new RenderTarget2D(GraphicsDevice,160,160,false,SurfaceFormat.Color,DepthFormat.Depth24);
            _portraitEffect=new BasicEffect(GraphicsDevice);_nextPortrait=0;
        }
        if(time.TotalGameTime.TotalSeconds>=_nextPortrait && _player() is {Model:not null} player)
        {
            BmdPreviewRenderer.RenderCharacterPortrait(player,_portrait,_portraitEffect!);
            _nextPortrait=time.TotalGameTime.TotalSeconds+.25;
        }
        var game=MuGame.Instance;
        if(game!=null && game.UiMouseState.LeftButton==ButtonState.Pressed && game.PrevUiMouseState.LeftButton==ButtonState.Released)
        {
            _pressHandled=HandlePointerPress(game.UiMouseState.Position);
            if(_pressHandled)Scene?.SetMouseInputConsumed();
        }
        base.Update(time);
    }
    public override bool OnClick()
    {
        if(_pressHandled){_pressHandled=false;return true;}
        return HandlePointerPress(MuGame.Instance.UiMouseState.Position);
    }
    private bool HandlePointerPress(Point point)
    {
        if(HelperSettingsBounds.Contains(point))
        {
            (Scene as Client.Main.Scenes.GameScene)?.ToggleMobileHelperSettings();
            return true;
        }
        if(HelperStartBounds.Contains(point))
        {
            (Scene as Client.Main.Scenes.GameScene)?.MuHelperController?.Toggle();
            return true;
        }
        if(Rect(0,4,114,114).Contains(point))
        {
            (Scene as Client.Main.Scenes.GameScene)?.ToggleClassicCharacterInfo();
            return true;
        }
        if(_modeListOpen)
        {
            for(int option=0;option<2;option++)
                if(ModeOptionBounds(option).Contains(point))
                {
                    if(_pk()!=(option==1))_toggle();
                    _modeListOpen=false;
                    return true;
                }
        }
        if(ModeBounds.Contains(point))
        {
            _modeListOpen=!_modeListOpen;
            return true;
        }
        _modeListOpen=false;
        return false;
    }
    internal static Stream OpenTexture(string name)
    {
#if DEBUG
        // Use the newest edited source image when previewing the mobile HUD on Windows.
        // Installed clients have no source tree and fall through to game-data overrides.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string sourceDirectory = Path.Combine(directory.FullName,"Client.Main","Data","MobileHud");
            if (!Directory.Exists(sourceDirectory)) continue;
            string source = Path.Combine(sourceDirectory,name+".png");
            string patch = Path.Combine(directory.FullName,"data","patch","files-e.g","Interface","NEW_HUD",name+".png");
            if (File.Exists(patch) && (!File.Exists(source) || File.GetLastWriteTimeUtc(patch)>File.GetLastWriteTimeUtc(source)))
                return File.OpenRead(patch);
            if (File.Exists(source)) return File.OpenRead(source);
            break;
        }
#endif
        if (!string.IsNullOrWhiteSpace(Constants.DataPath))
        {
            string path = Path.Combine(Constants.DataPath,"Interface","NEW_HUD",name+".png");
            if (File.Exists(path)) return File.OpenRead(path);
        }
        return typeof(MobilePlayerPanel).Assembly.GetManifestResourceStream($"Client.Main.Data.MobileHud.{name}.png")
            ?? throw new FileNotFoundException("Mobile HUD image was not found.",name+".png");
    }
    private void Text(SpriteBatch batch,string text,Rectangle bounds,float size,Color color,bool centered=true)
    {
        var font=GraphicsManager.Instance.Font;if(font==null)return;
        float scale=size*PanelScale/font.LineSpacing;
        var measured=font.MeasureString(text)*scale;
        if(measured.X>bounds.Width-4)scale*=Math.Max(0,(bounds.Width-4)/measured.X);
        measured=font.MeasureString(text)*scale;
        var position=new Vector2(centered?bounds.Center.X-measured.X/2:bounds.X+5*PanelScale,bounds.Center.Y-measured.Y/2);
        batch.DrawString(font,text,position+Vector2.One,Color.Black*.9f,0,Vector2.Zero,scale,SpriteEffects.None,0);
        batch.DrawString(font,text,position,color,0,Vector2.Zero,scale,SpriteEffects.None,0);
    }
    public override void Draw(GameTime time)
    {
        if(!Visible || _textures.Count==0)return;
        var batch=GraphicsManager.Instance.Sprite;
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.NonPremultiplied,SamplerState.LinearClamp,DepthStencilState.None,transform:UiScaler.SpriteTransform);
        var profile=Rect(0,4,114,114);
        batch.Draw(_textures["char_profile"],profile,Color.White);
        // Clip the live render to a circle using batched vertical strips: no CPU GPU readback.
        if(_portrait!=null)
        {
            var inner=Rect(8,12,98,98);
            for(int x=0;x<inner.Width;x++)
            {
                float normalized=(x+.5f-inner.Width*.5f)/(inner.Width*.5f);
                int half=(int)(MathF.Sqrt(MathF.Max(0,1-normalized*normalized))*inner.Height*.5f);
                if(half<1)continue;
                int top=inner.Center.Y-half;
                batch.Draw(_portrait,new Rectangle(inner.X+x,top,1,half*2),new Rectangle(x*160/inner.Width,(top-inner.Y)*160/inner.Height,Math.Max(1,160/inner.Width),Math.Max(1,half*2*160/inner.Height)),Color.White);
            }
        }
        batch.Draw(_textures["nameplate_top"],Rect(80,5,245,35),Color.White);
        Text(batch,_state.Name,Rect(125,11,101,23),16,Color.White,false); // name (x,y,width,height)
        Text(batch,$"Lv. {_state.Level:N0}",Rect(239,11,63,23),14,new Color(255,220,140)); // level (x,y,width,height)
        Bar(batch,0,"HP",_state.CurrentHealth,_state.MaximumHealth,"hp");
        Bar(batch,1,"MP",_state.CurrentMana,_state.MaximumMana,"mp");
        Bar(batch,2,"AG",_state.CurrentAbility,_state.MaximumAbility,"ag");
        Bar(batch,3,"SD",_state.CurrentShield,_state.MaximumShield,"sd");
        batch.Draw(_textures[_pk()?"pk_button_active":"normal_button_large"],ModeBounds,Color.White);
        Text(batch,_modeListOpen?"^":"v",Rect(AttackModeButtonX+AttackModeButtonWidth-18,AttackModeButtonY+5,14,AttackModeButtonHeight-10),14,Color.White);
        if(_modeListOpen)
            for(int option=0;option<2;option++)
            {
                var bounds=ModeOptionBounds(option);
                bool selected=_pk()==(option==1);
                batch.Draw(GraphicsManager.Instance.Pixel,bounds,new Color(176,139,60));
                batch.Draw(GraphicsManager.Instance.Pixel,new Rectangle(bounds.X+1,bounds.Y+1,bounds.Width-2,bounds.Height-2),
                    selected?new Color(63,49,24):new Color(14,18,25));
                Text(batch,option==0?"Normal":"PK",bounds,13,selected?new Color(255,220,140):Color.White);
            }
    }
    private void Bar(SpriteBatch batch,int row,string label,uint current,uint maximum,string name)
    {
        var bounds=Rect(111,43+row*23,238,22);var frame=_textures[$"bar_empty_{row+1}"];
        var source=row==0?new Rectangle(0,37,frame.Width,frame.Height-37):frame.Bounds;
        batch.Draw(frame,bounds,source,Color.White);
        float fraction=maximum==0?0:Math.Clamp(current/(float)maximum,0,1);
        var fill=_textures[$"bar_{name}_fill"];
        var inside=Rect(119,44+row*23,224,20); //bar (x,y,width,height)
        int width=(int)(inside.Width*fraction);
        if(width>0)batch.Draw(fill,new Rectangle(inside.X,inside.Y,width,inside.Height),new Rectangle(0,0,Math.Max(1,(int)(fill.Width*fraction)),fill.Height),Color.White);
        Text(batch,$"{label}  {current:N0} / {maximum:N0}",inside,13,Color.White);
    }
    public override void Dispose()
    {
        _portrait?.Dispose();_portraitEffect?.Dispose();foreach(var texture in _textures.Values)texture.Dispose();_textures.Clear();base.Dispose();
    }
}
