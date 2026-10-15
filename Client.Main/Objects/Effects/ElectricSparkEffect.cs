#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects.Effects.Joints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

/// <summary>MuMain FLARE_FORCE subtype 0: one energy joint and four thunder joints.</summary>
public sealed class ElectricSparkEffect : EffectObject
{
    private const int MaxHistory = 200;
    private struct Joint
    {
        public Vector3 Position, Start;
        public float Life, Velocity, Radius, Orbit, Steps, Delay, Brightness;
        public int Count;
    }
    private readonly Joint[] _joints = new Joint[5];
    private readonly Vector3[,] _tails = new Vector3[5, MaxHistory];
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[(MaxHistory - 1) * 12];
    private readonly Vector3 _angle;
    private Texture2D? _thunder;
    private int _tailLimit;
    public ElectricSparkEffect(WalkerObject caster, float? yaw)
    {
        float bearing = yaw ?? caster.Angle.Z;
        _angle = new Vector3(MathHelper.ToDegrees(caster.Angle.X), MathHelper.ToDegrees(caster.Angle.Y), MathHelper.ToDegrees(bearing));
        Position = caster.WorldPosition.Translation + SourceJointMath.MuMainRotate(new Vector3(0f, -90f, -50f), _angle);
        if (caster.CurrentAction == (int)PlayerAction.PlayerAttackRideAttackFlash) Position += Vector3.UnitZ * 80f;
        else if (caster.CurrentAction == (int)PlayerAction.PlayerFenrirAttackDarklordFlash) Position += Vector3.UnitZ * 40f;
        IsTransparent = true; AffectedByTransparency = false; LightEnabled = false;
        DepthState = DepthStencilState.DepthRead;
        BoundingBoxLocal = new BoundingBox(new Vector3(-2500f), new Vector3(2500f));
    }
    public override async Task LoadContent()
    {
        await base.LoadContent();
        // CreateJoint overrides TexType to JOINT_THUNDER for subtype 0 as well.
        _thunder = await TextureLoader.Instance.PrepareAndGetTexture("Effect/JointThunder01.jpg");
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        _tailLimit = Math.Clamp((int)(30f / MathF.Max(.01f, f)), 2, MaxHistory);
        for (int i = 0; i < 5; i++)
        {
            ref var joint = ref _joints[i];
            // This is a spawn height, not velocity. Preserve MuMain's 25-Hz
            // height at fast render rates; multiplying it by the current delta
            // puts an unmounted caster's ribbon underneath the terrain.
            joint.Start = Position + Vector3.UnitZ * 150f;
            joint.Position = joint.Start; joint.Steps = 1f; joint.Velocity = -3f;
            joint.Brightness = 1f; joint.Radius = i == 0 ? 0f : 80f;
            joint.Orbit = i is 2 or 4 ? -180f : 180f;
            joint.Delay = i is 1 or 2 ? MuGame.Random.Next(2, 5) : 0f;
            joint.Life = 20f + joint.Delay;
            if (i != 0) joint.Position += SourceJointMath.MuMainRotate(new Vector3(0f, 0f, 80f), new Vector3(80f, joint.Orbit, 0f));
        }
    }
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Status != GameControlStatus.Ready) return;
        float f = FPSCounter.Instance.FPS_ANIMATION_FACTOR;
        bool alive = false;
        for (int index = 0; index < 5; index++)
        {
            ref var joint = ref _joints[index];
            if (joint.Life <= 0f) continue;
            alive = true;
            if (joint.Count < _tailLimit - 1)
            {
                if (joint.Delay > 0f) joint.Delay -= f;
                else
                {
                    for (int step = 1; step < joint.Steps && joint.Count < _tailLimit - 1; step++)
                    {
                        // Source creates each tail before advancing this joint.
                        _tails[index, joint.Count++] = joint.Position;
                        joint.Start += SourceJointMath.MuMainRotate(new Vector3(0f, joint.Velocity * f, 0f), _angle);
                        if (index == 0) joint.Position = joint.Start;
                        else
                        {
                            // Preserve MuMain's actual branch (the >=11 || <=13 test is true for 1..4).
                            joint.Orbit -= 20f * f;
                            Vector3 orbit = SourceJointMath.MuMainRotate(new Vector3(0f, 0f, joint.Radius), new Vector3(0f, joint.Orbit, _angle.Z));
                            joint.Position = joint.Start + (index is 3 or 4 ? -orbit : orbit);
                            joint.Radius -= 2.5f * f;
                        }
                        joint.Velocity -= 2f * f;
                    }
                    joint.Steps += 2f * f;
                }
            }
            if (joint.Life < 10f) joint.Brightness *= MathF.Pow(1f / 1.3f, f);
            joint.Life -= f;
        }
        if (!alive) { World?.RemoveObject(this); Dispose(); }
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        for (int index = 0; index < 5; index++)
        {
            ref var joint = ref _joints[index];
            Texture2D? texture = _thunder;
            if (texture == null || joint.Life <= 0f) continue;
            float halfWidth = index == 0 ? 125f : 50f;
            Vector3 side = SourceJointMath.MuMainRotate(Vector3.UnitX * halfWidth, _angle);
            Vector3 up = SourceJointMath.MuMainRotate(Vector3.UnitZ * halfWidth, _angle);
            int count = 0;
            Color tint = new(new Vector3(1f, .8f, 1f) * joint.Brightness);
            for (int j = joint.Count - 1; j > 0; j--)
            {
                Vector3 a = _tails[index, j], b = _tails[index, j - 1];
                if (Vector3.DistanceSquared(a, b) > 3600f) continue;
                float scroll = ((int)gameTime.TotalGameTime.TotalMilliseconds % 1000) * .001f;
                float u = (j + 1f) / ((_tailLimit - 1) / 2f) - scroll, v = j / ((_tailLimit - 1) / 2f) - scroll;
                SourceEffectGeometry.Quad(_vertices, ref count, a, b, side, tint, u, v);
                Color verticalTint = new(new Vector3(1f, .8f, 1f) * (joint.Brightness * 2f * j / _tailLimit));
                SourceEffectGeometry.Quad(_vertices, ref count, a, b, up, verticalTint, u, v);
            }
            ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count);
        }
    }
}
