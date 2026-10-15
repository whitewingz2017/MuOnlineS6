#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain SummonSystem's book summons. All children are preloaded and bounded.</summary>
public sealed class SummonerBookCastEffect : StagedCombatEffect
{
    private readonly ushort _skill;
    private readonly Vector3 _target;
    private readonly int _tier;
    public SummonerBookCastEffect(WalkerObject caster, Vector3 target, ushort skill) : base(caster, 230f)
    {
        _target = target; _skill = skill;
        if (caster is PlayerObject player)
        {
            // Original Weapon[1] is the equipped summon book, not the staff.
            int book = skill - 223 + 21;
            string? bookPath = ItemDatabase.GetItemDefinition(5, (short)book)?.TexturePath;
            int level = bookPath != null && string.Equals(player.Weapon1.TexturePath, bookPath, StringComparison.OrdinalIgnoreCase) ? player.Weapon1.ItemLevel
                : bookPath != null && string.Equals(player.Weapon2.TexturePath, bookPath, StringComparison.OrdinalIgnoreCase) ? player.Weapon2.ItemLevel : 0;
            _tier = level >= 11 ? 2 : level >= 7 ? 1 : 0;
        }
    }
    protected override void BuildParts()
    {
        Vector3 first = _skill == 223 ? new(1,.5f,0) : _skill == 224 ? new(0,.7f,1) : new(.6f,.3f,.9f);
        Vector3 second = _skill == 223 ? new(1,.5f,0) : _skill == 224 ? new(0,0,1) : new(.8f,.1f,.6f);
        string[] rings = ["Suhwanzin1", "Suhwanzin11", "Suhwanzin111", "Suhwanzin2", "Suhwanzin22", "Suhwanzin222", "Suhwanzin4"];
        for (int i = 0; i < rings.Length; i++)
            AddPart(new Piece("Effect/" + rings[i] + ".bmd", Origin, Caster.Angle.Z, i == 6 ? 25 : 40, i == 6 ? 4 : 1,
                i < 3 ? first : i < 6 ? second : _skill == 223 ? new(1,.5f,.8f) : _skill == 224 ? new(.8f,.5f,1) : new(.9f,.1f,1),
                spin: i == 0 || i == 2 || i == 4 ? -3 : 3), 0);
        Piece? creature = null;
        if (_skill == 223)
        {
            Vector3 start = Origin + new Vector3((MuGame.Random.Next(2) == 0 ? -1 : 1) * MuGame.Random.Next(150,450),
                (MuGame.Random.Next(2) == 0 ? -1 : 1) * MuGame.Random.Next(150,450), 0);
            creature = new Piece("Skill/summon_sahamutt.bmd", start, Caster.Angle.Z, 80, 2, Vector3.One, target: _target)
                { Scale = _tier == 0 ? .35f : _tier == 1 ? .5f : .7f };
            AddPart(creature, 0);
        }
        else if (_skill == 224)
        {
            creature = new Piece("Skill/summon_neil.bmd", Origin + Forward * 100f + Vector3.UnitZ * 10f,
                Caster.Angle.Z, 80, 3, Vector3.One, target: _target);
            AddPart(creature, 0);
            AddPart(new Piece("Skill/summon_neil.bmd", creature.Position, Caster.Angle.Z,80,3,new(1,0,0),onlyMesh:1,sync:creature),0);
            for (int i = 0; i <= _tier; i++)
            {
                AddPart(new Piece($"Skill/nelleff_nife0{i+1}.bmd", _target, Caster.Angle.Z, 50, 5, Vector3.One, gate: creature, gateFrame: 8),0);
                AddPart(new Piece($"Skill/nelleff_nife0{i+1}.bmd", _target, Caster.Angle.Z, 50, 5, new(1,0,0), gate: creature, gateFrame: 8,onlyMesh:1),0);
                AddPart(new Piece($"Skill/nell_nifegrund0{i+1}.bmd", _target, Caster.Angle.Z, 50, 6, Vector3.One, gate: creature, gateFrame: 10),0);
            }
            AddPart(new Piece("Skill/nell_nifegrund01.bmd", creature.Position + Forward * 60f, Caster.Angle.Z, 50, 6, Vector3.One, gate: creature, gateFrame: 10),0);
        }
        var sprites = new BookSprites(Origin, _target, _skill, creature, _tier);
        AddPart(sprites, 0);
        if (_skill == 225)
            for (int i = 0; i < (1 << _tier); i++)
            {
                var spirit = new Piece("Skill/summon_lagul.bmd", _target + new Vector3(MuGame.Random.Next(-150,150),MuGame.Random.Next(-150,150),0),
                    MathHelper.PiOver2*i,160,7,Vector3.One,target:_target) { Scale = 100f/70f };
                sprites.Spirits[i] = spirit;
                AddPart(spirit,0);
            }
    }

