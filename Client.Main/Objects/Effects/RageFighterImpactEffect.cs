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

/// <summary>Preloaded uppercut and dragon-kick target particles with bounded storage.</summary>
public sealed class RageFighterImpactEffect : EffectObject
{
    private struct Particle {internal Vector3 Position,Velocity,Light;internal float Life,Scale,Age,Alpha,Rotation;internal byte Texture;}
    private readonly Particle[] _particles=new Particle[256];
    private readonly Texture2D?[] _textures=new Texture2D?[12];
    private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
    private readonly Stone[] _stones=new Stone[6];
    private readonly WalkerObject _caster;
    private readonly WalkerObject? _target;
    private readonly ushort _skill;
    private readonly Vector3 _roarGroundCenter;
    private int _next;
    private bool _roarFinished;
    private float _age,_roarAge;
    private int _hits;
    public RageFighterImpactEffect(WalkerObject caster,WalkerObject? target,ushort skill)
    {
        _caster=caster;_target=target;_skill=skill;Position=caster.WorldPosition.Translation;
        Vector3 direction=target==null?new(MathF.Sin(caster.Angle.Z),-MathF.Cos(caster.Angle.Z),0):target.WorldPosition.Translation-Position;
        direction.Z=0;if(direction.LengthSquared()>.01f)direction.Normalize();
        _roarGroundCenter=Position+direction*55;
        IsTransparent=true;AffectedByTransparency=false;LightEnabled=false;DepthState=DepthStencilState.DepthRead;
        BoundingBoxLocal=new(new(-1800),new(1800));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        string[] paths=["loungexflow.jpg","Spark.jpg","force_Pillar.jpg","lighting_mega01.jpg","flare01.jpg","Magic_Ground2.jpg","firehik01.jpg","firehik02.jpg","firehik03.jpg","Thunder01.jpg","spark02.jpg","flare01_red.jpg"];
        for(int i=0;i<paths.Length;i++)_textures[i]=await TextureLoader.Instance.PrepareAndGetTexture("Effect/"+paths[i]);
        if(_skill==261)
            for(int i=0;i<_stones.Length;i++)
            {
                if(IsDisposeRequested || World==null)return;
                var stone=new Stone{Model=await BMDLoader.Instance.Prepare("Skill/Stone0"+(1+MuGame.Random.Next(2))+".bmd"),World=World};
                _stones[i]=stone;await stone.Load();if(IsDisposeRequested || World==null){stone.Dispose();return;}World.Objects.Add(stone);
            }
    }
    private void Emit(Vector3 position,Vector3 velocity,Vector3 light,float life,float scale,byte texture)
    {
        _particles[_next]=new(){Position=position,Velocity=velocity,Light=light,Life=life,Scale=scale,Texture=texture,Alpha=texture is >=6 and <=8?0:1,Rotation=texture is 3 or >=6?MathHelper.ToRadians(MuGame.Random.Next(360)):0};
        _next=(_next+1)%_particles.Length;
    }
    public override void Update(GameTime time)
    {
        base.Update(time);if(Status!=GameControlStatus.Ready)return;
        float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;_age+=f;
        float frame=_caster is PlayerObject p?(float)p.GetAnimationTime():_age*.5f;
        if(_target!=null && _skill==261 && _hits<2 && _age*RageFighterJointEffect.Speed(_caster,(int)PlayerAction.PlayerSkillStamp,.4f)*2>=(_hits==0?3:11))
        {
            _hits++;Vector3 pos=_target.WorldPosition.Translation+Vector3.UnitZ*70;
            for(int i=0;i<2;i++)Emit(_target.WorldPosition.Translation+new Vector3(MuGame.Random.Next(-40,50),MuGame.Random.Next(-40,50),MuGame.Random.Next(70,170)),Vector3.Zero,Vector3.One,16,.9f,0);
            Emit(pos,Vector3.Zero,new(1,.5f,.2f),20,1.98f,5);
            for(int i=0;i<20;i++)Emit(pos,new(MuGame.Random.Next(-8,9),MuGame.Random.Next(-8,9),MuGame.Random.Next(5,15)),Vector3.One,20,.5f,1);
            for(int i=0;i<3;i++)_stones[(_hits-1)*3+i].Activate(pos);
        }
        if(_target!=null && (_skill==265 || _skill==270) && _caster.CurrentAction==(int)(_skill==270?PlayerAction.PlayerSkillPhoenixShot:PlayerAction.PlayerSkillDragonkick) && frame>5 && frame<(_caster.Model?.Actions[(int)(_skill==270?PlayerAction.PlayerSkillPhoenixShot:PlayerAction.PlayerSkillDragonkick)].NumAnimationKeys-2??15) && FPSCounter.Instance.RandFPSCheck(1))
        {
            Vector3 pos=_target.WorldPosition.Translation+Vector3.UnitZ*30;
            for(int i=0;i<8;i++)
            {
                Vector3 offset=new(MuGame.Random.Next(-50,50),MuGame.Random.Next(-50,50),MuGame.Random.Next(50));
                float scale=MuGame.Random.Next(50,90)*.01f*1.5f*1.8f;
                Emit(pos+offset,Vector3.Zero,new(.7f,.7f,1),5,scale*MuGame.Random.Next(6,14)*.1f,9);
                Emit(pos+offset,Vector3.Zero,new(.7f,.7f,1),5,scale*.9f,3);
                Emit(pos+offset,Vector3.Zero,new(.7f,.7f,1),10,1,10);
                Emit(pos+offset,new(0,0,2),new(.7f,.7f,1),20,.5f,1);
            }
        }
        if(_skill==264 && _caster.Model?.Actions is { } roarActions)
        {
            int action=(int)PlayerAction.PlayerSkillDragonlore;
            _roarFinished|=_caster.CurrentAction!=action || (action<roarActions.Length && frame>=roarActions[action].NumAnimationKeys-2);
        }
        if(_skill==264 && !_roarFinished && frame>5 && _age<100)
        {
            _roarAge+=f;
            if(FPSCounter.Instance.RandFPSCheck(1))
            {
                Vector3 pos=_caster.WorldPosition.Translation+Vector3.UnitZ*(_roarAge*7);
                byte kind=(byte)MuGame.Random.Next(6,9);
                Emit(pos,new(0,0,MuGame.Random.Next(64,88)*.1f),Vector3.One,
                    (kind==6?27:kind==8?17:12)+MuGame.Random.Next(5),MuGame.Random.Next(18,23)*.1f*MuGame.Random.Next(72,144)*.01f,kind);
                for(int i=0;i<4;i++)
                    Emit(pos+new Vector3(MuGame.Random.Next(-50,50),MuGame.Random.Next(-50,50),0),new(0,0,MuGame.Random.Next(64,88)*.1f),new(.5f),27+MuGame.Random.Next(5),MuGame.Random.Next(18,23)*.1f*MuGame.Random.Next(72,144)*.01f,6);
                Emit(new(pos.X,pos.Y,250),new(0,0,2),Vector3.One,20,1.5f,1);
            }
        }
        for(int i=0;i<_particles.Length;i++)
        {
            ref Particle particle=ref _particles[i];
            if(particle.Life<=0)continue;
            particle.Life-=f;particle.Age+=f;particle.Position+=particle.Velocity*f;
            if(particle.Texture is >=6 and <=8)
            {
                float threshold=particle.Texture==6?15:10;
                particle.Alpha=Math.Clamp(particle.Alpha+(particle.Life<threshold?-.2f:(MuGame.Random.Next(2)+2)*.1f)*f,0,1);
                particle.Scale=MathF.Max(0,particle.Scale-(MuGame.Random.Next(3)+(particle.Texture==8?7:5))*.01f*f);
                particle.Rotation+=MathHelper.ToRadians(3*f);
                if(particle.Scale<=0 || (particle.Alpha<=0 && particle.Age>1))particle.Life=0;
            }
            if(particle.Texture==9)particle.Rotation+=MathHelper.ToRadians(20*f);
            if(particle.Texture==3)particle.Alpha=MathF.Max(0,particle.Alpha-.15f*f);
            if(particle.Texture==10)particle.Scale*=MathF.Pow(.8f,f);
            if(particle.Texture==1)particle.Velocity-=Vector3.UnitZ*.4f*f;
        }
        if(_age>100||_caster.Status==GameControlStatus.Disposed){World?.RemoveObject(this);Dispose();}
    }
    private void Quad(Texture2D texture,Vector3 center,Vector3 right,Vector3 up,Vector3 light,int atlas=-1)
    {
        int n=0;SourceEffectGeometry.Quad(_vertices,ref n,center-up,center+up,right,new Color(light),0,1);
        if(atlas>=0)for(int i=0;i<n;i++)_vertices[i].TextureCoordinate=new Vector2(atlas%4*.25f+.005f,atlas/4*.25f+.005f)+_vertices[i].TextureCoordinate*.24f;
        ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,samplerOverride:SamplerState.LinearClamp);
    }
    public override void Draw(GameTime time)
    {
        if(!Visible||Status!=GameControlStatus.Ready)return;Matrix cam=Matrix.Invert(Camera.Instance.View);
        if(_skill==264 && _age>=20 && _age<70 && _textures[11]!=null)
        {
            Vector3 ground=_roarGroundCenter;
            ground.Z=(World?.Terrain.RequestTerrainHeight(ground.X,ground.Y)??ground.Z)+5;
            for(int i=0;i<2;i++)Quad(_textures[11]!,ground,Vector3.UnitX*150,Vector3.UnitY*150,Vector3.One*(70-_age)*.01f);
        }
        foreach(var p in _particles)
        {
            var texture=_textures[p.Texture];if(p.Life<=0||texture==null)continue;
            Vector3 right=cam.Right*MathF.Cos(p.Rotation)+cam.Up*MathF.Sin(p.Rotation);
            Vector3 up=cam.Up*MathF.Cos(p.Rotation)-cam.Right*MathF.Sin(p.Rotation);
            Quad(texture,p.Position,right*texture.Width*p.Scale*.5f,up*texture.Height*p.Scale*.5f,p.Light*(p.Texture is 3 or >=6 and <=8?p.Alpha:MathF.Min(1,p.Life/8)),p.Texture==0?Math.Clamp((int)(16-p.Life),0,15):-1);
        }
        if(_skill==261 && _textures[4]!=null)
        {
            float punchFrame=_age*RageFighterJointEffect.Speed(_caster,(int)PlayerAction.PlayerSkillStamp,.4f)*2;
            if(punchFrame<12)Quad(_textures[4]!,_caster.WorldPosition.Translation+Vector3.UnitZ*80,cam.Right*_textures[4]!.Width*2.5f,cam.Up*_textures[4]!.Height*2.5f,new(.47f,.36f,.24f));
        }
    }
    public override void Dispose()
    {
        foreach(var stone in _stones){stone?.World?.RemoveObject(stone);stone?.Dispose();}
        base.Dispose();
    }
    private sealed class Stone : ModelObject
    {
        private Vector3 _velocity;
        private float _life,_gravity;
        internal Stone()
        {
            Alpha=0;RenderShadow=false;LightEnabled=false;Light=new(1,.6f,.2f);
            BlendState=BlendState.AlphaBlend;DepthState=DepthStencilState.DepthRead;
            IsTransparent=true;AffectedByTransparency=false;ContinuousAnimation=true;
        }
        protected override bool AllowDynamicLightingShader=>false;
        protected override bool ShouldRenderMesh(int mesh)=>_life>0;
        internal void Activate(Vector3 position)
        {
            Position=position;_life=MuGame.Random.Next(20,36);Scale=MuGame.Random.Next(3,16)*.08f*1.98f;
            _gravity=MuGame.Random.Next(3,6);Angle=new(0,0,MathHelper.ToRadians(MuGame.Random.Next(360)));
            _velocity=Vector3.Transform(new Vector3(0,MuGame.Random.Next(64,192)*.1f,0),Matrix.CreateRotationZ(Angle.Z));
            _velocity.Z=15*FPSCounter.Instance.FPS_ANIMATION_FACTOR;Alpha=1;
        }
        public override void Update(GameTime time)
        {
            if(_life<=0)return;base.Update(time);
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;_life-=f;_velocity.Z-=_gravity*f;
            Position+=_velocity*f;Angle+=new Vector3(MathHelper.ToRadians(.5f*_life*f),MathHelper.ToRadians(.5f*_life*f),0);
            float height=World?.Terrain.RequestTerrainHeight(Position.X,Position.Y)??Position.Z;
            if(Position.Z<=height)
            {
                Position=new(Position.X,Position.Y,height);_velocity.X*=MathF.Pow(.6f,f);_velocity.Y*=MathF.Pow(.6f,f);
                _velocity.Z+=_life*f;Alpha=MathF.Max(0,Alpha-.1f*f);
            }
            if(_life<=0)Alpha=0;
        }
        public override void DrawMesh(int mesh)
        {
            var shader=GraphicsManager.Instance.AlphaTestEffect3D;bool colors=shader.VertexColorEnabled;
            try{shader.VertexColorEnabled=true;base.DrawMesh(mesh);}finally{shader.VertexColorEnabled=colors;}
        }
    }
}
