Shader "Hidden/BleedingColorsVHS"
{
    Properties
    {
        _Intensity("Intensity", Range(0,15)) = 3
        _ValueX("Shift", Range(-10,10)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "BleedingColorsVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _ValueX;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float3 m = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;

                float2 uvLeft  = float2(uv.x - _ValueX * 0.01f, uv.y);
                float2 uvRight = float2(uv.x + _ValueX * 0.01f, uv.y);

                float3 l = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvLeft).xyz;
                float3 r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvRight).xyz;

                float my = 0.299 * m.r + 0.587 * m.g + 0.114 * m.b;
                float lu = -0.147 * l.r - 0.289 * l.g + 0.436 * l.b;
                float rv =  0.615 * r.r - 0.515 * r.g - 0.100 * r.b;

                float3 mixed = float3(
                    my + 1.140 * rv,
                    my - 0.395 * lu - 0.581 * rv,
                    my + 2.032 * lu
                );

                return float4(lerp(m, mixed, _Intensity), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
