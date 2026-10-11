using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Utilities;

// Snapshot of MuMain Data/Items/Models/Group*.json, including omitted defaults
// from ItemModelDefinition.h. Values retain MuMain's model coordinate system.
internal readonly record struct MuMainInventoryDisplay(
    Vector3 Rotation, float Scale, float BodyHeight, Vector2 Anchor, Vector3 Offset)
{
    private static readonly Dictionary<string, float[]> Displays = Load();

    public static MuMainInventoryDisplay Get(ItemDefinition definition)
    {
        if (definition != null && Displays.TryGetValue(MuMainItemMetadata.Key(definition).ToString(), out var row))
            return new(new(row[0], row[1], row[2]), row[3], row[4], new(row[5], row[6]), new(row[7], row[8], row[9]));
        return new(new(270, -10, 0), 0.0025f, 0, new(0.5f, 0.6f), Vector3.Zero);
    }

    public static Matrix RotationMatrix(Vector3 degrees) =>
        Matrix.CreateRotationX(MathHelper.ToRadians(degrees.X)) *
        Matrix.CreateRotationY(MathHelper.ToRadians(degrees.Y)) *
        Matrix.CreateRotationZ(MathHelper.ToRadians(degrees.Z));

    public Matrix WorldMatrix(ItemDefinition definition, int width, int height, float rotationAngle)
    {
        // MuMain: 1-degree UI camera, far=2000, preview distance=far*0.1,
        // and a 640x480 reference UI with 20-pixel inventory tiles.
        float uiScale = Math.Min(width / (Math.Max(1, definition.Width) * 20f),
                                 height / (Math.Max(1, definition.Height) * 20f));
        float pixelsPerWorldUnit = 480f / (400f * MathF.Tan(MathHelper.ToRadians(0.5f))) * uiScale;
        Vector3 angles = Rotation;
        if (rotationAngle != 0f)
            angles.Y = rotationAngle; // MuMain replaces Y while hovering, rather than adding another local rotation.
        return Matrix.CreateTranslation(0, 0, BodyHeight) *
               RotationMatrix(angles) * Matrix.CreateScale(Scale * pixelsPerWorldUnit) *
               Matrix.CreateTranslation((Anchor.X - 0.5f) * width + Offset.X * pixelsPerWorldUnit,
                                        (0.5f - Anchor.Y) * height + Offset.Y * pixelsPerWorldUnit,
                                        Offset.Z * pixelsPerWorldUnit);
    }

    private static Dictionary<string, float[]> Load()
    {
        using var stream = typeof(MuMainInventoryDisplay).Assembly.GetManifestResourceStream("Client.Main.Data.mumain_item_display.json")
            ?? throw new InvalidOperationException("Missing MuMain inventory display metadata.");
        // Do not use reflection-based JsonSerializer here: trimmed/AOT Android
        // builds cannot reliably instantiate its dictionary/array converters.
        using var document = JsonDocument.Parse(stream);
        var displays = new Dictionary<string, float[]>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var values = property.Value;
            if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != 10)
                throw new InvalidOperationException($"Invalid MuMain display row: {property.Name}");
            var row = new float[10];
            for (int i = 0; i < row.Length; i++)
                row[i] = values[i].GetSingle();
            displays.Add(property.Name, row);
        }
        return displays;
    }
}
