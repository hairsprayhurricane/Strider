Shader "Custom/Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Transparent" "Queue"="Overlay" }

        Pass
        {
            Name "Outline"
            Cull Back
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 normalCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0.0));

                float2 offset = normalize(normalCS.xy);
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                clipPos.xy += offset * _OutlineWidth * clipPos.w;

                OUT.positionHCS = clipPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
