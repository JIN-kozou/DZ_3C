Shader "SnapshotProURP/GameBoy"
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

			float4 _GBDarkest;
			float4 _GBDark;
			float4 _GBLight;
			float4 _GBLightest;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
				float lum = dot(col, float3(0.3f, 0.59f, 0.11f));
				lum = pow(lum, _PowerRamp);
				int gb = lum * 4;

				col = lerp(_GBDarkest, _GBDark, saturate(gb));
				col = lerp(col, _GBLight, saturate(gb - 1.0f));
				col = lerp(col, _GBLightest, saturate(gb - 2.0f));

                return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
