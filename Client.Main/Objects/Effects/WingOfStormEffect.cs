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
// MuMain ApplyWingOfStorm's persistent bone-linked cloud and orange/red light layers.
public sealed class WingOfStormEffect : EffectObject
{
    private static readonly int[] Clouds=[9,20,19,10,18,28,27,36,35,38,37,53,48,62,70,72,71,78,79,80,87,90,91,106,102];
    private static readonly int[] Lights=[64,61,69,77,86,98,97,99,104,103,105,12,8,17,26,34,52,44,51,50,49,45];
    private readonly WingObject _wing;
    private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
    private Texture2D? _cloud,_light;
    private Task? _preparation;
    private float _time;
    private readonly Thunder[] _thunder = new Thunder[6];
    private static readonly int[] ThunderBones=[11,21,29,63,81,89];
    public WingOfStormEffect(WingObject wing)
    {
        _wing=wing;Hidden=true;IsTransparent=true;AffectedByTransparency=true;LightEnabled=false;
        BlendState=BlendState.Additive;DepthState=DepthStencilState.DepthRead;
        BoundingBoxLocal=new BoundingBox(new Vector3(-500),new Vector3(500));
    }
    private async Task Prepare()
    {
        var cloud=await TextureLoader.Instance.PrepareAndGetTexture("Effect/Clud64.jpg");
        var light=await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
        if(IsDisposeRequested)return;_cloud=cloud;_light=light;
        var model=await BMDLoader.Instance.Prepare("Effect/lightning_type01.bmd");
        for(int i=0;i<_thunder.Length;i++)
        {
            if(IsDisposeRequested)return;
            var part=new Thunder {Model=model};
            _thunder[i]=part;Children.Add(part);await part.Load();
        }
    }
    public override void Update(GameTime time)
    {
        base.Update(time);if(Status!=GameControlStatus.Ready)return;
        bool active=_wing.ItemIndex==36 && _wing.Model!=null && !_wing.Hidden;
        if(active)_preparation??=Prepare();
        Hidden=!active || _preparation==null || !_preparation.IsCompletedSuccessfully;
        _time=(float)time.TotalGameTime.TotalSeconds;
        if(!active){foreach(var part in _thunder)part?.Stop();}
        if(!Hidden && FPSCounter.Instance.RandFPSCheck(2))
        {
            var bones=_wing.GetBoneTransforms();
            for(int i=0;i<_thunder.Length;i++)
                if(bones!=null && ThunderBones[i]<bones.Length && FPSCounter.Instance.RandFPSCheck(20))
                    _thunder[i]?.Activate(bones[ThunderBones[i]].Translation);
        }
    }
    private void Quad(Texture2D? texture,int bone,float scale,Vector3 color,float rotation,Matrix camera,Matrix[] bones)
    {
        if(texture==null || (uint)bone>=(uint)bones.Length)return;
        Vector3 pos=Vector3.Transform(bones[bone].Translation,_wing.WorldPosition);
        Vector3 right=(camera.Right*MathF.Cos(rotation)+camera.Up*MathF.Sin(rotation))*texture.Width*scale*.5f;
        Vector3 up=(camera.Up*MathF.Cos(rotation)-camera.Right*MathF.Sin(rotation))*texture.Height*scale*.5f;
        int n=0;SourceEffectGeometry.Quad(_vertices,ref n,pos-up,pos+up,right,new Color(color*TotalAlpha),0,1);
        ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,blendOverride:ReferenceEquals(texture,_cloud)?Blendings.Negative:Blendings.OneOneAdditive,samplerOverride:SamplerState.LinearClamp);
    }
    public override void Draw(GameTime time)
    {
        if(!Visible || Hidden || Status!=GameControlStatus.Ready)return;
        var bones=_wing.GetBoneTransforms();if(bones==null)return;
        Matrix camera=Matrix.Invert(Camera.Instance.View);
        float luminosity=.5f+MathF.Abs(MathF.Sin(_time*.4f))*.4f;
        foreach(int bone in Clouds)Quad(_cloud,bone,.5f,new Vector3(luminosity),MathHelper.ToRadians(_time*10),camera,bones);
        float scale=MathF.Abs(MathF.Sin(_time*3))*.2f;
        foreach(int bone in Lights)
        {
            bool red=bone is 12 or 64 or 98 or 52;
            Quad(_light,bone,scale+(red?1.4f:.3f),red?new Vector3(.9f,0,0):new Vector3(.8f,.5f,.2f),0,camera,bones);
        }
    }
    private sealed class Thunder : ModelObject
    {
        private float _life;
        private bool _fading;
        public Thunder()
        {
            Hidden=true;RenderShadow=false;LightEnabled=false;Light=new Vector3(.95f,.95f,1f);
            BlendState=Blendings.OneOneAdditive;BlendMeshState=Blendings.OneOneAdditive;
            BlendMesh=-2;DepthState=DepthStencilState.DepthRead;IsTransparent=true;AffectedByTransparency=true;
            ContinuousAnimation=true;
        }
        public void Stop(){Hidden=true;_life=0;}
        public void Activate(Vector3 position)
        {
            Position=position+new Vector3(MuGame.Random.Next(-20,20),MuGame.Random.Next(-20,20),MuGame.Random.Next(-20,20));
            Scale=.3f+MuGame.Random.Next(100)*.002f;
            Angle=new Vector3(MathHelper.ToRadians(MuGame.Random.Next(360)),MathHelper.ToRadians(MuGame.Random.Next(360)),MathHelper.ToRadians(MuGame.Random.Next(360)));
            Alpha=1;_life=100;_fading=false;Hidden=false;
        }
        public override void Update(GameTime time)
        {
            RenderShadow=false; if(_life<=0)return;
            float factor=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _life-=factor;
            Alpha+=(_fading?-.3f:.3f)*factor;
            if(Alpha>=1){Alpha=1;_fading=true;}
            Angle+=new Vector3(MathHelper.ToRadians(.15f*factor));
            if(_life<=0 || Alpha<=0){Stop();return;}base.Update(time);
        }
    }

}

