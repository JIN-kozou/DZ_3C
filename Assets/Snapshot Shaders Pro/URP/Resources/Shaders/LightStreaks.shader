Shader "SnapshotProURP/LightStreaks"
{
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#define E 2.71828f

			Texture2D _BlurTex;

#if UNITY_VERSION < 600000
			float4 _BlitTexture_TexelSize;
#endif
			uint _KernelSize;
			float _Spread;
			float _LuminanceThreshold;
		ENDHLSL

        Pass
        {
			Name "HorizontalBlur"

			HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_horizontal

			float gaussian(int x)
			{
				float sigmaSqu = _Spread * _Spread;
				return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
			}

			float4 frag_horizontal(Varyings i) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 light = 0.0f;
				float kernelSum = 0.0f;

				int upper = ((_KernelSize - 1) / 2);
				int lower = -upper;

				float2 uv;

				for (int x = lower; x <= upper; ++x)
				{
					float gauss = gaussian(x);
					kernelSum += gauss;
					uv = i.texcoord + float2(_BlitTexture_TexelSize.x * x, 0.0f);

					float3 newLight = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
					float lum = dot(newLight, float3(0.3f, 0.59f, 0.11f));
					light += step(_LuminanceThreshold, lum) * newLight * gauss;
				}

				light /= kernelSum;

				return float4(light, 1.0f);
			}
            ENDHLSL
        }

		Pass
		{
			Name "Overlay"

			HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag_overlay

			float4 frag_overlay(Varyings i) : SV_TARGET
			{
				float4 mainCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
				float4 blurCol = SAMPLE_TEXTURE2D(_BlurTex, sampler_LinearClamp, i.texcoord);

				return mainCol + blurCol;
			}
            ENDHLSL
		}
    }
}
