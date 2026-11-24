Shader "Gilla/DreadOverlay" {
	Properties {
		[NoScaleOffset] _Scribbles ("Scribbles (8 marks across, 4 takes of each up)", 2D) = "black" {}
		_Color ("Ink", Vector) = (0.11,0.025,0.02,1)
		_Density ("Density (how near the monster is)", Range(0, 1)) = 0
		_Amount ("Share of the outer cells with a mark, at full density", Range(0, 1)) = 0.36
		_Cell ("Cell size (a mark at most in each)", Range(0.05, 0.5)) = 0.4
		_Clear ("Clear middle: no marks this near it", Range(0, 1)) = 0.3
		_Full ("As thick with marks as it gets from here out", Range(0.1, 2)) = 0.8
		_Switch ("Redraws a second", Range(0, 30)) = 16
		_Life ("Seconds a mark stays put", Range(0.05, 3)) = 0.45
		_Distance ("How far off the marks sit (m)", Range(0.3, 10)) = 2
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay" "IgnoreProjector"="True" }
		Pass {
			Name "DreadOverlay"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_Scribbles);
			SAMPLER(sampler_Scribbles);

			CBUFFER_START(UnityPerMaterial)
				float4 _Scribbles_ST;
				float4 _Color;
				float _Density;
				float _Amount;
				float _Cell;
				float _Clear;
				float _Full;
				float _Switch;
				float _Life;
				float _Distance;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float hash21(float2 p) {
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
				o.positionCS = TransformWorldToHClip(o.positionWS);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				if (_Density <= 0.0001) discard;

				float3 dirWS = i.positionWS - GetCameraPositionWS();
				float3 dirVS = mul((float3x3)UNITY_MATRIX_V, dirWS);
				float fwd = max(-dirVS.z, 1e-4);
				float2 tanXY = dirVS.xy / fwd;
				float2 p = tanXY * _Distance;

				// radial coverage: none inside _Clear, full beyond _Full
				float r = length(tanXY);
				float cover = saturate((r - _Clear) / max(_Full - _Clear, 1e-3));

				float cellSize = _Cell;
				float2 cellId = floor(p / cellSize);
				float2 local = frac(p / cellSize);

				float t = (_Switch > 0.001) ? floor(_Time.y * _Switch) / _Switch : _Time.y;
				float h0 = hash21(cellId);
				float life = max(_Life, 0.05);
				float epoch = floor(t / life + h0);
				float2 seed = cellId + epoch * float2(17.13, 31.71);

				float present = hash21(seed);
				if (present >= _Amount * _Density * cover) discard;

				float mark = floor(hash21(seed + 3.7) * 8.0);
				float take = (_Switch > 0.001) ? fmod(floor(_Time.y * _Switch) + floor(h0 * 4.0), 4.0) : 0.0;

				// slight jitter/scale inside the cell
				float sc = lerp(0.75, 1.0, hash21(seed + 9.1));
				float2 off = (float2(hash21(seed + 5.3), hash21(seed + 7.9)) - 0.5) * (1.0 - sc);
				float2 uv = (local - 0.5 - off) / sc + 0.5;
				if (any(uv < 0.0) || any(uv > 1.0)) discard;
				uv = (uv + float2(mark, take)) / float2(8.0, 4.0);

				half4 s = SAMPLE_TEXTURE2D(_Scribbles, sampler_Scribbles, uv);
				half ink = max(max(s.r, s.g), s.b) * s.a;
				half a = saturate(ink * _Color.a);
				if (a <= 0.001) discard;
				return half4(_Color.rgb, a);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
