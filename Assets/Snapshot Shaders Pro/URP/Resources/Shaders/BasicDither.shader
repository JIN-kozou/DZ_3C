Shader "SnapshotProURP/BasicDither"
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

			Texture2D _NoiseTex;

			float4 _NoiseTex_TexelSize;
			float _NoiseSize;
			float _ThresholdOffset;

			float4 _LightColor;
			float4 _DarkColor;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
				float lum = dot(col, float3(0.3f, 0.59f, 0.11f));

				float2 noiseUV = i.texcoord * _NoiseTex_TexelSize.xy * _ScreenParams.xy * 2.0f / _NoiseSize;
				float3 noiseColor = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, noiseUV).rgb;
				float threshold = dot(noiseColor, float3(0.3f, 0.59f, 0.11f)) + _ThresholdOffset;

#ifdef USE_SCENE_TEXTURE_ON
				col = lum < threshold ? _DarkColor.rgb : col;
#else
				col = lum < threshold ? _DarkColor.rgb : _LightColor.rgb;
#endif

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
