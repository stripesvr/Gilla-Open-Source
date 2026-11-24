Shader "Gilla/SkinVoid" {
	Properties {
		[MainTexture] _BaseMap ("Fur Shading (RGB) and Fur Mask (A)", 2D) = "white" {}
		_Deep ("Deep Space", Vector) = (0.01,0.012,0.05,1)
		_NebulaA ("Nebula", Vector) = (0.32,0.07,0.55,1)
		_NebulaB ("Nebula Hot", Vector) = (0.95,0.25,0.65,1)
		_NebulaC ("Nebula Cold", Vector) = (0.08,0.55,0.95,1)
		_NebulaScale ("Nebula Scale", Float) = 2.6
		_NebulaStrength ("Nebula Brightness", Range(0, 2)) = 0.9
		_StarDensity ("Star Density", Float) = 70
		_StarBrightness ("Star Brightness", Range(0, 4)) = 1.8
		_Drift ("Drift (turns per minute)", Float) = 0.35
		_RimColor ("Rim", Vector) = (0.45,0.85,1,1)
		_RimPower ("Rim Tightness", Range(0.5, 8)) = 3
		_RimStrength ("Rim Brightness", Range(0, 3)) = 1.2
		_PatchDark ("Face and Palms Darkness", Range(0, 1)) = 0.75
		[HideInInspector] _StainMap ("What's Landed On It (GillaStains)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _StainMap_ST;
			float4 _Deep;
			float4 _NebulaA;
			float4 _NebulaB;
			float4 _NebulaC;
			float _NebulaScale;
			float _NebulaStrength;
			float _StarDensity;
			float _StarBrightness;
			float _Drift;
			float4 _RimColor;
			float _RimPower;
			float _RimStrength;
			float _PatchDark;
		CBUFFER_END
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
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

			Varyings vert(Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = p.positionCS;
				OUT.positionWS = p.positionWS;
				OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
				OUT.positionOS = IN.positionOS.xyz;
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
				return OUT;
			}

			float hash31(float3 p) {
				p = frac(p * 0.1031);
				p += dot(p, p.yzx + 33.33);
				return frac((p.x + p.y) * p.z);
			}
			float vnoise(float3 p) {
				float3 i = floor(p); float3 f = frac(p);
				f = f * f * (3.0 - 2.0 * f);
				float a = hash31(i), b = hash31(i + float3(1,0,0));
				float c = hash31(i + float3(0,1,0)), d = hash31(i + float3(1,1,0));
				float e = hash31(i + float3(0,0,1)), g = hash31(i + float3(1,0,1));
				float h = hash31(i + float3(0,1,1)), k = hash31(i + float3(1,1,1));
				return lerp(lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y),
				            lerp(lerp(e,g,f.x), lerp(h,k,f.x), f.y), f.z);
			}
			float fbm(float3 p) {
				float v = 0, a = 0.5;
				[unroll] for (int i = 0; i < 4; i++) { v += a * vnoise(p); p = p * 2.03 + 1.7; a *= 0.5; }
				return v;
			}
			float3 rotY(float3 p, float a) {
				float s, c; sincos(a, s, c);
				return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z);
			}

			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
				float3 N = normalize(IN.normalWS);
				float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

				// Space pattern in object space, slowly rotating
				float ang = _Time.y * _Drift * (TWO_PI / 60.0);
				float3 sp = rotY(IN.positionOS, ang);

				float n1 = fbm(sp * _NebulaScale);
				float n2 = fbm(sp * _NebulaScale * 1.7 + 5.3);
				float3 neb = lerp(_NebulaA.rgb, _NebulaB.rgb, smoothstep(0.4, 0.75, n1));
				neb = lerp(neb, _NebulaC.rgb, smoothstep(0.45, 0.8, n2) * 0.7);
				float nebMask = smoothstep(0.35, 0.8, n1 * 0.6 + n2 * 0.5);
				float3 space = _Deep.rgb + neb * nebMask * _NebulaStrength;

				// Stars
				float3 sc = sp * _StarDensity;
				float3 cell = floor(sc);
				float3 fr = frac(sc) - 0.5;
				float h = hash31(cell);
				float3 off = float3(hash31(cell + 11.1), hash31(cell + 23.7), hash31(cell + 37.3)) - 0.5;
				float d = length(fr - off * 0.6);
				float star = step(0.9, h) * saturate(1.0 - d * 5.0);
				star = star * star;
				float twinkle = 0.6 + 0.4 * sin(_Time.y * (2.0 + h * 6.0) + h * 50.0);
				space += star * twinkle * _StarBrightness;

				// Fur mask: A=1 is fur, A=0 face/palm patches
				float fur = baseTex.a;
				space *= lerp(1.0 - _PatchDark, 1.0, fur);
				float3 shade = lerp(1.0, baseTex.rgb, fur);

				// Lighting (subtle; void mostly emissive)
				float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				float3 lit = mainLight.color * saturate(dot(N, mainLight.direction)) * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				for (uint li = 0u; li < cnt; ++li) {
					Light l = GetAdditionalLight(li, IN.positionWS);
					lit += l.color * saturate(dot(N, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
				}
				#endif
				lit += SampleSH(N);

				float3 col = space * shade * (0.55 + 0.45 * saturate(lit));

				// Rim
				float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
				col += _RimColor.rgb * rim * _RimStrength * lerp(1.0 - _PatchDark * 0.5, 1.0, fur);

				// Stains on top
				half4 stain = SAMPLE_TEXTURE2D(_StainMap, sampler_StainMap, IN.uv);
				col = lerp(col, stain.rgb * lit, saturate(stain.a));

				col = MixFog(col, IN.fogFactor);
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
			Varyings vert(Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
				float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 ld = normalize(_LightPosition - posWS);
				#else
				float3 ld = _LightDirection;
				#endif
				float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, ld));
				#if UNITY_REVERSED_Z
				cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
				#else
				cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				OUT.positionCS = cs;
				return OUT;
			}
			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
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
			Varyings vert(Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				return OUT;
			}
			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				return 0;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
