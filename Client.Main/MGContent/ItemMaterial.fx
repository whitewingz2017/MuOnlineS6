#if SM6
    #define VS_SHADERMODEL vs_6_0
    #define PS_SHADERMODEL ps_6_0
#elif OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_5_0
    #define PS_SHADERMODEL ps_5_0
#endif

#if SM6
    #define UNIFORM_DEFAULT(type, name, value) type name
#else
    #define UNIFORM_DEFAULT(type, name, value) type name = value
#endif

float4x4 World;
float4x4 WorldViewProjection;
#if !OPENGL
float4x4 BoneMatrices[256];
#endif

float3 EyePosition;
UNIFORM_DEFAULT(float3, LightDirection, float3(0.707, -0.707, 0));

#if SM6
Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0)
{
    Filter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D Chrome02Texture : register(t1);
SamplerState Chrome02Sampler : register(s1)
{
    Filter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D Shiny01Texture : register(t2);
SamplerState Shiny01Sampler : register(s2)
{
    Filter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D Chrome01Texture : register(t3);
SamplerState Chrome01Sampler : register(s3)
{
    Filter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D ShadowMap : register(t4);
SamplerState ShadowSampler : register(s4)
{
    Filter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
#else
Texture2D DiffuseTexture;
sampler DiffuseSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D Chrome02Texture;
sampler Chrome02Sampler = sampler_state
{
    Texture = <Chrome02Texture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D Shiny01Texture;
sampler Shiny01Sampler = sampler_state
{
    Texture = <Shiny01Texture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D Chrome01Texture;
sampler Chrome01Sampler = sampler_state
{
    Texture = <Chrome01Texture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D ShadowMap;
sampler ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

#if SM6
float4 SampleDiffuseSampler(float2 uv) { return DiffuseTexture.Sample(DiffuseSampler, uv); }
float4 SampleChrome02Sampler(float2 uv) { return Chrome02Texture.Sample(Chrome02Sampler, uv); }
float4 SampleShiny01Sampler(float2 uv) { return Shiny01Texture.Sample(Shiny01Sampler, uv); }
float4 SampleChrome01Sampler(float2 uv) { return Chrome01Texture.Sample(Chrome01Sampler, uv); }
float4 SampleShadowSampler(float2 uv) { return ShadowMap.Sample(ShadowSampler, uv); }
#define tex2D(s, uv) Sample##s(uv)
#define PS_COLOR SV_Target
#else
#define PS_COLOR COLOR
#endif

UNIFORM_DEFAULT(int, ItemOptions, 0);
UNIFORM_DEFAULT(int, SourceMeshIndex, 0);
UNIFORM_DEFAULT(int, ItemMaterialGroup, -1);
UNIFORM_DEFAULT(int, ItemMaterialIndex, -1);
UNIFORM_DEFAULT(float, HighLevelTexturesAvailable, 0.0);
UNIFORM_DEFAULT(float, Time, 0);
UNIFORM_DEFAULT(float, Alpha, 1.0);
UNIFORM_DEFAULT(float3, GlowColor, float3(0.6, 0.5, 0.0));
UNIFORM_DEFAULT(bool, IsAncient, false);
UNIFORM_DEFAULT(bool, IsExcellent, false);
float4x4 LightViewProjection;
UNIFORM_DEFAULT(float2, ShadowMapTexelSize, float2(1.0 / 2048.0, 1.0 / 2048.0));
UNIFORM_DEFAULT(float, ShadowBias, 0.0015);
UNIFORM_DEFAULT(float, ShadowNormalBias, 0.0025);
UNIFORM_DEFAULT(float, ShadowsEnabled, 0.0); // OpenGL compatible: use 0.0/1.0 instead of bool
UNIFORM_DEFAULT(float, ShadowStrength, 0.5);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

#if !OPENGL
struct VertexShaderInputSkinned
{
    float3 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : COLOR0;
    float2 BoneIndices : TEXCOORD1;
};
#endif

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TextureCoordinate : TEXCOORD2;
    float3 ViewDirection : TEXCOORD3;
};

struct VertexShaderOutputFast
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float2 TextureCoordinate : TEXCOORD2;
};



float SampleShadow(float3 worldPos, float3 normal)
{
    // ShadowsEnabled is uniform for the whole draw call. Avoid all nine
    // shadow-map samples when this material is rendered without shadows.
    if (ShadowsEnabled < 0.5)
        return 1.0;

    float4 lightPos = mul(float4(worldPos, 1.0), LightViewProjection);
    float3 proj = lightPos.xyz / lightPos.w;
    float2 uv = proj.xy * 0.5 + 0.5;
    float depth = proj.z * 0.5 + 0.5;

    // Branchless bounds check
    float2 uvClamped = saturate(uv);
    float inBounds = step(abs(uv.x - uvClamped.x) + abs(uv.y - uvClamped.y), 0.0001);

    float ndotl = saturate(dot(normal, -LightDirection));
    float bias = ShadowBias + ShadowNormalBias * (1.0 - ndotl);

    float2 offset = ShadowMapTexelSize * 0.5;
    float4 depths;
    depths.x = tex2D(ShadowSampler, uv + float2(-offset.x, -offset.y)).r;
    depths.y = tex2D(ShadowSampler, uv + float2( offset.x, -offset.y)).r;
    depths.z = tex2D(ShadowSampler, uv + float2(-offset.x,  offset.y)).r;
    depths.w = tex2D(ShadowSampler, uv + float2( offset.x,  offset.y)).r;
    float4 visibility = step(depth - bias, depths);
    float shadow = dot(visibility, float4(0.25, 0.25, 0.25, 0.25));
    return lerp(1.0, shadow, inBounds);
}

VertexShaderOutput BuildMaterialVertex(float4 localPosition, float3 localNormal, float2 textureCoordinate)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 worldPosition = mul(localPosition, World);

    output.Position = mul(localPosition, WorldViewProjection);
    output.WorldPosition = worldPosition.xyz;
    output.Normal = normalize(mul(localNormal, (float3x3)World));
    output.TextureCoordinate = textureCoordinate;
    output.ViewDirection = normalize(EyePosition - worldPosition.xyz);
    return output;
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    return BuildMaterialVertex(input.Position, input.Normal, input.TextureCoordinate);
}

#if !OPENGL
VertexShaderOutput MainVS_Skinned(in VertexShaderInputSkinned input)
{
    int positionBoneIndex = min(max((int)input.BoneIndices.x, 0), 255);
    int normalBoneIndex = min(max((int)input.BoneIndices.y, 0), 255);
    float4 localPosition = mul(float4(input.Position, 1.0), BoneMatrices[positionBoneIndex]);
    float3 localNormal = mul(input.Normal, (float3x3)BoneMatrices[normalBoneIndex]);
    return BuildMaterialVertex(localPosition, localNormal, input.TextureCoordinate);
}
#endif

VertexShaderOutputFast BuildMaterialVertexFast(float4 localPosition, float3 localNormal, float2 textureCoordinate)
{
    VertexShaderOutputFast output = (VertexShaderOutputFast)0;
    float4 worldPosition = mul(localPosition, World);
    output.Position = mul(localPosition, WorldViewProjection);
    output.WorldPosition = worldPosition.xyz;
    output.Normal = normalize(mul(localNormal, (float3x3)World));
    output.TextureCoordinate = textureCoordinate;
    return output;
}

VertexShaderOutputFast MainVS_Fast(in VertexShaderInput input)
{
    return BuildMaterialVertexFast(input.Position, input.Normal, input.TextureCoordinate);
}

#if !OPENGL
VertexShaderOutputFast MainVS_FastSkinned(in VertexShaderInputSkinned input)
{
    int positionBoneIndex = min(max((int)input.BoneIndices.x, 0), 255);
    int normalBoneIndex = min(max((int)input.BoneIndices.y, 0), 255);
    float4 localPosition = mul(float4(input.Position, 1.0), BoneMatrices[positionBoneIndex]);
    float3 localNormal = mul(input.Normal, (float3x3)BoneMatrices[normalBoneIndex]);
    return BuildMaterialVertexFast(localPosition, localNormal, input.TextureCoordinate);
}
#endif


UNIFORM_DEFAULT(float, SourceLevel, 0);
UNIFORM_DEFAULT(float3, SourceGlow, float3(1,.5,0));
UNIFORM_DEFAULT(float3, SourceShine, float3(1,1,1));
UNIFORM_DEFAULT(float3, SourceAncient, float3(.1,.6,1));
UNIFORM_DEFAULT(float, SourceExcellent, 1);
UNIFORM_DEFAULT(float, SourceGlowMask, 1);
UNIFORM_DEFAULT(float, SourceShineMask, 1);
UNIFORM_DEFAULT(float, SourceWhiteShine, 0);

UNIFORM_DEFAULT(float, SourceBadge, 0);
UNIFORM_DEFAULT(float, SourcePass, -1);
UNIFORM_DEFAULT(float, SourceWingIndex, -1);
UNIFORM_DEFAULT(float, SourceWingMesh, -1);
UNIFORM_DEFAULT(float, SourceEmissionGain, 1);
UNIFORM_DEFAULT(float, SourceUnlit, 0);
UNIFORM_DEFAULT(float, SourceStormMesh, -1);
UNIFORM_DEFAULT(float, SourceGrade, 0);
UNIFORM_DEFAULT(float, SourcePhoenixInventoryMesh, -1);
UNIFORM_DEFAULT(float, SourceMetalDraws, 1);

// MuMain RenderPartObjectEffect: base, CHROME, METAL, CHROME2/4 and option pass.
float4 SourceItemPixel(float3 position, float3 normal, float2 uv)
{
    if(SourceStormMesh>.5 && SourceStormMesh<1.5) uv.x+=floor(fmod(Time*25,16)/4)*.25;
    if(SourcePhoenixInventoryMesh>.5) uv.x-=frac(Time)*.09;
    float4 diffuse=tex2D(DiffuseSampler,uv);
    if(SourceWingIndex>=0 && SourcePass>=10)
    {
        normal=normalize(normal);
        if(SourcePass==10)
        {
            float pulse=SourceWingIndex==4 ? sin(Time)+1.1 : (sin(Time)+1)*.25;
            return float4(diffuse.rgb*((SourceWingIndex==4 || SourceWingIndex==5) && SourceEmissionGain>1 ? pulse : 1),diffuse.a*Alpha);
        }
        if(SourceWingIndex==39)
        {
            float pulse=abs(sin(Time));
            return float4((SourceWingMesh<.5?diffuse.rgb*(.1+pulse*.3):tex2D(Chrome01Sampler,uv).rgb*pulse*.8)*Alpha,Alpha);
        }
        float wave=frac(Time*.1);
        float2 chrome=SourceWingIndex==42
            ? float2((normal.z+normal.x)*.8+(frac(Time*.2)*1.2-.4)*2,normal.y+normal.x+(frac(Time*.2)*1.2-.4)*3)
            : float2(normal.z*.5+wave,normal.y*.5+wave*2);
        float3 color=SourceWingIndex==6?float3(.8,.6,1)*.5:float3(1,1,1);
        // Dimension samples CHROME; Darkness explicitly samples CHROME+1.
        float3 layer=SourceWingIndex==43?tex2D(Chrome01Sampler,chrome).rgb:tex2D(Chrome02Sampler,chrome).rgb;
        return float4(layer*color*Alpha,Alpha);
    }
    if(SourcePass<=0 && diffuse.a<.1)discard;
    if(SourceStormMesh>=0)
    {
        // Mesh 1 is MuMain's four-frame gold lightning across the membrane.
        // Boost that layer independently from edge highlights and +15 armor.
        float3 stormTint = SourceStormMesh>.5 && SourceStormMesh<1.5
            ? float3(.9,.6,.3)
            : float3(1,.7,.5);
        // Preserve MuMain's gold balance; boosting every channel above one burns it white.
        // The membrane overlay gets the shared gain after the dark body has been drawn.
        float stormGain = SourceStormMesh>.5 && SourceStormMesh<1.5 ? SourceEmissionGain : 1;
        return float4(diffuse.rgb*stormTint*stormGain,diffuse.a*Alpha);
    }
    if(SourceBadge>.5)
    {
        float wave2=frac(Time*.2)*1.2-.4;
        float2 chrome=float2((normal.z+normal.x)*.8+wave2*2,normal.y+normal.x+wave2*3);
        return float4(diffuse.rgb+tex2D(Chrome02Sampler,chrome).rgb*float3(1,.5,0),diffuse.a*Alpha);
    }
    if(SourceGrade>.5)
    {
        normal=normalize(normal);
        float luminance=.8-abs(sin(Time*1.8)*.5);
        float3 direction=float3(cos(Time),sin(Time*2),1);
        float d=dot(normal,direction);
        float2 chrome=float2(d+normal.y*.5+direction.y*3,1-d-normal.z*.5-frac(Time*.1)*3);
        float3 grade=diffuse.rgb*(luminance-.1);
        grade+=tex2D(Chrome01Sampler,uv+float2(abs(sin(Time*.5)),0)).rgb*(luminance-.3);
        grade+=tex2D(Chrome02Sampler,chrome).rgb;
        return float4(grade*1.6,diffuse.a*Alpha);
    }
    if(SourcePhoenixInventoryMesh>.5)
    {
        float pulse=(sin(Time*3)+1)*.3+.4;
        float2 chrome=float2(normal.z*.5+frac(Time*.1),normal.y*.5+frac(Time*.1)*2);
        return float4(diffuse.rgb+tex2D(Chrome01Sampler,chrome).rgb*pulse,diffuse.a*Alpha);
    }
    float options=max(0,(float)ItemOptions);
    float inputLevel=options-floor(options/16)*16;
    float level=SourceLevel,excellentAllowed=SourceExcellent,glowMask=SourceGlowMask,shineMask=SourceShineMask,whiteShine=SourceWhiteShine;
    float3 glow=SourceGlow,shine=SourceShine,ancient=SourceAncient;
    normal=normalize(normal);
    float light=max(.2,dot(normal,-LightDirection)*.8+.4);
    float3 bodyLight=SourceUnlit>.5?float3(1,1,1):float3(light,light,light);
    float3 result=diffuse.rgb*bodyLight*(level>=9?.9:level>=7?.8:1)*(level>=7?1.08:1);
    float3 baseColor=result;
    if(SourcePass==0) return float4(result*SourceEmissionGain,diffuse.a*Alpha);
    if(SourcePass>=0) result=float3(0,0,0);
    float wave=frac(Time*.1),wave2=frac(Time*.2)*1.2-.4;
    float2 chromeUv=float2(normal.z*.5+wave,normal.y*.5+wave*2);
    float2 metalUv=float2(normal.z*.5+.2,normal.y*.5+.5);
    if(HighLevelTexturesAvailable>.5 && level>=7)
    {
        if(SourcePass<0 || SourcePass==3)result+=tex2D(Chrome01Sampler,chromeUv).rgb*glow*glowMask;
        if(level>=9 && (SourcePass<0 || SourcePass==2))result+=tex2D(Shiny01Sampler,metalUv).rgb*glow*glowMask*SourceMetalDraws;
        if(level>=11 && (SourcePass<0 || SourcePass==1))
        {
            float2 primaryUv=float2((normal.z+normal.x)*.8+wave2*2,normal.y+normal.x+wave2*3);
            if(level>=13)
            {
                float3 animatedLight=float3(cos(Time),sin(Time*2),1);
                float d=dot(normal,animatedLight);
                primaryUv=float2(d+normal.y*.5+animatedLight.y*3,1-d-normal.z*.5-wave*3);
            }
            result+=tex2D(Chrome02Sampler,primaryUv).rgb*shine*shineMask;
        }
    }
    if((SourcePass<0 || SourcePass==4) && IsExcellent && excellentAllowed>.5)
    {
        float pulse=sin(Time*2)*.5+.5;
        result+=diffuse.rgb*float3(pulse,pulse*.3,1-pulse);
    }
    else if((SourcePass<0 || SourcePass==4) && IsAncient && HighLevelTexturesAvailable>.5)
    {
        float pulse=max(0,sin(Time)*.5+.4);
        float d=dot(normal,float3(0,-.1,-.8));
        result+=tex2D(Chrome02Sampler,float2(d,1-d)).rgb*ancient*pulse*shineMask;
    }
    float shadow=lerp(1-ShadowStrength,1,SampleShadow(position,normal));
    result=SourcePass<0 ? baseColor+(result-baseColor)*SourceEmissionGain : result*SourceEmissionGain;
    return float4(result*shadow*(SourcePass>0?Alpha:1),SourcePass>0?Alpha:diffuse.a*Alpha);
}
float4 MainPS_UpgradeFast(VertexShaderOutputFast input):PS_COLOR
{return SourceItemPixel(input.WorldPosition,input.Normal,input.TextureCoordinate);}
float4 MainPS(VertexShaderOutput input):PS_COLOR
{return SourceItemPixel(input.WorldPosition,input.Normal,input.TextureCoordinate);}

technique BasicColorDrawing_UpgradeFast
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS_Fast();
        PixelShader = compile PS_SHADERMODEL MainPS_UpgradeFast();
    }
}

#if !OPENGL
technique BasicColorDrawing_UpgradeFast_Skinned
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS_FastSkinned();
        PixelShader = compile PS_SHADERMODEL MainPS_UpgradeFast();
    }
}
#endif

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

#if !OPENGL
technique BasicColorDrawing_Skinned
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS_Skinned();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
#endif
