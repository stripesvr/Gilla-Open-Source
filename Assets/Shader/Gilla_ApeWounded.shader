Shader "Gilla/ApeWounded" {
	Properties {
		[MainTexture] _BaseMap ("Fur Texture", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.08
		[Header(Outline)] _OutlineColor ("Outline Colour", Vector) = (1,1,1,1)
		_EdgeWidth ("Outline Width", Range(0.001, 0.7)) = 0.25
		_RimSide ("Outline Side, as you see it (x right, y up)", Vector) = (1,0.3,0,0)
		_RimReach ("How Far Round It Reaches (higher = less of the outline)", Range(-0.9, 0.9)) = 0.15
		[Header(Wounds)] [NoScaleOffset] _FleshMap ("Raw Flesh Photo", 2D) = "red" {}
		_FleshScale ("Flesh Repeats (per unit of the body)", Float) = 5
		[NoScaleOffset] _WoundNoise ("What Roughens the Edges", 2D) = "gray" {}
		_Wound0 ("Wound", Vector) = (0,0,0,0)
		_Wound1 ("Wound", Vector) = (0,0,0,0)
		_Wound2 ("Wound", Vector) = (0,0,0,0)
		_Wound3 ("Wound", Vector) = (0,0,0,0)
		_Wound4 ("Wound", Vector) = (0,0,0,0)
		_Wound5 ("Wound", Vector) = (0,0,0,0)
		_Axis0 ("Way In, Length", Vector) = (0,0,1,1)
		_Axis1 ("Way In, Length", Vector) = (0,0,1,1)
		_Axis2 ("Way In, Length", Vector) = (0,0,1,1)
		_Axis3 ("Way In, Length", Vector) = (0,0,1,1)
		_Axis4 ("Way In, Length", Vector) = (0,0,1,1)
		_Axis5 ("Way In, Length", Vector) = (0,0,1,1)
		_Side0 ("Round From, Shape", Vector) = (1,0,0,0)
		_Side1 ("Round From, Shape", Vector) = (1,0,0,0)
		_Side2 ("Round From, Shape", Vector) = (1,0,0,0)
		_Side3 ("Round From, Shape", Vector) = (1,0,0,0)
		_Side4 ("Round From, Shape", Vector) = (1,0,0,0)
		_Side5 ("Round From, Shape", Vector) = (1,0,0,0)
		_Spot00 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot01 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot02 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot10 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot11 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot12 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot20 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot21 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot22 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot30 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot31 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot32 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot40 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot41 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot42 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot50 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot51 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_Spot52 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
		_RipOn ("Ripped In Two (GillaRip: its second UVs say how far from the tear)", Float) = 0
		[HideInInspector] _StainMap ("What's Landed On It (GillaStains)", 2D) = "black" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float _Smoothness;
			float4 _OutlineColor;
			float _EdgeWidth;
			float4 _RimSide;
			float _RimReach;
			float _FleshScale;
			float4 _Wound0, _Wound1, _Wound2, _Wound3, _Wound4, _Wound5;
			float4 _Axis0, _Axis1, _Axis2, _Axis3, _Axis4, _Axis5;
			float4 _Side0, _Side1, _Side2, _Side3, _Side4, _Side5;
			float4 _Spot00, _Spot01, _Spot02, _Spot10, _Spot11, _Spot12;
			float4 _Spot20, _Spot21, _Spot22, _Spot30, _Spot31, _Spot32;
			float4 _Spot40, _Spot41, _Spot42, _Spot50, _Spot51, _Spot52;
			float _RipOn;
			float4 _StainMap_ST;
		CBUFFER_END

		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		TEXTURE2D(_FleshMap); SAMPLER(sampler_FleshMap);
		TEXTURE2D(_WoundNoise); SAMPLER(sampler_WoundNoise);
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
				float2 uv2 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float3 normalWS : TEXCOORD3;
				float3 positionOS : TEXCOORD4;
				float3 normalOS : TEXCOORD5;
				float fogFactor : TEXCOORD6;
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
				o.positionOS = v.positionOS.xyz;
				o.normalOS = v.normalOS;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.uv2 = v.uv2;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float WoundMask(float3 pos, float4 w, float4 a, float4 s, float noise) {
				if (w.w <= 0.0) return 0.0;
				float3 d = pos - w.xyz;
				float3 ax = normalize(a.xyz + 1e-5);
				float along = dot(d, ax);
				float3 perp = d - ax * along;
				float3 sd = s.xyz - ax * dot(s.xyz, ax);
				sd = normalize(sd + 1e-5);
				float sc = dot(perp, sd);
				float3 rest = perp - sd * sc;
				float r = w.w;
				float depthLen = max(a.w, 0.01) * r;
				float e = (sc / (r * (1.0 + max(s.w, 0.0)))) ;
				float dist = sqrt(e * e + dot(rest, rest) / (r * r) + (along * along) / (depthLen * depthLen));
				return saturate((1.0 - dist + (noise - 0.5) * 0.5) * 8.0);
			}

			float SpotMask(float3 pos, float4 sp, float noise) {
				if (sp.w <= 0.0) return 0.0;
				float dist = length(pos - sp.xyz) / sp.w;
				return saturate((1.0 - dist + (noise - 0.5) * 0.6) * 6.0);
			}

			float3 Triplanar(float3 pos, float3 n) {
				float3 bw = abs(n); bw /= (bw.x + bw.y + bw.z + 1e-5);
				float3 p = pos * _FleshScale;
				float3 cx = SAMPLE_TEXTURE2D(_FleshMap, sampler_FleshMap, p.zy).rgb;
				float3 cy = SAMPLE_TEXTURE2D(_FleshMap, sampler_FleshMap, p.xz).rgb;
				float3 cz = SAMPLE_TEXTURE2D(_FleshMap, sampler_FleshMap, p.xy).rgb;
				return cx * bw.x + cy * bw.y + cz * bw.z;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

				float3 fur = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
				float noise = SAMPLE_TEXTURE2D(_WoundNoise, sampler_WoundNoise, i.positionOS.xy * _FleshScale * 0.5 + i.positionOS.z * 0.37).r;

				float3 po = i.positionOS;
				float wound = 0;
				wound = max(wound, WoundMask(po, _Wound0, _Axis0, _Side0, noise));
				wound = max(wound, WoundMask(po, _Wound1, _Axis1, _Side1, noise));
				wound = max(wound, WoundMask(po, _Wound2, _Axis2, _Side2, noise));
				wound = max(wound, WoundMask(po, _Wound3, _Axis3, _Side3, noise));
				wound = max(wound, WoundMask(po, _Wound4, _Axis4, _Side4, noise));
				wound = max(wound, WoundMask(po, _Wound5, _Axis5, _Side5, noise));

				float spot = 0;
				spot = max(spot, SpotMask(po, _Spot00, noise)); spot = max(spot, SpotMask(po, _Spot01, noise)); spot = max(spot, SpotMask(po, _Spot02, noise));
				spot = max(spot, SpotMask(po, _Spot10, noise)); spot = max(spot, SpotMask(po, _Spot11, noise)); spot = max(spot, SpotMask(po, _Spot12, noise));
				spot = max(spot, SpotMask(po, _Spot20, noise)); spot = max(spot, SpotMask(po, _Spot21, noise)); spot = max(spot, SpotMask(po, _Spot22, noise));
				spot = max(spot, SpotMask(po, _Spot30, noise)); spot = max(spot, SpotMask(po, _Spot31, noise)); spot = max(spot, SpotMask(po, _Spot32, noise));
				spot = max(spot, SpotMask(po, _Spot40, noise)); spot = max(spot, SpotMask(po, _Spot41, noise)); spot = max(spot, SpotMask(po, _Spot42, noise));
				spot = max(spot, SpotMask(po, _Spot50, noise)); spot = max(spot, SpotMask(po, _Spot51, noise)); spot = max(spot, SpotMask(po, _Spot52, noise));

				// Rip: second UV x = distance from tear
				float rip = _RipOn > 0.5 ? saturate((0.05 - i.uv2.x + (noise - 0.5) * 0.04) * 40.0) : 0.0;
				wound = max(wound, rip);

				float3 flesh = Triplanar(po, normalize(i.normalOS));
				float3 albedo = fur;
				albedo = lerp(albedo, flesh * 0.45, spot * 0.7);
				albedo = lerp(albedo, flesh, wound);

				float4 stain = SAMPLE_TEXTURE2D(_StainMap, sampler_StainMap, i.uv);
				albedo = lerp(albedo, stain.rgb, saturate(stain.a));

				float smooth = lerp(_Smoothness, 0.6, wound);
				float shin = exp2(10.0 * smooth + 1.0);

				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(shadowCoord);
				float3 col = albedo * SampleSH(n);
				float atten = ml.distanceAttenuation * ml.shadowAttenuation;
				col += albedo * ml.color * saturate(dot(n, ml.direction)) * atten;
				float3 h = normalize(ml.direction + v);
				col += ml.color * pow(saturate(dot(n, h)), shin) * smooth * atten;

				#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0; li < cnt; li++) {
					Light l = GetAdditionalLight(li, i.positionWS);
					col += albedo * l.color * saturate(dot(n, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
				}
				#endif

				// Rim outline on chosen screen side
				float3 nV = normalize(mul((float3x3)UNITY_MATRIX_V, n));
				float2 side = normalize(_RimSide.xy + 1e-5);
				float sideMask = step(_RimReach, dot(nV.xy, side));
				float fres = 1.0 - saturate(dot(n, v));
				float rim = step(1.0 - _EdgeWidth, fres) * sideMask * (1.0 - wound);
				col = lerp(col, _OutlineColor.rgb, rim);

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

			struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pw = TransformObjectToWorld(v.positionOS.xyz);
				float3 nw = TransformObjectToWorldNormal(v.normalOS);
				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 ld = normalize(_LightPosition - pw);
				#else
				float3 ld = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(pw, nw, ld));
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
