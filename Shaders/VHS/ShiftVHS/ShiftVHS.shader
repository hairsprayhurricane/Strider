Shader "Hidden/ShiftVHS"
{
    Properties
    {
        _ValueX("Horizontal Shift", Range(-1,1)) = 0.1
        _ValueY("Vertical Shift", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "ShiftVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ValueX;
            float _ValueY;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                uv.x += _ValueX;
                uv.y += _ValueY;
                float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
