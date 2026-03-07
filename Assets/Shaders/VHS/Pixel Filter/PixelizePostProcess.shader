Shader "Hidden/PixelizePostProcess"
{
    Properties
    {
        _PixelSize("Pixel Size", Float) = 10.0
        _Intensity("Intensity", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "PixelizePostProcess"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;
            float _Intensity;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv           = input.texcoord;
                float2 pixelatedUV  = floor(uv * _PixelSize) / _PixelSize;

                float3 pixelated = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixelatedUV).xyz;
                float3 original  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;

                return float4(lerp(original, pixelated, _Intensity), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
