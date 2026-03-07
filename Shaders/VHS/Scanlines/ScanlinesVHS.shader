Shader "Hidden/ScanlinesVHS"
{
    Properties
    {
        _Intensity("Intensity", Range(0,1)) = 0
        _Color("Scanline Color", Color) = (0,0,0,1)
        _ValueX("Lines Size", Range(1,10)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "ScanlinesVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _Intensity;
            float4 _Color;
            float  _ValueX;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float3 c  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;

                float screenHeight = _ScreenParams.y;
                if ((int)(uv.y * screenHeight / floor(_ValueX)) % 2 == 0)
                    return float4(c, 1.0);
                else
                    return float4(lerp(c, _Color.rgb, _Intensity), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
