using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Client.Main;

namespace Client.Main.Controls.UI.Game.Layouts
{
    /// <summary>
    /// Reads and writes user-editable layout JSON. User layouts live next to the game's
    /// configuration, while bundled sample layouts remain available as read-only defaults.
    /// </summary>
    public static class UiLayoutSerializer
    {
        private const int CurrentVersion = 1;
        private const int MaximumElements = 500;
        private const int MaximumDimension = 16384;
        private static readonly Regex LayoutNamePattern = new("^[A-Za-z0-9][A-Za-z0-9 _-]{0,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static string LayoutDirectory => Path.Combine(
            MuGame.ConfigDirectory ?? AppContext.BaseDirectory,
            "ui",
            "layouts");

        public static string GetLayoutPath(string name)
        {
            string safeName = ValidateLayoutName(name);
            return Path.Combine(LayoutDirectory, safeName + ".json");
        }

        public static string Save(UiLayoutDocument document)
        {
            ValidateAndNormalize(document);
            string destination = GetLayoutPath(document.Name);
            string directory = Path.GetDirectoryName(destination) ?? LayoutDirectory;
            Directory.CreateDirectory(directory);

            string temporaryPath = destination + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
                File.Move(temporaryPath, destination, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            return destination;
        }

        public static UiLayoutDocument Load(string name)
        {
            string safeName = ValidateLayoutName(name);
            string path = GetLayoutPath(safeName);
            if (File.Exists(path))
                return ReadDocument(File.ReadAllText(path), path, safeName);

            string dataLayoutPath = Path.Combine(Constants.DataPath ?? string.Empty, "ui", "layouts", safeName + ".json");
            if (File.Exists(dataLayoutPath))
                return ReadDocument(File.ReadAllText(dataLayoutPath), dataLayoutPath, safeName);

            // Import the old editor-only state only as the default CharacterWindow layout.
            string legacyPath = Path.Combine(MuGame.ConfigDirectory ?? AppContext.BaseDirectory, "ui-editor.json");
            if (string.Equals(safeName, "CharacterWindow", StringComparison.OrdinalIgnoreCase) && File.Exists(legacyPath))
            {
                UiLayoutDocument legacy = ReadDocument(File.ReadAllText(legacyPath), legacyPath, safeName);
                legacy.Name = safeName;
                return legacy;
            }

            using Stream resource = OpenBundledLayout(safeName);
            if (resource != null)
            {
                using var reader = new StreamReader(resource);
                return ReadDocument(reader.ReadToEnd(), $"bundled layout '{safeName}'", safeName);
            }

            throw new FileNotFoundException($"UI layout '{safeName}' was not found. Expected: {path}", path);
        }

        public static IReadOnlyList<string> ListAvailableLayouts()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddJsonFileNames(LayoutDirectory, names);
            AddJsonFileNames(Path.Combine(Constants.DataPath ?? string.Empty, "ui", "layouts"), names);

            foreach (string resource in typeof(UiLayoutSerializer).Assembly.GetManifestResourceNames())
            {
                const string marker = ".Controls.UI.Game.Layouts.";
                int markerIndex = resource.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0 || !resource.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = resource[(markerIndex + marker.Length)..^5];
                if (LayoutNamePattern.IsMatch(name))
                    names.Add(name);
            }

            return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static void ValidateAndNormalize(UiLayoutDocument document)
        {
            if (document == null)
                throw new InvalidDataException("The layout document is empty.");
            if (document.Version < 1 || document.Version > CurrentVersion)
                throw new InvalidDataException($"Unsupported UI layout version: {document.Version}.");

            document.Name = ValidateLayoutName(document.Name);
            if (document.Width <= 0 || document.Width > MaximumDimension || document.Height <= 0 || document.Height > MaximumDimension)
                throw new InvalidDataException($"Layout canvas dimensions must be between 1 and {MaximumDimension}.");
            if (document.Elements == null)
                throw new InvalidDataException("The layout elements list is missing.");
            if (document.Elements.Count > MaximumElements)
                throw new InvalidDataException($"A layout can contain at most {MaximumElements} elements.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UiLayoutElement element in document.Elements)
            {
                if (element == null)
                    throw new InvalidDataException("The layout contains an empty element.");
                if (string.IsNullOrWhiteSpace(element.Id) || !ids.Add(element.Id.Trim()))
                    throw new InvalidDataException("Every layout element must have a unique, non-empty ID.");

                element.Type = (element.Type ?? string.Empty).Trim().ToLowerInvariant();
                if (element.Type is not ("image" or "text" or "button" or "panel"))
                    throw new InvalidDataException($"Element '{element.Name ?? element.Id}' has unsupported type '{element.Type}'.");

                element.Name = string.IsNullOrWhiteSpace(element.Name) ? element.Type : element.Name.Trim();
                if (element.Width <= 0 || element.Width > MaximumDimension || element.Height <= 0 || element.Height > MaximumDimension)
                    throw new InvalidDataException($"Element '{element.Name}' has invalid dimensions.");
                if (!float.IsFinite(element.Opacity) || element.Opacity < 0f || element.Opacity > 1f)
                    throw new InvalidDataException($"Element '{element.Name}' has invalid opacity.");
                if (element.FontSize <= 0f)
                    element.FontSize = element.Type == "button" ? 11f : 14f;
                if (!float.IsFinite(element.FontSize) || element.FontSize < 1f || element.FontSize > 256f)
                    throw new InvalidDataException($"Element '{element.Name}' has invalid font size.");
                if (element.Text?.Length > 4096)
                    throw new InvalidDataException($"Element '{element.Name}' text exceeds the 4096 character limit.");

                element.Asset = NormalizeAssetReference(element.Asset, element.Name, element.Type == "image");
            }

            // Elements are rendered in ascending layer order. Stable sorting preserves the
            // user's order for older documents that happen to contain duplicate layer values.
            document.Elements = document.Elements
                .Select((element, index) => (element, index))
                .OrderBy(item => item.element.Layer)
                .ThenBy(item => item.index)
                .Select((item, layer) =>
                {
                    item.element.Layer = layer;
                    return item.element;
                })
                .ToList();
            document.Version = CurrentVersion;
        }

        private static UiLayoutDocument ReadDocument(string json, string source, string requestedName)
        {
            try
            {
                UiLayoutDocument document = JsonSerializer.Deserialize<UiLayoutDocument>(json, JsonOptions);
                if (document == null)
                    throw new InvalidDataException("The JSON did not contain a layout document.");
                if (string.IsNullOrWhiteSpace(document.Name))
                    document.Name = requestedName;
                ValidateAndNormalize(document);
                return document;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Failed to parse UI layout from {source}: {ex.Message}", ex);
            }
        }

        private static string ValidateLayoutName(string name)
        {
            string trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !LayoutNamePattern.IsMatch(trimmed))
                throw new InvalidDataException("Layout name must be 1-64 characters and contain only letters, numbers, spaces, hyphens, or underscores.");
            return trimmed;
        }

        private static string NormalizeAssetReference(string asset, string elementName, bool required)
        {
            if (string.IsNullOrWhiteSpace(asset))
            {
                if (required)
                    throw new InvalidDataException($"Image element '{elementName}' needs an Interface asset reference.");
                return string.Empty;
            }

            string normalized = asset.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalized) || normalized.Contains(':') ||
                !normalized.StartsWith("Interface/", StringComparison.OrdinalIgnoreCase) ||
                normalized.Split('/').Any(segment => segment is ".." or "."))
            {
                throw new InvalidDataException($"Element '{elementName}' has an invalid asset reference. Use a relative Interface/... path.");
            }

            return normalized;
        }

        private static Stream OpenBundledLayout(string name)
        {
            string suffix = $".Controls.UI.Game.Layouts.{name}.json";
            string resourceName = typeof(UiLayoutSerializer).Assembly.GetManifestResourceNames()
                .FirstOrDefault(resource => resource.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            return resourceName == null
                ? null
                : typeof(UiLayoutSerializer).Assembly.GetManifestResourceStream(resourceName);
        }

        private static void AddJsonFileNames(string directory, ISet<string> names)
        {
            if (!Directory.Exists(directory))
                return;

            foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (LayoutNamePattern.IsMatch(name))
                    names.Add(name);
            }
        }
    }
}
