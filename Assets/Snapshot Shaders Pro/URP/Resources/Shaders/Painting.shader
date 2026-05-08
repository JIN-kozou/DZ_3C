Shader "SnapshotProURP/Painting"
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
			int _KernelSize;

			struct region
			{
				float3 mean;
				float variance;
			};

			region calcRegion(int2 lower, int2 upper, int samples, float2 uv)
			{
				region r;
				float3 sum = 0.0;
				float3 squareSum = 0.0;

				for (int x = lower.x; x <= upper.x; ++x)
				{
					for (int y = lower.y; y <= upper.y; ++y)
					{
						float2 offset = float2(_BlitTexture_TexelSize.x * x, _BlitTexture_TexelSize.y * y);
						float3 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset);
						sum += tex;
						squareSum += tex * tex;
					}
				}

				r.mean = sum / samples;
				float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
				r.variance = length(variance); 

				return r;
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				int upper = (_KernelSize - 1) / 2;
				int lower = -upper;

				int samples = (upper + 1) * (upper + 1);

				region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, i.texcoord);
				region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, i.texcoord);
				region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, i.texcoord);
				region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, i.texcoord);

				float3 col = regionA.mean;
				float minVar = regionA.variance;

				float testVal;

				testVal = step(regionB.variance, minVar);
				col = lerp(col, regionB.mean, testVal);
				minVar = lerp(minVar, regionB.variance, testVal);

				testVal = step(regionC.variance, minVar);
				col = lerp(col, regionC.mean, testVal);
				minVar = lerp(minVar, regionC.variance, testVal);

				testVal = step(regionD.variance, minVar);
				col = lerp(col, regionD.mean, testVal);

				return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
