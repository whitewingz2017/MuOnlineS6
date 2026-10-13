#nullable enable
using System;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>BITMAP_MAGIC+1 subtypes 1/2/3 emit three healing joints per source frame.</summary>
    public sealed class ElfSkillGlowEffect : EffectObject
    {
        private readonly WalkerObject _owner;
        private readonly int _subtype;
        private float _frames;
        private int _emittedFrames;

        public ElfSkillGlowEffect(WalkerObject owner, int subtype)
        {
            _owner = owner;
            _subtype = subtype;
            Position = owner.WorldPosition.Translation;
            IsTransparent = false;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-250), new Vector3(250));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready || World == null)
                return;
            Position = _owner.WorldPosition.Translation;
            _frames += (float)gameTime.ElapsedGameTime.TotalSeconds * 25f;
            int end = Math.Min(20, (int)_frames);
            while (_emittedFrames < end)
            {
                _emittedFrames++;
                for (int j = 0; j < 3; j++)
                {
                    Vector3 angle = new Vector3(MuGame.Random.Next(90), 0, MuGame.Random.Next(360));
                    Vector3 origin = Position - SourceJointMath.MuMainRotate(new Vector3(0, -200, 0), angle) + Vector3.UnitZ * 120f;
                    var joint = SourceJointEffect.Healing(_owner, _subtype, origin, angle);
                    World.Objects.Add(joint);
                    _ = joint.Load();
                }
            }
            if (_frames >= 20f || _owner.Status == GameControlStatus.Disposed)
            {
                World.RemoveObject(this);
                Dispose();
            }
        }
    }
}
