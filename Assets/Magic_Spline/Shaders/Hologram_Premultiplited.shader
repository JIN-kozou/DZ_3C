// Made with Amplify Shader Editor — URP pass
Shader "Hologram_Premultiplited"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Tex_Noise("Tex_Noise", 2D) = "white" {}
		_SpeedTex1("Speed Tex1", Vector) = (0.21,0.12,-0.06,-0.36)
		_Distortion("Distortion", Float) = -0.13
		_Wave("Wave", Float) = 1.53
		_Colums("Colums", Float) = 0
		_Rows("Rows", Float) = 0
		_StartFrame("StartFrame", Float) = 0
		_Time1("Time1", Float) = 0
		_Speed("Speed", Float) = 0
		_EmissionKek("EmissionKek", Float) = 0
		_SpeedHolo("SpeedHolo", Float) = 1
		_T_Hologram("T_Hologram", 2D) = "white" {}
		_SizeHologram("SizeHologram", Vector) = (1,1,0,0)
		[Toggle(_HOLOGRAMA_ON)] _Holograma("Holograma", Float) = 0

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
			#pragma shader_feature_local_fragment _ _HOLOGRAMA_ON

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
				float4 _SizeHologram;
				float _SpeedHolo;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_TextureSample0);
			SAMPLER(sampler_TextureSample0);
			TEXTURE2D(_Tex_Noise);
			SAMPLER(sampler_Tex_Noise);
			TEXTURE2D(_T_Hologram);
			SAMPLER(sampler_T_Hologram);

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

				float2 texCoord27 = i.texcoord.xy;
				float fbtotaltiles32 = _Colums * _Rows;
				float fbcolsoffset32 = 1.0f / max(_Colums, 1e-5);
				float fbrowsoffset32 = 1.0f / max(_Rows, 1e-5);
				float fbspeed32 = _Time1 * _Speed;
				float2 fbtiling32 = float2(fbcolsoffset32, fbrowsoffset32);
				float fbcurrenttileindex32 = round( fmod( fbspeed32 + _StartFrame, max(fbtotaltiles32, 1.0)));
				fbcurrenttileindex32 += ( fbcurrenttileindex32 < 0) ? max(fbtotaltiles32, 1.0) : 0;
				float fblinearindextox32 = round ( fmod ( fbcurrenttileindex32, max(_Colums, 1.0)));
				float fboffsetx32 = fblinearindextox32 * fbcolsoffset32;
				float fblinearindextoy32 = round( fmod( ( fbcurrenttileindex32 - fblinearindextox32 ) / max(_Colums, 1e-5), max(_Rows, 1.0)));
				fblinearindextoy32 = ((int)(_Rows - 1)) - fblinearindextoy32;
				float fboffsety32 = fblinearindextoy32 * fbrowsoffset32;
				float2 fboffset32 = float2(fboffsetx32, fboffsety32);
				half2 fbuv32 = texCoord27 * fbtiling32 + fboffset32;

				float2 appendResult17 = float2(_SpeedTex1.x , _SpeedTex1.y);
				float2 texCoord14 = i.texcoord.xy;
				float2 panner19 = _Time.y * appendResult17 + texCoord14;
				float4 tex2DNode23 = SAMPLE_TEXTURE2D(_Tex_Noise, sampler_Tex_Noise, panner19);
				float2 appendResult15 = float2(_SpeedTex1.z , _SpeedTex1.w);
				float2 panner16 = _Time.y * appendResult15 + texCoord14;
				float4 tex2DNode22 = SAMPLE_TEXTURE2D(_Tex_Noise, sampler_Tex_Noise, (panner16 * _Wave));
				float2 animatedUV = fbuv32 + (( tex2DNode23.a * tex2DNode22.a * tex2DNode22 * tex2DNode23 ) * _Distortion ).rg;
				float4 temp_output_36_0 = ( ( float4(i.color) * SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, animatedUV) * i.color.a ) * _EmissionKek );
				float2 temp_cast_4 = (_SpeedHolo).xx;
				float2 panner41 = _Time.y * temp_cast_4;
				float2 texCoord45 = i.texcoord.xy * _SizeHologram.xy + panner41;
				#ifdef _HOLOGRAMA_ON
				float4 staticSwitch53 = ( temp_output_36_0 - SAMPLE_TEXTURE2D(_T_Hologram, sampler_T_Hologram, texCoord45) );
				#else
				float4 staticSwitch53 = temp_output_36_0;
				#endif

				half4 col = saturate( half4(staticSwitch53) );
				col.rgb = MixFog(col.rgb, i.fogFactor);
				return col;
			}
			ENDHLSL
		}
	}
	CustomEditor "ASEMaterialInspector"
}
