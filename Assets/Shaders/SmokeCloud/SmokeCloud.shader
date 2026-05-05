Shader "Custom/SmokeCloud"
{
    Properties
    {
        _Color        ("Color",           Color)            = (0.85, 0.85, 0.85, 1)
        _NoiseScale   ("Noise Scale",     Float)            = 3.0
        _NoiseOffset  ("Noise Offset",    Vector)           = (0, 0, 0, 0)
        _ScrollSpeed  ("Scroll Speed",    Float)            = 0.04
        _Density      ("Density",         Range(0, 1))      = 0.75
        _EdgeSoftness ("Edge Softness",   Range(0.01, 0.6)) = 0.45
        _WarpStrength ("Warp Strength",   Range(0, 1))      = 0.45
        _DepthSoft    ("Depth Softness",  Range(0.1, 10))   = 2.0
        _Dissolve     ("Dissolve",        Range(0, 1))      = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SmokeCloudForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _NoiseScale;
                float4 _NoiseOffset;
                float  _ScrollSpeed;
                float  _Density;
                float  _EdgeSoftness;
                float  _WarpStrength;
                float  _DepthSoft;
                float  _Dissolve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                float  eyeDepth    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- Procedural gradient noise + FBM ----

            float2 SC_hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float SC_gnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = dot(SC_hash2(i),                f);
                float b = dot(SC_hash2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(SC_hash2(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(SC_hash2(i + float2(1, 1)), f - float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float SC_fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    v += a * SC_gnoise(p);
                    p  = p * 2.1 + float2(1.7, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            // ---- Vertex ----

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Spherical billboard: replace object rotation with camera axes
                float4x4 M      = GetObjectToWorldMatrix();
                float3 center   = M._m03_m13_m23;
                float  scaleX   = length(M._m00_m10_m20);
                float  scaleY   = length(M._m01_m11_m21);

                float3 camRight = normalize(UNITY_MATRIX_V[0].xyz);
                float3 camUp    = normalize(UNITY_MATRIX_V[1].xyz);

                float3 worldPos = center
                    + camRight * IN.positionOS.x * scaleX
                    + camUp    * IN.positionOS.y * scaleY;

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.uv          = IN.uv;
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                OUT.eyeDepth    = -TransformWorldToView(worldPos).z;

                return OUT;
            }

            // ---- Fragment ----

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centeredUV = IN.uv * 2.0 - 1.0;

                // Animated FBM cloud shape
                float2 scroll   = float2(_ScrollSpeed, _ScrollSpeed * 0.6) * _Time.y;
                float2 noiseUV  = IN.uv * _NoiseScale + _NoiseOffset.xy + scroll;
                float  cloud    = SC_fbm(noiseUV) * 0.5 + 0.5;

                // Warp the circular mask with low-frequency noise → irregular blob shape
                float2 warpUV = IN.uv * 1.4 + _NoiseOffset.xy * 0.05 + scroll * 0.15;
                float2 warp   = float2(
                    SC_gnoise(warpUV + float2(3.1, 7.4)),
                    SC_gnoise(warpUV + float2(8.3, 1.7))
                ) * _WarpStrength;
                float  edgeMask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, length(centeredUV + warp));

                // Base shape = cloud density shaped by edge mask
                float shape = cloud * edgeMask;

                // Dissolve: erodes cloud from edges inward as _Dissolve goes 0→1
                float dissolveEdge = 0.08;
                float alpha = saturate((shape - _Dissolve) / dissolveEdge) * _Density;

                // Soft particles — requires Depth Texture enabled in URP Renderer asset
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float  sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                alpha *= saturate((sceneDepth - IN.eyeDepth) / _DepthSoft);

                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
