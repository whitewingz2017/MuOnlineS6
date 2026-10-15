#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Wings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Objects.Effects;

// Bone lists, colors and pulses from MuMain ItemEffects.cpp.
public sealed class ReferenceWingEffect : EffectObject
{
    private readonly WingObject _wing;
    private Texture2D? _light,_flare,_shiny,_energy,_blue,_thunder,_spirit;
    private Task? _prepare;
    private float _time;
    private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
    private readonly Particle[] _particles=new Particle[512];
    private readonly VertexPositionColorTexture[] _joints=new VertexPositionColorTexture[10*8*2*6];
    private readonly Vector3[] _jointStart=new Vector3[10],_jointEnd=new Vector3[10];
    public int ParticleCount {get;private set;}
    public int JointCount {get;private set;}
    public int SpriteCount {get;private set;}
    private int _next;
    private float _ticks;
    private short _index=-1;
    private struct Particle {public Vector3 Position,Color;public float Life,Scale,Rotation,Gravity;public bool Energy;}
    private static readonly int[] EternalCore=[24,31,15,8,53,35],EternalBlue=[22,23,25,29,30,28,32,13,16,14,12,9,7,6,57,58,40,39],EternalSmall=[56,38,51,45];
    private static readonly int[] IllusionRed=[5,6,7,8,18,19,23,24,25,27,37,38],IllusionGreen=[4,9,13,14,26,32,31,33];
    private static readonly int[] RuinCore=[6,15,24,56,47,38],RuinSpark=[7,16,25,57,48,39,11,22,31,63,54,40,10,21,30,62,53,41,9,20,29,61,52,42,8,19,28,60,51,43,18,27,59,50,17,26,58,49];
    private static readonly int[] DimensionGold=[7,30,31,43,8,20],DimensionPurple=[29,38,42,19,15,6];
    public ReferenceWingEffect(WingObject wing)
    {
        _wing=wing;Hidden=true;LightEnabled=false;IsTransparent=true;AffectedByTransparency=true;
        BlendState=Blendings.OneOneAdditive;DepthState=DepthStencilState.DepthRead;
        BoundingBoxLocal=new BoundingBox(new Vector3(-500),new Vector3(500));
    }
    private async Task Prepare()
    {
        _light=await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
        _flare=await TextureLoader.Instance.PrepareAndGetTexture("Effect/Flare.jpg");
        _shiny=await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny01.jpg");
        _energy=await TextureLoader.Instance.PrepareAndGetTexture("Effect/energy02.jpg");
        _blue=await TextureLoader.Instance.PrepareAndGetTexture("Effect/flareBlue.jpg");
        _thunder=await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointThunder01.jpg");
        _spirit=await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointSpirit01.jpg");
    }
    public override void Update(GameTime time)
    {
        base.Update(time);
        bool active=_wing.ItemIndex is 6 or 37 or 38 or 39 or 43 && _wing.Model!=null && !_wing.Hidden;
        if(_index!=_wing.ItemIndex){Array.Clear(_particles);ParticleCount=JointCount=SpriteCount=0;_index=_wing.ItemIndex;_ticks=0;}
        if(active)_prepare??=Prepare();
        Hidden=!active || _prepare?.IsCompletedSuccessfully!=true;
        _time=(float)time.TotalGameTime.TotalSeconds;
        if(Hidden)return;
        _ticks+=Math.Clamp((float)time.ElapsedGameTime.TotalSeconds,0,.2f)*25;
        while(_ticks>=1){_ticks--;Tick();}
    }
    private Vector3 Bone(int index,Matrix[] bones)=>Vector3.Transform(bones[index].Translation,_wing.WorldPosition);
    private void Spawn(int bone,float scale,bool energy,Matrix[] bones)
    {
        if((uint)bone>=(uint)bones.Length)return;
        _particles[_next++%_particles.Length]=new Particle{Position=Bone(bone,bones)+(energy?Vector3.Zero:new Vector3(MuGame.Random.Next(-5,5),MuGame.Random.Next(-5,5),0)),
            Color=energy?new(.6f,.4f,.7f):Vector3.One,Life=energy?8:MuGame.Random.Next(10,20),
            Scale=energy?scale*MuGame.Random.Next(128,192)*.01f:scale+MuGame.Random.Next(10)*.02f,
            Rotation=energy?MathHelper.ToRadians(MuGame.Random.Next(360)):0,Gravity=energy?0:-MuGame.Random.Next(10)*.3f,Energy=energy};
    }
    private void Tick()
    {
        for(int i=0;i<_particles.Length;i++)
        {
            ref var p=ref _particles[i];if(p.Life<=0)continue;p.Life--;
            if(p.Energy){p.Scale-=.04f;p.Rotation+=MathHelper.ToRadians(5);}
            else{p.Scale-=.01f;p.Rotation+=MathHelper.ToRadians(10);p.Position.Z+=p.Gravity;p.Color/=1.02f;}
        }
        var bones=_wing.GetBoneTransforms();if(bones==null)return;
        ParticleCount=0;foreach(var particle in _particles)if(particle.Life>0)ParticleCount++;
        JointCount=0;
        if(_index==6)for(int i=0;i<10;i++)
        {
            int start=i<5?22-i:7-(i-5),end=i<5?30-i:11+(i-5);
            if(start>=bones.Length || end>=bones.Length)continue;
            _jointStart[JointCount]=Bone(start,bones);_jointEnd[JointCount]=Bone(end,bones);JointCount++;
        }
        if(_index==38 && MuGame.Random.Next(2)==0){Spawn(13,.5f,false,bones);Spawn(31,.5f,false,bones);}
        if(_index==39)for(int i=0;i<RuinSpark.Length;i++)Spawn(RuinSpark[i],i<6?.1f:i<18?.3f:i<30?.5f:.7f,true,bones);
    }
    private void Quad(Texture2D? texture,Vector3 position,float scale,Vector3 color,float rotation,Matrix camera,float u0=0,float u1=1,float aspect=1)
    {
        if(texture==null || scale<=0)return;
        SpriteCount++;
        var right=(camera.Right*MathF.Cos(rotation)+camera.Up*MathF.Sin(rotation))*texture.Width*scale*.5f*aspect;
        var up=(camera.Up*MathF.Cos(rotation)-camera.Right*MathF.Sin(rotation))*texture.Height*scale*.5f;
        int n=0;SourceEffectGeometry.Quad(_vertices,ref n,position-up,position+up,right,new Color(color*TotalAlpha),0,1);
        // Particle atlas advances horizontally, rather than stretching the entire sheet.
        for(int i=0;i<n;i++){var uv=_vertices[i].TextureCoordinate;_vertices[i].TextureCoordinate=new Vector2(u0+uv.Y*(u1-u0),uv.X);}
        ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,samplerOverride:SamplerState.LinearClamp);
    }
    private void Group(int[] list,Texture2D? texture,float scale,Vector3 color,Matrix camera,Matrix[] bones)
    {foreach(int bone in list)if((uint)bone<(uint)bones.Length)Quad(texture,Bone(bone,bones),scale,color,0,camera);}
    public override void DrawAfter(GameTime time)
    {
        base.DrawAfter(time);if(Hidden || !Visible)return;
        var bones=_wing.GetBoneTransforms();if(bones==null)return;var camera=Matrix.Invert(Camera.Instance.View);
        SpriteCount=0;
        float scale=MathF.Abs(MathF.Sin(_time*3))*.2f,lumi=MathF.Abs(MathF.Sin(_time*3))*.3f;
        switch(_index)
        {
            case 6:
                float width=(MathF.Sin(_time*4)*.3f+.3f)*10+20;
                for(int i=0;i<JointCount;i++)Quad(_blue,_jointStart[i],width/28,new(.6f,.3f,.8f),0,camera);
                DrawJoints(_thunder,width,false);DrawJoints(_spirit,width+5,true);break;
            case 37:
                Group(EternalCore,_light,scale+1.3f,new(.5f+lumi,.5f+lumi,.6f+lumi),camera,bones);
                Group(EternalBlue,_light,scale+1.5f,new(.1f,.1f,.9f),camera,bones);
                Group(EternalSmall,_light,scale+.5f,new(.1f,.1f,.9f),camera,bones);break;
            case 38:
                scale=MathF.Abs(MathF.Sin(_time*2))*.2f;lumi=MathF.Abs(MathF.Sin(_time*2))*.4f;
                Group(IllusionRed,_flare,scale+.6f,new(.5f+lumi,lumi,lumi),camera,bones);
                Group(IllusionGreen,_light,1.3f,new(lumi,.5f+lumi,lumi),camera,bones);
                float pulse=(MathF.Sin(_time*4)+1)*.05f;
                Group([33],_light,.4f,new(.8f+pulse,.8f+pulse,.3f+pulse),camera,bones);break;
            case 39:Group(RuinCore,_light,scale+1.5f,new(.7f+lumi,.5f+lumi,.8f+lumi),camera,bones);break;
            case 43:
                scale=MathF.Abs(MathF.Sin(_time*2))*.2f;lumi=MathF.Abs(MathF.Sin(_time*2))*.4f;
                for(int i=0;i<DimensionGold.Length;i++)if((uint)DimensionGold[i]<(uint)bones.Length)
                    Quad(_flare,Bone(DimensionGold[i],bones),scale+(i<2?2:.5f),new Vector3(1+lumi,.7f+lumi,.2f+lumi)/(i<2?2:4),0,camera);
                Group(DimensionPurple,_flare,scale+2,new Vector3(.5f+lumi,.1f+lumi,.4f+lumi)/2,camera,bones);break;
        }
        for(int i=0;i<_particles.Length;i++){ref var p=ref _particles[i];if(p.Life<=0)continue;
            int frame=(23-(int)p.Life)/6%4;
            Quad(p.Energy?_energy:_shiny,p.Position,p.Scale,p.Color*(p.Energy?p.Life/24:1),p.Rotation,camera,p.Energy?frame*.25f:0,p.Energy?(frame+1)*.25f:1,p.Energy?.25f:1);}
    }
    private void DrawJoints(Texture2D? texture,float width,bool reverse)
    {
        if(texture==null)return;int count=0;
        // Native thunder subtype 14 and spirit subtype 4 use nine samples of
        // a ten-step bone-to-bone span, with zero-angle X/Z crossing tail planes.
        for(int joint=0;joint<JointCount;joint++)for(int face=0;face<2;face++)for(int point=1;point<9;point++)
        {
            var start=reverse?_jointEnd[joint]:_jointStart[joint];var end=reverse?_jointStart[joint]:_jointEnd[joint];
            var a=Vector3.Lerp(start,end,point/10f);var b=Vector3.Lerp(start,end,(point+1)/10f);
            var half=(face==0?Vector3.UnitX:Vector3.UnitZ)*(width*.5f);
            SourceEffectGeometry.Quad(_joints,ref count,a,b,half,new Color(new Vector3(.3f,.3f,1)*TotalAlpha),point/9f,(point+1)/9f);
        }
        ElfSupportBuffEffect.DrawWorldGeometry(texture,_joints,count);
    }
}
