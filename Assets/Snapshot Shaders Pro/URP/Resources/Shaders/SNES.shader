Shader "SnapshotProURP/SNES"
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

			#define EPSILON 1e-06

			int _BandingLevels;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
				col = pow(col, _PowerRamp);

				int r = (col.r - EPSILON) * _BandingLevels;
				int g = (col.g - EPSILON) * _BandingLevels;
				int b = (col.b - EPSILON) * _BandingLevels;

				float divisor = _BandingLevels - 1.0f;

                return float4(r / divisor, g / divisor, b / divisor, 1.0f);
            }
            ENDHLSL
        }
    }
}
