Shader "Hidden/TintPostProcessVHS"
{
    Properties
    {
        _ValueX("Y Shift", Float) = 1
        _ValueY("U Shift", Float) = 1
        _ValueZ("V Shift", Float) = 1
        _Switch("Swap UV", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "TintVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ValueX;
            float _ValueY;
            float _ValueZ;
            float _Switch;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 m  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;

                float y = ( 0.299 * m.r + 0.587 * m.g + 0.114 * m.b) * _ValueX;
                float u = (-0.147 * m.r - 0.289 * m.g + 0.436 * m.b) * _ValueY;
                float v = ( 0.615 * m.r - 0.515 * m.g - 0.100 * m.b) * _ValueZ;

                if (_Switch > 0) { float t = u; u = v; v = t; }

                float3 result = float3(
                    y + 1.140 * v,
                    y - 0.395 * u - 0.581 * v,
                    y + 2.032 * u
                );
                return float4(result, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
