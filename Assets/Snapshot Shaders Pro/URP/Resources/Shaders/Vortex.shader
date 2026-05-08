Shader "SnapshotProURP/Vortex"
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

			float2 _Center;
			float _Strength;
			float2 _Offset;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 distance = i.texcoord - _Center;
				float angle = length(distance) * _Strength;
				float x = cos(angle) * distance.x - sin(angle) * distance.y;
				float y = sin(angle) * distance.x + cos(angle) * distance.y;
				float2 uv = float2(x + _Center.x + _Offset.x, y + _Center.y + _Offset.y);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                return col;
            }
            ENDHLSL
        }
    }
}
