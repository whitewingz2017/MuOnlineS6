using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Skills;

internal static class ClassicSkillTooltip
{
    internal static string BuildText(SkillEntryState skill)
    {
        if (skill == null) return string.Empty;
        var definition = SkillDatabase.GetSkillDefinition(skill.SkillId);
        var lines = new List<string> { SkillDatabase.GetSkillName(skill.SkillId), "" };
        // The definition stores base damage, not the character's final damage range.
        if (definition?.Damage > 0) lines.Add($"Base Skill Damage: {definition.Damage}");
        uint range = SkillDatabase.GetSkillRange(skill.SkillId);
        if (range > 0) lines.Add($"Range: {range}");
        ushort mana = SkillDatabase.GetSkillManaCost(skill.SkillId);
        ushort ag = SkillDatabase.GetSkillAGCost(skill.SkillId);
        if (mana > 0) lines.Add($"Mana: {mana}");
        if (ag > 0) lines.Add($"AG: {ag}");
        if (definition != null)
        {
            lines.Add("");
            if (definition.RequiredLevel > 0) lines.Add($"Minimum Level Requirement: {definition.RequiredLevel}");
            if (definition.RequiredStrength > 0) lines.Add($"Strength Requirement: {definition.RequiredStrength}");
            if (definition.RequiredDexterity > 0) lines.Add($"Agility Requirement: {definition.RequiredDexterity}");
            if (definition.RequiredEnergy > 0)
            {
                int energy = 20 + definition.RequiredEnergy * Math.Max(1, (int)definition.RequiredLevel) * 4 / 100;
                lines.Add($"Energy Requirement: {energy}");
            }
            if (definition.RequiredLeadership > 0) lines.Add($"Command Requirement: {definition.RequiredLeadership}");
            if (definition.Delay > 0) lines.Add($"Cooldown: {definition.Delay / 1000f:0.0} sec");
        }
        if (Client.Main.Objects.Effects.Skills.SkillVisualEffectRegistry.GetBaseSkillId(skill.SkillId) == 1)
        {
            lines.Add("");
            lines.Add("Poison attribute");
        }
        return string.Join("\n", lines).TrimEnd();
    }

    internal static void Draw(string text, Rectangle anchor)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var graphics = GraphicsManager.Instance;
        var sprite = graphics?.Sprite;
        var font = graphics?.Font;
        var pixel = graphics?.Pixel;
        if (sprite == null || font == null || pixel == null) return;
        // DrawAfter runs outside DrawScreenUi's batch. Own and restore its state.
        using var scope = new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
            BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            null, null, UiScaler.SpriteTransform);
        string[] lines = text.Split('\n');
        const float scale = .56f;
        const int padding = 7;
        int lineHeight = (int)MathF.Ceiling(font.LineSpacing * scale);
        float width = 0;
        float height = padding * 2;
        foreach (string line in lines)
        {
            width = MathF.Max(width, font.MeasureString(line).X * scale);
            height += line.Length == 0 ? lineHeight * .45f : lineHeight;
        }
        var size = UiScaler.VirtualSize;
        int boxWidth = (int)MathF.Ceiling(width) + padding * 2;
        int boxHeight = (int)MathF.Ceiling(height);
        int x = Math.Clamp(anchor.Center.X - boxWidth / 2, 2, Math.Max(2, size.X - boxWidth - 2));
        int y = Math.Clamp(anchor.Top - boxHeight - 5, 2, Math.Max(2, size.Y - boxHeight - 2));
        var box = new Rectangle(x, y, boxWidth, boxHeight);
        sprite.Draw(pixel, box, new Color(0, 0, 0, 205));
        sprite.Draw(pixel, new Rectangle(x, y, boxWidth, 1), Color.Black);
        sprite.Draw(pixel, new Rectangle(x, y + boxHeight - 1, boxWidth, 1), Color.Black);
        sprite.Draw(pixel, new Rectangle(x, y, 1, boxHeight), Color.Black);
        sprite.Draw(pixel, new Rectangle(x + boxWidth - 1, y, 1, boxHeight), Color.Black);
        float lineY = y + padding;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0) { lineY += lineHeight * .45f; continue; }
            Color color = i == 0 || line.EndsWith(" attribute", StringComparison.Ordinal)
                ? new Color(128, 180, 255) : Color.White;
            float lineX = x + (boxWidth - font.MeasureString(line).X * scale) * .5f;
            var position = new Vector2(lineX, lineY);
            sprite.DrawString(font, line, position + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            sprite.DrawString(font, line, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            if (i == 0) sprite.DrawString(font, line, position + new Vector2(.5f, 0), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            lineY += lineHeight;
        }
    }
}
