Shader "SnapshotProURP/Scanlines"
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

			Texture2D _ScanlineTex;

			int _Size;
			float _Strength;
			float _ScrollSpeed;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

				float2 scanlineUV = i.texcoord * _ScreenParams.xy / _Size;
				scanlineUV.y += _Time.y * _ScrollSpeed;
				float3 scanlines = SAMPLE_TEXTURE2D(_ScanlineTex, sampler_LinearRepeat, scanlineUV).rgb;

				col = lerp(col, col * scanlines, _Strength);

				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
