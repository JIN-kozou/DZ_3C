// Shader created with Shader Forge — URP forward unlit-style emissive with main light shadows
Shader "Lessons RealTime VFX/remapTexAlphablend" {
    Properties {
        _ColorGrid ("Color Grid", Color) = (1,1,1,1)
        _ColorChecker01 ("Color Checker 01", Color) = (1,1,1,1)
        _ColorChecker02 ("Color Checker 02", Color) = (1,1,1,1)
        _MainTex ("MainTex", 2D) = "white" {}
        _ColorFog ("Color Fog", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _OffsetFog ("Offset Fog", Range(0, 50)) = 0
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }
        LOD 100

        Pass {
            Name "UniversalForwardOnly"
            Tags { "LightMode"="UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _LIGHT_COOKIES

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorChecker01;
                float4 _MainTex_ST;
                float4 _ColorFog;
                float _Intensity;
                float _OffsetFog;
                float4 _ColorChecker02;
                float4 _ColorGrid;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct Varyings {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 posWorld : TEXCOORD1;
            };

            Varyings vert (Attributes v) {
                Varyings o = (Varyings)0;
                o.uv0 = v.texcoord0;
                o.posWorld = TransformObjectToWorld(v.vertex.xyz);
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target {
                float4 _MainTex_var = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(i.uv0, _MainTex));
                float2 node_8564 = _MainTex_var.rgb.rg;
                float3 baseEmissive = lerp(lerp( lerp( _ColorGrid.rgb, _ColorChecker01.rgb, node_8564.r ), _ColorChecker02.rgb, node_8564.g ), _ColorFog.rgb, saturate(((i.posWorld.g + _OffsetFog) * _Intensity)));

                float4 shadowCoord = TransformWorldToShadowCoord(i.posWorld);
                Light mainLight = GetMainLight(shadowCoord);
                float attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                // Built-in multiplied by _LightColor0.a; URP main light uses float3 color — match prior look via attenuation only
                float3 finalColor = baseEmissive * attenuation;

                return half4(half3(finalColor), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
    CustomEditor "ShaderForgeMaterialInspector"
}
