Shader "Gilla/FX" {
	Properties {
		_Tint ("Tint", Vector) = (1,1,1,1)
		_Brightness ("Brightness", Range(0, 8)) = 1
		_Soft ("Softness", Range(0.2, 4)) = 1.5
		_Shape ("Shape (0 blob, 1 band across a trail)", Range(0, 1)) = 0
		_Ring ("Ring (0 blob, else a ring this thick)", Range(0, 1)) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend From", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend To", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "FX"
			Tags { "LightMode"="UniversalForward" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _Tint;
				float _Brightness;
				float _Soft;
				float _Shape;
				float _Ring;
				float _SrcBlend;
				float _DstBlend;
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
				float fogFactor : TEXCOORD1;
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
				o.uv = v.uv;
				o.color = v.color;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 c = i.uv * 2.0 - 1.0;
				float dBlob = length(c);
				float dBand = abs(c.y);
				float d = lerp(dBlob, dBand, _Shape);

				float ringD = abs(d - (1.0 - _Ring * 0.5)) / max(_Ring * 0.5, 1e-3);
				d = (_Ring > 0.001) ? ringD : d;

				float f = pow(saturate(1.0 - d), _Soft);

				float3 col = _Tint.rgb * i.color.rgb * _Brightness * f;
				float a = saturate(f * _Tint.a * i.color.a);
				col *= _Tint.a * i.color.a;
				col = MixFogColor(col, half3(0,0,0), i.fogFactor);
				return half4(col, a);
			}
			ENDHLSL
		}
	}
}
