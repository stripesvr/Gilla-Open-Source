Shader "Gilla/PickWet" {
	Properties {
		_Tip ("Spike's End, How Far Up", Vector) = (0,0,0,0)
		_Edge ("Chisel's End, How Far Up", Vector) = (0,0,0,0)
		_Dry ("How Dry", Range(0, 1)) = 0
		_Glint ("Wet Glint", Range(0, 2)) = 0.8
		_Ambient ("Never Darker Than", Range(0, 1)) = 0.12
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _Tip;
			float4 _Edge;
			float _Dry;
			float _Glint;
			float _Ambient;
		CBUFFER_END
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
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 positionOS : TEXCOORD2;
				float fog : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.positionOS = v.positionOS.xyz;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.fog = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float WetFrom(float4 endPt, float3 pos) {
				float d = distance(pos, endPt.xyz);
				float reach = max(endPt.w, 1e-4);
				return (endPt.w > 0) ? 1.0 - smoothstep(reach * 0.7, reach, d) : 0.0;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

				float wet = max(WetFrom(_Tip, i.positionOS), WetFrom(_Edge, i.positionOS));
				wet *= saturate(1.0 - _Dry);

				float3 baseCol = lerp(float3(0.45, 0.45, 0.47), float3(0.22, 0.22, 0.24), wet);
				float gloss = lerp(16.0, 128.0, wet);
				float specAmt = lerp(0.08, _Glint, wet);

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float3 h = normalize(ml.direction + v);
				float atten = ml.distanceAttenuation * ml.shadowAttenuation;
				float3 col = baseCol * ml.color * saturate(dot(n, ml.direction)) * atten;
				col += ml.color * specAmt * pow(saturate(dot(n, h)), gloss) * atten;

				#ifdef _ADDITIONAL_LIGHTS
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0u; li < cnt; li++) {
					Light l = GetAdditionalLight(li, i.positionWS);
					float a = l.distanceAttenuation * l.shadowAttenuation;
					float3 hh = normalize(l.direction + v);
					col += baseCol * l.color * saturate(dot(n, l.direction)) * a;
					col += l.color * specAmt * pow(saturate(dot(n, hh)), gloss) * a;
				}
				#endif

				float3 amb = max(SampleSH(n), _Ambient.xxx);
				col += baseCol * amb;
				col = max(col, baseCol * _Ambient);
				col = MixFog(col, i.fog);
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
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
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
			half4 frag (Varyings i) : SV_Target {
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
			struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}
			ENDHLSL
		}
	}
}
