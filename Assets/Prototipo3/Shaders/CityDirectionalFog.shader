Shader "Prototipo3/CityDirectionalFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.06, 0.09, 0.14, 0.82)
        _FogDensity ("Fog Density", Range(0.001, 0.1)) = 0.022
        _DirectionalIntensity ("Directional Forward Boost", Range(0.0, 3.0)) = 1.25
        _NightIntensity ("Night Intensity", Range(0.0, 1.0)) = 0.0
        _NearDistance ("Clear Radius", Float) = 11.5
        _FarDistance ("Far Distance", Float) = 32.0
        _NoiseScale ("Smog Noise Scale", Float) = 0.065
        _NoiseSpeed ("Smog Drift Speed", Float) = 0.35
        _MaxAlpha ("Max Smog Alpha", Range(0.1, 0.95)) = 0.80
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+400"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CityDirectionalFogPass"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogDensity;
                float _DirectionalIntensity;
                float _NightIntensity;
                float _NearDistance;
                float _FarDistance;
                float _NoiseScale;
                float _NoiseSpeed;
                float _MaxAlpha;
            CBUFFER_END

            float4 _PlayerPosition;
            float4 _PlayerForward;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FbmSmog(float2 xz)
            {
                float2 drift1 = float2(_Time.y * _NoiseSpeed, _Time.y * 0.37 * _NoiseSpeed);
                float2 drift2 = float2(-_Time.y * 0.55 * _NoiseSpeed, _Time.y * 0.28 * _NoiseSpeed);
                float n1 = ValueNoise(xz * _NoiseScale + drift1);
                float n2 = ValueNoise(xz * (_NoiseScale * 2.15) + drift2);
                float n3 = ValueNoise(xz * (_NoiseScale * 4.30) - drift1 * 0.6);
                return n1 * 0.55 + n2 * 0.30 + n3 * 0.15;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    bool isSky = (rawDepth <= 0.00001);
                #else
                    bool isSky = (rawDepth >= 0.99999);
                #endif

                float3 worldPos = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);

                float2 toPixelXZ = worldPos.xz - _PlayerPosition.xz;
                float distFromPlayer = length(toPixelXZ);
                float2 dirFromPlayer = toPixelXZ / max(0.001, distFromPlayer);
                float2 pForward = normalize(_PlayerForward.xz + float2(0.0001, 0.0001));

                // Manto animado de smog en espacio mundial
                float smogNoise = FbmSmog(isSky ? (_WorldSpaceCameraPos.xz + dirFromPlayer * 35.0) : worldPos.xz);
                float noisePerturb = (smogNoise - 0.5) * 3.8;

                // Bolsillo despejado alrededor del hámster (~11.5 m)
                float effectiveNear = max(5.0, _NearDistance + noisePerturb * 0.65);
                float effectiveSpan = max(4.0, _FarDistance - _NearDistance);
                float radialFactor = smoothstep(effectiveNear, effectiveNear + effectiveSpan, distFromPlayer);

                float forwardDot = saturate(dot(dirFromPlayer, pForward));
                float directionalBoost = 1.0 + forwardDot * _DirectionalIntensity * _NightIntensity;

                // Niebla suave de día + smog denso y móvil de noche
                float dayVsNightDensity = lerp(0.32, 1.0, _NightIntensity);
                float camDist = isSky ? 90.0 : length(worldPos - _WorldSpaceCameraPos);
                float depthHaze = 1.0 - exp(-max(0.0, camDist - 12.0) * (_FogDensity * dayVsNightDensity));

                float smogWisps = lerp(0.76, 1.24, smogNoise);
                float rawFog = max(radialFactor * directionalBoost, depthHaze) * smogWisps;

                // Mantener despejada el área cercana al personaje incluso con profundidad de cámara
                rawFog *= smoothstep(effectiveNear * 0.72, effectiveNear + 3.5, distFromPlayer);

                float finalAlpha = saturate(rawFog * _FogColor.a);
                finalAlpha = min(finalAlpha, _MaxAlpha);

                if (finalAlpha <= 0.004)
                {
                    discard;
                }

                float3 finalRgb = _FogColor.rgb * lerp(0.92, 1.12, smogNoise);
                return float4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
