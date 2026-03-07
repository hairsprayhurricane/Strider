Shader "Hidden/DistortionVHS"
{
    Properties
    {
        _Intensity("Intensity", Range(0,1)) = 0.01
        _ValueX("Noise value", Range(0,10)) = 4.51
        _Texture("Displacement map", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "DistortionVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float    _Intensity;
            float    _ValueX;
            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);

            float rand(float co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 n  = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv).xy;
                uv.x += n.x * rand(_ValueX + uv.x) * _Intensity;
                float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
