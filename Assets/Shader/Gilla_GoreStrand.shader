Shader "Gilla/GoreStrand" {
	Properties {
		_Glint ("Wet Glint", Range(0, 2)) = 0.7
		_Ambient ("Never Darker Than", Range(0, 1)) = 0.18
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float _Glint;
			float _Ambient;
		CBUFFER_END
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull Off
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
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float fogFactor : TEXCOORD3;
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
				o.color = v.color;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i, bool face : SV_IsFrontFace) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				n = face ? n : -n;
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
				// Gore base: dark wet red, modulated by vertex color
				float3 albedo = float3(0.45, 0.03, 0.03) * i.color.rgb;

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float atten = ml.distanceAttenuation * ml.shadowAttenuation;
				float3 h = normalize(ml.direction + v);
				float ndl = saturate(dot(n, ml.direction));
				float spec = pow(saturate(dot(n, h)), 64.0) * _Glint;
				float3 diff = albedo * ml.color * ndl * atten;
				float3 sp = ml.color * spec * atten;

				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0u; li < cnt; ++li) {
					Light l = GetAdditionalLight(li, i.positionWS);
					float a = l.distanceAttenuation * l.shadowAttenuation;
					float3 hh = normalize(l.direction + v);
					diff += albedo * l.color * saturate(dot(n, l.direction)) * a;
					sp += l.color * pow(saturate(dot(n, hh)), 64.0) * _Glint * a;
				}
				#endif

				float3 amb = albedo * SampleSH(n);
				float3 col = diff + amb;
				col = max(col, albedo * _Ambient);
				// wet rim glint
				float rim = pow(1.0 - saturate(dot(n, v)), 4.0) * _Glint * 0.25;
				col += sp + rim * float3(1.0, 0.6, 0.6);
				col = MixFog(col, i.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Off

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
				UNITY_VERTEX_OUTPUT_STEREO
			};

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
			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On
			ColorMask R
			Cull Off

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
				UNITY_VERTEX_OUTPUT_STEREO
			};
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
