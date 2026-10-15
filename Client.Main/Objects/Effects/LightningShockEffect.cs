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

public sealed class LightningShockEffect : EffectObject
{
    private struct Particle { public Vector3 Position,Light; public float Life,Scale; public int Texture; }
    private readonly WalkerObject _caster;
    private readonly Particle[] _particles=new Particle[512];
    private readonly Texture2D?[] _textures=new Texture2D?[8];
    private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
    private readonly ShockCrack[] _cracks = new ShockCrack[3];
    private Texture2D? _damage;
    private float _age,_impactAge=-1,_velocity,_gravity=1;
    private int _next;
    public LightningShockEffect(WalkerObject caster)
    {
        _caster=caster;Position=caster.WorldPosition.Translation+Vector3.UnitZ*280;
        IsTransparent=true;AffectedByTransparency=false;LightEnabled=false;DepthState=DepthStencilState.DepthRead;
        BoundingBoxLocal=new(new(-1200),new(1200));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        string[] paths=["Shiny02.jpg","Magic_Ground1.jpg","pin_lights.jpg","flare01.jpg","lighting_mega01.jpg","lighting_mega02.jpg","lighting_mega03.jpg","smoke01.jpg"];
        for(int i=0;i<paths.Length;i++)_textures[i]=await TextureLoader.Instance.PrepareAndGetTexture("Effect/"+paths[i]);
        _damage=await TextureLoader.Instance.PrepareAndGetTexture("Effect/damage01mono.jpg");
        for(int i=0;i<3;i++){_cracks[i]=new ShockCrack();_cracks[i].World=World;await _cracks[i].Load();}
    }
    private void Emit(Vector3 p,Vector3 light,float life,float scale,int texture)
    {
        _particles[_next]=new(){Position=p,Light=light,Life=life,Scale=scale,Texture=texture};
        _next=(_next+1)%_particles.Length;
    }
    public override void Update(GameTime time)
    {
        base.Update(time);if(Status!=GameControlStatus.Ready)return;
        float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;_age+=f;
        if(_impactAge<0)
        {
            if(_caster is PlayerObject player && player.GetAnimationTime()>6 || _age>5)
            {
                _gravity+=.1f*f;_velocity+=_gravity*f;
                Vector3 forward=new(MathF.Sin(_caster.Angle.Z),-MathF.Cos(_caster.Angle.Z),0);
                Position+=forward*20*f-Vector3.UnitZ*(75+_velocity)*f;
            }
            float height=World?.Terrain.RequestTerrainHeight(Position.X,Position.Y)??Position.Z;
            if(Position.Z<height)
            {
                Position=new(Position.X,Position.Y,height+10);_impactAge=0;
                foreach(var crack in _cracks){crack.Position=Position;World?.Objects.Add(crack);}
            }
        }
        else _impactAge+=f;
        if((_impactAge<12) && FPSCounter.Instance.RandFPSCheck(1))
        {
            for(int i=0;i<11;i++)
            {
                Vector3 p=Position+new Vector3(MuGame.Random.Next(-35,35),MuGame.Random.Next(-35,35),MuGame.Random.Next(-35,35));
                Emit(p,new(1,.2f,.1f),2,2.2f,3);
                if(_impactAge>=0 || MuGame.Random.Next(3)==0)Emit(p,new(1,.7f,.4f),8,MuGame.Random.Next(32,112)*.01f,MuGame.Random.Next(4,7));
            }
            if(_impactAge>=0)
            {
                for(int i=0;i<6;i++)
                {
                    float a=MuGame.Random.NextSingle()*MathHelper.TwoPi,r=MuGame.Random.Next(400);
                    Vector3 p=Position+new Vector3(MathF.Cos(a)*r,MathF.Sin(a)*r,20);
                    p.Z=(World?.Terrain.RequestTerrainHeight(p.X,p.Y)??Position.Z)+20;
                    Emit(p,new(1,0,0),8,MuGame.Random.Next(22,82)*.01f,MuGame.Random.Next(4,7));
                }
                Emit(Position,new(1,0,0),25,2.8f,7);
            }
            else if(_caster is PlayerObject owner)
                for(int i=0;i<2;i++)if(owner.TryGetBoneWorldMatrix(MuGame.Random.Next(41),out var bone))
                    Emit(bone.Translation,new(1,.5f,.4f),8,MuGame.Random.Next(22,82)*.01f,MuGame.Random.Next(4,7));
        }
        for(int i=0;i<_particles.Length;i++)if(_particles[i].Life>0){_particles[i].Life-=f;if(_particles[i].Texture==7){_particles[i].Scale+=.05f*f;_particles[i].Position+=Vector3.UnitZ*f;}}
        if(_age>=64||_caster.Status==GameControlStatus.Disposed){World?.RemoveObject(this);Dispose();}
    }
    private void Quad(Texture2D texture,Vector3 p,Vector3 right,Vector3 up,Vector3 light)
    {
        int n=0;SourceEffectGeometry.Quad(_vertices,ref n,p-up,p+up,right,new Color(light),0,1);
        ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,samplerOverride:SamplerState.LinearClamp);
    }
    public override void Draw(GameTime time)
    {
        if(!Visible||Status!=GameControlStatus.Ready)return;
        Matrix camera=Matrix.Invert(Camera.Instance.View);
        if(_impactAge<0)
        {
            float rotation=(float)time.TotalGameTime.TotalMilliseconds*.0006f*MathHelper.TwoPi;
            for(int i=0;i<6;i++)
            {
                var texture=_textures[i/2];if(texture==null)continue;
                float size=i<2?(i==0?3.2f:2.4f):i<4?(i==2?.8f:.4f):1.6f;
                float angle=i%2==0?rotation:-rotation;
                Vector3 right=(camera.Right*MathF.Cos(angle)+camera.Up*MathF.Sin(angle))*texture.Width*size*.5f;
                Vector3 up=(camera.Up*MathF.Cos(angle)-camera.Right*MathF.Sin(angle))*texture.Height*size*.5f;
                Quad(texture,Position,right,up,new(1,i<2?.4f:.2f,i<2?.4f:.2f));
            }
        }
        else if(_impactAge<10 && _textures[1]!=null)
            for(int i=0;i<5;i++)
            {
                float angle=_caster.Angle.Z+i*MathHelper.TwoPi/5;
                Vector3 p=Position+new Vector3(MathF.Cos(angle)*150,MathF.Sin(angle)*150,5);
                Quad(_textures[1]!,p,Vector3.UnitX*40,Vector3.UnitY*40,new Vector3(1,.2f,.05f)*(1-_impactAge/10));
            }
        if(_impactAge>=0 && _impactAge<12 && _damage!=null)
        {
            float size=(1+_impactAge*.5f)*50;
            float fade=_impactAge>5?MathF.Pow(.5f,_impactAge-5):1;
            Quad(_damage,Position,Vector3.UnitX*size,Vector3.UnitY*size,new Vector3(1,.8f,.5f)*fade);
            Quad(_damage,Position,Vector3.UnitX*size,Vector3.UnitY*size,new Vector3(1,0,0)*fade);
        }
        foreach(var p in _particles)
        {
            var texture=_textures[p.Texture];if(p.Life<=0||texture==null)continue;
            Quad(texture,p.Position,camera.Right*texture.Width*p.Scale*.5f,camera.Up*texture.Height*p.Scale*.5f,p.Light*MathF.Min(1,p.Life/8));
        }
    }
    public override void Dispose()
    {
        foreach(var crack in _cracks){crack?.World?.RemoveObject(crack);crack?.Dispose();}
        base.Dispose();
    }
    private sealed class ShockCrack : ModelObject
    {
        private float _life=20;
        internal ShockCrack()
        {
            Scale=1+MuGame.Random.Next(4)*.1f+MuGame.Random.Next(10)*.05f;
            Angle=new(0,0,MathHelper.ToRadians(MuGame.Random.Next(360)));
            Light=new(1,.4f,.2f);LightEnabled=false;Alpha=1;
            BlendMesh=-2;BlendMeshLight=1;BlendMeshState=Blendings.OneOneAdditive;
            BlendState=Blendings.OneOneAdditive;IsTransparent=true;AffectedByTransparency=false;
            DepthState=DepthStencilState.DepthRead;RenderShadow=false;AnimationSpeed=25*.3f;HoldOnLastFrame=true;ContinuousAnimation=true;
        }
        protected override bool AllowDynamicLightingShader=>false;
        public override async Task Load(){Model=await BMDLoader.Instance.Prepare("Effect/knight_plancrack_a.bmd");await base.Load();}
        public override void Update(GameTime time)
        {
            base.Update(time);if(Status!=GameControlStatus.Ready)return;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;Alpha*=MathF.Pow(.9f,f);BlendMeshLight=Alpha;_life-=f;
            if(_life<=0){World?.RemoveObject(this);Dispose();}
        }
    }

}
