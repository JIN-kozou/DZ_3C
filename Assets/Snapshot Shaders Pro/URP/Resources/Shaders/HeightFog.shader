Shader "SnapshotProURP/HeightFog"
{
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

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _FogStrength;

			float _StartDistance;
			float3 _DistanceStartColor;
			float _EndDistance;
			float3 _DistanceEndColor;
			float _DistanceFalloff;

			float _StartHeight;
			float3 _HeightStartColor;
			float _EndHeight;
			float3 _HeightEndColor;
			float _HeightFalloff;

			// Credit to https://alexanderameye.github.io/outlineshader.html:
			float3 DecodeNormal(float4 enc)
			{
				float kScale = 1.7777;
				float3 nn = enc.xyz*float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP);

				float worldDistance = distance(worldPos, _WorldSpaceCameraPos);

				float distanceMask = saturate((worldDistance - _StartDistance) / (_EndDistance - _StartDistance));
				distanceMask = pow(distanceMask, _DistanceFalloff);

				float heightMask = saturate((worldPos.y - _StartHeight) / (_EndHeight - _StartHeight));
				heightMask = pow(heightMask, _HeightFalloff);

				float3 fogDistanceColor = lerp(_DistanceStartColor.rgb, _DistanceEndColor.rgb, distanceMask);
				float3 fogHeightColor = lerp(_HeightStartColor, _HeightEndColor, heightMask);

				col = lerp(col, fogDistanceColor, distanceMask * _FogStrength);
				col = lerp(col, fogHeightColor, heightMask * _FogStrength);

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
