Shader "SnapshotProURP/Halftone"
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
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			Texture2D _HalftoneTexture;

			float _Softness;
			float _TextureSize;
			float2 _MinMaxLuminance;

			float4 _DarkColor;
			float4 _LightColor;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 halftoneUVs = i.texcoord * _ScreenParams.xy / _TextureSize;

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
				float lum = saturate(Luminance(col.rgb));

				float halftone = SAMPLE_TEXTURE2D(_HalftoneTexture, sampler_LinearRepeat, halftoneUVs).r;
				halftone = lerp(_MinMaxLuminance.x, _MinMaxLuminance.y, halftone);

				float halftoneSmooth = fwidth(halftone) * _Softness;
				halftoneSmooth = smoothstep(halftone - halftoneSmooth, halftone + halftoneSmooth, lum);

#ifdef USE_SCENE_TEXTURE_ON
				col = lerp(_DarkColor, col, halftoneSmooth);
#else
				col = lerp(_DarkColor, _LightColor, halftoneSmooth);
#endif

                return col;
            }
            ENDHLSL
        }
    }
}
