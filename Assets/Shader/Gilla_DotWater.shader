Shader "Gilla/DotWater" {
	Properties {
		_Deep ("Water", Vector) = (0.05,0.47,0.93,1)
		_Dots ("Dots", Vector) = (0.5,0.83,1,1)
		_Bright ("The Densest Tier's Dots", Vector) = (0.86,0.96,1,1)
		_AlphaDown ("How Solid, Looking Straight Down", Range(0, 1)) = 0.66
		_AlphaFar ("How Solid, Looking Along It", Range(0, 1)) = 0.97
		_DotSize ("Dot Size (screen pixels)", Range(1, 4)) = 2
		_Cell ("The Foam Map's Cell (m)", Float) = 0.125
		_FoamRange ("What 1 In The Foam Map Means (m)", Float) = 2.4
		[NoScaleOffset] _NoiseTex ("Noise (R big, G small)", 2D) = "gray" {}
		[NoScaleOffset] _FoamTex ("Foam Map (GillaSplashPool: R how far, G which)", 2D) = "white" {}
		_FoamRect ("Foam Map's Corner (x, z), Then 1 / Its Size", Vector) = (0,0,1,1)
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Pass {
			Name "DotWater"
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

			TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
			TEXTURE2D(_FoamTex);  SAMPLER(sampler_FoamTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _Deep;
				float4 _Dots;
				float4 _Bright;
				float _AlphaDown;
				float _AlphaFar;
				float _DotSize;
				float _Cell;
				float _FoamRange;
				float4 _FoamRect;
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
				float3 dotWS : TEXCOORD7;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert (Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.dotWS = p.positionWS;
				o.positionWS = p.positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float Hash21(float2 p) {
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			half4 frag (Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 n = normalize(i.normalWS);
				float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
				float facing = saturate(abs(dot(n, v)));
				float alpha = lerp(_AlphaFar, _AlphaDown, facing);

				// Foam map: world xz -> map uv, half-cell centred
				float2 foamUV = (i.positionWS.xz - _FoamRect.xy + 0.5 * _Cell) * _FoamRect.zw;
				half4 foam = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV);
				float dist = foam.r * _FoamRange;
				float nearFoam = saturate(1.0 - dist / max(_FoamRange, 1e-3));
				nearFoam *= nearFoam;

				// Noise drifting slowly
				float t = _Time.y;
				float nBig = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.positionWS.xz * 0.05 + t * float2(0.004, 0.003)).r;
				float nSmall = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.positionWS.xz * 0.3 - t * float2(0.01, 0.015)).g;

				float density = saturate((nBig - 0.45) * 0.8 + (nSmall - 0.5) * 0.3 + nearFoam);

				// Screen-space dot grid
				float2 pix = floor((float2(dot(i.dotWS, float3(1, 0, 0.7071)), dot(i.dotWS, float3(0, 1, 0.7071))) * 250.0) / max(_DotSize, 1.0));
				float sparse = (fmod(pix.x, 2.0) < 0.5 && fmod(pix.y, 2.0) < 0.5) ? 1.0 : 0.0;
				float h = Hash21(pix + floor(t * 2.0) * 0.0);
				float dotOn = (h < density) ? 1.0 : 0.0;
				dotOn *= lerp(sparse, 1.0, step(0.75, density));

				float3 dotCol = lerp(_Dots.rgb, _Bright.rgb, step(0.7, density));
				float3 col = lerp(_Deep.rgb, dotCol, dotOn);
				alpha = lerp(alpha, 1.0, dotOn * 0.5);

				col = MixFog(col, i.fogFactor);
				return half4(col, alpha);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
