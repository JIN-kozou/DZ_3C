Shader "SnapshotProURP/Cutout"
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

			Texture2D _CutoutTex;

			float4 _BorderColor;
			int _Stretch;
			float _Zoom;
			float2 _Offset;
			float4x4 _Rotation;

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float2 UVs = (i.texcoord - 0.5f) / _Zoom;

				float aspect = (_Stretch == 0) ? _ScreenParams.x / _ScreenParams.y : 1.0f;
				UVs = float2(aspect * UVs.x, UVs.y);

				float2x2 rotationMatrix = float2x2(_Rotation._m00, _Rotation._m01, _Rotation._m10, _Rotation._m11);
				UVs = mul(rotationMatrix, UVs);
				UVs += float2(_Offset.x * aspect, _Offset.y) + 0.5f;

				float cutoutAlpha = SAMPLE_TEXTURE2D(_CutoutTex, sampler_LinearClamp, UVs).a;
				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
				return lerp(col, _BorderColor, cutoutAlpha);
            }
            ENDHLSL
        }
    }
}
