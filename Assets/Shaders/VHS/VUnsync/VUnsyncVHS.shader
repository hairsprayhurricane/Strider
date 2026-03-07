Shader "Hidden/VUnsyncVHS"
{
    Properties
    {
        _ValueX("Height Shift", Range(-1,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "VUnsyncVHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ValueX;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float shiftedY = uv.y + _ValueX;
                float wrappedY;
                if      (shiftedY > 1.0) wrappedY = shiftedY - 1.0;
                else if (shiftedY < 0.0) wrappedY = shiftedY + 1.0;
                else                     wrappedY = shiftedY;

                float2 wrappedUV = float2(uv.x, wrappedY);

                float3 m = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
                float3 p = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, wrappedUV).xyz;

                if ((_ValueX < 0 && -uv.y + _ValueX < _ValueX) ||
                    (_ValueX > 0 &&  1.0 - uv.y + _ValueX > _ValueX))
                    return float4(m, 1.0);
                else
                    return float4(p, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
