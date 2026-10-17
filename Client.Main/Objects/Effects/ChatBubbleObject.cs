using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Controls.UI;
using Client.Main.Objects.Player;
using Client.Main.Models;
using System.Text;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Simple chat bubble displayed above a player for a short time.
    /// </summary>
    public class ChatBubbleObject : EffectObject
    {
        private const float DefaultLifetime = 5f;
        private const float OffsetZ = 60f;
        private const int PixelGap = 8;
        private const int MaxBubbleWidth = 200;
        // Keep UI-backed bubbles in the off-grid update path; labels project from the target each frame.
        private static readonly Vector3 OverlayLifecyclePosition = new(-Constants.TERRAIN_SCALE, -Constants.TERRAIN_SCALE, 0f);

        private string _text;
        private readonly ushort _targetId;
        private float _lifetime;
        private float _originalLifetime;

        private LabelControl _textLabel;
        private SpriteFont _font;
        private float _elapsed;

        /// <summary>
        /// Creates a new chat bubble.
        /// </summary>
        /// <param name="text">Message text to display.</param>
        /// <param name="targetId">Network id of the player.</param>
        /// <param name="playerName">Name of the player.</param>
        /// <param name="lifetime">Optional lifetime in seconds.</param>
        public ChatBubbleObject(string text, ushort targetId, string playerName, float lifetime = DefaultLifetime)
        {
            _text = text ?? string.Empty;
            _targetId = targetId;
            _lifetime = lifetime;
            _originalLifetime = lifetime;

            IsTransparent = true;
            AffectedByTransparency = false;
            Position = OverlayLifecyclePosition;
            BoundingBoxLocal = new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        public override async Task Load()
        {
            _font = GraphicsManager.Instance.Font;

            _textLabel = new LabelControl
            {
                Text = _text,
                FontSize = 12f,
                TextColor = Color.White,
                HasShadow = false,
                ShadowColor = Color.Black,
                ShadowOpacity = 0.8f,
                BackgroundColor = new Color(0, 0, 0, 160),
                Padding = new Margin { Left = 4, Right = 4, Top = 2, Bottom = 2 },
                UseManualPosition = true,
                UseControlSizeBackground = true,
                Visible = false
            };

            _textLabel.Text = WrapText(_textLabel.Text, _textLabel.FontSize, MaxBubbleWidth);

            if (World?.Scene != null)
            {
                World.Scene.Controls.Add(_textLabel);
                await _textLabel.Load();
            }

            Status = GameControlStatus.Ready;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;

            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsed >= _lifetime)
            {
                RemoveLabels();
                World?.RemoveObject(this);
                Dispose();
                return;
            }

            var target = ResolveTarget();
            if (target == null || target.Hidden || target.Status != GameControlStatus.Ready)
            {
                HideLabels();
                return;
            }

            UpdateLabelPosition(target);
        }

        public override void Draw(GameTime gameTime) { }

        private WalkerObject ResolveTarget()
        {
            if (World == null) return null;
            return World.TryGetWalkerById(_targetId, out var walker) ? walker : null;
        }

        private void UpdateLabelPosition(WalkerObject target)
        {
            Vector3 anchor = new(
                (target.BoundingBoxWorld.Min.X + target.BoundingBoxWorld.Max.X) * 0.5f,
                (target.BoundingBoxWorld.Min.Y + target.BoundingBoxWorld.Max.Y) * 0.5f,
                target.BoundingBoxWorld.Max.Z + OffsetZ);

            Vector3 screen = GraphicsDevice.Viewport.Project(
                anchor,
                Camera.Instance.Projection,
                Camera.Instance.View,
                Matrix.Identity);

            if (screen.Z < 0f || screen.Z > 1f)
            {
                HideLabels();
                return;
            }

            // Convert screen coordinates to virtual coordinates for UI system
            var virtualPos = UiScaler.ToVirtual(new Point((int)screen.X, (int)screen.Y));

            Vector2 textSize = MeasureLabelSize(_textLabel);
            _textLabel.ControlSize=textSize.ToPoint();
            int width=(int)MathF.Ceiling(textSize.X)+_textLabel.Padding.Left+_textLabel.Padding.Right;
            int height=(int)MathF.Ceiling(textSize.Y)+_textLabel.Padding.Top+_textLabel.Padding.Bottom;
            _textLabel.X=(int)(virtualPos.X-width*.5f);
            // Keep the message above the existing nameplate; do not repeat the name.
            _textLabel.Y=(int)virtualPos.Y-height-PixelGap-8;
            _textLabel.Visible=true;
        }

        private void HideLabels()
        {
            if (_textLabel != null)
                _textLabel.Visible = false;
        }

        private void RemoveLabels()
        {
            if (_textLabel != null)
            {
                _textLabel.Parent?.Controls.Remove(_textLabel);
                _textLabel.Dispose();
                _textLabel = null;
            }
        }

        private Vector2 MeasureLabelSize(LabelControl label)
        {
            if (_font == null)
                return Vector2.Zero;

            var font=GraphicsManager.GetUiFont(label.FontSize,out float scale);
            Vector2 size = font.MeasureString(label.Text) * scale;

            if (label.HasShadow)
            {
                size.X += (float)Math.Ceiling(Math.Abs(label.ShadowOffset.X));
                size.Y += (float)Math.Ceiling(Math.Abs(label.ShadowOffset.Y));
            }

            if (label.IsBold)
            {
                size.X += (float)Math.Ceiling(label.BoldStrength * 2);
                size.Y += (float)Math.Ceiling(label.BoldStrength * 2);
            }

            return size;
        }

        private string WrapText(string rawText, float fontSize, int maxWidth)
        {
            if (_font == null || string.IsNullOrEmpty(rawText))
                return rawText;

            var font=GraphicsManager.GetUiFont(fontSize,out float scale);
            var words = rawText.Split(' ');
            var sb = new StringBuilder();
            var current = new StringBuilder();

            foreach (var w in words)
            {
                string test = current.Length == 0 ? w : current + " " + w;
                float width = font.MeasureString(test).X * scale;

                if (width <= maxWidth)
                {
                    current.Clear();
                    current.Append(test);
                }
                else
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(current);
                    current.Clear();
                    current.Append(w);
                }
            }

            if (current.Length > 0)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(current);
            }

            return sb.ToString();
        }

        public void AppendMessage(string newMessage)
        {
            if (string.IsNullOrEmpty(newMessage)) return;
            
            _text = newMessage + "\n" + _text;
            _lifetime = _elapsed + _originalLifetime;
            
            if (_textLabel != null)
            {
                _textLabel.Text = WrapText(_text, _textLabel.FontSize, MaxBubbleWidth);
            }
        }

        public ushort TargetId => _targetId;

        public override void Dispose()
        {
            RemoveLabels();
            base.Dispose();
        }
    }
}
