Shader "Gilla/Water" {
	Properties {
		[MainColor] _BaseColor ("Water Colour", Color) = (0.15,0.36,0.21,1)
		_FoamColor ("Foam Colour", Vector) = (0.82,0.88,0.82,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.8
		[NoScaleOffset] [Normal] _WaveMap ("Ripples (normal map, tiling)", 2D) = "bump" {}
		_WaveScale ("Ripples' Tile (m)", Float) = 4.5
		_WaveStrength ("Ripples' Strength", Range(0, 2)) = 0.2
		_WaveSpeed ("Ripples' Drift (m/s)", Float) = 0.12
		_RippleSpeed ("Ripples' Spread (m/s)", Float) = 0.75
		_RippleSpacing ("Ripples' Crests Apart (m)", Float) = 0.14
		_RippleTilt ("Ripples' Strength", Range(0, 2)) = 0.6
		_RippleLife ("Ripples' Life (s, the hardest)", Float) = 3.2
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseColor;
			float4 _FoamColor;
			float _Smoothness;
			float4 _WaveMap_ST;
			float _WaveScale;
			float _WaveStrength;
			float _WaveSpeed;
			float _RippleSpeed;
			float _RippleSpacing;
			float _RippleTilt;
			float _RippleLife;
		CBUFFER_END
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

			TEXTURE2D(_WaveMap); SAMPLER(sampler_WaveMap);

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float4 tangentWS : TEXCOORD2;
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
				VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = n.normalWS;
				o.tangentWS = float4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float3 Shade(Light l, float3 N, float3 V, float3 albedo, float spec) {
				float3 radiance = l.color * l.distanceAttenuation * l.shadowAttenuation;
				float ndl = saturate(dot(N, l.direction));
				float3 H = SafeNormalize(l.direction + V);
				float s = pow(saturate(dot(N, H)), spec) * _Smoothness;
				return radiance * (albedo * ndl + s * ndl);
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float t = _Time.y;
				float2 wxz = i.positionWS.xz;
				float tile = max(_WaveScale, 0.001);
				float2 uv1 = wxz / tile + float2(1, 0.6) * _WaveSpeed * t / tile;
				float2 uv2 = wxz / (tile * 0.73) + float2(-0.7, 1) * _WaveSpeed * t / tile;
				float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, uv1));
				float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveMap, sampler_WaveMap, uv2));
				float3 nTS = normalize(float3((n1.xy + n2.xy) * _WaveStrength, 1));

				// Concentric splash ripples from object origin, repeating each life cycle
				float3 originWS = TransformObjectToWorld(float3(0, 0, 0));
				float2 dv = wxz - originWS.xz;
				float d = length(dv);
				float2 dir = dv / max(d, 1e-4);
				float life = max(_RippleLife, 0.01);
				float age = fmod(t, life);
				float front = _RippleSpeed * age;
				float k = 6.2831853 / max(_RippleSpacing, 0.001);
				float behind = front - d;
				float mask = saturate(behind / max(_RippleSpacing, 0.001)) * exp(-max(behind, 0) * 2.0);
				mask *= saturate(1 - age / life);
				float phase = k * behind;
				float slope = cos(phase) * mask * _RippleTilt;
				float crest = saturate(sin(phase)) * mask;

				// ripple slope in world xz; convert to tangent-ish offset in world space
				float3 N0 = normalize(i.normalWS);
				float3 T = normalize(i.tangentWS.xyz);
				float3 B = cross(N0, T) * i.tangentWS.w;
				float3 N = normalize(T * nTS.x + B * nTS.y + N0 * nTS.z);
				N = normalize(N + float3(dir.x, 0, dir.y) * slope);

				float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float foam = saturate(crest * crest * 1.5);
				float3 albedo = lerp(_BaseColor.rgb, _FoamColor.rgb, foam);
				float spec = exp2(10 * _Smoothness + 1);

				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(shadowCoord);
				float3 col = Shade(ml, N, V, albedo, spec);

				#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0u; li < cnt; ++li) {
					Light l = GetAdditionalLight(li, i.positionWS);
					col += Shade(l, N, V, albedo, spec);
				}
				#endif

				col += SampleSH(N) * albedo;
				float fres = pow(1 - saturate(dot(N, V)), 5) * _Smoothness;
				col += SampleSH(reflect(-V, N)) * fres;

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
			half frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return i.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
