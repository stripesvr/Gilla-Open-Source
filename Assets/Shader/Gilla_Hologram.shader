Shader "Gilla/Hologram" {
	Properties {
		_TopColor ("Top", Vector) = (0.25,1,0.55,1)
		_BottomColor ("Foot", Vector) = (0,0.07,0.03,1)
		_RimColor ("Rim", Vector) = (0.7,1,0.8,1)
		_RimPower ("Rim Tightness", Range(0.5, 8)) = 2.5
		_Base ("Gradient Foot (world Y)", Float) = 0
		_Height ("Gradient Height (m)", Float) = 0.5
		_ScanDensity ("Scanlines per Metre", Float) = 90
		_ScanSpeed ("Scanline Rise (per s)", Float) = 0.6
		_ScanStrength ("Scanline Brightness", Range(0, 1)) = 0.35
		_Flicker ("Flicker", Range(0, 0.5)) = 0.08
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Pass {
			Name "Hologram"
			Tags { "LightMode"="UniversalForward" }
			Blend One One
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _TopColor;
				float4 _BottomColor;
				float4 _RimColor;
				float _RimPower;
				float _Base;
				float _Height;
				float _ScanDensity;
				float _ScanSpeed;
				float _ScanStrength;
				float _Flicker;
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

			Varyings vert (Attributes v) {
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

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 n = normalize(i.normalWS);
				float3 vdir = normalize(GetWorldSpaceViewDir(i.positionWS));
				float t = saturate((i.positionWS.y - _Base) / max(_Height, 1e-4));
				float3 col = lerp(_BottomColor.rgb, _TopColor.rgb, t);

				float rim = pow(1.0 - saturate(abs(dot(n, vdir))), _RimPower);
				col += _RimColor.rgb * rim;

				float scan = frac(i.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed * _ScanDensity);
				scan = smoothstep(0.0, 0.5, scan) * smoothstep(1.0, 0.5, scan);
				col *= 1.0 + scan * _ScanStrength;

				float fl = sin(_Time.y * 37.0) * sin(_Time.y * 13.3 + 1.7);
				col *= 1.0 - _Flicker * (0.5 + 0.5 * fl);

				col *= ComputeFogIntensity(i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}
	}
}
