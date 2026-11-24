Shader "Gilla/Gold" {
	Properties {
		[MainTexture] _BaseMap ("Texture", 2D) = "white" {}
		[MainColor] _BaseColor ("Colour", Color) = (1,0.78,0.34,1)
		_Metallic ("Metallic (the gold)", Range(0, 1)) = 0.95
		_Smoothness ("Smoothness (the gold)", Range(0, 1)) = 0.82
		_ClothSmoothness ("Smoothness (not metal)", Range(0, 1)) = 0.45
		[Toggle] _MetalFromAlpha ("Metal Where The Texture's Alpha Says", Float) = 0
		_Lift ("Lift (keeps it from going dark where it reflects little)", Range(0, 1)) = 0.06
		_Key ("Overhead Hot Spot", Range(0, 4)) = 1.6
		_Shade ("Darker Underneath", Range(0, 1)) = 0.5
		_Sparkle ("Glitter (the grain's tilt)", Range(0, 1)) = 0.38
		_SparkleTiling ("Glitter Grain (specks across its texture)", Float) = 260
		_SparkleTint ("Glitter Light And Dark", Range(0, 1)) = 0.55
		_GlintScale ("Glint Facets a Metre", Float) = 50
		_GlintShare ("Facets That Glint", Range(0, 1)) = 0.6
		_GlintSharp ("Glint Sharpness", Range(20, 2000)) = 380
		_GlintStrength ("Glint Brightness", Range(0, 20)) = 8
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _BaseColor;
			float _Metallic;
			float _Smoothness;
			float _ClothSmoothness;
			float _MetalFromAlpha;
			float _Lift;
			float _Key;
			float _Shade;
			float _Sparkle;
			float _SparkleTiling;
			float _SparkleTint;
			float _GlintScale;
			float _GlintShare;
			float _GlintSharp;
			float _GlintStrength;
		CBUFFER_END

		float3 GoldHash3(float3 p)
		{
			p = frac(p * float3(0.1031, 0.1030, 0.0973));
			p += dot(p, p.yxz + 33.33);
			return frac((p.xxy + p.yxx) * p.zyx);
		}
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _FORWARD_PLUS
			#pragma multi_compile_fog
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
				float fogFactor : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes IN)
			{
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = p.positionCS;
				OUT.positionWS = p.positionWS;
				OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
				return OUT;
			}

			float3 ShadeLight(Light l, float3 N, float3 V, float3 diffCol, float3 specCol, float rough, float3 glintN, float glintMask)
			{
				float3 H = SafeNormalize(l.direction + V);
				float NdotL = saturate(dot(N, l.direction));
				float NdotH = saturate(dot(N, H));
				float r2 = max(rough * rough, 0.002);
				float d = NdotH * NdotH * (r2 - 1.0) + 1.0;
				float spec = r2 / (4.0 * PI * d * d + 1e-4) * 0.25 * PI;
				float glint = pow(saturate(dot(glintN, H)), _GlintSharp) * glintMask * _GlintStrength;
				float3 rad = l.color * (l.distanceAttenuation * l.shadowAttenuation);
				return rad * (NdotL * (diffCol + specCol * spec) + specCol * glint * NdotL);
			}

			half4 frag(Varyings IN) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
				float3 albedo = tex.rgb * _BaseColor.rgb;
				float metalMask = _MetalFromAlpha > 0.5 ? tex.a : 1.0;
				float metallic = _Metallic * metalMask;
				float smooth = lerp(_ClothSmoothness, _Smoothness, metalMask);

				float3 N = normalize(IN.normalWS);
				float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

				// Glitter grain: per-speck normal tilt in UV space
				float2 sg = IN.uv * _SparkleTiling;
				float3 sh = GoldHash3(float3(floor(sg), 7.0));
				float3 tilt = (sh - 0.5) * 2.0 * _Sparkle * metalMask;
				N = normalize(N + tilt);
				float speckTone = lerp(1.0 - _SparkleTint * 0.5, 1.0 + _SparkleTint * 0.5, sh.z);
				speckTone = lerp(1.0, speckTone, saturate(_Sparkle * 3.0) * metalMask);

				// Glints: world-space facet cells
				float3 cell = floor(IN.positionWS * _GlintScale);
				float3 gh = GoldHash3(cell + 17.0);
				float3 glintN = normalize(N + (gh - 0.5) * 0.6);
				float glintMask = step(1.0 - _GlintShare, GoldHash3(cell + 3.1).x) * metalMask;

				float3 diffCol = albedo * (1.0 - metallic);
				float3 specCol = lerp(0.04.xxx, albedo, metallic) * speckTone;
				float rough = 1.0 - smooth;
				rough = rough * rough;

				float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				float3 col = ShadeLight(mainLight, N, V, diffCol, specCol, rough, glintN, glintMask);

				#if defined(_ADDITIONAL_LIGHTS)
				uint cnt = GetAdditionalLightsCount();
				#if USE_FORWARD_PLUS
				InputData inputData = (InputData)0;
				inputData.positionWS = IN.positionWS;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
				for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
				{
					Light l = GetAdditionalLight(li, IN.positionWS, half4(1,1,1,1));
					col += ShadeLight(l, N, V, diffCol, specCol, rough, glintN, glintMask);
				}
				#endif
				LIGHT_LOOP_BEGIN(cnt)
					Light l = GetAdditionalLight(lightIndex, IN.positionWS, half4(1,1,1,1));
					col += ShadeLight(l, N, V, diffCol, specCol, rough, glintN, glintMask);
				LIGHT_LOOP_END
				#endif

				// Ambient: SH diffuse + reflection probe
				float3 sh9 = SampleSH(N);
				float3 R = reflect(-V, N);
				float3 env = GlossyEnvironmentReflection(R, rough, 1.0);
				float NdotV = saturate(dot(N, V));
				float3 fres = specCol + (max(smooth.xxx, specCol) - specCol) * pow(1.0 - NdotV, 5.0);
				col += diffCol * sh9 + env * fres;

				// Overhead hot spot and darker underneath
				float up = N.y;
				col += specCol * _Key * pow(saturate(dot(R, float3(0,1,0))), 8.0) * saturate(smooth + 0.2);
				col *= lerp(1.0, 1.0 - _Shade, saturate(-up * 0.5 + 0.5) * saturate(-up + 0.5));

				// Lift keeps metal from going black
				col = max(col, albedo * _Lift);

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

			Varyings vert(Attributes IN)
			{
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
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
			half4 frag(Varyings IN) : SV_Target
			{
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

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			Varyings vert(Attributes IN)
			{
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				return OUT;
			}
			half4 frag(Varyings IN) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				return 0;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
