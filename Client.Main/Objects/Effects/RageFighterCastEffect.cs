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

/// <summary>Bounded source-model Rage Fighter casting families.</summary>
public sealed class RageFighterCastEffect : StagedCombatEffect
{
    private readonly ushort _skill;
    private readonly WalkerObject? _target;
    private readonly Vector3? _targetPosition;
    public RageFighterCastEffect(WalkerObject caster,WalkerObject? target,ushort skill,Vector3? targetPosition=null):base(caster,150){_skill=skill;_target=target;_targetPosition=targetPosition;}
    protected override void BuildParts()
    {
        Vector3 target=_target?.WorldPosition.Translation??_targetPosition??Origin+Forward*150;
        if(_skill==260)AddPart(new Part("wolf_head_effect",Caster,10/RageFighterJointEffect.Speed(Caster,(int)PlayerAction.PlayerSkillThrust,.4f),0,1.5f,Vector3.One),0);
        if(_skill==261)
        {
            AddPart(new Part("down_left_punch",Caster,100,1,1,Vector3.One),0);
            AddPart(new Part("down_right_punch",Caster,100,1,1,Vector3.One,gateFrame:6),0);
        }
        if(_skill==262)
        {
            AddPart(new Part("wolf_head_effect2",Caster,4/RageFighterJointEffect.Speed(Caster,(int)PlayerAction.PlayerSkillGiantswing,.4f),6,1,new(.12f,.12f,.18f),gateFrame:6,bone:4),0);
            AddPart(new Part("wolf_head_effect2",Caster,4/RageFighterJointEffect.Speed(Caster,(int)PlayerAction.PlayerSkillGiantswing,.4f),6,1.5f,new(.12f,.12f,.18f),gateFrame:11,bone:27),0);
            AddPart(new Part("shockwave01",Caster,15,7,1,new(.4f,.5f,1),gateFrame:6,bone:0),0);
            AddPart(new Part("shockwave02",Caster,15,7,.5f,new(.5f,.5f,1),gateFrame:11,bone:4),0);
            AddPart(new Part("shockwave_spin01",Caster,20,7,.5f,Vector3.One,gateFrame:11,bone:4),0);
            AddPart(new Part("shockwave_spin01",Caster,20,7,.9f,Vector3.One,gateFrame:11,bone:4),0);
        }
        if(_skill is 263 or 266 or 267 or 268)
        {
            Vector3 tint=_skill==263?new(1,.5f,.2f):_skill==266?new(.5f,.55f,1):_skill==267?new(.85f,.2f,1):new(1,.2f,0);
            AddPart(new Part("wind_foce",Caster,_skill==263?70:50,_skill==263?9:2,1,tint),0);
            AddPart(new Part("wind_foce",Caster,70,9,1,_skill==263?new(1,.5f,0):tint),10);
            AddPart(new Part("wind_foce",Caster,70,9,1,_skill==263?new(1,.5f,0):tint),20);
            if(_skill!=263)
                for(int i=0;i<3;i++)AddPart(new Part("shockwave_ground01",Caster,20,3,1-i*.15f,_skill==266?new(.6f,.65f,1):_skill==267?new(1,.35f,1):new(1,.1f,0)),0);
        }
        if(_skill==264)
        {
            Vector3 direction=target-Origin;direction.Z=0;
            if(direction.LengthSquared()>.01f)direction.Normalize();else direction=Forward;
            Vector3 crackAngle=new(0,0,MathHelper.ToRadians(MuGame.Random.Next(45,225)));
            for(int meshGroup=0;meshGroup<2;meshGroup++)
                AddPart(new Part("knight_plancrack_dragon",Caster,110,8,.8f,
                    meshGroup==0?new(1,.8f,.2f):new(1,.2f,.1f),onlyMesh:meshGroup)
                    {Position=Origin+direction*55+Vector3.UnitZ*10,Angle=crackAngle},20);
        }
        if(_skill is 265 or 269)
        {
            if(_skill==265)AddPart(new Part("dragon_kick_dummy",Caster,35,1,1,new(.7f,.7f,1)),0);
            float yaw=_skill==265?MathF.Atan2(target.X-Origin.X,-(target.Y-Origin.Y)):Caster.Angle.Z;
            for(int i=0;i<7;i++)
            {
                int family=i<2?3:i<5?1:2;
                Vector3 offset=Vector3.Transform(new Vector3(-10,-60+(family==1?0:20),135),Matrix.CreateRotationZ(yaw));
                AddPart(new Part("multishot0"+family,Caster,family==1?16:family==2?13:12,4,0,Vector3.One)
                    {Position=Origin+offset,Angle=new(0,0,yaw)},0);
            }
        }
        // MuMain phoenix_shot_effect is a skeleton-only asset (zero meshes).
        if(_skill==270)AddPart(new Part("phoenix_shot_effect",Caster,35,5,1,Vector3.One){Position=target},0);
        if(_skill==264)
        {
            AddPart(new Part("shockwave01",Caster,15,7,1,new(1,.12f,0),gateFrame:5),0);
            AddPart(new Part("shockwave_ground01",Caster,20,11,1,new(1,.1f,0),gateFrame:5),0);
        }
        AddPart(new Sprites(Caster,_target,_skill),0);
        if(_skill is 260 or 262)AddPart(new RageFighterWolfEchoEffect(Caster,_skill),0);
        if(_skill is 260 or 261 or 262 or 264 or 265)AddPart(new RageFighterJointEffect(Caster,_skill),0);
        if(_skill is 261 or 264 or 265 or 270)AddPart(new RageFighterImpactEffect(Caster,_target,_skill),0);
    }
    private sealed class Part : ModelObject
    {
        private readonly string _path;private readonly WalkerObject _caster;private readonly int _kind;private float _life;
        private float _age;private readonly int _onlyMesh;private readonly Vector3 _sourceLight;
        private readonly float _gateFrame;private readonly int _bone;private bool _started;
        internal Part(string path,WalkerObject caster,float life,int kind,float scale,Vector3 light,float gateFrame=0,int bone=-1,int onlyMesh=-1)
        {
            _gateFrame=gateFrame;_bone=bone;_onlyMesh=onlyMesh;
            _sourceLight=light;_path="Effect/"+path+".bmd";_caster=caster;_kind=kind;_life=life;Position=caster.WorldPosition.Translation;Angle=caster.Angle;
            Scale=scale;Light=light;LightEnabled=false;Alpha=1;BlendMesh=-2;BlendMeshLight=1;
            BlendMeshState=Blendings.OneOneAdditive;BlendState=Blendings.OneOneAdditive;DepthState=DepthStencilState.DepthRead;
            IsTransparent=true;AffectedByTransparency=false;RenderShadow=false;AnimationSpeed=kind==8?25*.35f:kind==4?25/6f:kind==1?50*RageFighterJointEffect.Speed(caster,(int)PlayerAction.PlayerSkillStamp,.4f):25*.5f;HoldOnLastFrame=true;ContinuousAnimation=true;
            BoundingBoxLocal=new(new(-1000),new(1000));
        }
        protected override bool AllowDynamicLightingShader=>false;
        protected override bool ShouldRenderMesh(int mesh)=>(_started || _gateFrame==0) && (_onlyMesh<0 || (_onlyMesh==0?mesh!=1:mesh==1));
        public override void DrawMesh(int mesh)
        {
            var shader=GraphicsManager.Instance.AlphaTestEffect3D;
            bool colors=shader.VertexColorEnabled;Vector3 diffuse=shader.DiffuseColor;
            try
            {
                shader.VertexColorEnabled=true;
                shader.DiffuseColor=Vector3.One;
                base.DrawMesh(mesh);
            }
            finally {shader.VertexColorEnabled=colors;shader.DiffuseColor=diffuse;}
        }
        public override async Task Load()
        {
            Model=await BMDLoader.Instance.Prepare(_path);await base.Load();
            string? replacement=_kind==11?"Kwave2.jpg":_kind==7 && _gateFrame==5?"Damage2.jpg":_path.EndsWith("shockwave_spin01.bmd")?"Damage1.jpg":null;
            if(replacement!=null)
            {
                var texture=await TextureLoader.Instance.PrepareAndGetTexture("Effect/"+replacement);
                if(texture!=null)for(int i=0;i<Model.Meshes.Length;i++)SetMeshTextureOverride(i,texture);
            }
        }
        public override void Update(GameTime time)
        {
            if(!_started && _gateFrame>0 && _caster is PlayerObject owner)
            {
                float frame=(float)owner.GetAnimationTime();
                if(_path.EndsWith("down_right_punch.bmd",StringComparison.OrdinalIgnoreCase))
                    frame=_age*RageFighterJointEffect.Speed(_caster,(int)PlayerAction.PlayerSkillStamp,.4f)*2;
                if(frame<_gateFrame){_age+=FPSCounter.Instance.FPS_ANIMATION_FACTOR;return;}
            }
            _started=true;
            base.Update(time);if(Status!=GameControlStatus.Ready)return;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;_age+=f;
            if(_kind==8)
            {
                Alpha*=MathF.Pow(.96f,f);Light=_sourceLight*Alpha;
                TextureCoordinateOffset=new(0,-((float)time.TotalGameTime.TotalMilliseconds%2000)*.0001f);
                if(GetAnimationTime()>1 && _life>80)_life=80;
            }
            if(_bone>=0 && _caster is PlayerObject boneOwner && boneOwner.TryGetBoneWorldMatrix(_bone,out var attachment))Position=attachment.Translation;
            if(_kind==6){Angle=_caster.Angle;Alpha=MathF.Min(1,_life/20);}
            if(_kind==7)
            {
                if(_gateFrame==5){Scale*=MathF.Pow(1.24f,f);Light*=MathF.Pow(.45f,f);Position=new(Position.X,Position.Y,Position.Z*MathF.Pow(1.19f,f));}
                else {Scale+=.2f*f;Light*=MathF.Pow(_path.EndsWith("shockwave02.bmd")?.8f:.65f,f);}
            }
            if(_kind==0 && _caster is PlayerObject p && p.TryGetBoneWorldMatrix(27,out var bone))Position=bone.Translation;
            if(_kind==1)
            {
                Position=_caster.WorldPosition.Translation;Angle=_caster.Angle;
                if(_path.Contains("punch",StringComparison.OrdinalIgnoreCase) && GetAnimationTime()>=(_path.EndsWith("down_right_punch.bmd")?8:6))_life=0;
            }
            if(_kind is 2 or 9)
            {
                Angle=new(0,0,MathHelper.ToRadians(-(float)time.TotalGameTime.TotalMilliseconds*(_kind==9?.9f:.3f)));
                Scale=MathF.Min(3,Scale+.1f*f);Alpha*=MathF.Pow(.92f,f);Light*=MathF.Pow(_kind==9?.85f:.9f,f);
            }
            if(_kind is 3 or 11){Scale*=MathF.Pow(1.2f,f);Light*=MathF.Pow(_kind==11?.9f:.8f,f);}
            if(_kind==4)
            {
                Scale+=(_path.EndsWith("01.bmd")?.2f:_path.EndsWith("02.bmd")?.3f:.25f)*f;
                Alpha=MathF.Max(0,_life/18);
            }
            if(_kind==0)Alpha=MathF.Min(1,_life/20);
            BlendMeshLight=Alpha*(_kind is 2 or 3 or 9 or 11?6:1);_life-=f;
            if(_life<=0){World?.RemoveObject(this);Dispose();}
        }
    }
    private sealed class Sprites : EffectObject
    {
        private readonly WalkerObject _caster;private readonly WalkerObject? _target;private readonly ushort _skill;
        private readonly Texture2D?[] _textures=new Texture2D?[9];
        private readonly VertexPositionColorTexture[] _vertices=new VertexPositionColorTexture[6];
        private struct HitFlash {internal Vector3 Position,Light;internal float Life,Scale;internal byte Kind;internal bool Shrinking;}
        private readonly HitFlash[] _hits=new HitFlash[64];
        private int _nextHit;
        private float _age,_damageAge;private int _damageTick=-1;private bool _damageStarted,_shinyHit;
        internal Sprites(WalkerObject caster,WalkerObject? target,ushort skill)
        {_caster=caster;_target=target;_skill=skill;Position=caster.WorldPosition.Translation;IsTransparent=true;AffectedByTransparency=false;LightEnabled=false;DepthState=DepthStencilState.DepthRead;BoundingBoxLocal=new(new(-1800),new(1800));}
        public override async Task LoadContent()
        {
            await base.LoadContent();string[] paths=["sbumb.jpg","!SwordEff.jpg","SwordEff_mono.jpg","Lava.jpg","Magic_Ground1.jpg","clouds2.jpg","flare01.jpg","Damage1.jpg","ring.jpg"];
            for(int i=0;i<paths.Length;i++)_textures[i]=await TextureLoader.Instance.PrepareAndGetTexture("Effect/"+paths[i]);
        }
        public override void Update(GameTime time)
        {
            base.Update(time);if(Status!=GameControlStatus.Ready)return;_age+=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if(_skill==260 && _target!=null && _age>3 && _age<20 && FPSCounter.Instance.RandFPSCheck(1))
            {
                _hits[_nextHit]=new(){Position=_target.WorldPosition.Translation+new Vector3(MuGame.Random.Next(-40,40),MuGame.Random.Next(-40,40),MuGame.Random.Next(30,150)),Light=Vector3.One,Life=4,Scale=(1.5f+MuGame.Random.Next(10)*.02f)*2.1f};
                _nextHit=(_nextHit+1)%_hits.Length;
            }
            if(_skill is 266 or 267 or 268 && _age<15 && (int)(15-_age)%3!=0 && FPSCounter.Instance.RandFPSCheck(1))
            {
                _hits[_nextHit]=new(){Position=_caster.WorldPosition.Translation,Light=_skill==266?new(.2f,.4f,1):_skill==267?new(.5f,.28f,1):new(1,.15f,0),Kind=2,Life=20,Scale=1};
                _nextHit=(_nextHit+1)%_hits.Length;
            }
            if(_skill==262 && _target!=null)
            {
                float frame=(float)_caster.GetAnimationTime();
                if(frame>6)_damageStarted=true;
                if(frame>11 && !_shinyHit)
                {
                    _shinyHit=true;_hits[_nextHit]=new(){Position=_target.WorldPosition.Translation,Light=Vector3.One,Kind=8,Life=20,Scale=1.9f};
                    _nextHit=(_nextHit+1)%_hits.Length;
                }
                if(_damageStarted)
                {
                    _damageAge+=f;int life=(int)(20-_damageAge);
                    if(life>0 && life<17 && life%4==0 && life!=_damageTick)
                    {
                        _damageTick=life;
                        _hits[_nextHit]=new(){Position=_target.WorldPosition.Translation+new Vector3(MuGame.Random.Next(-40,50),MuGame.Random.Next(-40,50),MuGame.Random.Next(50,150)),Light=new(.6f,.94f,1),Kind=7,Life=5,Scale=(1.5f+MuGame.Random.Next(5)*.01f)};
                        _nextHit=(_nextHit+1)%_hits.Length;
                    }
                }
            }
            for(int i=0;i<_hits.Length;i++)
            {
                ref HitFlash flash=ref _hits[i];if(flash.Life<=0)continue;flash.Life-=f;
                if(flash.Kind==2)
                {
                    flash.Scale*=MathF.Pow(flash.Shrinking?.8f:1.2f,f);flash.Light*=MathF.Pow(.94f,f);
                    if(flash.Scale>6)flash.Shrinking=true;
                    if(flash.Shrinking && flash.Scale<1)flash.Life=0;
                }
                if(flash.Kind==7){flash.Scale*=MathF.Pow(1.2f,f);flash.Light*=MathF.Pow(.8f,f);}
            }
            if(_age>=140){World?.RemoveObject(this);Dispose();}
        }
        private void Quad(Texture2D texture,Vector3 center,Vector3 right,Vector3 up,Vector3 light)
        {
            int n=0;SourceEffectGeometry.Quad(_vertices,ref n,center-up,center+up,right,new Color(light),0,1);
            ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,samplerOverride:SamplerState.LinearClamp);
        }
        public override void Draw(GameTime time)
        {
            if(!Visible||Status!=GameControlStatus.Ready)return;
            Vector3 center=_caster.WorldPosition.Translation;Matrix cam=Matrix.Invert(Camera.Instance.View);
            foreach(var hit in _hits)
            {
                if(hit.Life<=0 || _textures[hit.Kind] is not { } texture)continue;
                if(hit.Kind==0)
                {
                    int frame=Math.Clamp((int)(4-hit.Life),0,3),n=0;
                    SourceEffectGeometry.Quad(_vertices,ref n,hit.Position-cam.Up*texture.Height*hit.Scale*.5f,
                        hit.Position+cam.Up*texture.Height*hit.Scale*.5f,cam.Right*texture.Width*.125f*hit.Scale,new Color(hit.Light),0,1);
                    for(int i=0;i<n;i++)_vertices[i].TextureCoordinate=new Vector2(frame*.25f+.005f+_vertices[i].TextureCoordinate.X*.24f,_vertices[i].TextureCoordinate.Y);
                    ElfSupportBuffEffect.DrawWorldGeometry(texture,_vertices,n,samplerOverride:SamplerState.LinearClamp);
                }
                else Quad(texture,hit.Position+(hit.Kind==2?Vector3.UnitZ*31*hit.Scale*FPSCounter.Instance.FPS_ANIMATION_FACTOR:Vector3.Zero),cam.Right*texture.Width*hit.Scale*.45f,cam.Up*texture.Height*hit.Scale*.55f,hit.Light);
            }
            if(_skill is 263 or 266 or 267 or 268 && _age<30 && _textures[4]!=null)
            {
                Vector3 tint=_skill==266?new(.5f,.55f,.9f):_skill==267?new(.78f,.2f,1):new(1,.15f,0);
                float magicScale=MathF.Pow(1.1f,_age),size=magicScale*50;
                if(magicScale<=8)
                {
                    float fade=MathF.Pow(.95f,_age)*MathF.Pow(.5f,MathF.Max(0,_age-MathF.Log(4)/MathF.Log(1.1f)));
                    for(int i=0;i<2;i++)Quad(_textures[4]!,center+Vector3.UnitZ*5,Vector3.UnitX*size,Vector3.UnitY*size,(_skill==263?new Vector3(1,.4f,0):tint)*fade);
                }
                if(_textures[5]!=null)
                {
                    float cloudScale=MathF.Min(_skill==263?5:3,MathF.Pow(1.08f,_age));
                    float cloudAngle=-(float)time.TotalGameTime.TotalMilliseconds*.5f*MathHelper.Pi/180;
                    Vector3 right=new(MathF.Cos(cloudAngle)*cloudScale*50,MathF.Sin(cloudAngle)*cloudScale*50,0);
                    Vector3 up=new(-right.Y,right.X,0);
                    Vector3 cloudTint=_skill==263?new(1,.6f,.6f):_skill==266?new(.65f,.8f,1):_skill==267?new(.8f,.4f,1):new(1,.3f,.2f);
                    for(int i=0;i<(_skill==263?1:3);i++)Quad(_textures[5]!,center+Vector3.UnitZ*(8+i*.1f),right,up,cloudTint*MathF.Pow(.9f,_age));
                }
            }
        }
    }
}
