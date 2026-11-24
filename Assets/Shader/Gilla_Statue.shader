Shader "Gilla/Statue" {
	Properties {
		[MainTexture] _BaseMap ("Plaster", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (0.78,0.78,0.76,1)
		_Tile ("Plaster tile (m)", Float) = 0.35
		_AO ("How dark the creases go", Range(0, 1.5)) = 1
		_Rim ("Edge light", Range(0, 1)) = 0.12
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float _Tile;
			float _AO;
			float _Rim;
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
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _FORWARD_PLUS
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

			Varyings vert (Attributes v) {
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

			half3 Plaster (float3 pws, float3 n) {
				float3 w = pow(abs(n), 4.0);
				w /= max(dot(w, 1.0), 1e-4);
				float s = 1.0 / max(_Tile, 1e-3);
				float2 px = pws.zy * s;
				float2 py = pws.xz * s;
				float2 pz = pws.xy * s;
				float2 ux = TRANSFORM_TEX(px, _BaseMap);
				float2 uy = TRANSFORM_TEX(py, _BaseMap);
				float2 uz = TRANSFORM_TEX(pz, _BaseMap);
				return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, ux).rgb * w.x
				     + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uy).rgb * w.y
				     + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uz).rgb * w.z;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
				half3 albedo = Plaster(i.positionWS, n) * _BaseColor.rgb;

				half vAO = dot(i.color.rgb, half3(0.333, 0.333, 0.334));
				half hemi = saturate(n.y * 0.5 + 0.5);
				half ao = saturate(lerp(1.0, vAO * lerp(0.75, 1.0, hemi), saturate(_AO)));
				ao = saturate(ao - max(_AO - 1.0, 0.0) * (1.0 - ao));

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				half3 diff = ml.color * saturate(dot(n, ml.direction)) * ml.distanceAttenuation * ml.shadowAttenuation;

				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				InputData inputData = (InputData)0;
				inputData.positionWS = i.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
				LIGHT_LOOP_BEGIN(cnt)
					Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
					diff += l.color * saturate(dot(n, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
				LIGHT_LOOP_END
				#endif

				half3 amb = SampleSH(n) * ao;
				half rim = pow(1.0 - saturate(dot(n, v)), 3.0) * _Rim;
				half3 col = albedo * (diff * lerp(1.0, ao, 0.5) + amb) + rim * (ml.color * 0.5 + amb);
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

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
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
			half4 frag (Varyings i) : SV_Target {
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
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return i.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
