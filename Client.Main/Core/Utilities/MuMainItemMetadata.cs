// Snapshot of MuMain src/bin/Data/Items and Items/Models. Keep client Item.bmd stats authoritative.
using Microsoft.Xna.Framework;
using Client.Main.Controls.UI.Game.Inventory;
namespace Client.Main.Core.Utilities;
internal static class MuMainItemMetadata
{
    public static int Key(ItemDefinition d) => d.Group * 512 + d.Id;
    public static bool IsSecondWing(ItemDefinition d) => Key(d) is 6147 or 6148 or 6149 or 6150 or 6186;
    public static bool IsThirdWing(ItemDefinition d) => Key(d) is 6180 or 6181 or 6182 or 6183 or 6184 or 6187 or 6194;
    public static bool IsWing(ItemDefinition d) => Key(d) is 6144 or 6145 or 6146 or 6147 or 6148 or 6149 or 6150 or 6180 or 6181 or 6182 or 6183 or 6184 or 6185 or 6186 or 6187 or 6193 or 6194 or 6274 or 6275 or 6276 or 6277 or 6278 or 6279 or 6686;
    public static bool IsSummonerBook(ItemDefinition d) => Key(d) is 2581 or 2582 or 2583;
    public static bool IsArchangel(ItemDefinition d) => Key(d) is 19 or 1037 or 2066 or 2570 or 2596;
    public static Vector3 InventoryRotation(ItemDefinition d) => MuMainInventoryDisplay.Get(d).Rotation;
}
