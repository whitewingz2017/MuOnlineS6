#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud;

public sealed class MobileTargetControl : UIControl
{
    private readonly Func<WalkerObject?> _target;
    public MobileTargetControl(Func<WalkerObject?> target)
    {
        _target=target;Interactive=false;AutoViewSize=false;
        BackgroundColor=Color.Transparent;BorderThickness=0;
    }
    public override async Task Load()
    {
        await base.Load();
        await PetHealthBarRenderer.LoadAsync();
    }
    public override void Update(GameTime time)
    {
        Visible=UiThemeManager.IsMobileInterfaceActive;
        int width=(int)(220*MobilePlayerPanel.PanelScale);
        X=UiScaler.WindowBounds.Center.X-width/2;Y=UiScaler.WindowBounds.Top+8;
        if(new Rectangle(X,Y,width,46).Intersects(MobilePlayerPanel.HelperStartBounds))
            Y=MobilePlayerPanel.HelperStartBounds.Bottom+6;
        ControlSize=ViewSize=new Point(width,46);
        base.Update(time);
    }
    public override void Draw(GameTime time)
    {
        if(!Visible || _target() is not {Visible:true} target || target.World!=Scene?.World ||
            target.Hidden || target is MonsterObject {IsDead:true} || target is PlayerObject {IsDead:true} ||
            Scene?.World?.Objects.Contains(target)!=true)return;
        var font=GraphicsManager.Instance.Font;
        if(font==null)return;
        float? health=target is MonsterObject monster?monster.HealthFraction:
            target is PlayerObject player?player.HealthFraction:null;
        var batch=GraphicsManager.Instance.Sprite;
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,
            SamplerState.LinearClamp,DepthStencilState.None,transform:UiScaler.SpriteTransform);
        void Text(string text,int y,float size)
        {
            float scale=size/font.LineSpacing;
            var measured=font.MeasureString(text)*scale;
            if(measured.X>ViewSize.X)scale*=ViewSize.X/measured.X;
            var position=new Vector2(X+(ViewSize.X-font.MeasureString(text).X*scale)/2,y);
            batch.DrawString(font,text,position+Vector2.One,Color.Black,0,Vector2.Zero,scale,SpriteEffects.None,0);
            batch.DrawString(font,text,position,Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);
        }
        Text(target.DisplayName,Y,15);
        PetHealthBarRenderer.Draw(batch,new Rectangle(X,Y+22,ViewSize.X,13),health);
        Text(health.HasValue?$"{health.Value:P0}":"HP: --",Y+22,11);
    }
}
