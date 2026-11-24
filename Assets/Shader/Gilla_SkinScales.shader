Shader "Gilla/SkinScales" {
	Properties {
		[MainTexture] _BaseMap ("Scales (RGB) and Where They Take the Fur Colour (A)", 2D) = "white" {}
		_FurColor ("Fur Colour (each player's own)", Vector) = (0.3,0.585,0.155,1)
		_Sheen ("Sheen", Range(0, 1)) = 0.5
		_GlintColor ("Edge Glint (times the fur colour)", Vector) = (1.6,1.4,0.9,1)
		_GlintPower ("Edge Glint Tightness", Range(0.5, 8)) = 3
		_GlintStrength ("Edge Glint Brightness", Range(0, 2)) = 0.6
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.08
		[Header(Outline)] _OutlineColor ("Outline Colour", Vector) = (1,1,1,1)
		_EdgeWidth ("Outline Width", Range(0.02, 0.7)) = 0.25
		_RimSide ("Outline Side, as you see it (x right, y up)", Vector) = (1,0.3,0,0)
		_RimReach ("How Far Round It Reaches (higher = less of the outline)", Range(-0.9, 0.9)) = 0.15
		[HideInInspector] _StainMap ("What's Landed On It (GillaStains)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _StainMap_ST;
			float4 _FurColor;
			float _Sheen;
			float4 _GlintColor;
			float _GlintPower;
			float _GlintStrength;
			float4 _BaseColor;
			float _Smoothness;
			float4 _OutlineColor;
			float _EdgeWidth;
			float4 _RimSide;
			float _RimReach;
		CBUFFER_END
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_StainMap); SAMPLER(sampler_StainMap);
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull Back ZWrite On
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float fogFactor : TEXCOORD3;
				float2 uv2 : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.uv2 = TRANSFORM_TEX(v.uv, _StainMap);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
				half3 albedo = lerp(tex.rgb, tex.rgb * _FurColor.rgb * 2.0, tex.a) * _BaseColor.rgb;
				half4 stain = SAMPLE_TEXTURE2D(_StainMap, sampler_StainMap, i.uv2);
				albedo = lerp(albedo, stain.rgb, saturate(stain.a));

				float3 n = normalize(i.normalWS);
				float3 vDir = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float shininess = exp2(10.0 * _Smoothness + 1.0);

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float3 h = normalize(ml.direction + vDir);
				float att = ml.distanceAttenuation * ml.shadowAttenuation;
				float ndl = saturate(dot(n, ml.direction));
				half3 col = albedo * ml.color * ndl * att;
				half3 spec = ml.color * att * pow(saturate(dot(n, h)), shininess) * _Smoothness * ndl;

				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0; li < cnt; li++) {
					Light l = GetAdditionalLight(li, i.positionWS);
					float a = l.distanceAttenuation * l.shadowAttenuation;
					float d = saturate(dot(n, l.direction));
					col += albedo * l.color * d * a;
					float3 hh = normalize(l.direction + vDir);
					spec += l.color * a * pow(saturate(dot(n, hh)), shininess) * _Smoothness * d;
				}
				#endif

				col += albedo * SampleSH(n);
				col += spec;

				float fres = 1.0 - saturate(dot(n, vDir));
				// sheen: soft broad fresnel
				col += albedo * _Sheen * fres * fres * 0.5;
				// edge glint tinted by fur colour
				col += _GlintColor.rgb * _FurColor.rgb * pow(fres, _GlintPower) * _GlintStrength;

				// one-sided outline rim in view space
				float3 nV = mul((float3x3)UNITY_MATRIX_V, n);
				float2 side = _RimSide.xy;
				side = dot(side, side) > 1e-5 ? normalize(side) : float2(0, 0);
				float sideMask = saturate((dot(nV.xy, side) - _RimReach) / max(1.0 - _RimReach, 1e-3));
				float edge = smoothstep(1.0 - _EdgeWidth, 1.0 - _EdgeWidth * 0.5, fres);
				col = lerp(col, _OutlineColor.rgb, edge * sideMask * _OutlineColor.a);

				col = MixFog(col, i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On ZTest LEqual ColorMask 0 Cull Back
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			float3 _LightDirection;
			float3 _LightPosition;
			struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pws = TransformObjectToWorld(v.positionOS.xyz);
				float3 nws = TransformObjectToWorldNormal(v.normalOS);
				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 ld = normalize(_LightPosition - pws);
				#else
				float3 ld = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(pws, nws, ld));
				#if UNITY_REVERSED_Z
				cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
				#else
				cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				o.positionCS = cs;
				return o;
			}
			half4 frag(Varyings i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On ColorMask R Cull Back
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag(Varyings i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
			ENDHLSL
		}
	}
	FallBack Off
}
