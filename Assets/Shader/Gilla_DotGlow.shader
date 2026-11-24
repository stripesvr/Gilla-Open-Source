Shader "Gilla/DotGlow" {
	Properties {
		[HDR] _Color ("Colour", Vector) = (1,0.15,0.11,1)
		_Base ("The Foot Tier's Share of Dots", Range(0, 1)) = 0.85
		_Step ("Each Tier's Share of the One Under It's", Range(0.1, 1)) = 0.6
		_DotSize ("Dot Size", Range(1, 4)) = 2
		_Density ("Dots Across The Surface", Range(20, 1000)) = 400
		_FadeStart ("Starts Fading At (metres)", Range(0, 30)) = 10
		_FadeEnd ("Gone By (metres)", Range(0.1, 40)) = 18
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
		Pass {
			Name "DotGlow"
			Tags { "LightMode"="UniversalForward" }
			Blend One One
			ZWrite Off
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _Color;
				float _Base;
				float _Step;
				float _DotSize;
				float _Density;
				float _FadeStart;
				float _FadeEnd;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float Hash21(float2 p)
			{
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.uv = v.uv;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Redux: dots are pinned to the surface (its UVs) instead of the screen. Screen-pixel
				// dots differ between the two eyes and swim as your head moves, which is trippy in VR.
				float2 cell = floor(i.uv * (_Density / max(_DotSize, 1.0)));

				// Tiers stacked up the surface: the foot tier is densest, each tier above keeps _Step of the one below.
				float tier = floor(saturate(i.uv.y) * 4.999);
				float share = _Base * pow(_Step, tier);

				float rnd = Hash21(cell + tier * 17.0);
				float dotMask = rnd < share ? 1.0 : 0.0;

				float dist = distance(GetCameraPositionWS(), i.positionWS);
				float fade = 1.0 - saturate((dist - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));

				float k = dotMask * fade * _Color.a;
				return half4(_Color.rgb * k, k);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
