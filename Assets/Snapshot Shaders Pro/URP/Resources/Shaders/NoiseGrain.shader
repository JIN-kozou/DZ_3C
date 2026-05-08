Shader "SnapshotProURP/NoiseGrain"
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
			#pragma shader_feature_local __ USE_QUINTIC_INTERP

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _Strength;
			float _Speed;
			float _NoiseSize;
			float _AspectRatio;

			// Generate time-sensitive random numbers between 0 and 1.
			float rand(float2 pos)
			{
				return frac(sin(dot(pos + _Time.y * _Speed, float2(12.9898f, 78.233f))) * 43758.5453123f);
			}

			// Generate a random vector on the unit circle.
			float2 randUnitCircle(float2 pos)
			{
				float randVal = rand(pos);
				float theta = 2.0f * PI * randVal;

				return float2(cos(theta), sin(theta));
			}

			// Quintic interpolation curve.
			float quinterp(float f)
			{
				return f*f*f * (f * (f * 6.0f - 15.0f) + 10.0f);
			}

			// Hermite interpolation curve.
			float hermite(float f)
			{
				return f*f * (3.0f - f * 2.0f);
			}

			// Perlin gradient noise generator.
			float perlin2D(float2 positionSS)
			{
				float2 pos00 = floor(positionSS);
				float2 pos10 = pos00 + float2(1.0f, 0.0f);
				float2 pos01 = pos00 + float2(0.0f, 1.0f);
				float2 pos11 = pos00 + float2(1.0f, 1.0f);

				float2 rand00 = randUnitCircle(pos00);
				float2 rand10 = randUnitCircle(pos10);
				float2 rand01 = randUnitCircle(pos01);
				float2 rand11 = randUnitCircle(pos11);

				float dot00 = dot(rand00, pos00 - positionSS);
				float dot10 = dot(rand10, pos10 - positionSS);
				float dot01 = dot(rand01, pos01 - positionSS);
				float dot11 = dot(rand11, pos11 - positionSS);

				float2 d = frac(positionSS);
#if USE_QUINTIC_INTERP
				float x1 = lerp(dot00, dot10, quinterp(d.x));
				float x2 = lerp(dot01, dot11, quinterp(d.x));
				float y = lerp(x1, x2, quinterp(d.y));
#else
				float x1 = lerp(dot00, dot10, hermite(d.x));
				float x2 = lerp(dot01, dot11, hermite(d.x));
				float y = lerp(x1, x2, hermite(d.y));
#endif

				return y;
			}

			float4 frag(Varyings i) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

				float2 pos = i.texcoord * _ScreenParams.xy / _NoiseSize;
				float n = perlin2D(pos);

				return col - _Strength * n;
			}
			ENDHLSL
		}
    }
}
