Shader "SnapshotProURP/Mosaic"
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
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			Texture2D _OverlayTex;

			float4 _OverlayColor;
			int _XTileCount;
			int _YTileCount;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord).rgb;

				float2 overlayUV = i.texcoord * float2(_XTileCount, _YTileCount);
				float4 overlayCol = SAMPLE_TEXTURE2D(_OverlayTex, sampler_LinearRepeat, overlayUV) * _OverlayColor;

				col = lerp(col, overlayCol.rgb, overlayCol.a);

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
