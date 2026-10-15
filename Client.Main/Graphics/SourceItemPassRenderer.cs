using Microsoft.Xna.Framework.Graphics;
namespace Client.Main.Graphics;

// MuMain draws the body, chrome2/4, metal, chrome, then the item option layer.
// Keep depth writes on the base and use additive, read-only depth for overlays.
internal static class SourceItemPassRenderer
{
    internal static void Draw(GraphicsDevice device, Effect effect, int primitiveCount, bool special)
    {
        if(SourceWingMaterial.Draw(device,effect,primitiveCount))return;
        var selector=effect.Parameters["SourcePass"];
        if(special || selector==null)
        {
            selector?.SetValue(-1f);
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList,0,0,primitiveCount);
            return;
        }
        var blend=device.BlendState;
        var depth=device.DepthStencilState;
        var emission=effect.Parameters["SourceEmissionGain"];
        float baseEmission=emission?.GetValueSingle() ?? 1f;
        float level=effect.Parameters["SourceLevel"]?.GetValueSingle() ?? 0;
        bool options=(effect.Parameters["IsExcellent"]?.GetValueBoolean() ?? false) ||
                     (effect.Parameters["IsAncient"]?.GetValueBoolean() ?? false);
        try
        {
            for(int pass=0;pass<5;pass++)
            {
                if(pass==1 && level<11 || pass==2 && level<9 || pass==3 && level<7 || pass==4 && !options)continue;
                selector.SetValue((float)pass);
                emission?.SetValue(pass==0?baseEmission:SourceItemMaterialRules.EmissiveBrightness);
                device.BlendState=pass==0?blend:Blendings.OneOneAdditive;
                device.DepthStencilState=pass==0?depth:DepthStencilState.DepthRead;
                effect.CurrentTechnique.Passes[0].Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,0,0,primitiveCount);
            }
        }
        finally
        {
            selector.SetValue(-1f);
            emission?.SetValue(baseEmission);
            device.BlendState=blend;device.DepthStencilState=depth;
        }
    }
}
