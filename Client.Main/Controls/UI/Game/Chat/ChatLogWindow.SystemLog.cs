using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI;

public partial class ChatLogWindow
{
    private readonly List<(string Text, MessageType Type, double Start)> _systemLog = new();
    private double _systemLogTime;

    private static bool IsSystemLogType(MessageType type) => type is
        MessageType.System or MessageType.Error or MessageType.Warning or
        MessageType.Info or MessageType.Notice or MessageType.Loot or MessageType.Experience;

    private void AddSystemLog(string text, MessageType type)
    {
        _systemLog.Add((text, type, _systemLogTime));
        if (_systemLog.Count > 8) _systemLog.RemoveAt(0);
    }

    private void DrawSystemLog()
    {
        if (!_isSysMsgVisible || _systemLog.Count == 0) return;
        var batch = GraphicsManager.Instance.Sprite;
        var font = GraphicsManager.GetUiFont(Math.Max(12f,11f/Math.Max(.1f,UiScaler.Scale)), out float scale);
        var bounds = UiScaler.WindowBounds;
        float width = Math.Min(460f, bounds.Width * 0.44f);
        var lines = new List<(string Text, MessageType Type, double Start)>();
        float lineHeight = font.LineSpacing * scale + 2;
        foreach (var entry in _systemLog)
        {
            string remaining = entry.Text;
            while (!string.IsNullOrEmpty(remaining))
            {
                int count = remaining.Length;
                while (count > 1 && font.MeasureString(remaining[..count]).X * scale > width) count--;
                if (count < remaining.Length)
                {
                    int space = remaining.LastIndexOf(' ', count - 1, count);
                    if (space > count / 2) count = space;
                }
                lines.Add((remaining[..count], entry.Type, entry.Start));
                remaining = remaining[count..].TrimStart();

            }
        }
        int first = Math.Max(0, lines.Count - 6);
        float y = bounds.Center.Y - (lines.Count - first) * lineHeight * .5f;
        for (int i = first; i < lines.Count; i++)
        {
            var line = lines[i];
            double age = Math.Max(0, _systemLogTime - line.Start);
            float progress = Math.Clamp((float)age / .28f, 0, 1);
            float slide = (1 - progress) * (1 - progress) * width;
            float alpha = Math.Clamp((15f - (float)age) / 2f, 0, 1) * Alpha;
            OutlinedTextRenderer.Draw(batch, font, line.Text,
                new Vector2(bounds.Left + 5 - slide, y), ChatTextColors.For(line.Type) * alpha, scale);
            y += lineHeight;
        }
    }
}