    internal sealed class Piece : ModelObject
    {
        private readonly string _path;
        private readonly int _kind, _spin;
        private readonly Vector3 _target;
        private readonly Piece? _gate, _sync;
        private readonly int _onlyMesh;
        private readonly float _gateFrame;
        private float _life, _age, _step, _distance, _yawNoise;
        private bool _started;
        internal bool KnifeReady { get; private set; }
        internal bool GroundReady { get; private set; }
        internal bool Exploding { get; private set; }
        internal Piece(string path, Vector3 position, float yaw, float life, int kind, Vector3 light,
            int spin=0, Vector3 target=default, Piece? gate=null, float gateFrame=0, int onlyMesh=-1, Piece? sync=null)
        {
            _onlyMesh=onlyMesh; _sync=sync; _path=path; _kind=kind; _life=life; _spin=spin; _target=target; _gate=gate; _gateFrame=gateFrame;
            Position=position; Angle=new(0,0,yaw); Light=light; LightEnabled=false;
            Scale=1; Alpha=kind is 1 or 4 or 5 ? 1 : 0;
            BlendMesh=kind is 1 or 4 ? 0 : -1; BlendMeshLight=0;
            BlendMeshState=Blendings.OneOneAdditive; BlendState=BlendState.AlphaBlend;
            DepthState=DepthStencilState.DepthRead; IsTransparent=true; AffectedByTransparency=false; RenderShadow=false;
            AnimationSpeed=(kind == 3 ? .35f : .5f)*25f;
            HoldOnLastFrame=kind is 2 or 3; BoundingBoxLocal=new(new(-1500),new(1500));
        }
        protected override bool AllowDynamicLightingShader => false;
        protected override bool ShouldRenderMesh(int mesh) => (_gate==null || _started) && (_onlyMesh>=0 ? mesh==_onlyMesh : _kind is 3 or 5 ? mesh!=1 : true);
        protected override bool IsBlendMesh(int mesh) => _onlyMesh==1 || _kind is 6 or 7 || (_kind==3 && Alpha<.7f) || base.IsBlendMesh(mesh);
        public override void DrawMesh(int mesh)
        {
            var shader=GraphicsManager.Instance.AlphaTestEffect3D;
            bool colors=shader.VertexColorEnabled; Vector3 diffuse=shader.DiffuseColor;
            try { shader.VertexColorEnabled=true; shader.DiffuseColor=Vector3.One; base.DrawMesh(mesh); }
            finally {shader.VertexColorEnabled=colors; shader.DiffuseColor=diffuse;}
        }
        public override async Task Load() { Model=await BMDLoader.Instance.Prepare(_path); await base.Load(); }
        internal Vector3 Bone(int index)
        {
            var bones=GetBoneTransforms();
            return bones != null && index < bones.Length ? (bones[index]*WorldPosition).Translation : Position;
        }
        public override void Update(GameTime gameTime)
        {
            if (_gate != null && !_started)
            {
                if (!(_gateFrame == 8 ? _gate.KnifeReady : _gate.GroundReady)) return;
            }
            _started=true; Hidden=false;
            base.Update(gameTime);
            if(Status!=GameControlStatus.Ready) return;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _age+=f; _step+=f;
            if(_kind is 1 or 4)
            {
                while(_step>=1) { _step-=1; BlendMeshLight=MathF.Max(0,BlendMeshLight+(_life<20?-.03f:BlendMeshLight<.5f?.05f:0)); }
                if(_kind==4) Scale+=.6f*f;
                else Angle+=Vector3.UnitZ*MathHelper.ToRadians(_spin*f);
            }
            else if(_kind==2)
            {
                if(_life<20) { CurrentAction=0; Alpha-=.05f*f; }
                else if(CurrentAction==0 && Alpha<.3f)
                {
                    Alpha+=.05f*f; FaceTarget(); _distance=Vector2.Distance(new(Position.X,Position.Y),new(_target.X,_target.Y));
                }
                else
                {
                    CurrentAction=1;
                    double frame=GetAnimationTime();
                    if(frame<3 && Alpha<.7f) Alpha+=.05f*f;
                    if(frame>=11) { Alpha-=.3f*f; Exploding=true; }
                    if(frame<=4) { FaceTarget(); _distance=Vector2.Distance(new(Position.X,Position.Y),new(_target.X,_target.Y)); }
                    if(frame>4 && frame<12) Position+=new Vector3(MathF.Sin(Angle.Z),-MathF.Cos(Angle.Z),0)*(_distance/(frame<10?13f:45f))*f;
                }
                Position=new(Position.X,Position.Y,World?.Terrain.RequestTerrainHeight(Position.X,Position.Y) ?? Position.Z);
            }
            else if(_kind==3)
            {
                Alpha+=(_life<20?-.05f:Alpha<.7f?.04f:0)*f;
                KnifeReady |= GetAnimationTime()>8; GroundReady |= GetAnimationTime()>10;
            }
            else if(_kind is 5 or 6)
                Alpha+=(_life<10?-.05f:_kind==6?.1f:.05f)*f;
            else if(_kind==7)
            {
                // JOINT_SPIRIT subtype 24 drives the visible Lagul, not its hidden subtype-0 emitter.
                Vector3 goal=_target+Vector3.UnitZ*80f;
                float desired=MathF.Atan2(goal.X-Position.X,-(goal.Y-Position.Y));
                float delta=MathHelper.WrapAngle(desired-Angle.Z);
                _yawNoise+=MuGame.Random.Next(-16,16)*.8f*f;
                float turn=Math.Clamp(delta,-MathHelper.ToRadians(5)*f,MathHelper.ToRadians(5)*f);
                Angle=new(0,0,Angle.Z+turn+MathHelper.ToRadians(_yawNoise*f));
                _yawNoise*=MathF.Pow(.8f,f);
                Position+=new Vector3(MathF.Sin(Angle.Z),-MathF.Cos(Angle.Z),0)*10f*f;
                Position=new(Position.X,Position.Y,MathHelper.Lerp(Position.Z,goal.Z,1-MathF.Pow(.9f,f)));
                Alpha=_life<30?_life/40f:_life>144?(160-_life)/20f:1f;
            }
            if(_sync!=null)
            {
                Position=_sync.Position; Angle=_sync.Angle; Alpha=_sync.Alpha;
                SetStaticAnimationPose(_sync.CurrentAction,_sync.CurrentFrame);
            }
            Alpha=Math.Clamp(Alpha,0,1);
            if(_kind is 3 or 5 or 6 or 7) BlendMeshLight=Alpha;
            _life-=f;
            if(_life<=0) { World?.RemoveObject(this); Dispose(); }
        }
        private void FaceTarget() => Angle=new(0,0,MathF.Atan2(_target.X-Position.X,-(_target.Y-Position.Y)));
    }

