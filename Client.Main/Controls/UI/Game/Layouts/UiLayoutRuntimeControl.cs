using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Layouts
{
    /// <summary>
    /// Runtime host for one saved layout. It uses the normal GameControl tree, UI controls,
    /// UiScaler transform, and TextureLoader; it has no dependency on the F11 editor.
    /// </summary>
    public sealed class UiLayoutRuntimeControl : UIControl
    {
        private readonly ILogger _logger;
        private UiLayoutDocument _document;
        private string _layoutName;
        private int _loadGeneration;

        public string LayoutName => _layoutName;
        public string LastError { get; private set; }
        public IReadOnlyList<string> LastWarnings { get; private set; } = Array.Empty<string>();
        public UiLayoutDocument Document => _document;

        /// <summary>Raised when a runtime layout button is clicked; serialized layouts carry no executable code.</summary>
        public event Action<UiLayoutElement> ButtonClicked;
        public event Action<string> LoadFailed;

        public UiLayoutRuntimeControl(ILogger logger = null)
        {
            _logger = logger ?? MuGame.AppLoggerFactory?.CreateLogger<UiLayoutRuntimeControl>();
            AutoViewSize = false;
            Interactive = false;
            Visible = false;
            BackgroundColor = Color.Transparent;
        }

        /// <summary>
        /// Loads a named JSON layout and creates its controls. Missing assets are reported as
        /// warnings and skipped, while the rest of a valid layout remains usable.
        /// </summary>
        public async Task<bool> LoadLayoutAsync(string name, bool show = false)
        {
            int generation = Interlocked.Increment(ref _loadGeneration);
            UiLayoutDocument document;
            try
            {
                document = UiLayoutSerializer.Load(name);
            }
            catch (Exception ex)
            {
                return FailLoad(name, ex);
            }

            var warnings = new List<string>();
            var textures = new Texture2D[document.Elements.Count];
            var assetLoads = new List<Task>();
            for (int i = 0; i < document.Elements.Count; i++)
            {
                UiLayoutElement element = document.Elements[i];
                if (string.IsNullOrWhiteSpace(element.Asset) || element.Type is not ("image" or "button"))
                    continue;

                int index = i;
                assetLoads.Add(LoadTextureAsync(element.Asset, index, textures, warnings));
            }

            await Task.WhenAll(assetLoads).ConfigureAwait(false);
            if (generation != Volatile.Read(ref _loadGeneration))
                return false;

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action apply = () =>
            {
                if (generation != Volatile.Read(ref _loadGeneration))
                {
                    completion.TrySetResult(false);
                    return;
                }

                try
                {
                    ReplaceChildren(document, textures);
                    _document = document;
                    _layoutName = document.Name;
                    LastWarnings = warnings;
                    LastError = warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings);
                    if (warnings.Count > 0)
                        _logger?.LogWarning("Loaded UI layout {LayoutName} with warnings: {Warnings}", document.Name, LastError);
                    Visible = show;
                    if (show)
                        BringToFront();
                    completion.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    FailLoad(document.Name, ex);
                    completion.TrySetResult(false);
                }
            };

            if (MuGame.IsMainThread)
                apply();
            else
                MuGame.ScheduleOnMainThread(apply, MainThreadDispatcher.WorkPriority.High, "UiLayout.ApplyLayout");

            return await completion.Task.ConfigureAwait(false);
        }

        public Task<bool> ReloadAsync(bool show = true)
        {
            if (string.IsNullOrWhiteSpace(_layoutName))
                return Task.FromResult(false);
            return LoadLayoutAsync(_layoutName, show);
        }

        public void Show()
        {
            if (_document == null)
                return;
            Visible = true;
            BringToFront();
        }

        public void Hide() => Visible = false;

        private async Task LoadTextureAsync(string asset, int index, Texture2D[] textures, List<string> warnings)
        {
            try
            {
                Texture2D texture = await TextureLoader.Instance.PrepareAndGetTexture(asset).ConfigureAwait(false);
                textures[index] = texture;
                if (texture == null)
                    lock (warnings)
                        warnings.Add($"Failed to load UI asset: {asset}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed loading UI layout asset {Asset}", asset);
                lock (warnings)
                    warnings.Add($"Failed to load UI asset: {asset} ({ex.Message})");
            }
        }

        private void ReplaceChildren(UiLayoutDocument document, Texture2D[] textures)
        {
            foreach (GameControl child in Controls.ToArray())
                child.Dispose();

            ControlSize = new Point(document.Width, document.Height);
            ViewSize = ControlSize;
            CenterInVirtualViewport();

            for (int i = 0; i < document.Elements.Count; i++)
            {
                UiLayoutElement element = document.Elements[i];
                if (element.Type == "image" && textures[i] == null)
                    continue;
                AddRuntimeElement(element, textures[i]);
            }
        }

        private void AddRuntimeElement(UiLayoutElement element, Texture2D texture)
        {
            GameControl control;
            switch (element.Type)
            {
                case "image":
                    var image = new SpriteControl
                    {
                        TexturePath = element.Asset,
                        AutoViewSize = false
                    };
                    image.SetTexture(texture);
                    control = image;
                    break;
                case "text":
                    control = new LabelControl
                    {
                        Text = element.Text ?? string.Empty,
                        FontSize = element.FontSize,
                        TextColor = ModernHudTheme.TextWhite,
                        IsBold = true
                    };
                    break;
                case "button":
                    var button = new ButtonControl
                    {
                        Text = element.Text ?? string.Empty,
                        TexturePath = string.IsNullOrWhiteSpace(element.Asset) ? null : element.Asset,
                        FontSize = element.FontSize,
                        TextColor = ModernHudTheme.TextWhite,
                        HoverTextColor = ModernHudTheme.TextGold,
                        BackgroundColor = ModernHudTheme.BgMid,
                        HoverBackgroundColor = ModernHudTheme.BgLight,
                        PressedBackgroundColor = ModernHudTheme.BgDark
                    };
                    button.Click += (_, _) => ButtonClicked?.Invoke(element);
                    control = button;
                    break;
                case "panel":
                    control = new RuntimePanelControl();
                    break;
                default:
                    // The serializer validates types before controls are created.
                    throw new InvalidOperationException($"Unsupported UI element type: {element.Type}");
            }

            control.Name = element.Name;
            control.AutoViewSize = false;
            control.X = element.X;
            control.Y = element.Y;
            control.ControlSize = new Point(element.Width, element.Height);
            control.ViewSize = control.ControlSize;
            control.Visible = element.Visible;
            control.Alpha = element.Opacity;
            if (control is TextureControl textureControl)
                textureControl.Alpha = element.Opacity;
            if (control is LabelControl label)
                label.Alpha = element.Opacity;
            control.Interactive = false;
            Controls.Add(control);
        }

        private void CenterInVirtualViewport()
        {
            Point virtualSize = UiScaler.VirtualSize;
            X = Math.Max(0, (virtualSize.X - ViewSize.X) / 2);
            Y = Math.Max(0, (virtualSize.Y - ViewSize.Y) / 2);
        }

        private bool FailLoad(string name, Exception exception)
        {
            LastError = $"Failed to load UI layout: {name}{Environment.NewLine}{exception.Message}";
            LastWarnings = Array.Empty<string>();
            _logger?.LogError(exception, "Failed loading UI layout {LayoutName}", name);
            LoadFailed?.Invoke(LastError);
            return false;
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            if (_document != null)
                CenterInVirtualViewport();
        }

        public override void Dispose()
        {
            Interlocked.Increment(ref _loadGeneration);
            ButtonClicked = null;
            LoadFailed = null;
            base.Dispose();
        }

        private sealed class RuntimePanelControl : UIControl
        {
            public RuntimePanelControl()
            {
                BackgroundColor = ModernHudTheme.BgMid;
                BorderColor = ModernHudTheme.BorderInner;
                BorderThickness = 1;
            }
        }
    }
}
