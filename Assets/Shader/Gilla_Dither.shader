Shader "Gilla/Dither" {
	Properties {
		_MainTex ("Picture", 2D) = "white" {}
		_Tint ("Tint", Vector) = (1,1,1,1)
		_Cells ("Pixels across, down", Vector) = (160,160,0,0)
		_Lod ("Mip level to read (the source's size over the pixels')", Float) = 0
		_Levels ("Levels a channel", Float) = 6
		_Gain ("Brightness", Range(0, 1)) = 1
		_Reveal ("Reveal from the middle", Range(0, 1.5)) = 1.5
		_Glitch ("Glitch shove (pixels)", Float) = 0
		_GlitchBand ("Glitch band (middle, half height, 0 to 1)", Vector) = (0.5,0.05,0,0)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Tint;
				float4 _Cells;
				float _Lod;
				float _Levels;
				float _Gain;
				float _Reveal;
				float _Glitch;
				float4 _GlitchBand;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float fogFactor : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.fogFactor = ComputeFogFactor(o.positionCS.z);
				return o;
			}

			float Bayer4(float2 p) {
				uint2 q = (uint2)fmod(abs(p), 4.0);
				const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
				return (m[q.y * 4 + q.x] + 0.5) / 16.0;
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float2 cells = max(_Cells.xy, 1.0);
				float2 uv = i.uv;

				// Glitch: shove rows inside the band sideways by whole pixels
				float bandHalf = max(_GlitchBand.y, 1e-5);
				float inBand = step(abs(uv.y - _GlitchBand.x), bandHalf);
				uv.x += inBand * _Glitch / cells.x;

				float2 cell = floor(uv * cells);
				float2 cuv = (cell + 0.5) / cells;

				half4 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, cuv, _Lod);
				col *= _Tint;
				col.rgb *= _Gain;

				// Ordered dither + quantize
				float levels = max(_Levels, 2.0);
				float steps = levels - 1.0;
				float d = Bayer4(cell);
				col.rgb = floor(saturate(col.rgb) * steps + d) / steps;

				// Reveal from the middle (cell-stepped, dithered edge)
				float r = length((cell + 0.5) / cells - 0.5) * 2.0;
				float reveal = step(r, _Reveal + (d - 0.5) * 0.1);
				col.a *= reveal;

				col.rgb = MixFog(col.rgb, i.fogFactor);
				return col;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
