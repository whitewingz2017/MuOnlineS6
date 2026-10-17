using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI;

internal static class ChatTextColors
{
    public static Color For(MessageType type) => type switch
    {
        MessageType.Whisper => new(255, 50, 170),
        MessageType.Party => new(0, 225, 255),
        MessageType.Guild => new(0, 230, 170),
        MessageType.Union => new(245, 205, 40),
        MessageType.Gens => new(150, 225, 100),
        MessageType.World or MessageType.Location => new(255, 140, 35),
        MessageType.Trade or MessageType.GM or MessageType.Notice or MessageType.Info => new(255, 205, 55),
        MessageType.Error => new(255, 55, 45),
        MessageType.Warning => new(255, 85, 30),
        MessageType.Loot => new(190, 180, 255),
        MessageType.Experience => new(225, 155, 220),
        MessageType.System => new(80, 165, 255),
        _ => new(225, 225, 225)
    };
}
