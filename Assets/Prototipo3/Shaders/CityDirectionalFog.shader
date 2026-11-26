Shader "Prototipo3/CityDirectionalFog"
{
    Properties
    {
        _FogColor ("Smog Core Color", Color) = (0.62, 0.59, 0.50, 0.52)
        _HorizonTint ("Smog Wisps Tint", Color) = (0.76, 0.72, 0.62, 1.0)
        _FogDensity ("Smog Density", Range(0.001, 0.12)) = 0.032
        _DirectionalIntensity ("Directional Forward Boost", Range(0.0, 3.0)) = 1.15
        _NightIntensity ("Night Intensity", Range(0.0, 1.0)) = 0.0
        _NearDistance ("Clear Bubble Radius", Float) = 5.8
        _FarDistance ("Full Smog Radius", Float) = 16.5
        _NoiseScale ("Smog Noise Scale", Float) = 0.085
        _NoiseSpeed ("Smog Drift Speed", Float) = 0.42
        _MaxAlpha ("Max Smog Alpha", Range(0.05, 0.95)) = 0.84
        _GroundLevelY ("Ground Y", Float) = 0.0
        _StreetFogHeight ("Street Pooling Height", Float) = 3.4
        _HeightFalloff ("Rooftop Height Falloff", Float) = 0.26
        _IndoorFade ("Indoor Attenuation", Range(0.0, 1.0)) = 1.0
        _LampWarmColor ("Streetlamp Halo Color", Color) = (1.0, 0.78, 0.40, 1.0)
        _LampClearStrength ("Streetlamp Clear Strength", Range(0.0, 0.8)) = 0.38
        _LampGlowStrength ("Streetlamp Glow Strength", Range(0.0, 1.5)) = 0.65
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+400"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "CityDirectionalFogPass"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
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
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _HorizonTint;
                float _FogDensity;
                float _DirectionalIntensity;
                float _NightIntensity;
                float _NearDistance;
                float _FarDistance;
                float _NoiseScale;
                float _NoiseSpeed;
                float _MaxAlpha;
                float _GroundLevelY;
                float _StreetFogHeight;
                float _HeightFalloff;
                float _IndoorFade;
                float4 _LampWarmColor;
                float _LampClearStrength;
                float _LampGlowStrength;
            CBUFFER_END

            float4 _PlayerPosition;
            float4 _PlayerForward;
            int _ActiveLampCount;
            float4 _ActiveLamps[8];

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
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    bool isSkyOrEmpty = (rawDepth <= 0.00001) || (rawDepth >= 0.99999);
                #else
                    bool isSkyOrEmpty = (rawDepth >= 0.99999) || (rawDepth <= 0.00001);
                #endif

                float3 camPos = GetCameraPositionWS();
                float3 rayDir = normalize(input.positionWS - camPos);
                float3 camForward = GetViewForwardDir();

                // Plano analítico de respaldo si el depth buffer apunta al cielo o un driver móvil devuelve profundidad vacía
                float groundDenom = min(-0.015, rayDir.y);
                float groundT = (_GroundLevelY - camPos.y) / groundDenom;
                float3 groundWorldPos = camPos + rayDir * clamp(groundT, 0.0, 110.0);

                float3 worldPos = groundWorldPos;
                if (!isSkyOrEmpty)
                {
                    float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float rayDist = eyeDepth / max(0.001, dot(rayDir, camForward));
                    worldPos = camPos + rayDir * rayDist;
                }

                float2 toPixelXZ = worldPos.xz - _PlayerPosition.xz;
                float distFromPlayer = length(toPixelXZ);
                float2 dirFromPlayer = toPixelXZ / max(0.001, distFromPlayer);
                float2 pForward = normalize(_PlayerForward.xz + float2(0.0001, 0.0001));

                // Manto animado de smog urbano en espacio mundial
                float smogNoise = FbmSmog(worldPos.xz);
                float swirlDetail = ValueNoise(worldPos.xz * (_NoiseScale * 1.45) + float2(-_Time.y * _NoiseSpeed * 0.42, _Time.y * _NoiseSpeed * 0.61));
                float combinedNoise = saturate(smogNoise * 0.72 + swirlDetail * 0.28);
                float noisePerturb = (combinedNoise - 0.5) * 2.2;

                // Burbuja orgánica despejada alrededor de Kilo (~5.8m) calibrada al encuadre isométrico
                float effectiveNear = max(3.6, _NearDistance + noisePerturb);
                float effectiveFar = max(effectiveNear + 3.5, _FarDistance);
                float radialFactor = smoothstep(effectiveNear, effectiveFar, distFromPlayer);

                float forwardDot = saturate(dot(dirFromPlayer, pForward));
                float directionalBoost = 1.0 + forwardDot * _DirectionalIntensity * _NightIntensity;

                // Estratificación vertical: smog denso estancado en calles/callejones (0..3.4m) y más tenue sobre azoteas
                float heightAboveGround = max(0.0, worldPos.y - _GroundLevelY);
                float streetPool = isSkyOrEmpty
                    ? 0.85
                    : lerp(0.32, 1.0, exp(-max(0.0, heightAboveGround - _StreetFogHeight * 0.35) * _HeightFalloff));

                // Bruma atmosférica sutil por distancia de cámara + niebla densa de noche
                float dayVsNightDensity = lerp(0.45, 1.0, _NightIntensity);
                float camDist = isSkyOrEmpty ? 65.0 : length(worldPos - camPos);
                float depthHaze = 1.0 - exp(-max(0.0, camDist - 13.0) * (_FogDensity * dayVsNightDensity));

                float smogWisps = lerp(0.72, 1.28, combinedNoise);
                float rawFog = max(radialFactor * directionalBoost, depthHaze * 0.85) * smogWisps * streetPool;

                // Mantener nítido el círculo inmediato alrededor del hámster
                rawFog *= smoothstep(effectiveNear * 0.74, effectiveNear + 2.4, distFromPlayer);

                // Halos cálidos de vapor de sodio bajo hasta 8 faroles encendidos cercanos (solo 1 pase)
                float lampGlowTotal = 0.0;
                float lampClearTotal = 0.0;
                if (_NightIntensity > 0.01 && _ActiveLampCount > 0)
                {
                    [loop]
                    for (int l = 0; l < 8; l++)
                    {
                        if (l >= _ActiveLampCount) break;
                        float4 lamp = _ActiveLamps[l];
                        float2 dXZ = worldPos.xz - lamp.xz;
                        float distSq = dot(dXZ, dXZ);
                        // Halo ~7.5m de radio sobre la calle (56.25 = 7.5^2)
                        float atten = saturate(1.0 - distSq / 56.25);
                        atten *= atten * lamp.w;
                        float hMask = saturate(1.0 - max(0.0, heightAboveGround - 1.5) * 0.22);
                        lampGlowTotal += atten * hMask;
                        lampClearTotal = max(lampClearTotal, atten * hMask);
                    }
                }

                rawFog *= (1.0 - saturate(lampClearTotal * _LampClearStrength));

                float finalAlpha = saturate(rawFog * _FogColor.a);
                finalAlpha = min(finalAlpha, _MaxAlpha) * _IndoorFade;

                if (finalAlpha <= 0.004)
                {
                    discard;
                }

                float3 finalRgb = lerp(_FogColor.rgb, _HorizonTint.rgb, combinedNoise * 0.45) * lerp(0.90, 1.12, combinedNoise);
                finalRgb = lerp(finalRgb, _LampWarmColor.rgb, saturate(lampGlowTotal * _LampGlowStrength * _NightIntensity));

                return float4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
