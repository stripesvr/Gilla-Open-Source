Shader "Gilla/ApeFur" {
	Properties {
		[MainTexture] _BaseMap ("Fur Texture", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.08
		_FurTile ("Fur Tiling (per metre, no-stretch mapping)", Float) = 7
		[Header(Outline)] _OutlineColor ("Outline Colour", Vector) = (1,1,1,1)
		_EdgeWidth ("Outline Width", Range(0.02, 0.7)) = 0.25
		_RimSide ("Outline Side, as you see it (x right, y up)", Vector) = (1,0.3,0,0)
		_RimReach ("How Far Round It Reaches (higher = less of the outline)", Range(-0.9, 0.9)) = 0.15
		[HideInInspector] _StainMap ("What's Landed On It (GillaStains)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _StainMap_ST;
			float4 _BaseColor;
			float _Smoothness;
			float4 _OutlineColor;
			float _EdgeWidth;
			float4 _RimSide;
			float _RimReach;
			float _FurTile;
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
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _FORWARD_PLUS
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				float3 bindPos : TEXCOORD2;   // GillaFurMap: the rest-pose position, so the fur never slides or stretches
				float3 bindNrm : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float fogFactor : TEXCOORD3;
				float2 uvStain : TEXCOORD4;
				float3 triP : TEXCOORD5;
				float3 triN : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes i) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_TRANSFER_INSTANCE_ID(i, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(i.normalOS);
				o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
				o.uvStain = TRANSFORM_TEX(i.uv, _StainMap);
				bool baked = dot(i.bindNrm, i.bindNrm) > 0.01;
				o.triP = (baked ? i.bindPos : i.positionOS.xyz) * _FurTile;
				o.triN = baked ? i.bindNrm : i.normalOS;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half3 Shade(Light l, float3 n, float3 v, half3 albedo, float gloss) {
				half3 lc = l.color * l.distanceAttenuation * l.shadowAttenuation;
				float ndl = saturate(dot(n, l.direction));
				float3 h = SafeNormalize(l.direction + v);
				float spec = pow(saturate(dot(n, h)), exp2(gloss * 10 + 1)) * gloss;
				return lc * (albedo * ndl + spec);
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				// triplanar: three flat projections blended by the surface direction, so nothing is stretched
				float3 bw = pow(abs(normalize(i.triN)), 4);
				bw /= (bw.x + bw.y + bw.z);
				half4 tex = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.triP.zy) * bw.x
				           + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.triP.xz) * bw.y
				           + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.triP.xy) * bw.z) * _BaseColor;
				half4 stain = SAMPLE_TEXTURE2D(_StainMap, sampler_StainMap, i.uvStain);
				half3 albedo = lerp(tex.rgb, stain.rgb, saturate(stain.a));

				float3 n = normalize(i.normalWS);
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				half3 col = albedo * SampleSH(n);
				col += Shade(ml, n, v, albedo, _Smoothness);

				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				#if USE_FORWARD_PLUS
				InputData inputData = (InputData)0;
				inputData.positionWS = i.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
				#endif
				LIGHT_LOOP_BEGIN(cnt)
					Light al = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
					col += Shade(al, n, v, albedo, _Smoothness);
				LIGHT_LOOP_END
				#endif

				// Screen-side rim outline
				float3 nVS = normalize(mul((float3x3)UNITY_MATRIX_V, n));
				float fres = 1.0 - saturate(dot(n, v));
				float edge = smoothstep(1.0 - _EdgeWidth, 1.0 - _EdgeWidth + 0.05, fres);
				float2 side = _RimSide.xy;
				side = dot(side, side) > 1e-5 ? normalize(side) : float2(0, 1);
				float2 nxy = nVS.xy;
				nxy = dot(nxy, nxy) > 1e-5 ? normalize(nxy) : float2(0, 0);
				float facing = smoothstep(_RimReach - 0.05, _RimReach + 0.05, dot(nxy, side));
				col = lerp(col, _OutlineColor.rgb, edge * facing * _OutlineColor.a);

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
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes i) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_TRANSFER_INSTANCE_ID(i, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pws = TransformObjectToWorld(i.positionOS.xyz);
				float3 nws = TransformObjectToWorldNormal(i.normalOS);
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
			Varyings vert(Attributes i) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_TRANSFER_INSTANCE_ID(i, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
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
