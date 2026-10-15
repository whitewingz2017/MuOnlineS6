#nullable enable
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
namespace Client.Main.Objects.Effects.Skills;

[SkillVisualEffect(214)]
public sealed class DrainLifeSkillEffect : ISkillVisualEffect
{
    public WorldObject? CreateEffect(SkillEffectContext context)
    {
        if (context.World == null || context.Caster == null) return null;
        if (context.TargetId != 0 && context.World.TryGetWalkerById((ushort)(context.TargetId & 0x7FFF), out var target))
            return new DrainLifeEffect(context.Caster, target);
        // Remote area animations supply a tile, not ExtraTargetId. Resolve only
        // an unambiguous occupant of that exact tile; do not guess a nearby victim.
        if (!context.TargetPosition.HasValue) return null;
        Vector2 tile = new Vector2(context.TargetPosition.Value.X, context.TargetPosition.Value.Y) / Constants.TERRAIN_SCALE - new Vector2(.5f);
        WalkerObject? occupant = null;
        foreach (var obj in context.World.Objects)
        {
            if (obj is not WalkerObject walker || walker == context.Caster || Vector2.DistanceSquared(walker.Location, tile) > .01f) continue;
            if (occupant != null) return null;
            occupant = walker;
        }
        return occupant == null ? null : new DrainLifeEffect(context.Caster, occupant);
    }
}
