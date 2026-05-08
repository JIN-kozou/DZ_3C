Shader "SnapshotProURP/Posterize"
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

			int _RedLevels;
			int _GreenLevels;
			int _BlueLevels;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = saturate(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb);
				col = pow(col, _PowerRamp);

				int r = lerp(0.0f, 1.0f - EPSILON, col.r) * _RedLevels;
				int g = lerp(0.0f, 1.0f - EPSILON, col.g) * _GreenLevels;
				int b = lerp(0.0f, 1.0f - EPSILON, col.b) * _BlueLevels;

                return float4(r / (float)_RedLevels, g / (float)_GreenLevels, b / (float)_BlueLevels, 1.0f);
            }
            ENDHLSL
        }
    }
}
