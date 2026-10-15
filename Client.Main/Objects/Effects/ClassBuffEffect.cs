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

namespace Client.Main.Objects.Effects
{
    /// <summary>Source buff emitters: FLARE_FORCE 5/6/7, ORORA 0..3 and FLARE 43/44.</summary>
    public sealed class ClassBuffEffect : EffectObject
    {
        public override WorldObjectRenderPolicy RenderPolicy => base.RenderPolicy.With(forceVisible: true, alwaysUpdate: true);
        public PlayerObject Owner { get; }
        private readonly BuffManager _buffs;
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4096];
        private readonly GoldLight[] _gold = new GoldLight[16];
        private int _nextGold;
        private int _frame;
        private float _accumulator;
        private float _markLight = 1f;
        private readonly float[] _ororaLight = { 1f, 1f, 1f, 1f };
        private readonly int[] _sparkAge = { 16, 16 };
        private Texture2D? _orora, _marks, _spark, _pin, _fire, _flare;
        private static readonly int[] MarkBones = { 20, 20, 19, 18, 17, 2, 35, 26, 36, 27, 37, 28, 39, 30 };
        private static readonly float[] MarkScale = { 1.5f, 1.5f, .6f, 1.1f, .9f, .8f, .6f, .6f, .8f, .8f, .8f, .8f, .7f, .7f };
        private struct GoldLight { public Vector3 Position; public int Life; public float Speed, Brightness; }

