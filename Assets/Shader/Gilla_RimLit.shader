Shader "Gilla/RimLit" {
	Properties {
		[MainTexture] _BaseMap ("Photo", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.1
		[Header(Reflection)] [NoScaleOffset] _EnvMap ("Sky Photo (equirectangular)", 2D) = "black" {}
		_EnvColor ("Reflection Tint", Vector) = (1,1,1,1)
		_EnvStrength ("Reflection", Range(0, 4)) = 0
		_Fire ("Fire (colour in the facets)", Range(0, 1)) = 0
		[Header(Outline)] _OutlineColor ("Outline Colour", Vector) = (1,1,1,1)
		_EdgeWidth ("Outline Width (m)", Range(0, 0.01)) = 0.0015
		_EdgePixels ("At Least (screen pixels, for far away)", Range(0, 4)) = 1.2
		_RimSide ("Outline Side, as you see it (x right, y up)", Vector) = (1,0.3,0,0)
		_RimReach ("How Far Round It Reaches (higher = less of the outline)", Range(-0.9, 0.9)) = 0.15
	}

	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float _Smoothness;
			float4 _EnvColor;
			float _EnvStrength;
			float _Fire;
			float4 _OutlineColor;
			float _EdgeWidth;
			float _EdgePixels;
			float4 _RimSide;
			float _RimReach;
		CBUFFER_END

		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_EnvMap); SAMPLER(sampler_EnvMap);
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull Back ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fog
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
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float3 Hue(float h) {
				float3 c = abs(frac(h + float3(0, 2.0/3.0, 1.0/3.0)) * 6 - 3) - 1;
				return saturate(c);
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
				float3 n = normalize(i.normalWS);
				float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(shadowCoord);
				float sp = exp2(10 * _Smoothness + 1);
				float3 col = SampleSH(n) * tex.rgb;
				float3 spec = 0;
				{
					float a = ml.shadowAttenuation * ml.distanceAttenuation;
					float3 h = normalize(ml.direction + V);
					col += tex.rgb * ml.color * saturate(dot(n, ml.direction)) * a;
					spec += ml.color * pow(saturate(dot(n, h)), sp) * _Smoothness * a;
				}
				#if defined(_ADDITIONAL_LIGHTS)
				uint count = GetAdditionalLightsCount();
				LIGHT_LOOP_BEGIN(count)
					Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
					float a = l.shadowAttenuation * l.distanceAttenuation;
					float3 h = normalize(l.direction + V);
					col += tex.rgb * l.color * saturate(dot(n, l.direction)) * a;
					spec += l.color * pow(saturate(dot(n, h)), sp) * _Smoothness * a;
				LIGHT_LOOP_END
				#endif
				col += spec;

				// equirectangular reflection
				float3 r = reflect(-V, n);
				float2 euv = float2(atan2(r.x, r.z) / (2 * PI) + 0.5, asin(clamp(r.y, -1, 1)) / PI + 0.5);
				float3 env = SAMPLE_TEXTURE2D_LOD(_EnvMap, sampler_EnvMap, euv, 0).rgb * _EnvColor.rgb * _EnvStrength;
				float fres = 0.04 + 0.96 * pow(1 - saturate(dot(n, V)), 5);
				env *= lerp(0.3, 1.0, fres);

				// fire: dispersion colour per facet
				float3 fn = normalize(cross(ddy(i.positionWS), ddx(i.positionWS)));
				float3 fire = Hue(dot(fn, V) * 3 + dot(r, float3(0.3, 0.5, 0.7)) * 2);
				env = lerp(env, env * fire * 2 + fire * 0.15 * _EnvStrength, _Fire);
				col += env;

				col = MixFog(col, i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}

		Pass {
			Name "Outline"
			Tags { "LightMode"="SRPDefaultUnlit" }
			Cull Front ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 normalVS : TEXCOORD0;
				float fogFactor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pWS = TransformObjectToWorld(v.positionOS.xyz);
				float3 nWS = TransformObjectToWorldNormal(v.normalOS);
				float dist = length(GetCameraPositionWS() - pWS);
				float pix = dist * 2.0 / max(_ScreenParams.y * abs(UNITY_MATRIX_P[1][1]), 1e-4);
				if (unity_OrthoParams.w > 0.5) pix = 2.0 * unity_OrthoParams.y / _ScreenParams.y;
				float w = max(_EdgeWidth, _EdgePixels * pix);
				pWS += nWS * w;
				o.positionCS = TransformWorldToHClip(pWS);
				o.normalVS = TransformWorldToViewDir(nWS, true);
				o.fogFactor = ComputeFogFactor(o.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float2 side = _RimSide.xy;
				side = dot(side, side) > 1e-6 ? normalize(side) : float2(1, 0);
				float2 nv = i.normalVS.xy;
				nv = dot(nv, nv) > 1e-6 ? normalize(nv) : float2(0, 0);
				clip(dot(nv, side) - _RimReach);
				return half4(MixFog(_OutlineColor.rgb, i.fogFactor), 1);
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
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pWS = TransformObjectToWorld(v.positionOS.xyz);
				float3 nWS = TransformObjectToWorldNormal(v.normalOS);
				#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
					float3 ld = normalize(_LightPosition - pWS);
				#else
					float3 ld = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(pWS, nWS, ld));
				#if UNITY_REVERSED_Z
					cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
				#else
					cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				o.positionCS = cs;
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}
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

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return i.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
