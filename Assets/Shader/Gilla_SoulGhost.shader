Shader "Gilla/SoulGhost" {
	Properties {
		_Color ("Glow", Vector) = (0.6,0.35,1,1)
		_Core ("Middle", Vector) = (0.12,0.06,0.25,1)
		_RimPower ("Rim Tightness", Range(0.5, 8)) = 2
		_Bands ("Bands per Metre", Float) = 14
		_Alpha ("Fade", Range(0, 1)) = 1
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Pass {
			Name "SoulGhost"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha One
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _Color;
				float4 _Core;
				float _RimPower;
				float _Bands;
				float _Alpha;
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
				float fogFactor : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
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

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 vd = normalize(GetWorldSpaceViewDir(i.positionWS));
				float ndv = saturate(abs(dot(n, vd)));
				float rim = pow(1.0 - ndv, _RimPower);

				float band = 0.5 + 0.5 * sin((i.positionWS.y * _Bands - _Time.y * 2.0) * 6.2831853);
				band = lerp(0.65, 1.0, band);

				float3 col = lerp(_Core.rgb, _Color.rgb, rim) * band;
				float a = saturate(lerp(0.25, 1.0, rim) * band * _Color.a) * _Alpha;
				col = MixFogColor(col, half3(0,0,0), i.fogFactor);
				return half4(col, a);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
