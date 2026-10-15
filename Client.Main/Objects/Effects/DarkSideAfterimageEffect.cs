#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Objects.Effects;

/// <summary>Visual-only equipment snapshots for the server's Dark Side victims.</summary>
public sealed class DarkSideAfterimageEffect : EffectObject
{
    private readonly PlayerObject _caster;
    private readonly WalkerObject _target;
    private readonly int _index;
    private Ghost? _ghost;
    private float _age;
    public DarkSideAfterimageEffect(PlayerObject caster,WalkerObject target,int index)
    {
        _caster=caster;_target=target;_index=index;Position=target.WorldPosition.Translation;
        IsTransparent=true;AffectedByTransparency=false;LightEnabled=false;BoundingBoxLocal=new(new(-1000),new(1000));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        _ghost=new Ghost{Model=_caster.Model,World=World};
        ModelObject[] equipment=[_caster.Helm,_caster.Armor,_caster.Pants,_caster.Gloves,_caster.Boots];
        foreach(var piece in equipment)
            if(piece.Model!=null && !piece.Hidden)_ghost.Children.Add(new Ghost{Model=piece.Model,LinkParentAnimation=true,World=World});
        ModelObject[] weapons=[_caster.Weapon1,_caster.Weapon2];
        foreach(var weapon in weapons)
            if(weapon.Model!=null && !weapon.Hidden)
                _ghost.Children.Add(new Ghost{Model=weapon.Model,LinkParentAnimation=false,ParentBoneLink=weapon.ParentBoneLink,
                    Angle=weapon.Angle,Scale=weapon.Scale,World=World});
        await _ghost.Load();
        World?.Objects.Add(_ghost);
    }
    public override void Update(GameTime time)
    {
        base.Update(time);if(Status!=GameControlStatus.Ready||_ghost==null)return;
        _age+=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        float phase=_age-_index*2;
        Vector3 victim=_target.WorldPosition.Translation;
        // MonkSystem::SetDummy rotates (150,150) by 60 degrees per copy.
        float angle=_index*MathHelper.Pi/3;
        Vector3 offset=new(150*(MathF.Cos(angle)-MathF.Sin(angle)),150*(MathF.Sin(angle)+MathF.Cos(angle)),0);
        Vector3 start=victim+offset;
        Vector3 direction=-Vector3.Normalize(offset);
        float ticks=MathF.Max(0,phase);
        // CDummyUnit accumulates a .7 distance frame and advances by another .7.
        float distance=.245f*ticks*(ticks+1);
        _ghost.Position=start+direction*distance;
        _ghost.Angle=new(0,0,MathF.Atan2(direction.X,-direction.Y));
        float fadeStart=(-1+MathF.Sqrt(1+4*300/.245f))/2;
        _ghost.Alpha=phase<0?0:distance<=300?MathF.Min(.6f,.2f+ticks*.02f):MathF.Max(0,.6f-(ticks-fadeStart)*.07f);
        if(_age>40 || _target.Status==GameControlStatus.Disposed){World?.RemoveObject(this);Dispose();}
    }
    public override void Dispose(){_ghost?.World?.RemoveObject(_ghost);_ghost?.Dispose();base.Dispose();}
    private sealed class Ghost:ModelObject
    {
        internal Ghost()
        {
            CurrentAction=(int)PlayerAction.PlayerSkillDarksideAttack;
            Light=new(1,.6f,.6f);LightEnabled=false;Alpha=1;
            BlendState=Blendings.OneOneAdditive;BlendMesh=-2;BlendMeshLight=1;BlendMeshState=Blendings.OneOneAdditive;
            IsTransparent=true;AffectedByTransparency=false;DepthState=DepthStencilState.DepthRead;RenderShadow=false;
            AnimationSpeed=25*.05f;HoldOnLastFrame=true;ContinuousAnimation=true;
        }
        protected override bool AllowDynamicLightingShader=>false;
        public override async Task Load()
        {
            if(!LinkParentAnimation && Model?.Actions is { } actions && CurrentAction<actions.Length && actions[CurrentAction].PlaySpeed>0)
                AnimationSpeed=25*.05f/actions[CurrentAction].PlaySpeed;
            await base.Load();
        }
        public override void DrawMesh(int mesh)
        {
            var shader=GraphicsManager.Instance.AlphaTestEffect3D;bool colors=shader.VertexColorEnabled;
            try{shader.VertexColorEnabled=true;base.DrawMesh(mesh);}finally{shader.VertexColorEnabled=colors;}
        }
    }
}
