Shader "SnapshotProURP/SepiaTone"
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

			float _Strength;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
				
				float3x3 sepia = float3x3
				(
					0.393f, 0.349f, 0.272f,		// Red.
					0.769f, 0.686f, 0.534f,		// Green.
					0.189f, 0.168f, 0.131f		// Blue.
				);

				float3 sepiaTint = mul(col.rgb, sepia);

				col.rgb = lerp(col.rgb, sepiaTint, _Strength);

                return col;
            }
            ENDHLSL
        }
    }
}
