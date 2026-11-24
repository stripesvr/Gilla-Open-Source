Shader "Gilla/SkinSmoke" {
	Properties {
		[MainTexture] _BaseMap ("Fur Shading (RGB) and Fur Mask (A)", 2D) = "white" {}
		_Dark ("Smoke", Vector) = (0.022,0.018,0.03,1)
		_Wisp ("Wisps", Vector) = (0.45,0.32,0.7,1)
		_WispStrength ("Wisp Brightness", Range(0, 2)) = 0.45
		_WispWidth ("Wisp Thinness (lower: thinner)", Range(0.01, 0.3)) = 0.12
		_Scale ("Smoke Scale (per model unit)", Float) = 18
		_Rise ("Rise Speed", Float) = 0.35
		_RimColor ("Rim", Vector) = (0.62,0.32,1,1)
		_RimPower ("Rim Tightness", Range(0.5, 8)) = 2.6
		_RimStrength ("Rim Brightness", Range(0, 3)) = 1.1
		_Lit ("How Much the Room Lights It", Range(0, 1)) = 0.2
		[HideInInspector] _StainMap ("What's Landed On It (GillaStains)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _StainMap_ST;
			float4 _Dark;
			float4 _Wisp;
			float _WispStrength;
			float _WispWidth;
			float _Scale;
			float _Rise;
			float4 _RimColor;
			float _RimPower;
			float _RimStrength;
			float _Lit;
		CBUFFER_END
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_StainMap); SAMPLER(sampler_StainMap);
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull Back
			ZWrite On

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
				float3 positionOS : TEXCOORD3;
				float fogFactor : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float hash3(float3 p) {
				p = frac(p * 0.3183099 + 0.1);
				p *= 17.0;
				return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
			}
			float noise3(float3 x) {
				float3 i = floor(x);
				float3 f = frac(x);
				f = f * f * (3.0 - 2.0 * f);
				return lerp(lerp(lerp(hash3(i), hash3(i + float3(1,0,0)), f.x),
				                 lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), f.x), f.y),
				            lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), f.x),
				                 lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), f.x), f.y), f.z);
			}
			float fbm(float3 p) {
				float v = 0.0; float a = 0.5;
				for (int k = 0; k < 4; k++) { v += a * noise3(p); p = p * 2.03 + 1.7; a *= 0.5; }
				return v;
			}

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.positionOS = v.positionOS.xyz;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 vdir = normalize(GetWorldSpaceViewDir(i.positionWS));
				half4 fur = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

				// rising smoke in object space
				float3 sp = i.positionOS * _Scale;
				sp.y -= _Time.y * _Rise * _Scale * 0.1;
				float warp = fbm(sp * 0.5 + float3(0, _Time.y * _Rise, 0));
				float f = fbm(sp + warp * 2.0);
				float band = abs(frac(f * 3.0) - 0.5) * 2.0;
				float wisp = 1.0 - smoothstep(0.0, _WispWidth, 1.0 - band);
				wisp = saturate(1.0 - smoothstep(0.0, _WispWidth, abs(f - 0.5) * 2.0 * (1.0 - band * 0.5)));
				wisp = max(wisp, 0.0);

				float3 col = _Dark.rgb * lerp(1.0, fur.rgb * 2.0, fur.a);
				col += _Wisp.rgb * wisp * _WispStrength;

				// lighting
				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float3 light = ml.color * ml.distanceAttenuation * ml.shadowAttenuation * saturate(dot(n, ml.direction));
				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0; li < cnt; li++) {
					Light l = GetAdditionalLight(li, i.positionWS);
					light += l.color * l.distanceAttenuation * l.shadowAttenuation * saturate(dot(n, l.direction));
				}
				#endif
				light += SampleSH(n);
				col = lerp(col, col * light * 4.0, _Lit);

				float rim = pow(saturate(1.0 - saturate(dot(n, vdir))), _RimPower);
				col += _RimColor.rgb * rim * _RimStrength;

				half4 stain = SAMPLE_TEXTURE2D(_StainMap, sampler_StainMap, TRANSFORM_TEX(i.uv, _StainMap));
				col = lerp(col, stain.rgb * max(light, 0.2), saturate(stain.a));

				col = MixFog(col, i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On ZTest LEqual ColorMask 0
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			float3 _LightDirection;
			float3 _LightPosition;
			struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 ws = TransformObjectToWorld(v.positionOS.xyz);
				float3 nws = TransformObjectToWorldNormal(v.normalOS);
				#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
				float3 ld = normalize(_LightPosition - ws);
				#else
				float3 ld = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, ld));
				#if UNITY_REVERSED_Z
				cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
				#else
				cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				o.positionCS = cs;
				return o;
			}
			half4 frag(Varyings i) : SV_Target { UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On ColorMask R
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag(Varyings i) : SV_Target { UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
			ENDHLSL
		}
	}
	FallBack Off
}
