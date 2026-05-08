Shader "SnapshotProURP/Neon"
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
			#pragma multi_compile_local_fragment __ USE_SCENE_TEXTURE_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _ColorSensitivity;
			float _ColorStrength;
			float _DepthSensitivity;
			float _DepthStrength;
			float _NormalsSensitivity;
			float _NormalsStrength;
			float4 _BackgroundColor;
			float _DepthThreshold;
			float _SaturationFloor;
			float _LightnessFloor;
			float4 _EmissiveColor;

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

			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 rgb2hsv(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
				float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);

				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			// Credit: http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
			float3 hsv2rgb(float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}

            float4 frag (Varyings i) : SV_TARGET
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

				float2 leftUV = i.texcoord + float2(1.0f / -_ScreenParams.x, 0.0f);
				float2 rightUV = i.texcoord + float2(1.0f / _ScreenParams.x, 0.0f);
				float2 bottomUV = i.texcoord + float2(0.0f, 1.0f / -_ScreenParams.y);
				float2 topUV = i.texcoord + float2(0.0f, 1.0f / _ScreenParams.y);

				float3 col0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, leftUV).rgb;
				float3 col1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, rightUV).rgb;
				float3 col2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, bottomUV).rgb;
				float3 col3 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, topUV).rgb;

				float3 c0 = col1 - col0;
				float3 c1 = col3 - col2;

				float edgeCol = sqrt(dot(c0, c0) + dot(c1, c1));
				edgeCol = edgeCol > _ColorSensitivity ? _ColorStrength : 0;

#if UNITY_REVERSED_Z
				float depth0 = SampleSceneDepth(leftUV);
				float depth1 = SampleSceneDepth(rightUV);
				float depth2 = SampleSceneDepth(bottomUV);
				float depth3 = SampleSceneDepth(topUV);
#else
				float depth0 = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(leftUV));
				float depth1 = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(rightUV));
				float depth2 = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(bottomUV));
				float depth3 = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(topUV));
#endif

				depth0 = Linear01Depth(depth0, _ZBufferParams);
				depth1 = Linear01Depth(depth1, _ZBufferParams);
				depth2 = Linear01Depth(depth2, _ZBufferParams);
				depth3 = Linear01Depth(depth3, _ZBufferParams);

				float d0 = depth1 - depth0;
				float d1 = depth3 - depth2;

				float edgeDepth = sqrt(d0 * d0 + d1 * d1);
				edgeDepth = edgeDepth > _DepthSensitivity ? _DepthStrength : 0;

				float3 normal0 = DecodeNormal(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, leftUV));
				float3 normal1 = DecodeNormal(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, rightUV));
				float3 normal2 = DecodeNormal(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, bottomUV));
				float3 normal3 = DecodeNormal(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, topUV));

				float n0 = normal1 - normal0;
				float3 n1 = normal3 - normal2;

				float edgeNormal = sqrt(dot(n0, n0) + dot(n1, n1));
				edgeNormal = edgeNormal > _NormalsSensitivity ? _NormalsStrength : 0;

				float edge = max(max(edgeCol, edgeDepth), edgeNormal);

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif
				depth = Linear01Depth(depth, _ZBufferParams);
				edge = depth > _DepthThreshold ? 0.0f : edge;

				float3 hsvTex = rgb2hsv(col.rgb);
				hsvTex.y = max(hsvTex.y, _SaturationFloor);
				hsvTex.z = max(hsvTex.z, _LightnessFloor);
				float3 neonCol = hsv2rgb(hsvTex);

#ifdef USE_SCENE_TEXTURE_ON
				float4 background = col;
#else
				float4 background = _BackgroundColor;
#endif

				return background + float4(neonCol * edge * _EmissiveColor, 1.0f);
            }
            ENDHLSL
        }
    }
}
