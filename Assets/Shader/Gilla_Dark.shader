Shader "Gilla/Dark" {
	Properties {
		_Dark ("How much light's left", Range(0, 1)) = 0.2
		_Beam ("The flashlight's cone: 0 off, 1 on", Range(0, 1)) = 1
		_Inner ("Fully lit within this (cosine of its angle)", Range(0, 1)) = 0.96
		_Outer ("No light past this (cosine of its angle)", Range(0, 1)) = 0.84
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay" "IgnoreProjector"="True" }

		Pass {
			Name "Darkness"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float _Dark;
				float _Beam;
				float _Inner;
				float _Outer;
			CBUFFER_END

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

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 dir = normalize(i.positionWS - _WorldSpaceCameraPos);
				float3 fwd = normalize(-UNITY_MATRIX_V[2].xyz);
				float c = dot(dir, fwd);
				float lo = min(_Outer, _Inner - 0.0001);
				float cone = smoothstep(lo, _Inner, c) * _Beam;

				float alpha = (1.0 - _Dark) * (1.0 - cone);
				return half4(0, 0, 0, saturate(alpha));
			}
			ENDHLSL
		}
	}
	FallBack Off
}
