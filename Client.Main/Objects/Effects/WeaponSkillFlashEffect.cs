using System;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain BITMAP_SHINY+2: 18 source frames, attached to the right weapon.</summary>
public sealed class WeaponSkillFlashEffect : SpriteObject
{
    private readonly WalkerObject _caster;
    private float _remainingFrames = 18f;
    public override string TexturePath => "Effect/Shiny03.jpg";

    public WeaponSkillFlashEffect(WalkerObject caster)
    {
        _caster = caster;
        Position = caster.WorldPosition.Translation;
        BlendState = BlendState.Additive;
        DepthState = DepthStencilState.DepthRead;
        IsTransparent = true;
        AffectedByTransparency = true;
        LightEnabled = false;
        Light = Vector3.One;
        Scale = 0f;
        BoundingBoxLocal = new BoundingBox(new Vector3(-120), new Vector3(120));
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready)
            return;
        _remainingFrames -= (float)gameTime.ElapsedGameTime.TotalSeconds * 25f;
        if (_remainingFrames <= 0 || _caster.World != World)
        {
            World?.RemoveObject(this);
            Dispose();
            return;
        }
        // MuMain HandPosition: right-hand link bone 10, local offset (0,-120,0).
        var bones = _caster.GetBoneTransforms();
        Position = bones != null && bones.Length > 10
            ? Vector3.Transform(Vector3.Transform(new Vector3(0, -120, 0), bones[10]), _caster.WorldPosition)
            : _caster.WorldPosition.Translation + new Vector3(0, 0, 100);
        Scale = MathF.Sin(MathHelper.ToRadians(_remainingFrames * 10f)) * 3f;
    }
}
