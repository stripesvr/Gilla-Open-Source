Shader "Gilla/Toast" {
	Properties {
		[MainTexture] _BaseMap ("Panel", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "Toast"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseMap_ST;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes IN) {
				Varyings OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				OUT.color = IN.color;
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * IN.color;
				clip(c.a - 0.001h);
				return c;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
