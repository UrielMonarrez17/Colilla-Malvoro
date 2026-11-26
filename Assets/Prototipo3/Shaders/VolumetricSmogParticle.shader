Shader "Prototipo3/VolumetricSmogParticle"
{
    Properties
    {
        _MainTex ("Cloud Puff Texture", 2D) = "white" {}
        _FogColor ("Cloud Core Color", Color) = (0.60, 0.57, 0.49, 0.30)
        _HorizonTint ("Cloud Highlight Color", Color) = (0.78, 0.74, 0.64, 1.0)
        _SoftParticlesFade ("Soft Particles Depth Fade", Range(0.1, 5.0)) = 1.6
        _ClearRadius ("Player Clear Radius", Float) = 4.5
        _NoiseScale ("Internal Turbulence Scale", Float) = 0.14
        _NoiseSpeed ("Internal Turbulence Speed", Float) = 0.35
        _NightIntensity ("Night Intensity", Range(0.0, 1.0)) = 0.0
        _IndoorFade ("Indoor Attenuation", Range(0.0, 1.0)) = 1.0
        _MaxAlpha ("Max Cloud Alpha", Range(0.05, 0.90)) = 0.65
        _LampWarmColor ("Streetlamp Halo Color", Color) = (1.0, 0.78, 0.40, 1.0)
        _LampClearStrength ("Streetlamp Clear Strength", Range(0.0, 0.8)) = 0.30
        _LampGlowStrength ("Streetlamp Glow Strength", Range(0.0, 2.0)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+200"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "VolumetricSmogParticlePass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FogColor;
                float4 _HorizonTint;
                float _SoftParticlesFade;
                float _ClearRadius;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NightIntensity;
                float _IndoorFade;
                float _MaxAlpha;
                float4 _LampWarmColor;
                float _LampClearStrength;
                float _LampGlowStrength;
            CBUFFER_END

            float4 _PlayerPosition;
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

            float Turbulence2Oct(float2 xz)
            {
                float2 d1 = float2(_Time.y * _NoiseSpeed, _Time.y * 0.43 * _NoiseSpeed);
                float2 d2 = float2(-_Time.y * 0.61 * _NoiseSpeed, _Time.y * 0.31 * _NoiseSpeed);
                float n1 = ValueNoise(xz * _NoiseScale + d1);
                float n2 = ValueNoise(xz * (_NoiseScale * 2.25) + d2);
                return n1 * 0.65 + n2 * 0.35;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.eyeDepth = -vertexInput.positionVS.z;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 puffSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float wispNoise = Turbulence2Oct(input.positionWS.xz);
                float shapeMask = saturate(puffSample.a * lerp(0.72, 1.28, wispNoise));

                // Soft-Particles para difuminar contra calles y paredes sin cortes planos
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);
                #if UNITY_REVERSED_Z
                    bool isSkyOrEmpty = (rawDepth <= 0.00001) || (rawDepth >= 0.99999);
                #else
                    bool isSkyOrEmpty = (rawDepth >= 0.99999) || (rawDepth <= 0.00001);
                #endif

                float softFade = 1.0;
                if (!isSkyOrEmpty)
                {
                    float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    softFade = saturate((sceneEyeDepth - input.eyeDepth) / max(0.15, _SoftParticlesFade));
                }

                // Burbuja despejada suave (~4.5m) alrededor de Kilo
                float distToPlayer = length(input.positionWS.xz - _PlayerPosition.xz);
                float playerClearMask = smoothstep(_ClearRadius * 0.42, _ClearRadius + 1.8, distToPlayer);

                // Sombreado esférico volumétrico simulado en cada nube
                float2 centeredUV = input.uv * 2.0 - 1.0;
                float dome = sqrt(saturate(1.0 - dot(centeredUV, centeredUV)));
                float3 cloudRgb = lerp(_HorizonTint.rgb, _FogColor.rgb, dome * 0.65) * lerp(0.92, 1.10, wispNoise);

                // Dispersión cálida de vapor de sodio bajo hasta 8 faroles encendidos cercanos de noche
                float lampGlowTotal = 0.0;
                float lampClearTotal = 0.0;
                if (_NightIntensity > 0.01 && _ActiveLampCount > 0)
                {
                    [loop]
                    for (int l = 0; l < 8; l++)
                    {
                        if (l >= _ActiveLampCount) break;
                        float4 lamp = _ActiveLamps[l];
                        float2 dXZ = input.positionWS.xz - lamp.xz;
                        float distSq = dot(dXZ, dXZ);
                        float atten = saturate(1.0 - distSq / 64.0);
                        atten *= atten * lamp.w;
                        lampGlowTotal += atten;
                        lampClearTotal = max(lampClearTotal, atten);
                    }
                }

                cloudRgb = lerp(cloudRgb, _LampWarmColor.rgb, saturate(lampGlowTotal * _LampGlowStrength * _NightIntensity));
                float lampAlphaMul = 1.0 - saturate(lampClearTotal * _LampClearStrength);

                float alpha = shapeMask * input.color.a * _FogColor.a * softFade * playerClearMask * lampAlphaMul * _IndoorFade;
                alpha = min(alpha, _MaxAlpha);

                if (alpha <= 0.003)
                {
                    discard;
                }

                return half4(cloudRgb * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
