// Made with Amplify Shader Editor — URP pass added for Universal Render Pipeline
// Original Built-in pipeline; use this SubShader when project uses URP.
Shader "GD/Particles/UniversalPremultiplitedFlipbookDistortion"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Flipbook("Flipbook", 2D) = "white" {}
		_Tex1("Tex1", 2D) = "white" {}
		_Add_Blend("Add_Blend", Range( 0 , 4)) = 0
		_SpeedTex1("Speed Tex1", Vector) = (0.21,0.12,-0.06,-0.36)
		_Distortion("Distortion", Float) = -0.13
		_Wave("Wave", Float) = 1.53
		_Colums("Colums", Float) = 0
		_Rows("Rows", Float) = 0
		_StartFrame("StartFrame", Float) = 0
		_Time1("Time1", Float) = 0
		_Speed("Speed", Float) = 0
		_EmissionKek("EmissionKek", Float) = 0

	}


	SubShader
	{
		LOD 0

		Tags
		{
			"RenderPipeline"="UniversalPipeline"
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"UniversalMaterialType"="Unlit"
		}
		Blend One OneMinusSrcAlpha
		ColorMask RGB
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass
		{
			Name "UniversalForwardOnly"
			Tags { "LightMode"="UniversalForwardOnly" }

			HLSLPROGRAM

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma shader_feature_local _ SOFTPARTICLES_ON

			// URP Core fog helpers expect these when not using ShaderLibrary/Fog.hlsl (file missing in some installs)
			#ifndef FOG_LINEAR_KEYWORD_DECLARED
			#define FOG_LINEAR_KEYWORD_DECLARED 1
			#define FOG_EXP_KEYWORD_DECLARED 1
			#define FOG_EXP2_KEYWORD_DECLARED 1
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			#define ASE_NEEDS_FRAG_COLOR

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _TintColor;
				float _InvFade;
				float _Colums;
				float _Rows;
				float _Speed;
				float _StartFrame;
				float _Time1;
				float4 _SpeedTex1;
				float _Wave;
				float _Distortion;
				float _EmissionKek;
				float _Add_Blend;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_Flipbook);
			SAMPLER(sampler_Flipbook);
			TEXTURE2D(_Tex1);
			SAMPLER(sampler_Tex1);

			struct Attributes
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				half fogFactor : TEXCOORD1;
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD2;
				#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v)
			{
				Varyings o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 vertexWS = TransformObjectToWorld(v.vertex.xyz);
				o.positionCS = TransformWorldToHClip(vertexWS);

				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos(o.positionCS);
				float3 viewPos = TransformWorldToView(vertexWS);
				o.projPos.z = -viewPos.z;
				#endif

				o.color = v.color;
				o.texcoord = v.texcoord;
				o.fogFactor = ComputeFogFactor(o.positionCS.z);
				return o;
			}

			half4 frag (Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				#ifdef SOFTPARTICLES_ON
				float2 uvDepth = i.projPos.xy / i.projPos.w;
				float rawDepth = SampleSceneDepth(uvDepth);
				float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
				float partZ = i.projPos.z;
				float fade = saturate (_InvFade * (sceneZ - partZ));
				i.color.a *= fade;
				#endif

				float2 uv0123 = i.texcoord.xy * float2(1, 1);
				float fbtotaltiles147 = _Colums * _Rows;
				float fbcolsoffset147 = 1.0f / max(_Colums, 1e-5);
				float fbrowsoffset147 = 1.0f / max(_Rows, 1e-5);
				float fbspeed147 = _Time1 * _Speed;
				float2 fbtiling147 = float2(fbcolsoffset147, fbrowsoffset147);
				float fbcurrenttileindex147 = round( fmod( fbspeed147 + _StartFrame, max(fbtotaltiles147, 1.0)));
				fbcurrenttileindex147 += ( fbcurrenttileindex147 < 0) ? max(fbtotaltiles147, 1.0) : 0;
				float fblinearindextox147 = round ( fmod ( fbcurrenttileindex147, max(_Colums, 1.0)));
				float fboffsetx147 = fblinearindextox147 * fbcolsoffset147;
				float fblinearindextoy147 = round( fmod( ( fbcurrenttileindex147 - fblinearindextox147 ) / max(_Colums, 1e-5), max(_Rows, 1.0)));
				fblinearindextoy147 = ((int)(_Rows - 1)) - fblinearindextoy147;
				float fboffsety147 = fblinearindextoy147 * fbrowsoffset147;
				float2 fboffset147 = float2(fboffsetx147, fboffsety147);
				half2 fbuv147 = uv0123 * fbtiling147 + fboffset147;

				float2 appendResult114 = float2(_SpeedTex1.x , _SpeedTex1.y);
				float2 uv0113 = i.texcoord.xy;
				float2 panner117 = _Time.y * appendResult114 + uv0113;
				float4 tex2DNode120 = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, panner117);
				float2 appendResult112 = float2(_SpeedTex1.z , _SpeedTex1.w);
				float2 panner116 = _Time.y * appendResult112 + uv0113;
				float4 tex2DNode121 = SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, (panner116 * _Wave));
				float2 flipUV = fbuv147 + (( tex2DNode120.a * tex2DNode121.a * tex2DNode121 * tex2DNode120 ) * _Distortion ).rg;
				float4 tex2DNode127 = SAMPLE_TEXTURE2D(_Flipbook, sampler_Flipbook, flipUV);
				float4 temp_output_143_0 = ( float4(i.color) * tex2DNode127 * _EmissionKek * i.color.a );
				float4 appendResult169 = float4(temp_output_143_0.rgb , ( temp_output_143_0 * ( i.color.a * ( tex2DNode127.a * _Add_Blend ) ) ).r);

				half4 col = saturate( half4(appendResult169) );
				col.rgb = MixFog(col.rgb, i.fogFactor);
				return col;
			}
			ENDHLSL
		}
	}
	CustomEditor "ASEMaterialInspector"
}
