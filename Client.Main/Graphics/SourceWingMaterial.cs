using Microsoft.Xna.Framework.Graphics;
using Client.Main.Controllers;
namespace Client.Main.Graphics;

internal static class SourceWingMaterial
{
    internal static bool IsSpecial(int group,int index) => group==12 && index is 4 or 5 or 6 or 39 or 40 or 42 or 43 or 50;
    internal static bool Draw(GraphicsDevice device,Effect effect,int primitives)
    {
        int index=(int)(effect.Parameters["SourceWingIndex"]?.GetValueSingle() ?? -1);
        if(index<0)return false;
        int mesh=(int)(effect.Parameters["SourceWingMesh"]?.GetValueSingle() ?? -1);
        var blend=device.BlendState;var depth=device.DepthStencilState;
        // Ruin's layer repeats with linear filtering; Chrome06 uses point/clamp.
        var chrome=effect.Parameters[index==39?"Chrome01Texture":"Chrome02Texture"];var oldTexture=chrome?.GetValueTexture2D();
        var selector=effect.Parameters["SourcePass"];
        try
        {
            for(int pass=0;pass<2;pass++)
            {
                // Darkness's chrome is behind its textured body in MuMain.
                bool overlay=index==6?pass==0:pass==1;
                if(overlay && (index is 4 or 5 or 40 or 50 || index is 42 or 43 && mesh!=1))continue;
                if(index==39 && mesh==0 && !overlay)continue;
                if(overlay)
                {
                    var graphics=GraphicsManager.Instance;
                    chrome?.SetValue(index==39?graphics.WingRuinLayerTexture:index==42?graphics.WingChrome06Texture:graphics.ItemChrome02Texture);
                    device.BlendState=Blendings.OneOneAdditive;device.DepthStencilState=DepthStencilState.DepthRead;
                }
                else{device.BlendState=blend;device.DepthStencilState=depth;}
                selector?.SetValue(overlay?11f:10f);
                effect.CurrentTechnique.Passes[0].Apply();device.DrawIndexedPrimitives(PrimitiveType.TriangleList,0,0,primitives);
            }
        }
        finally{selector?.SetValue(-1f);chrome?.SetValue(oldTexture);device.BlendState=blend;device.DepthStencilState=depth;}
        return true;
    }
}
