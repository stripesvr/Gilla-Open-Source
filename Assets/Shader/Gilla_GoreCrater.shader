Shader "Gilla/GoreCrater" {
	Properties {
		[MainTexture] _BaseMap ("Raw Flesh Photo", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_Bump ("How Much the Photo Stands Out", Float) = 0.02
		_Wet ("How Wet", Range(0, 1)) = 0.3
		_Tiles ("Photo Repeats (per unit of the body)", Float) = 6
		[NoScaleOffset] _WoundNoise ("What Breaks the Blood Up", 2D) = "gray" {}
		_DownThere ("Light From All Round at the Bottom", Range(0, 1)) = 0.3
		_Bounce ("Light Bouncing Round Inside", Range(0, 2)) = 0.85
		_Open ("Out in the Open, Not Down a Hole (GillaRip: lit as any surface is)", Range(0, 1)) = 0
		_Film ("Blood Everywhere", Range(0, 1)) = 0.52
		_Runs ("Blood In Patches", Range(0, 1)) = 0.4
		_Least ("Blood At the Least", Range(0, 1)) = 0.35
		_Pool ("Blood Where It Faces Up", Range(0, 1)) = 0.9
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float _Bump;
			float _Wet;
			float _Tiles;
			float _DownThere;
			float _Bounce;
			float _Open;
			float _Film;
			float _Runs;
			float _Least;
			float _Pool;
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

			TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
			TEXTURE2D(_WoundNoise); SAMPLER(sampler_WoundNoise);

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

			float3 Triplanar(float3 pos, float3 n, out float lum) {
				float3 w = pow(abs(n), 4); w /= (w.x + w.y + w.z + 1e-5);
				float3 p = pos * _Tiles;
				float3 cx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(p.zy, _BaseMap)).rgb;
				float3 cy = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(p.xz, _BaseMap)).rgb;
				float3 cz = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(p.xy, _BaseMap)).rgb;
				float3 c = cx * w.x + cy * w.y + cz * w.z;
				lum = dot(c, float3(0.299, 0.587, 0.114));
				return c;
			}

			float NoiseTri(float3 pos, float3 n) {
				float3 w = abs(n); w /= (w.x + w.y + w.z + 1e-5);
				float3 p = pos * (_Tiles * 0.35);
				return SAMPLE_TEXTURE2D(_WoundNoise, sampler_WoundNoise, p.zy).r * w.x
				     + SAMPLE_TEXTURE2D(_WoundNoise, sampler_WoundNoise, p.xz).r * w.y
				     + SAMPLE_TEXTURE2D(_WoundNoise, sampler_WoundNoise, p.xy).r * w.z;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 nOS = normalize(i.normalOS);
				float3 N = normalize(i.normalWS);
				float lum;
				float3 flesh = Triplanar(i.positionOS, nOS, lum) * _BaseColor.rgb;

				// bump from photo luminance
				float3 dpx = ddx(i.positionWS), dpy = ddy(i.positionWS);
				float dhx = ddx(lum) * _Bump, dhy = ddy(lum) * _Bump;
				float3 r1 = cross(dpy, N), r2 = cross(N, dpx);
				float det = dot(dpx, r1);
				float3 grad = sign(det) * (dhx * r1 + dhy * r2);
				N = normalize(abs(det) * N - grad * 50.0);

				// blood
				float n = NoiseTri(i.positionOS, nOS);
				float patches = smoothstep(0.5 - _Runs * 0.3, 0.6, n);
				float film = _Film * lerp(1.0, patches, _Runs);
				float up = saturate(N.y) * _Pool * smoothstep(0.3, 0.7, n + 0.2);
				float blood = saturate(max(_Least, max(film, up)));
				float3 bloodCol = float3(0.32, 0.02, 0.02) * (0.6 + 0.4 * lum);
				float3 albedo = lerp(flesh, bloodCol, blood);
				float wet = saturate(_Wet + blood * 0.5);

				float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float shininess = lerp(16.0, 256.0, wet);

				float4 sc = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(sc);
				float3 direct = 0;
				{
					float3 H = normalize(ml.direction + V);
					float ndl = saturate(dot(N, ml.direction));
					float atten = ml.shadowAttenuation * ml.distanceAttenuation;
					direct += ml.color * atten * (albedo * ndl + wet * pow(saturate(dot(N, H)), shininess) * ndl);
				}
				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0; li < cnt; li++) {
					Light l = GetAdditionalLight(li, i.positionWS);
					float3 H = normalize(l.direction + V);
					float ndl = saturate(dot(N, l.direction));
					float atten = l.shadowAttenuation * l.distanceAttenuation;
					direct += l.color * atten * (albedo * ndl + wet * pow(saturate(dot(N, H)), shininess) * ndl);
				}
				#endif

				float3 sh = SampleSH(N);
				// down a hole: direct light occluded, ambient flattened, bounce tinted by flesh
				float3 holeAmb = SampleSH(float3(0,1,0)) * _DownThere;
				float3 bounce = albedo * albedo * _Bounce * (dot(sh, 0.333) + 0.15);
				float3 hole = albedo * holeAmb + bounce * 0.5 + direct * 0.25;
				float3 open = albedo * sh + direct;
				float3 col = lerp(hole, open, _Open);

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
				return 0;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
