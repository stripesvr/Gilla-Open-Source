Shader "Gilla/Glass" {
	Properties {
		[MainColor] _BaseColor ("Colour (alpha: how solid)", Color) = (0.55,0.78,1,0.28)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.85
		_Fade ("How Much of It Is There", Range(0, 1)) = 1
		_DitherCell ("Dither Cell (m)", Range(0.002, 0.1)) = 0.012
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
	}
	SubShader {
		// Redux: real see-through glass (alpha blended), not a screen-door dither.
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseColor;
			float _Smoothness;
			float _Fade;
			float _DitherCell;
			float _Cull;
		CBUFFER_END

		float GlassHash(float3 p)
		{
			p = frac(p * float3(0.1031, 0.1030, 0.0973));
			p += dot(p, p.yxz + 33.33);
			return frac((p.x + p.y) * p.z);
		}

		// World-space screen-door dither: each cell of size _DitherCell is either present or not.
		void GlassDitherClip(float3 posWS)
		{
			float cov = saturate(_BaseColor.a) * saturate(_Fade);
			float3 cell = floor(posWS / max(_DitherCell, 0.0005));
			float t = GlassHash(cell);
			clip(cov - t - 0.0001);
		}
		ENDHLSL

		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Cull [_Cull]
			ZWrite Off
			Blend One OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float fogFactor : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS) * (front ? 1.0 : -1.0);
				float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
				float smooth = saturate(_Smoothness);
				float specPow = exp2(10.0 * smooth + 1.0);
				float3 albedo = _BaseColor.rgb;

				// The body of the glass: a faint tint of what's behind it (alpha below), lit softly.
				float3 col = SampleSH(n) * albedo * saturate(_BaseColor.a + 0.25);
				float3 shine = 0;

				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
				Light ml = GetMainLight(shadowCoord);
				{
					float3 atten = ml.color * ml.distanceAttenuation * ml.shadowAttenuation;
					float ndl = saturate(dot(n, ml.direction));
					float3 h = SafeNormalize(ml.direction + v);
					float spec = pow(saturate(dot(n, h)), specPow) * smooth;
					col += atten * albedo * ndl * saturate(_BaseColor.a);
					shine += atten * spec * ndl * 4.0;
				}
				#if defined(_ADDITIONAL_LIGHTS)
				uint count = GetAdditionalLightsCount();
				for (uint li = 0u; li < count; li++)
				{
					Light l = GetAdditionalLight(li, i.positionWS, half4(1,1,1,1));
					float3 atten = l.color * l.distanceAttenuation * l.shadowAttenuation;
					float ndl = saturate(dot(n, l.direction));
					float3 h = SafeNormalize(l.direction + v);
					float spec = pow(saturate(dot(n, h)), specPow) * smooth;
					col += atten * albedo * ndl * saturate(_BaseColor.a);
					shine += atten * spec * ndl * 4.0;
				}
				#endif

				// Fresnel reflection of the environment
				float fres = pow(1.0 - saturate(dot(n, v)), 5.0);
				float3 r = reflect(-v, n);
				float3 env = GlossyEnvironmentReflection(r, 1.0 - smooth, 1.0);
				float reflectAmount = lerp(0.14, 1.0, fres) * smooth;
				shine += env * reflectAmount;

				// Premultiplied: the tinted body lets the scene through by (1 - alpha); reflections and
				// highlights are added on top, and the glass gets more solid at glancing angles.
				float alpha = saturate(lerp(max(saturate(_BaseColor.a) * 1.25, 0.3), 1.0, fres * 0.9)) * saturate(_Fade);
				col = col * alpha + shine * saturate(_Fade);
				col = MixFogColor(col, half3(0, 0, 0), i.fogFactor);
				return half4(col, alpha);
			}
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
			Cull [_Cull]
			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 pw = TransformObjectToWorld(v.positionOS.xyz);
				float3 nw = TransformObjectToWorldNormal(v.normalOS);
				#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
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
				o.positionWS = pw;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				GlassDitherClip(i.positionWS);
				return 0;
			}
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			Cull [_Cull]
			ZWrite On
			ColorMask R

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
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = TransformWorldToHClip(o.positionWS);
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				GlassDitherClip(i.positionWS);
				return i.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
