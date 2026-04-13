Shader "Custom/ShadowSampler"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ShadowSample"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Обязательно — иначе shadow map не будет доступен
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Главный свет ---
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float mainShadow     = mainLight.shadowAttenuation;   // 0=тень, 1=свет
                float mainAttenuation = mainLight.distanceAttenuation;
                float mainContrib    = mainShadow * mainAttenuation * mainLight.color.r; // яркость

                // --- Дополнительные источники ---
                float additionalContrib = 0.0;
                uint additionalCount = GetAdditionalLightsCount();

                for (uint i = 0u; i < additionalCount; i++)
                {
                    // half4(1,1,1,1) = нет shadow mask, стандартно для realtime
                    Light light = GetAdditionalLight(i, IN.positionWS, half4(1,1,1,1));
                    float contrib = light.shadowAttenuation
                                  * light.distanceAttenuation
                                  * dot(light.color, float3(0.299, 0.587, 0.114)); // luminance
                    additionalContrib += contrib;
                }

                // R = тень главного света (0=тень, 1=свет)
                // G = суммарный вклад доп. источников
                // B = пусто (можно добавить ambient)
                return half4(mainShadow, saturate(additionalContrib), 0.0, 1.0);
            }
            ENDHLSL
        }
    }
}