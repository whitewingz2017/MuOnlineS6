using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Editor
{
    /// <summary>
    /// Phase 2 asset browser. It discovers Interface assets below Constants.DataPath and
    /// renders thumbnails through the existing TextureLoader without copying or converting files.
    /// </summary>
    internal sealed class GameUiEditorAssetBrowserControl : UIControl
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ozt", ".tga", ".ozj", ".jpg", ".ozp", ".png", ".ozd", ".dds"
        };

        private readonly TextFieldControl _searchBox;
        private readonly List<AssetEntry> _allAssets = new();
        private readonly List<AssetEntry> _filteredAssets = new();
        private readonly ConcurrentDictionary<string, Texture2D> _thumbnails = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _thumbnailRequests = new(StringComparer.OrdinalIgnoreCase);
        private int _scrollOffset;
        private string _status = "Scanning Interface...";
        private string _selectedPath;

        private const int HeaderHeight = 74;
        private const int RowHeight = 72;
        private const int ThumbnailSize = 54;
        private const int VisibleRows = 7;

        public event Action<string> AssetSelected;

        public string SelectedPath => _selectedPath;
        public int AssetCount => _filteredAssets.Count;

        public GameUiEditorAssetBrowserControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(360, 610);
            ViewSize = ControlSize;
            Interactive = true;
            BackgroundColor = new Color(12, 16, 24, 248);
            BorderColor = ModernHudTheme.BorderInner;
            BorderThickness = 1;

            _searchBox = TextFieldControl.Create();
            _searchBox.X = 12;
            _searchBox.Y = 34;
            _searchBox.ControlSize = new Point(336, 26);
            _searchBox.ViewSize = _searchBox.ControlSize;
            _searchBox.Placeholder = "Search Interface assets...";
            _searchBox.FontSize = 10f;
            _searchBox.TextColor = ModernHudTheme.TextWhite;
            _searchBox.BackgroundColor = ModernHudTheme.BgDarkest;
            _searchBox.BorderColor = ModernHudTheme.BorderInner;
            _searchBox.ValueChanged += OnSearchChanged;
            Controls.Add(_searchBox);
        }

        public override async Task Load()
        {
            await base.Load();
            ScanAssetLibrary();
        }

        private void ScanAssetLibrary()
        {
            _allAssets.Clear();
            _filteredAssets.Clear();
            _scrollOffset = 0;

            string interfaceRoot = Path.Combine(Constants.DataPath ?? string.Empty, "Interface");
            if (!Directory.Exists(interfaceRoot))
            {
                _status = $"Interface folder not found: {interfaceRoot}";
                return;
            }

            try
            {
                foreach (string file in Directory.EnumerateFiles(interfaceRoot, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file);
                    if (!SupportedExtensions.Contains(extension))
                        continue;

                    string relativePath = Path.GetRelativePath(Constants.DataPath, file).Replace('\\', '/');
                    _allAssets.Add(new AssetEntry(relativePath, Path.GetFileName(file)));
                }

                _allAssets.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.RelativePath, right.RelativePath));
                ApplyFilter();
                _status = $"{_allAssets.Count} image assets discovered";
            }
            catch (Exception ex)
            {
                _status = $"Asset scan failed: {ex.Message}";
            }
        }

        private void OnSearchChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = _searchBox?.Value?.Trim() ?? string.Empty;
            _filteredAssets.Clear();

            foreach (AssetEntry asset in _allAssets)
            {
                if (query.Length == 0 || asset.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase))
                    _filteredAssets.Add(asset);
            }

            int maxScroll = Math.Max(0, _filteredAssets.Count - VisibleRows);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        public override bool ProcessMouseScroll(int scrollDelta)
        {
            int direction = scrollDelta > 0 ? -1 : 1;
            int next = Math.Clamp(_scrollOffset + direction, 0, Math.Max(0, _filteredAssets.Count - VisibleRows));
            if (next == _scrollOffset)
                return false;

            _scrollOffset = next;
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Update(gameTime);
            RequestVisibleThumbnails();

            MouseState mouse = MuGame.Instance.UiMouseState;
            MouseState previous = MuGame.Instance.PrevUiMouseState;
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released)
            {
                Point position = mouse.Position;
                if (_searchBox.Visible && _searchBox.DisplayRectangle.Contains(position))
                {
                    // The editor shell owns focus when opened. Explicitly transfer it to the
                    // existing TextFieldControl so its normal keyboard polling receives input.
                    _searchBox.Focus();
                    Scene?.SetMouseInputConsumed();
                    return;
                }

                for (int row = 0; row < VisibleRows; row++)
                {
                    int index = _scrollOffset + row;
                    if (index >= _filteredAssets.Count)
                        break;

                    Rectangle rowRect = GetRowRectangle(row);
                    if (!rowRect.Contains(position))
                        continue;

                    _selectedPath = _filteredAssets[index].RelativePath;
                    GameUiEditorDragState.Begin(_selectedPath);
                    AssetSelected?.Invoke(_selectedPath);
                    Scene?.SetMouseInputConsumed();
                    break;
                }
            }
        }

        private void RequestVisibleThumbnails()
        {
            int last = Math.Min(_filteredAssets.Count, _scrollOffset + VisibleRows);
            for (int i = _scrollOffset; i < last; i++)
            {
                AssetEntry asset = _filteredAssets[i];
                if (_thumbnails.ContainsKey(asset.RelativePath) ||
                    !_thumbnailRequests.TryAdd(asset.RelativePath, 0))
                    continue;

                _ = LoadThumbnailAsync(asset.RelativePath);
            }
        }

        private async Task LoadThumbnailAsync(string relativePath)
        {
            try
            {
                Texture2D texture = await TextureLoader.Instance.PrepareAndGetTexture(relativePath);
                if (texture != null)
                    _thumbnails[relativePath] = texture;
            }
            catch
            {
                // Missing or unsupported assets remain represented by the placeholder tile.
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
                return;

            base.Draw(gameTime);

            SpriteBatch sprite = GraphicsManager.Instance.Sprite;
            Texture2D pixel = GraphicsManager.Instance.Pixel;
            SpriteFont font = GraphicsManager.GetUiFont(10f, out float fontScale) ?? GraphicsManager.Instance.Font;
            if (sprite == null || pixel == null || font == null)
                return;

            Rectangle bounds = DisplayRectangle;
            sprite.DrawString(font, "ASSET BROWSER", new Vector2(bounds.X + 12, bounds.Y + 10), ModernHudTheme.TextGold, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
            sprite.DrawString(font, _status, new Vector2(bounds.X + 12, bounds.Y + 62), ModernHudTheme.TextGray, 0f, Vector2.Zero, fontScale * 0.78f, SpriteEffects.None, 0f);

            for (int row = 0; row < VisibleRows; row++)
            {
                int index = _scrollOffset + row;
                Rectangle rowRect = GetRowRectangle(row);
                if (index >= _filteredAssets.Count)
                {
                    sprite.Draw(pixel, rowRect, new Color(8, 11, 17, 120));
                    continue;
                }

                AssetEntry asset = _filteredAssets[index];
                bool selected = StringComparer.OrdinalIgnoreCase.Equals(asset.RelativePath, _selectedPath);
                Color rowColor = selected ? new Color(64, 55, 34, 235) : new Color(20, 27, 37, 230);
                sprite.Draw(pixel, rowRect, rowColor);
                DrawThumbnail(sprite, pixel, asset, rowRect);

                string name = Truncate(font, asset.FileName, 10f, rowRect.Width - 86);
                string path = Truncate(font, asset.RelativePath, 8f, rowRect.Width - 86);
                sprite.DrawString(font, name, new Vector2(rowRect.X + 68, rowRect.Y + 14), ModernHudTheme.TextWhite, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                sprite.DrawString(font, path, new Vector2(rowRect.X + 68, rowRect.Y + 36), ModernHudTheme.TextGray, 0f, Vector2.Zero, fontScale * 0.8f, SpriteEffects.None, 0f);
            }
        }

        private void DrawThumbnail(SpriteBatch sprite, Texture2D pixel, AssetEntry asset, Rectangle rowRect)
        {
            Rectangle thumbnailRect = new(rowRect.X + 8, rowRect.Y + 9, ThumbnailSize, ThumbnailSize);
            sprite.Draw(pixel, thumbnailRect, new Color(5, 7, 12, 255));

            if (_thumbnails.TryGetValue(asset.RelativePath, out Texture2D texture) && texture != null && !texture.IsDisposed)
            {
                float scale = Math.Min((float)ThumbnailSize / texture.Width, (float)ThumbnailSize / texture.Height);
                int width = Math.Max(1, (int)(texture.Width * scale));
                int height = Math.Max(1, (int)(texture.Height * scale));
                Rectangle fitted = new(
                    thumbnailRect.X + (ThumbnailSize - width) / 2,
                    thumbnailRect.Y + (ThumbnailSize - height) / 2,
                    width,
                    height);
                sprite.Draw(texture, fitted, Color.White);
            }
            else
            {
                sprite.Draw(pixel, new Rectangle(thumbnailRect.X + 15, thumbnailRect.Y + 25, 24, 2), ModernHudTheme.TextDark);
                sprite.Draw(pixel, new Rectangle(thumbnailRect.X + 26, thumbnailRect.Y + 14, 2, 24), ModernHudTheme.TextDark);
            }
        }

        private Rectangle GetRowRectangle(int row)
        {
            Rectangle bounds = DisplayRectangle;
            return new Rectangle(bounds.X + 8, bounds.Y + HeaderHeight + row * RowHeight, bounds.Width - 16, RowHeight - 4);
        }

        private static string Truncate(SpriteFont font, string value, float size, int maxWidth)
        {
            if (string.IsNullOrEmpty(value) || maxWidth <= 0)
                return string.Empty;

            if (font.MeasureString(value).X * size / 10f <= maxWidth)
                return value;

            string result = value;
            while (result.Length > 3 && font.MeasureString(result + "...").X * size / 10f > maxWidth)
                result = result[..^1];
            return result + "...";
        }

        private readonly record struct AssetEntry(string RelativePath, string FileName);
    }
}
