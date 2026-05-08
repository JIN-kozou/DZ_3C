Shader "SnapshotProURP/SobelNeon"
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
			float _SaturationFloor;
			float _LightnessFloor;
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

			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 rgb2hsv(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
				float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 hsv2rgb(float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float s = sobel(i.texcoord);
				float3 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).xyz;

				float3 hsvTex = rgb2hsv(tex);
				hsvTex.y = max(hsvTex.y, _SaturationFloor);
				hsvTex.z = max(hsvTex.z, _LightnessFloor);
				float3 col = hsv2rgb(hsvTex);

				return lerp(_BackgroundColor, float4(col, 1.0f), s);
            }
            ENDHLSL
        }
    }
}