        public ClassBuffEffect(PlayerObject owner, BuffManager buffs)
        {
            Owner = owner; _buffs = buffs;
            Position = owner.WorldPosition.Translation;
            IsTransparent = false; AffectedByTransparency = false;
            BlendState = Blendings.OneOneAdditive; DepthState = DepthStencilState.DepthRead;
            LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-200), new Vector3(200, 200, 450));
            Children.Add(new ElfSupportBuffEffect(owner, buffs, soulBarrier: true));
            Children.Add(new RageFighterBuffWindEffect(owner,buffs));
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();
            _orora = await TextureLoader.Instance.PrepareAndGetTexture("Effect/hikorora.jpg");
            _marks = await TextureLoader.Instance.PrepareAndGetTexture("Effect/lightmarks.jpg");
            _spark = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Spark.jpg");
            _pin = await TextureLoader.Instance.PrepareAndGetTexture("Effect/pin_lights.jpg");
            _fire = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Fire04.jpg");
            _flare = await TextureLoader.Instance.PrepareAndGetTexture("Effect/flare.jpg");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Position = Owner.WorldPosition.Translation;
            if (Status != GameControlStatus.Ready) return;
            _accumulator = MathF.Min(_accumulator + (float)gameTime.ElapsedGameTime.TotalSeconds * 25f, 10f);
            while (_accumulator >= 1f)
            {
                _accumulator -= 1f; _frame++;
                if (_frame % 30 == 0 && World != null &&
                    (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.CriticalDamage) || _buffs.HasBuff(Owner.NetworkId, BuffEffectId.CriticalDamageMastery)) &&
                    MuGame.Random.Next(20) == 0)
                {
                    for (int hand = 0; hand < 2; hand++)
                    {
                        var weapon = hand == 0 ? Owner.Weapon1 : Owner.Weapon2;
                        if (weapon.Hidden || weapon.Model == null || weapon.ItemGroup == 6) continue;
                        var wave = new BuffCastModelEffect(Owner, true, hand);
                        World.Objects.Add(wave);
                        _ = wave.Load();
                    }
                }
                // LIGHT_MARKS: 65 frames, loop at five, dim then brighten.
                int markLife = 65 - _frame % 60;
                _markLight *= markLife >= 35 ? 1f / 1.035f : 1.035f;
                for (int i = 0; i < 4; i++)
                {
                    int age = _frame % (i < 2 ? 95 : 20);
                    if (age == 0) _ororaLight[i] = 1f;
                    if (.2f + age * (i < 2 ? .01f : .04f) >= .8f)
                        _ororaLight[i] /= i < 2 ? 1.05f : 1.33f;
                }
                for (int i = 0; i < 2; i++)
                {
                    _sparkAge[i]++;
                    if (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.Berserk) && MuGame.Random.Next(60) == 0)
                        _sparkAge[i] = 0;
                }
                for (int i = 0; i < _gold.Length; i++)
                {
                    ref var light = ref _gold[i];
                    if (light.Life <= 0) continue;
                    light.Life--; light.Speed += 1f; light.Position.Z += light.Speed;
                    light.Brightness /= 1.05f;
                    if (light.Position.Z >= 400f) light.Life = 0;
                }
                if (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.Reflection) && MuGame.Random.Next(3) == 0)
                {
                    _gold[_nextGold] = new GoldLight {
                        Position = Position + new Vector3(MuGame.Random.Next(-30, 30), MuGame.Random.Next(-30, 30), 30),
                        Life = 15, Speed = MuGame.Random.Next(2, 4), Brightness = 1f };
                    _nextGold = (_nextGold + 1) % _gold.Length;
                }
            }
        }

        public override void DrawAfter(GameTime gameTime)
        {
            base.DrawAfter(gameTime);
            if (Status != GameControlStatus.Ready || Owner.Hidden || Owner.IsDead) return;
            if (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.Berserk)) DrawBerserker();
            if (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.Reflection)) DrawReflection();
            if (_buffs.HasBuff(Owner.NetworkId, BuffEffectId.CriticalDamage) ||
                _buffs.HasBuff(Owner.NetworkId, BuffEffectId.CriticalDamageMastery)) DrawCritical();
        }

        private void DrawBerserker()
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                int age = _frame % (i < 2 ? 95 : 20);
                if (Owner.TryGetBoneWorldMatrix(i % 2 == 0 ? 37 : 28, out var bone))
                    Billboard(_orora, ref count, bone.Translation, .2f + age * (i < 2 ? .01f : .04f),
                        new Color(.9f, 0f, .1f) * _ororaLight[i], MathHelper.ToRadians(age * (i < 2 ? 5 : 20) * (i % 2 == 0 ? 1 : -1)));
            }
            Draw(_orora, count); count = 0;
            for (int i = 0; i < MarkBones.Length; i++)
                if (Owner.TryGetBoneWorldMatrix(MarkBones[i], out var bone))
                    Billboard(_marks, ref count, bone.Translation, MarkScale[i], new Color(.9f, 0f, .1f) * _markLight);
            Draw(_marks, count); count = 0;
            for (int i = 0; i < 2; i++)
                if (_sparkAge[i] < 16 && Owner.TryGetBoneWorldMatrix(i == 0 ? 37 : 28, out var bone))
                    Billboard(_spark, ref count, bone.Translation, .4f, Color.White, frame: _sparkAge[i] / 4, frames: 4);
            Draw(_spark, count);
        }

        private void DrawReflection()
        {
            int count = 0;
            for (int i = 0; i < _gold.Length; i++)
            {
                ref var light = ref _gold[i];
                if (light.Life <= 0) continue;
                Color tint = new Color(.9f, .6f, .1f) * light.Brightness;
                Billboard(_pin, ref count, light.Position, 1.5f, tint);
                Billboard(_pin, ref count, light.Position, .5f, tint);
            }
            Draw(_pin, count);
        }

        private void DrawCritical()
        {
            // MuMain emits every 1200ms, each weapon receives FLARE_FORCE sub5/6/7.
            int age = _frame % 30;
            if (age >= 15) return;
            int count = 0;
            float light = age < 9 ? 1f : MathF.Pow(1f / 1.5f, age - 8);
            Color tint = new Color(1f, .8f, 1f) * light;
            for (int hand = 0; hand < 2; hand++)
            {
                var weapon = hand == 0 ? Owner.Weapon1 : Owner.Weapon2;
                if (weapon.Hidden || weapon.Model == null || weapon.ItemGroup == 6 ||
                    (weapon.ItemGroup == 4 && (weapon.Type == 7 || weapon.Type == 15))) continue;
                if (!Owner.TryGetBoneWorldMatrix(hand == 0 ? 33 : 42, out var bone)) continue;
                for (int subtype = 5; subtype <= 7; subtype++)
                {
                    Vector3 start = Vector3.Transform(new Vector3(0, 20, 0), bone);
                    Vector3 direction = new Vector3(0, weapon.ItemGroup == 4 ? 0 : -4, 0);
                    float radius = 30f - .15f * age * (age + 1) * .5f;
                    int tails = Math.Min(age + 1, 30);
                    Vector3 previous = start;
                    float angle = subtype * 90f + (subtype % 2 == 1 ? 40f : -40f) * age * (age + 1) * .5f;
                    for (int tail = 0; tail < tails; tail++)
                    {
                        start += Vector3.TransformNormal(direction, bone);
                        angle += subtype % 2 == 1 ? 40f : -40f;
                        Vector3 point = start + Joints.SourceJointMath.MuMainRotate(new Vector3(0, 0, radius), new Vector3(angle, 0, 0));
                        Vector3 width = Vector3.TransformNormal(Vector3.UnitX * 10f, bone);
                        float uvStep = 1f / Math.Max(1, tails - 1);
                        Ribbon(ref count, previous, point, width, tint, (tails - tail) * uvStep, uvStep);
                        Ribbon(ref count, previous, point, Vector3.TransformNormal(Vector3.UnitZ * 10f, bone), tint, (tails - tail) * uvStep, uvStep);
                        previous = point; radius -= .15f;
                        if (weapon.ItemGroup != 4) direction.Y -= .1f;
                    }
                }
            }
            Draw(_fire, count);
            count = 0;
            for (int hand = 0; hand < 2; hand++)
            {
                var weapon = hand == 0 ? Owner.Weapon1 : Owner.Weapon2;
                if (!weapon.Hidden && weapon.Model != null && weapon.ItemGroup != 6 &&
                    Owner.TryGetBoneWorldMatrix(hand == 0 ? 33 : 42, out var bone))
                    Billboard(_flare, ref count, bone.Translation, .5f * light, tint);
            }
            Draw(_flare, count);
        }

        private void Draw(Texture2D? texture, int count)
        {
            if (texture != null) ElfSupportBuffEffect.DrawWorldGeometry(texture, _vertices, count);
        }

        private void Billboard(Texture2D? texture, ref int count, Vector3 center, float scale, Color tint,
            float rotation = 0f, int frame = 0, int frames = 1)
        {
            if (texture == null) return;
            Matrix camera = Matrix.Invert(Camera.Instance.View);
            Vector3 right = (camera.Right * MathF.Cos(rotation) + camera.Up * MathF.Sin(rotation)) * texture.Width * scale * .5f;
            Vector3 up = (camera.Up * MathF.Cos(rotation) - camera.Right * MathF.Sin(rotation)) * texture.Height * scale * .5f;
            Quad(ref count, center - right + up, center + right + up, center + right - up, center - right - up,
                tint, frames == 4 ? new Vector2(frame % 2 * .5f, frame / 2 * .5f) : Vector2.Zero,
                frames == 4 ? new Vector2((frame % 2 + 1) * .5f, (frame / 2 + 1) * .5f) : Vector2.One);
        }

        private void Ribbon(ref int count, Vector3 a, Vector3 b, Vector3 width, Color tint, float u, float uvStep = 1f / 16f)
        {
            if (count + 6 > _vertices.Length) return;
            var v0 = new VertexPositionColorTexture(a - width, tint, new Vector2(u, 0));
            var v1 = new VertexPositionColorTexture(a + width, tint, new Vector2(u, 1));
            var v2 = new VertexPositionColorTexture(b + width, tint, new Vector2(u - uvStep, 1));
            var v3 = new VertexPositionColorTexture(b - width, tint, new Vector2(u - uvStep, 0));
            _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
            _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
        }

        private void Quad(ref int count, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector2 uv0, Vector2 uv1)
        {
            if (count + 6 > _vertices.Length) return;
            var v0 = new VertexPositionColorTexture(a, color, uv0);
            var v1 = new VertexPositionColorTexture(b, color, new Vector2(uv1.X, uv0.Y));
            var v2 = new VertexPositionColorTexture(c, color, uv1);
            var v3 = new VertexPositionColorTexture(d, color, new Vector2(uv0.X, uv1.Y));
            _vertices[count++] = v0; _vertices[count++] = v1; _vertices[count++] = v2;
            _vertices[count++] = v0; _vertices[count++] = v2; _vertices[count++] = v3;
        }
    }
}
