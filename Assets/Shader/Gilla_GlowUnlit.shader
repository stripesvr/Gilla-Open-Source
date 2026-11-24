Shader "Gilla/GlowUnlit" {
	Properties {
		[MainTexture] _BaseMap ("Texture", 2D) = "white" {}
		[MainColor] _BaseColor ("Colour", Color) = (1,1,1,1)
		_Glow ("Brightness", Range(0, 6)) = 1.5
		_RimColor ("Rim", Vector) = (1,1,1,1)
		_RimPower ("Rim Tightness", Range(0.5, 12)) = 3
		_RimStrength ("Rim Brightness", Range(0, 6)) = 0
		_DarkMiddle ("Darken Middle (black hole)", Range(0, 1)) = 0
		_Pulse ("Pulse", Range(0, 1)) = 0
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
				float4 _BaseColor;
				float _Glow;
				float4 _RimColor;
				float _RimPower;
				float _RimStrength;
				float _DarkMiddle;
				float _Pulse;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float fogFactor : TEXCOORD3;
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
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
				float3 n = normalize(IN.normalWS);
				float3 v = normalize(GetWorldSpaceViewDir(IN.positionWS));
				float ndv = saturate(dot(n, v));
				float rim = pow(1.0 - ndv, _RimPower);

				float pulse = 1.0 + _Pulse * 0.5 * sin(_Time.y * 4.0);
				float3 col = tex.rgb * _BaseColor.rgb * _Glow * pulse;
				// black hole: darken where facing camera
				col *= lerp(1.0, 1.0 - ndv, _DarkMiddle);
				col += _RimColor.rgb * rim * _RimStrength * pulse;

				col = MixFog(col, IN.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
