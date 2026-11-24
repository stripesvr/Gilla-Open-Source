Shader "Gilla/LampHalo" {
	Properties {
		_Strength ("Strength", Float) = 1
		_NearFade ("Gone this close (m)", Float) = 0.6
		_FarFade ("Gone this far (m)", Float) = 32
		_FlickerSpeed ("Flicker speed", Float) = 6
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "LampHalo"
			Tags { "LightMode"="UniversalForward" }
			Blend One One
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float _Strength;
				float _NearFade;
				float _FarFade;
				float _FlickerSpeed;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float3 positionWS : TEXCOORD1;
				float seed : TEXCOORD2;
				float fogFactor : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.uv = v.uv;
				o.color = v.color;
				float3 objPos = TransformObjectToWorld(float3(0,0,0));
				o.seed = frac(sin(dot(objPos, float3(12.9898, 78.233, 37.719))) * 43758.5453);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float2 d = i.uv * 2.0 - 1.0;
				float r2 = dot(d, d);
				float halo = saturate(1.0 - r2);
				halo = halo * halo;
				halo += exp(-r2 * 12.0) * 0.5;

				float dist = distance(_WorldSpaceCameraPos.xyz, i.positionWS);
				float nearF = saturate((dist - _NearFade) / max(_NearFade, 1e-3));
				float farF = saturate((_FarFade - dist) / max(_FarFade * 0.25, 1e-3));

				float t = _Time.y * _FlickerSpeed + i.seed * 100.0;
				float flicker = 0.85 + 0.1 * sin(t) + 0.05 * sin(t * 2.37 + 1.3);

				float3 col = i.color.rgb * i.color.a * halo * nearF * farF * flicker * _Strength;
				col = MixFogColor(col, half3(0,0,0), i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}
	}
}
