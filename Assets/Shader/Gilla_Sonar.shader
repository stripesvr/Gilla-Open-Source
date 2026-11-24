Shader "Gilla/Sonar" {
	Properties {
		_Color ("Colour", Vector) = (0.35,0.95,1,1)
		_Fade ("How Much of It Shows", Range(0, 1)) = 1
		_BandSpacing ("Scan Bands Apart (m)", Float) = 0.18
		_BandSpeed ("Scan Bands Rise (m/s)", Float) = 0.9
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "Sonar"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha One
			ZWrite Off
			ZTest Always
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _Color;
				float _Fade;
				float _BandSpacing;
				float _BandSpeed;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 vd = normalize(GetWorldSpaceViewDir(i.positionWS));
				float rim = pow(1.0 - saturate(abs(dot(n, vd))), 2.0);
				float spacing = max(_BandSpacing, 0.001);
				float ph = frac((i.positionWS.y - _Time.y * _BandSpeed) / spacing);
				float band = smoothstep(0.75, 1.0, ph) + smoothstep(0.15, 0.0, ph) * 0.5;
				float a = saturate(0.15 + rim * 0.85 + band * 0.5) * _Color.a * _Fade;
				return half4(_Color.rgb * (1.0 + band), a);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
