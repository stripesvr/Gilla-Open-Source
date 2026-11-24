Shader "Gilla/DotDrop" {
	Properties {
		_DotSize ("Dot Size (screen pixels)", Range(1, 4)) = 2
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		LOD 100

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

			CBUFFER_START(UnityPerMaterial)
				float _DotSize;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float fogFactor : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float3 dotWS : TEXCOORD7;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.dotWS = p.positionWS;
				o.color = v.color;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Screen-space dot grid: each cell is (_DotSize + 1) pixels, the dot fills _DotSize pixels.
				float dotSize = max(1.0, floor(_DotSize + 0.5));
				float cell = dotSize + 1.0;
				float2 pix = floor((float2(dot(i.dotWS, float3(1, 0, 0.7071)), dot(i.dotWS, float3(0, 1, 0.7071))) * 250.0));
				float2 local = fmod(pix, cell);
				clip(dotSize - 0.5 - max(local.x, local.y));

				half3 col = i.color.rgb;
				col = MixFog(col, i.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
