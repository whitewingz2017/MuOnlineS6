using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud;

public sealed class MobilePickupControl : UIControl
{
    private readonly System.Action _pickup;
    private Texture2D _texture;
    private bool _pressHandled;
    // Fixed beside the first right-hand potion position, regardless of potion setting.
    public static Rectangle ButtonBounds => new(UiScaler.WindowBounds.Right-259,UiScaler.WindowBounds.Bottom-382,61,61);
    public MobilePickupControl(System.Action pickup = null)
    {
        _pickup=pickup;
        AutoViewSize=false;Interactive=true;BackgroundColor=Color.Transparent;BorderThickness=0;
        var bounds=ButtonBounds;X=bounds.X;Y=bounds.Y;ControlSize=ViewSize=bounds.Size;
    }
    public override void Update(GameTime time)
    {
        Visible=UiThemeManager.IsMobileInterfaceActive && (Scene is not GameScene scene || scene.MobileCombatControlsVisible);
        var bounds=ButtonBounds;X=bounds.X;Y=bounds.Y;ControlSize=ViewSize=bounds.Size;
        if(Visible && (_texture==null || _texture.IsDisposed))
        {
            using var stream=MobilePlayerPanel.OpenTexture("hand_pickup");
            _texture=Texture2D.FromStream(MuGame.Instance.GraphicsDevice,stream);
        }
        var game=MuGame.Instance;
        if(Visible && game.UiMouseState.LeftButton==Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
            game.PrevUiMouseState.LeftButton==Microsoft.Xna.Framework.Input.ButtonState.Released &&
            ContainsPointerPoint(game.UiMouseState.Position) &&
            (Scene?.MouseHoverControl==null || Scene.MouseHoverControl==this || Scene.MouseHoverControl==Scene.World))
        {
            _pressHandled=true;
            ActivatePickup();
            Scene?.SetMouseInputConsumed();
        }
        base.Update(time);
    }
    public override bool OnClick()
    {
        if(_pressHandled){_pressHandled=false;return true;}
        ActivatePickup();
        return true;
    }
    private void ActivatePickup()
    {
        if(_pickup!=null)_pickup();
        else (Scene as GameScene)?.PickupNearestItem();
    }
    public override void Draw(GameTime time)
    {
        if(!Visible || _texture==null)return;
        var batch=GraphicsManager.Instance.Sprite;
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,transform:UiScaler.SpriteTransform);
        batch.Draw(_texture,ButtonBounds,Color.White*(IsMousePressed ? 0.75f : 1f));
    }
    public override void Dispose(){_texture?.Dispose();base.Dispose();}
}
