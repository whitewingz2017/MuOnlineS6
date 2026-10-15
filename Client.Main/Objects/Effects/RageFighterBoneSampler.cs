#nullable enable
using System;
using Client.Data.BMD;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects;

/// <summary>Samples historical BMD poses without changing the live actor's pose or shared asset.</summary>
internal static class RageFighterBoneSampler
{
    internal static Vector3 Sample(BMD? model, int action, float frame, int bone, Matrix world)
    {
        if (model?.Bones == null || model.Actions.Length == 0 || bone < 0 || bone >= model.Bones.Length)
            return world.Translation;
        action = Math.Clamp(action, 0, model.Actions.Length - 1);
        return (Pose(model, action, frame, bone, 0) * world).Translation;
    }

    private static Matrix Pose(BMD model, int action, float frame, int bone, int depth)
    {
        if (depth > model.Bones.Length) return Matrix.Identity;
        var definition = model.Bones[bone];
        if (definition == BMDTextureBone.Dummy || action >= definition.Matrixes.Length) return Matrix.Identity;
        var keys = definition.Matrixes[action];
        int count = Math.Min(keys.Position.Length, keys.Quaternion.Length);
        if (count == 0) return Matrix.Identity;
        frame = Math.Clamp(frame, 0, count - 1);
        int first = (int)frame, second = Math.Min(first + 1, count - 1);
        float fraction = frame - first;
        var local = Matrix.CreateFromQuaternion(Quaternion.Lerp(keys.Quaternion[first], keys.Quaternion[second], fraction));
        local.Translation = Vector3.Lerp(keys.Position[first], keys.Position[second], fraction);
        if (bone == 0 && model.Actions[action].LockPositions)
            local.Translation = new(keys.Position[0].X, keys.Position[0].Y, local.Translation.Z);
        return definition.Parent >= 0 && definition.Parent < model.Bones.Length
            ? local * Pose(model, action, frame, definition.Parent, depth + 1) : local;
    }
}
