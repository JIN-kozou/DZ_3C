Shader "SnapshotProURP/Sharpen"
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

#if UNITY_VERSION < 600000
			float4 _BlitTexture_TexelSize;
#endif
			float _Intensity;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
				col += 4.0f * col * _Intensity;

				float2 s = _BlitTexture_TexelSize.xy;
				col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0,	   -s.y)).rgb * _Intensity;
				col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(-s.x,    0)).rgb * _Intensity;
				col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(s.x,     0)).rgb * _Intensity;
				col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0,     s.y)).rgb * _Intensity;

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
