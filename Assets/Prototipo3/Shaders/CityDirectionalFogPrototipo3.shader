Shader "Prototipo3/CityDirectionalFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.05, 0.04, 0.08, 0.95)
        _CrimsonTint ("Crimson Fog Tint", Color) = (0.4, 0.05, 0.08, 0.8)
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
            Name "CityDirectionalFogPrototipo3Pass"
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
                float3 viewRay : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _CrimsonTint;
                float _FogDensity;
                float _DirectionalIntensity;
                float _NightIntensity;
                float _NearDistance;
                float _FarDistance;
            CBUFFER_END

            float3 _PlayerWorldPos;
            float3 _PlayerForwardDir;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.viewRay = mul(unity_CameraToWorld, float4(input.positionOS.xy * _ProjectionParams.z, _ProjectionParams.z, 0.0)).xyz;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                #if defined(UNITY_REVERSED_Z)
                    float rawDepth = SampleSceneDepth(input.uv);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(input.uv));
                #endif

                float linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                if (linearEyeDepth >= _FarDistance * 4.0)
                {
                    float3 mixedSkyFog = lerp(_FogColor.rgb, _CrimsonTint.rgb, 0.25);
                    return float4(mixedSkyFog, _FogColor.a * _NightIntensity * 0.75);
                }

                float distFog = saturate((linearEyeDepth - _NearDistance) / (_FarDistance - _NearDistance));
                distFog = 1.0 - exp(-distFog * distFog * (_FogDensity * 120.0));

                float3 worldPos = _WorldSpaceCameraPos + normalize(input.viewRay) * linearEyeDepth;
                float3 dirFromPlayer = normalize(worldPos - _PlayerWorldPos);
                float dotForward = dot(dirFromPlayer, _PlayerForwardDir);

                float directionalFactor = 1.0 + saturate(dotForward) * _DirectionalIntensity;
                float finalFogFactor = saturate(distFog * directionalFactor * _NightIntensity);

                float3 blendedFog = lerp(_FogColor.rgb, _CrimsonTint.rgb, saturate(dotForward * 0.4));
                return float4(blendedFog, finalFogFactor * _FogColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
