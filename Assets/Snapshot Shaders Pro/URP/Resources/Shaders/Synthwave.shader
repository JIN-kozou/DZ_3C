Shader "SnapshotProURP/Synthwave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
			#pragma multi_compile __ USE_SCENE_TEXTURE_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#define EPSILON 1e-06

			float4 _BackgroundColor;
			float4 _LineColor1;
			float4 _LineColor2;
			float _LineColorMix;
			float _LineWidth;
			float _LineFalloff;
			float3 _GapWidth;
			float3 _Offset;
			float3 _AxisMask;

			float4 frag(Varyings i) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
				float skyboxCheck = depth;
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
				float skyboxCheck = 1.0f - depth;
#endif

				float4 pixelPositionCS = float4(i.texcoord * 2.0f - 1.0f, depth, 1.0f);

#if UNITY_UV_STARTS_AT_TOP
				pixelPositionCS.y = -pixelPositionCS.y;
#endif

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP) + _Offset;

				float3 modWorldPos = fmod(abs(worldPos) + _GapWidth / 2.0f, _GapWidth);

				float3 distWorldPos = abs((_GapWidth / 2.0f) - modWorldPos);

				float3 stepWorldPos = 1.0f - smoothstep(_LineWidth, _LineWidth + _LineFalloff, distWorldPos);
				stepWorldPos *= _AxisMask;

				float lines = saturate(dot(float3(1.0f, 1.0f, 1.0f), stepWorldPos));

				// Fix for weird issues with the skybox.
				if (skyboxCheck < EPSILON)
				{
					lines = 0.0f;
				}

#ifdef USE_SCENE_TEXTURE_ON
				float4 background = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
#else
				float4 background = _BackgroundColor;
#endif
				float4 lineColor = lerp(_LineColor1, _LineColor2, pow(i.texcoord.y, _LineColorMix));
				return lerp(background, lineColor, lines);
			}
			ENDHLSL
		}
    }
}
