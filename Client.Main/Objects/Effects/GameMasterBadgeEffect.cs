#nullable enable
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Objects.Effects;

// MuMain CSParts PARTS_WEBZEN: server GM flag/effect, MuSign model, head bone 20.
public sealed class GameMasterBadgeEffect : ModelObject
{
    private readonly PlayerObject _owner;
    private Task? _preparation;
    public GameMasterBadgeEffect(PlayerObject owner)
    {
        _owner=owner;ItemLevel=11;MaterialItemGroup=-3;Hidden=true;LightEnabled=false;RenderShadow=false;
        IsTransparent=false;AffectedByTransparency=true;
        BlendState=BlendState.AlphaBlend;DepthState=DepthStencilState.DepthRead;
        ContinuousAnimation=true;AnimationSpeed=12.5f;
    }
    private async Task Prepare()
    {
        var model=await BMDLoader.Instance.Prepare("Skill/MuSign.bmd");
        if(IsDisposeRequested)return;
        Model=model;await LoadContent();
    }
    protected override void RecalculateWorldPosition()
    {
        if(_owner.TryGetBoneWorldMatrix(20,out var head))
        {
            Vector3 position=Vector3.Transform(new Vector3(70,-5,0),head);
            WorldPosition=Matrix.CreateScale(_owner.WorldPosition.Right.Length())*
                Matrix.CreateRotationZ(_owner.Angle.Z)*Matrix.CreateTranslation(position);
        }
    }
    public override void Update(GameTime time)
    {
        var state=MuGame.Network?.GetCharacterState();
        bool gm=state!=null && ((state.Id==_owner.NetworkId && (((byte)state.Status & 0x28)!=0)) || state.HasActiveBuff(28,_owner.NetworkId));
        if(gm)_preparation??=Prepare();
        Hidden=!gm || _owner.Hidden || _preparation==null || !_preparation.IsCompletedSuccessfully || Model==null;
        RenderShadow=false;RecalculateWorldPosition();base.Update(time);
    }
}
