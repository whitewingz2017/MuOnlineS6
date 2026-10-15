#nullable enable
using System;
using Client.Main.Objects.Effects.Joints;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects;

/// <summary>The paired subtype-0 ribbons from MuMain's player Lightning cast.</summary>
public sealed class PlayerLightningEffect : StagedCombatEffect
{
    private readonly Func<Vector3> _target;

    public PlayerLightningEffect(WalkerObject caster, Func<Vector3> target) : base(caster, 3f)
    {
        _target = target;
    }

    protected override void BuildParts()
    {
        Vector3 source = Caster is PlayerObject player && player.TryGetHandWorldMatrix(false, out var hand)
            ? hand.Translation : Origin + Vector3.UnitZ * 80f;
        float yaw = MathHelper.ToDegrees(Caster.Angle.Z);
        AddPart(SourceJointEffect.PlayerLightning(source, _target, yaw, 50f), 0f);
        AddPart(SourceJointEffect.PlayerLightning(source, _target, yaw, 10f), 0f);
    }
}
