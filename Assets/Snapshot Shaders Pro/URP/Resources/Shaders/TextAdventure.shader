Shader "SnapshotProURP/TextAdventure"
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
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			#define EPSILON 1.0e-6

			Texture2D _CharacterAtlas;

			int _CharacterCount;
			float2 _CharacterSize;
			float4 _BackgroundColor;
			float4 _CharacterColor;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord).rgb;

				float luminance = saturate(Luminance(LinearToSRGB(col)));
				luminance = saturate(luminance - EPSILON);

				float2 uv = (i.texcoord * _CharacterSize) % 1.0f;
				uv.x = (floor(luminance * _CharacterCount) + uv.x) / _CharacterCount;

				float characterMask = SAMPLE_TEXTURE2D(_CharacterAtlas, sampler_LinearRepeat, uv).r;
				return lerp(_BackgroundColor, _CharacterColor, characterMask);
            }
            ENDHLSL
        }
    }
}
