using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

// Virtual joystick used by the Classic mobile HUD.
namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Virtual movement joystick in the lower-left corner, matching the MU mobile style.
    /// While dragged, it moves the character continuously in the stick direction.
    /// Behavior reference: Main_JoyStickUI.lua from MU Dragon Havoc
    /// (drag -> direction -> targetCell = cell + direction -> MoveTo; release -> stop).
    /// </summary>
    public sealed class VirtualJoystickControl : UIControl
    {
        // Base and knob radii, based on the MU Immortal proportions (174 and 78).
        private const float BaseRadius = 87f;
        private const float KnobRadius = 33.345f; // Reduced from the original 39px knob.
        // Dead zone: below this distance, movement is not emitted.
        private const float DeadZone = 0.28f;

        // Joystick artwork extracted from MU Immortal (atlas_main: stick_bg/stick_main).
        private const string StickBgPath = "Interface/DH/mi_stick_bg.OZP";
        private const string StickKnobPath = "Interface/DH/mi_stick_knob.OZP";
        private Texture2D _texBase;
        private Texture2D _texKnob;
        private UiThemeId _loadedTheme = (UiThemeId)(-1);

        private bool _active;
        internal static int? CapturedTouchId { get; private set; }
        private Vector2 _pointer;
        private HeroObject _movementHero;
        private Vector2 _center;        // Fixed base center in virtual UI coordinates.
        private Vector2 _knob;          // Current knob position in virtual UI coordinates.

        public VirtualJoystickControl()
        {
            Interactive = true;
            AutoViewSize = false;
            // Anchor the base to the lower-left corner of the virtual space.
            int marginX = 40;
            int marginBottom = 56;   // Raise the stick slightly above the bottom bar.
            int size = (int)(BaseRadius * 2f);
            ControlSize = new Point(size, size);
            ViewSize = new Point(size, size);
            X = marginX;
            Y = UiScaler.VirtualSize.Y - size - marginBottom;
        }

        public override async Task Load()
        {
            await base.Load();
            if (_loadedTheme == UiThemeManager.CurrentId)
                return;
            try { _texBase = await UiThemeManager.LoadThemeTextureAsync(StickBgPath); } catch { _texBase = null; }
            try { _texKnob = await UiThemeManager.LoadThemeTextureAsync(StickKnobPath); } catch { _texKnob = null; }
            _loadedTheme = UiThemeManager.CurrentId;
        }

        private Vector2 Center =>
            new(DisplayRectangle.X + DisplayRectangle.Width / 2f,
                DisplayRectangle.Y + DisplayRectangle.Height / 2f);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Visible || Scene == null || MuGame.Network is { IsConnected: false })
            {
                CapturedTouchId = null;
                _active = false;
                _knob = Vector2.Zero;
                DetachMovementHero();
                return;
            }

            var hero = (Scene as GameScene)?.Hero;
            var mouse = MuGame.Instance.UiMouseState;
            var prev = MuGame.Instance.PrevUiMouseState;
            bool down = mouse.LeftButton == ButtonState.Pressed;
            bool prevDown = prev.LeftButton == ButtonState.Pressed;
            var p = new Vector2(mouse.Position.X, mouse.Position.Y);

            _center = Center;

            var touches = MuGame.Instance.Touch;
            if (touches.Count > 0 || CapturedTouchId.HasValue)
            {
                down = false;
                prevDown = _active;
                foreach (var touch in touches)
                {
                    var point = UiScaler.ToVirtual(new Point((int)touch.Position.X, (int)touch.Position.Y)).ToVector2();
                    if (!CapturedTouchId.HasValue && touch.State == TouchLocationState.Pressed &&
                        Vector2.Distance(point, _center) <= BaseRadius * 1.35f)
                        CapturedTouchId = touch.Id;
                    if (touch.Id != CapturedTouchId) continue;
                    p = point;
                    down = touch.State is TouchLocationState.Pressed or TouchLocationState.Moved;
                }
            }
            _pointer = p;

            // Começa a arrastar se o toque iniciou dentro da base.
            if (!_active && down && !prevDown)
            {
                float startR = BaseRadius * 1.35f; // área de captura um pouco maior
                if (Vector2.Distance(p, _center) <= startR)
                {
                    _active = true;
                }
            }

            if (_active)
            {
                // Enquanto ativo, consome o ponteiro para o clique-no-mundo não disparar.
                if (!CapturedTouchId.HasValue) Scene.SetMouseInputConsumed();

                if (!down)
                {
                    // Soltou: recentra o manípulo. NÃO chama StopMovement (deixa o char
                    // terminar o passo atual suavemente — evita o "pulo" ao soltar).
                    _active = false;
                    CapturedTouchId = null;
                    _knob = Vector2.Zero;
                    DetachMovementHero();
                    return;
                }

                // Vetor do analógico (tela, y para baixo), clampeado ao raio da base.
                Vector2 v = p - _center;
                float dist = v.Length();
                float clamped = MathHelper.Clamp(dist, 0f, BaseRadius);
                Vector2 dir = dist > 0.0001f ? v / dist : Vector2.Zero;
                _knob = dir * clamped;

                float norm = clamped / BaseRadius; // 0..1
                if (norm < DeadZone || hero == null)
                {
                    DetachMovementHero();
                    // Zona morta: não move (mantém o manípulo desenhado).
                    return;
                }

                if (!ReferenceEquals(_movementHero, hero))
                {
                    DetachMovementHero();
                    _movementHero = hero;
                    hero.MovementStepCompleted += ContinueMovement;
                }
                DriveMovement(hero, dir);
            }
        }

        private void ContinueMovement(WalkerObject walker)
        {
            if (!_active || !Visible || !ReferenceEquals(walker, _movementHero) ||
                Scene is not GameScene scene || !ReferenceEquals(scene.Hero, walker))
                return;
            Vector2 offset = _pointer - Center;
            if (offset.LengthSquared() < BaseRadius * BaseRadius * DeadZone * DeadZone)
                return;
            DriveMovement(_movementHero, Vector2.Normalize(offset));
        }

        private void DetachMovementHero()
        {
            if (_movementHero != null)
                _movementHero.MovementStepCompleted -= ContinueMovement;
            _movementHero = null;
        }

        public override void Dispose()
        {
            CapturedTouchId = null;
            DetachMovementHero();
            base.Dispose();
        }

        private static Vector2 GetStepDirection(Vector2 screenDir)
        {
            Vector2 raw = new(screenDir.X + screenDir.Y, screenDir.X - screenDir.Y);
            return new Vector2(Math.Sign(MathF.Round(raw.X * 1.2f)),
                Math.Sign(MathF.Round(raw.Y * 1.2f)));
        }

        private void DriveMovement(HeroObject hero, Vector2 screenDir)
        {
            (Scene as GameScene)?.CancelAutoAttack();
            // Only one neighboring tile is requested. While held, the completion
            // callback supplies the next tile before the animation can become idle.
            if (hero.IsMoving || hero.MovementIntent)
                return;

            Vector2 step = GetStepDirection(screenDir);
            if (step == Vector2.Zero || Scene?.World is not WalkableWorldControl world)
                return;

            Vector2 origin = new((int)hero.Location.X, (int)hero.Location.Y);
            Vector2 target = GetMovementTarget(origin, step, world.IsWalkable);
            if (target != origin)
                hero.MoveTo(target, sendToServer: true, usePathfinding: false);
        }

        private static Vector2 GetMovementTarget(Vector2 origin, Vector2 step, Func<Vector2, bool> isWalkable)
        {
            Vector2 next = origin + step;
            if (next.X < 0 || next.Y < 0 || next.X >= Constants.TERRAIN_SIZE ||
                next.Y >= Constants.TERRAIN_SIZE || !isWalkable(next))
                return origin;
            return next;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;

            var pixel = GraphicsManager.Instance?.Pixel;
            var sb = GraphicsManager.Instance?.Sprite;
            if (pixel == null || sb == null) return;

            _center = Center;
            Vector2 knobPos = _center + (_active ? _knob : Vector2.Zero);

            float alpha = _active ? 1.0f : 0.7f;

            // Arte real do Dragon Havoc, se carregada; senão desenho procedural.
            if (_texBase != null && !_texBase.IsDisposed && _texKnob != null && !_texKnob.IsDisposed)
            {
                var baseRect = CenteredRect(_center, BaseRadius);
                var knobRect = CenteredRect(knobPos, KnobRadius);
                sb.Draw(_texBase, baseRect, Color.White * alpha);
                sb.Draw(_texKnob, knobRect, Color.White * alpha);
            }
            else
            {
                // Fallback procedural (sprite ausente).
                DrawRing(sb, pixel, _center, BaseRadius, 3f, new Color(230, 230, 235) * (alpha * 0.9f));
                DrawDisc(sb, pixel, _center, BaseRadius - 3f, new Color(20, 24, 30) * (alpha * 0.5f));
                DrawTick(sb, pixel, _center, BaseRadius);
                DrawDisc(sb, pixel, knobPos, KnobRadius, new Color(60, 70, 85) * alpha);
                DrawRing(sb, pixel, knobPos, KnobRadius, 3f, new Color(235, 200, 120) * alpha);
                DrawDisc(sb, pixel, knobPos, KnobRadius * 0.5f, new Color(235, 200, 120) * (alpha * 0.6f));
            }

            base.Draw(gameTime);
        }

        private static Rectangle CenteredRect(Vector2 center, float radius)
            => new((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2f), (int)(radius * 2f));

        // ── Helpers de desenho (círculos com o pixel branco) ─────────────
        private static void DrawDisc(SpriteBatch sb, Texture2D pixel, Vector2 c, float r, Color color)
        {
            int ir = (int)r;
            for (int y = -ir; y <= ir; y++)
            {
                int half = (int)MathF.Sqrt(r * r - y * y);
                sb.Draw(pixel, new Rectangle((int)(c.X - half), (int)(c.Y + y), half * 2, 1), color);
            }
        }

        private static void DrawRing(SpriteBatch sb, Texture2D pixel, Vector2 c, float r, float thickness, Color color)
        {
            float rIn = r - thickness;
            int ir = (int)r;
            for (int y = -ir; y <= ir; y++)
            {
                float yy = y;
                float outer = r * r - yy * yy;
                if (outer < 0) continue;
                int xo = (int)MathF.Sqrt(outer);
                float innerSq = rIn * rIn - yy * yy;
                if (innerSq <= 0)
                {
                    sb.Draw(pixel, new Rectangle((int)(c.X - xo), (int)(c.Y + y), xo * 2, 1), color);
                }
                else
                {
                    int xi = (int)MathF.Sqrt(innerSq);
                    sb.Draw(pixel, new Rectangle((int)(c.X - xo), (int)(c.Y + y), xo - xi, 1), color);
                    sb.Draw(pixel, new Rectangle((int)(c.X + xi), (int)(c.Y + y), xo - xi, 1), color);
                }
            }
        }

        private static void DrawTick(SpriteBatch sb, Texture2D pixel, Vector2 c, float r)
        {
            var col = new Color(200, 200, 210) * 0.25f;
            int len = 10;
            // topo, base, esq, dir
            sb.Draw(pixel, new Rectangle((int)c.X - 1, (int)(c.Y - r + 4), 2, len), col);
            sb.Draw(pixel, new Rectangle((int)c.X - 1, (int)(c.Y + r - 4 - len), 2, len), col);
            sb.Draw(pixel, new Rectangle((int)(c.X - r + 4), (int)c.Y - 1, len, 2), col);
            sb.Draw(pixel, new Rectangle((int)(c.X + r - 4 - len), (int)c.Y - 1, len, 2), col);
        }
    }
}
