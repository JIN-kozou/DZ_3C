Shader "SnapshotProURP/SobelOutline"
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
			#pragma multi_compile __ USE_SCENE_TEXTURE_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#if UNITY_VERSION < 600000
			float4 _BlitTexture_TexelSize;
#endif
			float _Threshold;
			float4 _OutlineColor;
			float4 _BackgroundColor;

			float sobel(float2 uv)
			{
				float3 x = 0;
				float3 y = 0;

				float2 pixel = _BlitTexture_TexelSize.xy;

				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-pixel.x, -pixel.y)).rgb * -1.0f;
				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-pixel.x, 0)).rgb * -2.0f;
				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-pixel.x, pixel.y)).rgb * -1.0f;

				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(pixel.x, -pixel.y)).rgb * 1.0f;
				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(pixel.x, 0)).rgb * 2.0f;
				x += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(pixel.x, pixel.y)).rgb * 1.0f;

				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-pixel.x, -pixel.y)).rgb * -1.0f;
				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, -pixel.y)).rgb * -2.0f;
				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(pixel.x, -pixel.y)).rgb * -1.0f;

				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-pixel.x, pixel.y)).rgb * 1.0f;
				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, pixel.y)).rgb * 2.0f;
				y += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(pixel.x, pixel.y)).rgb * 1.0f;

				float xLum = dot(x, float3(0.2126729, 0.7151522, 0.0721750));
				float yLum = dot(y, float3(0.2126729, 0.7151522, 0.0721750));

				return saturate(sqrt(xLum * xLum + yLum * yLum));
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float s = sobel(i.texcoord);
				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

#ifdef USE_SCENE_TEXTURE_ON
				float4 background = col;
#else
				float4 background = _BackgroundColor;
#endif

				return lerp(background, _OutlineColor, s);
            }
            ENDHLSL
        }
    }
}
