Shader "SnapshotProURP/Drawing"
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

			Texture2D _DrawingTex;

			float _OverlayOffset;
			float _Strength;
			float _Tiling;
			float _Smudge;
			float _DepthThreshold;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 drawingUV = (i.texcoord + _OverlayOffset) * _Tiling;
				drawingUV.y *= _ScreenParams.y / _ScreenParams.x;

				float3 drawingCol = (SAMPLE_TEXTURE2D(_DrawingTex, sampler_LinearRepeat, drawingUV).rgb +
					SAMPLE_TEXTURE2D(_DrawingTex, sampler_LinearRepeat, drawingUV / 3.0f).rgb) / 2.0f;

				float2 texUV = i.texcoord + drawingCol * _Smudge;
				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, texUV).rgb;

				float lum = dot(col, float3(0.3f, 0.59f, 0.11f));
				float3 drawing = lerp(col, drawingCol * col, (1.0f - lum) * _Strength);

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif
				depth = Linear01Depth(depth, _ZBufferParams);

				return float4(depth < _DepthThreshold ? drawing : col, 1.0f);
            }
            ENDHLSL
        }
    }
}
