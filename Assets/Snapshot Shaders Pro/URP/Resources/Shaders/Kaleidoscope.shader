Shader "SnapshotProURP/Kaleidoscope"
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

			float _SegmentCount;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 shiftUV = _ScreenParams.xy * (i.texcoord - 0.5f);

				float radius = sqrt(dot(shiftUV, shiftUV));
				float angle = atan2(shiftUV.y, shiftUV.x);

				float segmentAngle = PI * 2.0f / _SegmentCount;
				angle -= segmentAngle * floor(angle / segmentAngle);
				angle = min(angle, segmentAngle - angle);

				float2 uv = float2(cos(angle), sin(angle)) * radius + _ScreenParams.xy / 2.0f;
				uv = max(min(uv, _ScreenParams.xy * 2.0f - uv), -uv) / _ScreenParams.xy;

				return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
