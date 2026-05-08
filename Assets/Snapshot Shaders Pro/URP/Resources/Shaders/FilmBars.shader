Shader "SnapshotProURP/FilmBars"
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

			float _Aspect;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
				float aspect = _ScreenParams.x / _ScreenParams.y;
				float bars = step(abs(0.5f - i.texcoord.y) * 2.0f, aspect / _Aspect);

                return col * bars;
            }
            ENDHLSL
        }
    }
}
