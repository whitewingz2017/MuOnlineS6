#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Objects.Effects;

/// <summary>MuMain wind-force subtype one; lifetime belongs to server buff state.</summary>
public sealed class RageFighterBuffWindEffect : ModelObject
{
    private readonly PlayerObject _owner;
    private readonly BuffManager _buffs;
    private readonly Mirror _mirror;
    private struct Smoke { internal Vector3 Position; internal float Life,Scale,Alpha,Rotation,Gravity; }
    private readonly Smoke[] _smoke=new Smoke[64];
    private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
    private Texture2D? _smokeTexture;
    private Texture2D? _redLight;
    private static readonly int[] RedLightBones=[12,17,5,10,36,27,37,28,11,35,2,3,36,20,27,4,26];
    private Vector3 _lastOwnerPosition;
    private int _nextSmoke;
    public RageFighterBuffWindEffect(PlayerObject owner,BuffManager buffs)
    {
        _owner=owner;_buffs=buffs;Scale=1;Alpha=0;
        LightEnabled=false;BlendMesh=-2;BlendMeshLight=1;BlendMeshState=Blendings.OneOneAdditive;
        BlendState=Blendings.OneOneAdditive;IsTransparent=true;AffectedByTransparency=false;
        DepthState=DepthStencilState.DepthRead;RenderShadow=false;LinkParentAnimation=false;ContinuousAnimation=true;
        BoundingBoxLocal=new(new(-500),new(500));
        _mirror=new Mirror();Children.Add(_mirror);
    }
    protected override bool AllowDynamicLightingShader=>false;
    public override async Task Load()
    {
        Model=await BMDLoader.Instance.Prepare("Effect/wind_foce.bmd");await base.Load();
        _smokeTexture=await TextureLoader.Instance.PrepareAndGetTexture("Effect/smokelines01.jpg");
        _redLight=await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01_red.jpg");
        _lastOwnerPosition=_owner.WorldPosition.Translation;
    }
    public override void Update(GameTime time)
    {
        base.Update(time);if(Status!=GameControlStatus.Ready)return;
        bool active=_buffs.HasBuff(_owner.NetworkId,BuffEffectId.IgnoreDefense)||_buffs.HasBuff(_owner.NetworkId,BuffEffectId.IncreaseHealth)||_buffs.HasBuff(_owner.NetworkId,BuffEffectId.IncreaseBlock);
        Alpha=active&&!_buffs.HasBuff(_owner.NetworkId,BuffEffectId.Invisible)?1:0;
        bool mirrorActive=_buffs.HasBuff(_owner.NetworkId,BuffEffectId.IncreaseBlock);
        if(!mirrorActive)_mirror.Stop();
        else if(!_mirror.Active && MuGame.Random.Next(70)==7)_mirror.Start();
        float milliseconds=(float)time.TotalGameTime.TotalMilliseconds;
        Light=new Vector3(.6f,.6f,1)*((MathF.Sin(milliseconds*.001f)+1)*.25f+.2f);
        Angle=new(0,0,MathHelper.ToRadians(-milliseconds*.3f));
        // This object is a child of the world-positioned ClassBuffEffect.
        Position=Vector3.Zero;Scale=1;
        BlendMeshLight=6;
        float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        Vector3 ownerPosition=_owner.WorldPosition.Translation,delta=ownerPosition-_lastOwnerPosition;
        if(Alpha>0 && _buffs.HasBuff(_owner.NetworkId,BuffEffectId.IgnoreDefense) && FPSCounter.Instance.RandFPSCheck(1))
        {
            int bone=FPSCounter.Instance.RandFPSCheck(2)?26:35;
            _smoke[_nextSmoke]=new(){Position=RageFighterBoneSampler.Sample(_owner.Model,(int)_owner.CurrentAction,(float)_owner.GetAnimationTime(),bone,_owner.WorldPosition),
                Life=35,Scale=MuGame.Random.Next(96,192)*.01f*.005f,Rotation=MathHelper.ToRadians(MuGame.Random.Next(360)),Gravity=MuGame.Random.Next(12,28)*.1f};
            _nextSmoke=(_nextSmoke+1)%_smoke.Length;
        }
        for(int i=0;i<_smoke.Length;i++)
        {
            ref Smoke smoke=ref _smoke[i];if(smoke.Life<=0)continue;
            smoke.Life-=f;smoke.Position+=delta+Vector3.UnitZ*smoke.Gravity*f;
            smoke.Alpha=Math.Clamp(smoke.Alpha+(smoke.Life<10?-.1f:(MuGame.Random.Next(5)+2)*.1f)*f,0,.7f);
            smoke.Scale+=(MuGame.Random.Next(3)+6)*.01f*f;
        }
        _lastOwnerPosition=ownerPosition;
    }
    public override void Draw(GameTime time)
    {
        base.Draw(time);if(Alpha<=0 || !Visible || _smokeTexture==null)return;
        Matrix camera=Matrix.Invert(Camera.Instance.View);
        if(_redLight!=null)
        {
            Vector3 center=_owner.WorldPosition.Translation;
            float milliseconds=(float)time.TotalGameTime.TotalMilliseconds;
            float pulse=(MathF.Sin(milliseconds*.0005f)+1)*.25f+.2f;
            for(int i=0;i<2;i++)DrawRed(center+Vector3.UnitZ*5,Vector3.UnitX*175,Vector3.UnitY*175,pulse);
            for(int i=0;i<RedLightBones.Length;i++)
            {
                float brightness=(MathF.Sin(milliseconds*(i is 5 or 7 or 8 or 9 or 12 or 14 or 15 or 16?.005f:.01f))+1)*.25f+.2f;
                Vector3 bone=RageFighterBoneSampler.Sample(_owner.Model,(int)_owner.CurrentAction,(float)_owner.GetAnimationTime(),RedLightBones[i],_owner.WorldPosition);
                DrawRed(bone,camera.Right*_redLight.Width*.75f,camera.Up*_redLight.Height*.75f,brightness);
            }
        }
        foreach(var smoke in _smoke)
        {
            if(smoke.Life<=0 || smoke.Alpha<=0)continue;
            Vector3 right=(camera.Right*MathF.Cos(smoke.Rotation)+camera.Up*MathF.Sin(smoke.Rotation))*_smokeTexture.Width*smoke.Scale*.5f;
            Vector3 up=(camera.Up*MathF.Cos(smoke.Rotation)-camera.Right*MathF.Sin(smoke.Rotation))*_smokeTexture.Height*smoke.Scale*.5f;
            int count=0;SourceEffectGeometry.Quad(_vertices,ref count,smoke.Position-up,smoke.Position+up,right,new Color(new Vector3(1,.12f,0)*smoke.Alpha),0,1);
            ElfSupportBuffEffect.DrawWorldGeometry(_smokeTexture,_vertices,count,samplerOverride:SamplerState.LinearWrap);
        }
    }
    private void DrawRed(Vector3 center,Vector3 right,Vector3 up,float brightness)
    {
        int count=0;SourceEffectGeometry.Quad(_vertices,ref count,center-up,center+up,right,new Color(Vector3.One*brightness),0,1);
        ElfSupportBuffEffect.DrawWorldGeometry(_redLight!,_vertices,count,samplerOverride:SamplerState.LinearClamp);
    }
    public override void DrawMesh(int mesh)
    {
        var shader=GraphicsManager.Instance.AlphaTestEffect3D;bool colors=shader.VertexColorEnabled;
        try{shader.VertexColorEnabled=true;base.DrawMesh(mesh);}finally{shader.VertexColorEnabled=colors;}
    }
    private sealed class Mirror : ModelObject
    {
        private float _life;
        internal bool Active=>_life>0;
        internal Mirror()
        {
            Alpha=0;LightEnabled=false;RenderShadow=false;ContinuousAnimation=true;LinkParentAnimation=false;
            BlendMesh=-2;BlendMeshLight=6;BlendMeshState=Blendings.OneOneAdditive;BlendState=Blendings.OneOneAdditive;
            IsTransparent=true;AffectedByTransparency=false;DepthState=DepthStencilState.DepthRead;
        }
        internal void Start(){_life=50;Scale=1;Light=new(.6f,.6f,1);Alpha=1;}
        internal void Stop(){_life=0;Alpha=0;}
        protected override bool AllowDynamicLightingShader=>false;
        public override async Task Load(){Model=await BMDLoader.Instance.Prepare("Effect/wind_foce_mirror.bmd");await base.Load();}
        public override void Update(GameTime time)
        {
            base.Update(time);if(Status!=GameControlStatus.Ready || !Active)return;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _life-=f;Scale=MathF.Min(1.5f,Scale*MathF.Pow(1.015f,f));Light*=MathF.Pow(.9f,f);
            // Compensate for the parent wind's -.3 rotation to obtain the source +.4 world rotation.
            Angle=new(0,0,MathHelper.ToRadians((float)time.TotalGameTime.TotalMilliseconds*.7f));
            if(_life<=0)Stop();
        }
        public override void DrawMesh(int mesh)
        {
            var shader=GraphicsManager.Instance.AlphaTestEffect3D;bool colors=shader.VertexColorEnabled;
            try{shader.VertexColorEnabled=true;base.DrawMesh(mesh);}finally{shader.VertexColorEnabled=colors;}
        }
    }
}
