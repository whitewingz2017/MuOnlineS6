using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Objects.Effects;

internal static class SourceEffectGeometry
{
    internal static void Quad(VertexPositionColorTexture[] vertices, ref int count, Vector3 a, Vector3 b, Vector3 halfWidth, Color tint, float u, float v)
    {
        var p = new VertexPositionColorTexture(a - halfWidth, tint, new Vector2(u, 0));
        var q = new VertexPositionColorTexture(a + halfWidth, tint, new Vector2(u, 1));
        var r = new VertexPositionColorTexture(b + halfWidth, tint, new Vector2(v, 1));
        var s = new VertexPositionColorTexture(b - halfWidth, tint, new Vector2(v, 0));
        vertices[count++] = p; vertices[count++] = q; vertices[count++] = r;
        vertices[count++] = p; vertices[count++] = r; vertices[count++] = s;
    }
}
