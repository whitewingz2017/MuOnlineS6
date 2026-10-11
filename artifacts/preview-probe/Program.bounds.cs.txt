using System.Reflection;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Client.Data.BMD;
using Microsoft.Xna.Framework;

var displayType = typeof(ItemDatabase).Assembly.GetType("Client.Main.Core.Utilities.MuMainInventoryDisplay")!;
var get = displayType.GetMethod("Get")!;
var worldMethod = displayType.GetMethod("WorldMatrix")!;
var bonesMethod = typeof(BmdPreviewRenderer).GetMethod("BuildBoneMatrices", BindingFlags.NonPublic | BindingFlags.Static)!;
foreach(var (group,id) in new (byte,short)[]{(0,0),(4,0),(12,0),(14,0),(14,6),(7,0)})
{
    var def = ItemDatabase.GetItemDefinition(group,id);
    var path = Path.Combine("C:/OpenMU/Data",def.TexturePath);
    var bmd = await new BMDReader().Load(path);
    var bones = (Matrix[])bonesMethod.Invoke(null,[bmd,def])!;
    var display = get.Invoke(null,[def]);
    var world = (Matrix)worldMethod.Invoke(display,[def,def.Width*34,def.Height*34,0f])!;
    var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
    foreach(var mesh in bmd.Meshes) foreach(var vertex in mesh.Vertices) {
        var v=Vector3.Transform(new Vector3(vertex.Position.X,vertex.Position.Y,vertex.Position.Z),bones[vertex.Node]*world);
        min=Vector3.Min(min,v); max=Vector3.Max(max,v);
    }
    Console.WriteLine($"{group}:{id} {def.TexturePath} target={def.Width*34}x{def.Height*34} bounds={min}..{max}");
}
