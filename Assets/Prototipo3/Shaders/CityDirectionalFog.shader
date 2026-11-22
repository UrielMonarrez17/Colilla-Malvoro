Shader "Prototipo3/CityDirectionalFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.04, 0.06, 0.11, 0.95)
        _FogDensity ("Fog Density", Range(0.001, 0.1)) = 0.025
        _DirectionalIntensity ("Directional Forward Boost", Range(0.0, 3.0)) = 1.6
        _NightIntensity ("Night Intensity", Range(0.0, 1.0)) = 1.0
        _NearDistance ("Near Distance", Float) = 2.0
        _FarDistance ("Far Distance", Float) = 35.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+500" 
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
            CBUFFER_END

            float4 _PlayerPosition;
            float4 _PlayerForward;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                if (_NightIntensity <= 0.001)
                {
                    discard;
                }

                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    bool isSky = (rawDepth <= 0.00001);
                #else
                    bool isSky = (rawDepth >= 0.99999);
                #endif

                float3 worldPos = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float camDist = length(worldPos - _WorldSpaceCameraPos);

                float3 toPixel = worldPos - _PlayerPosition.xyz;
                float distFromPlayer = length(toPixel.xz);
                float2 dirFromPlayer = toPixel.xz / max(0.001, distFromPlayer);
                float2 pForward = normalize(_PlayerForward.xz + float2(0.0001, 0.0001));

                float forwardDot = dot(dirFromPlayer, pForward);
                float directionalFactor = saturate(forwardDot);

                float distFactor = saturate((distFromPlayer - _NearDistance) / max(0.1, _FarDistance));
                float directionalFog = distFactor * (1.0 + directionalFactor * _DirectionalIntensity);
                
                float depthFog = 1.0 - exp(-camDist * _FogDensity);
                float combinedFog = saturate(max(depthFog, directionalFog * 0.85));

                if (isSky)
                {
                    combinedFog = 0.85;
                }

                float finalAlpha = combinedFog * _NightIntensity * _FogColor.a;
                return float4(_FogColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
