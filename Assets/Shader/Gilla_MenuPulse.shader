Shader "Gilla/MenuPulse" {
	Properties {
		[MainTexture] _BaseMap ("Picture", 2D) = "white" {}
		_Color ("Colour", Vector) = (0.86,0.06,0.13,1)
		_Color2 ("Pulses To", Vector) = (0.95,0.32,0.62,1)
		_Period ("A Pulse's Seconds", Range(0.2, 6)) = 1.6
		_Wash ("The Wash Over Its Dark", Range(0, 0.5)) = 0.12
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			Cull Back
			ZWrite On
			Blend Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseMap_ST;
				float4 _Color;
				float4 _Color2;
				float _Period;
				float _Wash;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float fogFactor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
				float period = max(_Period, 0.0001);
				float pulse = 0.5 - 0.5 * cos(_Time.y * 6.28318530718 / period);
				half3 tint = lerp(_Color.rgb, _Color2.rgb, pulse);
				half3 col = tex.rgb * tint;
				// wash the dark areas with the tint so they aren't pure black
				half lum = dot(tex.rgb, half3(0.299, 0.587, 0.114));
				col += tint * _Wash * (1.0 - lum);
				col = MixFog(col, i.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