    private sealed class BookSprites : EffectObject
    {
        private struct Particle { internal Vector3 Position,Velocity,Light; internal float Life,MaxLife,Scale,Rotation; internal byte Kind; }
        private readonly Particle[] _particles=new Particle[768];
        private readonly Vector3 _origin,_target;
        private readonly ushort _skill;
        private readonly Piece? _creature;
        private readonly int _tier;
        internal readonly Piece?[] Spirits=new Piece?[4];
        private readonly Vector3[,] _tails=new Vector3[4,200];
        private readonly Vector3[,,] _energyTails=new Vector3[4,6,16];
        private static readonly int[] EnergyBones=[24,28,32,44,48,52];
        private int _tailLimit=160, _energyLimit=12;
        private readonly int[] _tailCounts=new int[4];
        private readonly VertexPositionColorTexture[] _quad=new VertexPositionColorTexture[6];
        private readonly VertexPositionColorTexture[] _ribbon=new VertexPositionColorTexture[200*12];
        private Texture2D? _impact,_fire,_cloud,_smoke,_water,_spirit,_explosion,_flare,_shiny,_energy;
        private int _next;
        private float _age;
        private static readonly int[] FireBones=[13,23,39,49,3,4,5,61];
        internal BookSprites(Vector3 origin,Vector3 target,ushort skill,Piece? creature,int tier)
        {
            _origin=origin;_target=target;_skill=skill;_creature=creature;_tier=tier;Position=origin;
            IsTransparent=true;AffectedByTransparency=false;LightEnabled=false;DepthState=DepthStencilState.DepthRead;
            BoundingBoxLocal=new(new(-3000),new(3000));
        }
        public override async Task LoadContent()
        {
            await base.LoadContent();
            _energy=await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointLaser01.jpg");
            _explosion=await TextureLoader.Instance.PrepareAndGetTexture("Effect/loungexflow.jpg");
            _flare=await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare01.jpg");
            _shiny=await TextureLoader.Instance.PrepareAndGetTexture("Effect/shiny05.jpg");
            _impact=await TextureLoader.Instance.PrepareAndGetTexture("Effect/empact01.jpg");
            _fire=await TextureLoader.Instance.PrepareAndGetTexture("Effect/firehik02.jpg");
            _cloud=await TextureLoader.Instance.PrepareAndGetTexture("Effect/clouds.jpg");
            _smoke=await TextureLoader.Instance.PrepareAndGetTexture("Effect/smoke01.jpg");
            _water=await TextureLoader.Instance.PrepareAndGetTexture("Effect/water.jpg");
            _spirit=await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointSpirit01.jpg");
        }
        private void Emit(Vector3 p,Vector3 velocity,Vector3 light,float life,float scale,byte kind)
        {
            _particles[_next]=new(){Position=p,Velocity=velocity,Light=light,Life=life,MaxLife=life,Scale=scale,Kind=kind,Rotation=MuGame.Random.Next(360)*MathHelper.Pi/180};
            _next=(_next+1)%_particles.Length;
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime); if(Status!=GameControlStatus.Ready)return;
            float f=FPSCounter.Instance.FPS_ANIMATION_FACTOR;_age+=f;
            _tailLimit=Math.Clamp((int)(40f/MathF.Max(.01f,f)),3,200);
            _energyLimit=Math.Clamp((int)(3f/MathF.Max(.01f,f)),3,16);
            if(_skill==223 && _creature?.Status==GameControlStatus.Ready && FPSCounter.Instance.RandFPSCheck(1))
                foreach(int bone in FireBones)
                    for(int i=0;i<3;i++) Emit(_creature.Bone(bone),new(0,0,MuGame.Random.Next(15,30)*.25f),Vector3.One*_creature.Alpha*.3f,MuGame.Random.Next(8,20),(5-i)*MuGame.Random.Next(20,50)*.01f,0);
            if(_skill==223 && _creature?.Status==GameControlStatus.Ready && _creature.Exploding && FPSCounter.Instance.RandFPSCheck(_tier==0?3:_tier==1?2:1))
            {
                int range=_tier==0?15:_tier==1?40:75;
                Vector3 p=_creature.Position+new Vector3(MuGame.Random.Next(-range,range),MuGame.Random.Next(-range,range),MuGame.Random.Next(30,110));
                Emit(p,Vector3.Zero,Vector3.One,16,.15f*MuGame.Random.Next(1,10),4);
            }
            if(_skill==225 && _age<160 && FPSCounter.Instance.RandFPSCheck(5-_tier))
            {
                Vector3 p=_target+new Vector3(MuGame.Random.Next(-250,250),MuGame.Random.Next(-250,250),0);
                Emit(p,Vector3.Zero,new(.5f,.1f,.8f),32,3.5f*(1+MuGame.Random.Next(48,80)*.01f),2);
                p=_target+new Vector3(MuGame.Random.Next(-200,200),MuGame.Random.Next(-200,200),0);
                Emit(p+Vector3.UnitZ*3,Vector3.Zero,new(.6f,.1f,1),60,2,1);
                Emit(p,new(0,0,2),new(.6f,.1f,1),MuGame.Random.Next(20,30),MuGame.Random.Next(50,82)*.01f,3);
            }
            for(int i=0;i<4;i++)
            {
                if(Spirits[i]?.Status!=GameControlStatus.Ready)continue;
                int count=Math.Min(_tailLimit-1,_tailCounts[i]);
                for(int k=count;k>0;k--)_tails[i,k]=_tails[i,k-1];
                _tails[i,0]=Spirits[i]!.Position;_tailCounts[i]=count+1;
                for(int bone=0;bone<6;bone++)
                {
                    for(int k=_energyLimit-1;k>0;k--)_energyTails[i,bone,k]=_energyTails[i,bone,k-1];
                    _energyTails[i,bone,0]=Spirits[i]!.Bone(EnergyBones[bone]);
                }
            }
            for(int i=0;i<_particles.Length;i++)
            {
                if(_particles[i].Life<=0)continue;
                _particles[i].Life-=f;_particles[i].Position+=_particles[i].Velocity*f;
                if(_particles[i].Kind==1)
                {
                    _particles[i].Scale+=.03f*f;
                    _particles[i].Light*=MathF.Pow(1/1.05f,f);
                }
                if(_particles[i].Kind==2)
                {
                    _particles[i].Velocity+=Vector3.UnitZ*.1f*f;
                    _particles[i].Scale+=.05f*f;
                    _particles[i].Light=new Vector3(.5f,.1f,.8f)*MathF.Max(0,_particles[i].Life/32f);
                }
                if(_particles[i].Kind==3)
                {
                    _particles[i].Scale=MathF.Max(0,_particles[i].Scale-.013f*f);
                    _particles[i].Light*=MathF.Pow(1/1.02f,f);
                }
                if(_particles[i].Kind==0) _particles[i].Scale=MathF.Max(0,_particles[i].Scale-MuGame.Random.Next(10,30)*.004f*f);
            }
            if(_age>=225) {World?.RemoveObject(this);Dispose();}
        }
        private void Quad(Texture2D texture,Vector3 center,Vector3 right,Vector3 up,Vector3 light,BlendState? blend=null, int atlasFrame=-1)
        {
            int n=0; SourceEffectGeometry.Quad(_quad,ref n,center-up,center+up,right,new Color(light),0,1);
            if(atlasFrame>=0)
                for(int i=0;i<n;i++) _quad[i].TextureCoordinate = new Vector2((atlasFrame%4)*.25f+.005f,(atlasFrame/4)*.25f+.005f)+_quad[i].TextureCoordinate*.24f;
            ElfSupportBuffEffect.DrawWorldGeometry(texture,_quad,n,blendOverride:blend,samplerOverride:SamplerState.LinearClamp);
        }
        public override void Draw(GameTime gameTime)
        {
            if(!Visible||Status!=GameControlStatus.Ready)return;
            Matrix cam=Matrix.Invert(Camera.Instance.View);
            if(_flare!=null && _age<44)
            {
                float fade=MathF.Min(1,_age*.05f)*MathF.Min(1,(44-_age)/20);
                int layers=Math.Min(5,(int)MathF.Ceiling((44-_age)/3));
                for(int i=0;i<layers;i++) Quad(_flare,_origin+Vector3.UnitZ*(3+i*.1f),Vector3.UnitX*600,Vector3.UnitY*600,Vector3.One*fade,Blendings.Negative);
            }
            if(_impact!=null && _age<40)
            {
                Vector3 tint=_skill==223?new(1,.6f,.4f):_skill==224?new(.7f,.7f,1):new(.6f,.6f,.9f);
                float alpha=MathF.Min(1,_age*.05f)*MathF.Min(1,(40-_age)/20);
                for(int i=0;i<2;i++)
                {
                    float a=MathHelper.ToRadians(_age*(i==0?-8:4));float size=120*(i==0?1:1.2f);
                    Quad(_impact,_origin+Vector3.UnitZ*5,new(MathF.Cos(a)*size,MathF.Sin(a)*size,0),new(-MathF.Sin(a)*size,MathF.Cos(a)*size,0),tint*alpha);
                }
            }
            for(int i=0;i<_particles.Length;i++)
            {
                ref Particle p=ref _particles[i];if(p.Life<=0)continue;
                Texture2D? t=p.Kind==0?_fire:p.Kind==1?_cloud:p.Kind==2?_smoke:p.Kind==3?_water:_explosion;if(t==null)continue;
                float fade=MathF.Min(1,p.Life/10);
                if(p.Kind==1)
                {
                    float radius=p.Scale*50f,c=MathF.Cos(p.Rotation),r=MathF.Sin(p.Rotation);
                    Quad(t,p.Position,new(c*radius,r*radius,0),new(-r*radius,c*radius,0),p.Light*fade);
                    continue;
                }
                Quad(t,p.Position,cam.Right*t.Width*p.Scale*.5f,cam.Up*t.Height*p.Scale*.5f,p.Light*fade, atlasFrame:p.Kind==4?Math.Clamp((int)(p.MaxLife-p.Life),0,15):-1);
            }
            if(_energy!=null)
                for(int i=0;i<4;i++)
                {
                    var spirit=Spirits[i];if(spirit?.Status!=GameControlStatus.Ready)continue;
                    int n=0,count=Math.Min(_energyLimit,_tailCounts[i]);
                    for(int bone=0;bone<6;bone++)
                        for(int k=1;k<count;k++)
                        {
                            Vector3 a=_energyTails[i,bone,k-1],b=_energyTails[i,bone,k];
                            Color color=new(new Vector3(.5f,.5f,.9f)*spirit.Alpha);
                            SourceEffectGeometry.Quad(_ribbon,ref n,a,b,cam.Right*5,color,(k-1)/(float)count,k/(float)count);
                            SourceEffectGeometry.Quad(_ribbon,ref n,a,b,Vector3.UnitZ*5,color,(k-1)/(float)count,k/(float)count);
                        }
                    if(n>0)ElfSupportBuffEffect.DrawWorldGeometry(_energy,_ribbon,n);
                }
            if(_shiny!=null)
                for(int i=0;i<4;i++)
                {
                    var spirit=Spirits[i];if(spirit?.Status!=GameControlStatus.Ready)continue;
                    for(int bone=54;bone<=59;bone++)
                        Quad(_shiny,spirit.Bone(bone),cam.Right*_shiny.Width*spirit.Scale*.15f,cam.Up*_shiny.Height*spirit.Scale*.15f,new Vector3(.2f,.3f,1)*spirit.Alpha);
                }
            if(_spirit!=null)
                for(int i=0;i<4;i++)
                {
                    var spirit=Spirits[i];if(spirit?.Status!=GameControlStatus.Ready)continue;
                    int n=0,count=_tailCounts[i];
                    for(int k=1;k<count;k++)
                    {
                        Vector3 a=_tails[i,k-1],b=_tails[i,k];
                        Vector3 d=b-a;if(d.LengthSquared()<.01f)continue;
                        Vector3 side=Vector3.Cross(Vector3.Normalize(d),cam.Forward);
                        if(side.LengthSquared()<.01f)side=cam.Right;else side.Normalize();
                        Color color=new(new Vector3(.7f,.7f,.9f)*spirit.Alpha*(1f-k/(float)count));
                        SourceEffectGeometry.Quad(_ribbon,ref n,a,b,side*50,color,(k-1)/(float)count,k/(float)count);
                        SourceEffectGeometry.Quad(_ribbon,ref n,a,b,side*10,color,(k-1)/(float)count,k/(float)count);
                    }
                    if(n>0)ElfSupportBuffEffect.DrawWorldGeometry(_spirit,_ribbon,n,blendOverride:Blendings.OneOneAdditive);
                }
        }
    }
}
