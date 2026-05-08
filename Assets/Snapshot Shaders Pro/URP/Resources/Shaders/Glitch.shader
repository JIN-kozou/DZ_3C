Shader "SnapshotProURP/Glitch"
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

			Texture2D _OffsetTex;

			float _OffsetStrength;
			float _VerticalTiling;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 uv = i.texcoord;

				float offset = SAMPLE_TEXTURE2D(_OffsetTex, sampler_LinearRepeat, float2(uv.x, uv.y * _VerticalTiling));
				uv.x += (offset - 0.5f) * _OffsetStrength + 1.0f;
				uv.x %= 1;

				return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
