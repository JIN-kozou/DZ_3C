Shader "Shader Graphs/AppearShader_RevealedOpaque"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Reveal("Reveal Along U", Range(0, 1)) = 1
        _RevealSoft("Reveal Soft", Range(0, 0.5)) = 0.04
        _AppearSphereEdgeSoft("Sphere Edge Soft", Range(0, 0.5)) = 0.08
        _RevealOpaqueThreshold("Alpha Clip Threshold", Range(0, 1)) = 0.02
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "AppearRevealLib.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Reveal;
                half _RevealSoft;
                half _AppearSphereEdgeSoft;
                half _RevealOpaqueThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            void ClipByRevealMask(float3 positionWS, float u, out half alpha)
            {
                AppearReveal_OpaqueSurfaceAlpha_float(
                    positionWS,
                    u,
                    _Reveal,
                    _RevealSoft,
                    _AppearSphereEdgeSoft,
                    alpha);
                clip(alpha - _RevealOpaqueThreshold);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half alpha;
                ClipByRevealMask(input.positionWS, input.uv.x, alpha);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo * (mainLight.color * ndotl + SampleSH(normalWS));
                half3 color = MixFog(diffuse, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AppearRevealLib.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Reveal;
                half _RevealSoft;
                half _AppearSphereEdgeSoft;
                half _RevealOpaqueThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half DepthFrag(Varyings input) : SV_TARGET
            {
                half alpha;
                AppearReveal_OpaqueSurfaceAlpha_float(
                    input.positionWS,
                    input.uv.x,
                    _Reveal,
                    _RevealSoft,
                    _AppearSphereEdgeSoft,
                    alpha);
                clip(alpha - _RevealOpaqueThreshold);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "AppearRevealLib.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Reveal;
                half _RevealSoft;
                half _AppearSphereEdgeSoft;
                half _RevealOpaqueThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_TARGET
            {
                half alpha;
                AppearReveal_OpaqueSurfaceAlpha_float(
                    input.positionWS,
                    input.uv.x,
                    _Reveal,
                    _RevealSoft,
                    _AppearSphereEdgeSoft,
                    alpha);
                clip(alpha - _RevealOpaqueThreshold);
                float3 normalWS = NormalizeNormalPerPixel(normalize(input.normalWS));
                return half4(normalWS, 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
