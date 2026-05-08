Shader "SnapshotProURP/FancyDither"
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
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			Texture2D _NoiseTex;

			float4 _NoiseTex_TexelSize;
			float _NoiseSize;
			float _ThresholdOffset;

			float4 _LightColor;
			float4 _DarkColor;

			float _Blend;

			// Credit to https://alexanderameye.github.io/outlineshader.html:
			float3 DecodeNormal(float4 enc)
			{
				float kScale = 1.7777;
				float3 nn = enc.xyz*float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, depth, UNITY_MATRIX_I_VP);

				float3 noiseUV = worldPos / _NoiseSize;

				float4 noiseX = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, noiseUV.zy);
				float4 noiseY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, noiseUV.xz);
				float4 noiseZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, noiseUV.xy);

				float3 normal = DecodeNormal(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, i.texcoord));

				float3 blend = pow(abs(normal), _Blend);
				blend /= dot(blend, 1.0f);

				float lum = Luminance(col);

				float3 noiseColor = noiseX * blend.x + noiseY * blend.y + noiseZ * blend.z;
				float threshold = dot(noiseColor, 1.0f) + _ThresholdOffset;

				col = lum < threshold ? _DarkColor.rgb : _LightColor.rgb;

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
