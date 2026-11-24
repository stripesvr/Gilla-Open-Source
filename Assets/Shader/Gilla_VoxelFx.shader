Shader "Gilla/VoxelFx" {
	Properties {
		_Cell ("Blocks On A Grid This Big (m)", Float) = 0.125
		_DotSize ("Dot Size (screen pixels)", Range(1, 4)) = 2
		_Age ("How Long Since It Went Off (s; before that, nothing)", Float) = -1
		_Turn ("Turned About The Vertical (cos, sin)", Vector) = (1,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "VoxelFx"
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
				float _Cell;
				float _DotSize;
				float _Age;
				float4 _Turn;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float fade : TEXCOORD0;
				float fogFactor : TEXCOORD1;
				float3 dotWS : TEXCOORD7;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float Hash13(float3 p)
			{
				p = frac(p * 0.1031);
				p += dot(p, p.yzx + 33.33);
				return frac((p.x + p.y) * p.z);
			}

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 p = v.positionOS.xyz;
				float2 cs = _Turn.xy;
				float len = max(length(cs), 1e-4);
				cs /= len;
				p = float3(p.x * cs.x - p.z * cs.y, p.y, p.x * cs.y + p.z * cs.x);

				float cell = max(_Cell, 1e-4);
				float3 cellId = floor(p / cell);
				float rnd = Hash13(cellId + 17.0);
				float age = max(_Age, 0.0);

				float3 dir = normalize(cellId + 0.5 + 1e-3);
				float3 burst = dir * age * (1.0 + rnd * 2.0);
				burst.y += age * 2.0 - 4.9 * age * age * 0.5;

				p = (cellId + 0.5) * cell + (p - (cellId + 0.5) * cell) + burst;
				p = floor(p / cell + 0.5) * cell;

				float3 posWS = TransformObjectToWorld(p);
				o.positionCS = TransformWorldToHClip(posWS);
				o.dotWS = posWS;
				o.color = v.color;
				o.fade = (_Age < 0.0) ? 0.0 : saturate(1.0 - age / (1.0 + rnd));
				o.fogFactor = ComputeFogFactor(o.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				if (_Age < 0.0 || i.fade <= 0.001) discard;

				float ds = max(_DotSize, 1.0);
				float2 m = fmod(abs((float2(dot(i.dotWS, float3(1, 0, 0.7071)), dot(i.dotWS, float3(0, 1, 0.7071))) * 250.0)), ds * 2.0);
				if (m.x >= ds || m.y >= ds) discard;

				float3 col = i.color.rgb * i.color.a * i.fade * 1.4;
				col = MixFogColor(col, half3(0, 0, 0), i.fogFactor);
				return half4(col, 1.0);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
