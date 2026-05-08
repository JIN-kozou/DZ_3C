Shader "SnapshotProURP/WorldScan"
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
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			Texture2D _OverlayRampTex;

			float3 _ScanOrigin;
			float _ScanDist;
			float _ScanWidth;
			float4 _OverlayColor;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP);

				float fragDist = distance(worldPos, _ScanOrigin);

				float4 scanColor = 0.0f;

				if (fragDist < _ScanDist && fragDist > _ScanDist - _ScanWidth)
				{
					float scanUV = (fragDist - _ScanDist) / (_ScanWidth * 1.01f);

					scanColor = SAMPLE_TEXTURE2D(_OverlayRampTex, sampler_LinearRepeat, float2(scanUV, 0.5f));
					scanColor *= _OverlayColor;
				}

				float4 textureSample = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

				return lerp(textureSample, scanColor, scanColor.a);
            }
            ENDHLSL
        }
    }
}
