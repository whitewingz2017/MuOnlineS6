using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Worlds
{
    /// <summary>GOBoid invasion flyover; never participates in targeting or movement.</summary>
    internal sealed class InvasionDragonObject : ModelObject
    {
        private readonly bool _golden;
        private Texture2D _chrome;
        private Texture2D _metal;
        private float _remaining = (128 + MuGame.Random.Next(128)) / 25f;
        private float _phase = MuGame.Random.Next(10) * 0.1f;
        public bool Expired => _remaining <= 0f || Status == GameControlStatus.Error;
        protected override bool UseSourceModelLighting => LightEnabled;
        protected override bool AllowGpuSkinning => false; // Environment passes share the CPU pose on every backend.

        public InvasionDragonObject(bool golden)
        {
            _golden = golden;
            Interactive = false;
            RenderShadow = false;
            UseSunLight = false;
            LightEnabled = true;
            AffectedByTransparency = false;
            CurrentAction = 7; // MONSTER01_DIE + 1: flying animation.
            AnimationSpeed = 12.5f; // MuMain PlaySpeed .5 at 25 Hz.
            // GOBoid moves at -90 degrees, then adds +90 for RenderBoids.
            // Movement stays on -Y; the model itself must be drawn at zero yaw.
            Angle = Vector3.Zero;
            BoundingBoxLocal = new BoundingBox(new Vector3(-250), new Vector3(250));
            Children.Add(new InvasionFireEffect(this));
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare("Monster/Monster32.bmd");
            if (Model?.Actions is { Length: > 7 } && Model.Actions[7] is { } flight)
                AnimationSpeed = 12.5f / (flight.PlaySpeed == 0f ? 1f : flight.PlaySpeed);
            if (_golden)
            {
                _metal = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Shiny01.jpg");
                _chrome = await TextureLoader.Instance.PrepareAndGetTexture("Effect/Chrome01.jpg");
            }
            await base.Load();
        }

        public override void Update(GameTime time)
        {
            if (Status != GameControlStatus.Ready) return;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            _remaining -= dt;
            _phase += Scale * 1.25f * dt;
            Vector3 position = Position;
            position.Y -= Scale * 1000f * dt;
            position.Z = (World?.Terrain.RequestTerrainHeight(position.X, position.Y) ?? 0f)
                + 400f - MathF.Abs(MathF.Sin(_phase)) * 100f;
            Position = position;
            base.Update(time);
        }

        public override void Draw(GameTime time)
        {
            base.Draw(time);
            if (!_golden || Model?.Meshes == null)
                return;

            var gold = new Vector3(1f, 0.5f, 0f);
            float seconds = (float)time.TotalGameTime.TotalSeconds;
            for (int mesh = 0; mesh < Model.Meshes.Length; mesh++)
            {
                DrawSourceEnvironmentMesh(mesh, GetMeshTexture(mesh), new Vector3(1f, 0.72f, 0.04f), 2, seconds);
                DrawSourceEnvironmentMesh(mesh, _metal, gold, 0, seconds);
                DrawSourceEnvironmentMesh(mesh, _chrome, gold, 1, seconds);
            }
        }
    }
}
