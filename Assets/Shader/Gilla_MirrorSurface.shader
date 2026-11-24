Shader "Gilla/MirrorSurface" {
	Properties {
		_Tint ("Tint", Vector) = (0.94,0.96,1,1)
		[HideInInspector] [NoScaleOffset] _MirrorTexL ("Left Eye's Reflection", 2D) = "black" {}
		[HideInInspector] [NoScaleOffset] _MirrorTexR ("Right Eye's Reflection", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			Cull Back
			ZWrite On
			ZTest LEqual
			Blend Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MirrorTexL); SAMPLER(sampler_MirrorTexL);
			TEXTURE2D(_MirrorTexR); SAMPLER(sampler_MirrorTexR);

			CBUFFER_START(UnityPerMaterial)
				float4 _Tint;
				float4 _MirrorTexL_ST;
				float4 _MirrorTexR_ST;
			CBUFFER_END

			// Set from MirrorSurface.cs: world -> mirror camera clip space, per eye
			float4x4 _MirrorLookupL;
			float4x4 _MirrorLookupR;

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float fogFactor : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = vpi.positionCS;
				OUT.positionWS = vpi.positionWS;
				OUT.fogFactor = ComputeFogFactor(vpi.positionCS.z);
				return OUT;
			}

			half4 frag (Varyings IN) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				float4 wp = float4(IN.positionWS, 1.0);
				half3 col;
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				bool rightEye = unity_StereoEyeIndex == 1;
				#else
				bool rightEye = false;
				#endif
				if (rightEye) {
					float4 c = mul(_MirrorLookupR, wp);
					float2 uv = c.xy / max(abs(c.w), 1e-5) * 0.5 + 0.5;
					uv = TRANSFORM_TEX(uv, _MirrorTexR);
					col = SAMPLE_TEXTURE2D(_MirrorTexR, sampler_MirrorTexR, uv).rgb;
				} else {
					float4 c = mul(_MirrorLookupL, wp);
					float2 uv = c.xy / max(abs(c.w), 1e-5) * 0.5 + 0.5;
					uv = TRANSFORM_TEX(uv, _MirrorTexL);
					col = SAMPLE_TEXTURE2D(_MirrorTexL, sampler_MirrorTexL, uv).rgb;
				}
				col *= _Tint.rgb;
				col = MixFog(col, IN.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}

		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On
			ColorMask R
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _Tint;
				float4 _MirrorTexL_ST;
				float4 _MirrorTexR_ST;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				return OUT;
			}
			half frag (Varyings IN) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				return IN.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
