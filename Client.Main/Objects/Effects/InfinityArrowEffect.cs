#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain MODEL_INFINITY_ARROW subtype 0 expands into five visible models.</summary>
public sealed class InfinityArrowEffect : StagedCombatEffect
{
    public InfinityArrowEffect(WalkerObject caster) : base(caster, 76f) { }

    protected override void BuildParts()
    {
        var anchor = new ArrowPart(Caster, 0);
        AddPart(anchor, 0f);
        AddPart(new InfinityArrowFlareEffect(anchor), 0f);
        for (int i = 1; i < 5; i++) AddPart(new ArrowPart(Caster, i), 0f);
    }

    private sealed class ArrowPart : ModelObject
    {
        private readonly WalkerObject _owner;
        private readonly int _part;
        private float _life;
        private Vector3 _direction;
        private Vector3 _drift;
        public ArrowPart(WalkerObject owner, int part)
        {
            _owner = owner; _part = part; _life = part == 4 ? 15f : 40f;
            Position = owner.WorldPosition.Translation; Angle = owner.Angle;
            Scale = 1f; Light = part == 4 ? new Vector3(1f, .5f, .3f) : Vector3.One;
            _direction = Light;
            LightEnabled = true; RenderShadow = false; IsTransparent = true;
            AffectedByTransparency = false; DepthState = DepthStencilState.DepthRead;
            // MuMain RenderObject draws parts 1..4 with RENDER_BRIGHT.
            // The base model only supplies bones for flare joints; its body
            // is never rendered by the native special-case branch.
            HiddenMesh = part == 0 ? -2 : -1;
            BlendMesh = -2; BlendMeshState = Blendings.OneOneAdditive;
            BlendState = Blendings.OneOneAdditive;
            ContinuousAnimation = true;
            AnimationSpeed = 7.5f;
        }
        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Skill/arrowsre0{_part + 1}.bmd", _part == 3 ? "Effect" : "Skill");
            await base.Load();
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (_owner.Status == GameControlStatus.Disposed || _life <= 0f) { World?.RemoveObject(this); Dispose(); return; }
            float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            Angle = _owner.Angle;
            Vector3 origin = _owner.WorldPosition.Translation;
            if (_part == 0 && _owner is PlayerObject player && player.TryGetBoneWorldMatrix(29, out var bone)) origin = bone.Translation;
            if (_part < 4) Light *= MathF.Pow(.95f, f);
            var motion = SourceJointMath.MuMainRotate(_direction, new Vector3(MathHelper.ToDegrees(Angle.X), MathHelper.ToDegrees(Angle.Y), MathHelper.ToDegrees(Angle.Z))) * f;
            if (_part == 4) _drift += motion;
            Position = origin + (_part == 4 ? _drift : motion);
            _life -= f;
        }
    }
}
