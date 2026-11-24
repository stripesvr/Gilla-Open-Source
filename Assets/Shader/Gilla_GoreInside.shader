Shader "Gilla/GoreInside" {
	Properties {
		[MainTexture] _BaseMap ("Photo", 2D) = "white" {}
		_Tile ("Photo Repeats (per unit of the body)", Float) = 9
		_Pale ("How Light the Photo Is Kept", Float) = 0.7
		_Soak ("How Bloody It Is Away From a Break", Float) = 0.28
		_Bump ("How Much the Photo Stands Out", Float) = 0.008
		_Wet ("How Wet", Range(0, 1)) = 0.15
		_Marrow ("Inside", Vector) = (0.13,0.025,0.02,1)
		[NoScaleOffset] _WoundNoise ("What Breaks the Blood Up", 2D) = "gray" {}
		_DownThere ("Light From All Round at the Bottom", Range(0, 1)) = 0.3
		_Bounce ("Light Bouncing Round Inside", Range(0, 2)) = 0.85
		_Open ("Out in the Open, Not Down a Hole (GillaRip: lit as any surface is)", Range(0, 1)) = 0
	}

	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float _Tile;
			float _Pale;
			float _Soak;
			float _Bump;
			float _Wet;
			float4 _Marrow;
			float _DownThere;
			float _Bounce;
			float _Open;
		CBUFFER_END

		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_WoundNoise); SAMPLER(sampler_WoundNoise);
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull Back ZWrite On ZTest LEqual

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile _ _FORWARD_PLUS
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
				float3 normalOS : TEXCOORD3;
				float fogFactor : TEXCOORD4;
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
				o.positionOS = v.positionOS.xyz;
				o.normalOS = v.normalOS;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float3 Tri(float3 pos, float3 w) {
				float3 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(pos.zy, _BaseMap)).rgb;
				float3 b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(pos.xz, _BaseMap)).rgb;
				float3 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(pos.xy, _BaseMap)).rgb;
				return a * w.x + b * w.y + c * w.z;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 nOS = normalize(i.normalOS);
				float3 w = pow(abs(nOS), 4.0);
				w /= max(dot(w, 1.0), 1e-4);
				float3 pos = i.positionOS * _Tile;

				float3 photo = Tri(pos, w);
				float lum = dot(photo, float3(0.299, 0.587, 0.114));

				// Bump from luminance gradient
				float e = 0.02;
				float lx = dot(Tri(pos + float3(e,0,0), w), 0.333);
				float ly = dot(Tri(pos + float3(0,e,0), w), 0.333);
				float lz = dot(Tri(pos + float3(0,0,e), w), 0.333);
				float3 grad = (float3(lx, ly, lz) - lum) / e;
				grad -= nOS * dot(grad, nOS);
				float3 nBumpOS = normalize(nOS - grad * _Bump * _Tile);
				float3 N = normalize(TransformObjectToWorldNormal(nBumpOS));
				if (dot(N, i.normalWS) < 0) N = normalize(i.normalWS);

				// Blood breakup
				float2 nuv = (i.positionOS.xy + i.positionOS.zz * 0.37) * _Tile * 0.15;
				float noise = SAMPLE_TEXTURE2D(_WoundNoise, sampler_WoundNoise, nuv).r;
				float blood = saturate(_Soak + (noise - 0.5) * 1.5);

				float3 tinted = photo * _Pale;
				float3 albedo = lerp(tinted, _Marrow.rgb * (0.6 + lum), blood);
				albedo = lerp(albedo, albedo * 0.6, _Wet * blood);

				float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float hole = 1.0 - _Open;
				float smooth = lerp(0.2, 0.9, _Wet);
				float specPow = exp2(smooth * 10.0 + 1.0);

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float directScale = lerp(0.25, 1.0, _Open);

				float3 col = 0;
				{
					float3 L = ml.direction;
					float att = ml.shadowAttenuation * ml.distanceAttenuation;
					float ndl = saturate(dot(N, L));
					float3 H = normalize(L + V);
					float sp = pow(saturate(dot(N, H)), specPow) * _Wet * 2.0;
					col += ml.color * att * directScale * (albedo * ndl + sp * ndl);
				}

				#if defined(_ADDITIONAL_LIGHTS)
				InputData inputData = (InputData)0;
				inputData.positionWS = i.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
				uint count = GetAdditionalLightsCount();
				LIGHT_LOOP_BEGIN(count)
					Light al = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
					float3 L = al.direction;
					float att = al.distanceAttenuation * al.shadowAttenuation;
					float ndl = saturate(dot(N, L));
					float3 H = normalize(L + V);
					float sp = pow(saturate(dot(N, H)), specPow) * _Wet * 2.0;
					col += al.color * att * (albedo * ndl + sp * ndl);
				LIGHT_LOOP_END
				#endif

				float3 sh = SampleSH(N);
				float ambScale = lerp(_DownThere, 1.0, _Open);
				col += albedo * sh * ambScale;
				// light bouncing round the inside of the cavity, tinted by marrow
				float3 bounce = albedo * _Marrow.rgb * 4.0 * _Bounce * hole * (sh + ml.color * 0.25 * ml.distanceAttenuation);
				col += bounce * 0.5;

				// wet rim sheen
				float fres = pow(1.0 - saturate(dot(N, V)), 4.0) * _Wet;
				col += fres * sh * 0.5;

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
				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 L = normalize(_LightPosition - pWS);
				#else
					float3 L = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(pWS, nWS, L));
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
