Shader "SnapshotProURP/Underwater"
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
			#pragma multi_compile __ USE_CAUSTICS_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

			#define EPSILON 1e-06

			Texture2D _BumpMap;
			float _Strength;
			float4 _WaterColor;
			float _FogStrength;

			Texture2D _CausticsTexture;
			float _CausticsNoiseSpeed;
			float _CausticsNoiseScale;
			float _CausticsNoiseStrength;
			float3 _CausticsScrollVelocity1;
			float3 _CausticsScrollVelocity2;
			float2 _CausticsTiling;
			float4 _CausticsTint;

			// Based on https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/:
			float4 triplanarSample(Texture2D tex, SamplerState texSampler, float3 uv, float3 normals, float blend)
			{
				float2 uvX = uv.zy;
				float2 uvY = uv.xz;
				float2 uvZ = uv.xy;

				if (normals.x < 0)
				{
					uvX.x = -uvX.x;
				}

				if (normals.y < 0)
				{
					uvY.x = -uvY.x;
				}

				if (normals.z >= 0)
				{
					uvZ.x = -uvZ.x;
				}

				float4 colX = SAMPLE_TEXTURE2D(tex, texSampler, uvX);
				float4 colY = SAMPLE_TEXTURE2D(tex, texSampler, uvY);
				float4 colZ = SAMPLE_TEXTURE2D(tex, texSampler, uvZ);

				float3 blending = pow(abs(normals), blend);
				blending /= dot(blending, 1.0f);

				return (colX * blending.x + colY * blending.y + colZ * blending.z);
			}

			// Generate random numbers between 0 and 1.
			float rand(float2 pos)
			{
				return frac(sin(dot(pos, float2(12.9898f, 78.233f))) * 43758.5453123f);
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

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 timeUV = (i.texcoord + _Time.x) % 1.0f;
				float4 bumpTex = SAMPLE_TEXTURE2D(_BumpMap, sampler_LinearRepeat, timeUV);
				float2 normal = bumpTex.wy * 2.0f - 1.0f;

				float2 normalUV = i.texcoord + (1.0f / _ScreenParams.xy) * normal.xy * _Strength;
				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, normalUV).xyz;

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
				float skyboxCheck = depth;
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
				float skyboxCheck = 1.0f - depth;
#endif

#ifdef USE_CAUSTICS_ON

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP);// + _Offset;
				float3 worldNormal = normalize(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, i.texcoord));

				float noise = perlin2D((worldPos.xz + _Time.y * _CausticsNoiseSpeed) * _CausticsNoiseScale) * _CausticsNoiseStrength;

				float4 triplanar1 = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y + noise) * _CausticsTiling.x, worldNormal, 1.0f);
				float4 triplanar2 = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y + noise) * _CausticsTiling.y, worldNormal, 1.0f);

				float4 caustics = triplanar1 + triplanar2;

				// Fix for weird issues with the skybox.
				if (skyboxCheck < EPSILON)
				{
					caustics = 0.0f;
				}
#else
				float4 caustics = 0.0f;
#endif

				depth = Linear01Depth(depth, _ZBufferParams);

				col = lerp(col, _WaterColor.xyz, depth * _FogStrength);
				col = lerp(col, caustics * _CausticsTint, caustics.r);
				
				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
