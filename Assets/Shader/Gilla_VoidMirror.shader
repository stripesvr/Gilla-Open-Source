Shader "Gilla/VoidMirror" {
	Properties {
		[HDR] _Spark ("Speck Colour", Vector) = (0.3,1.8,0.4,1)
		_Cell ("Speck Spacing (m)", Range(0.01, 0.2)) = 0.036
		_Depth ("Specks Behind the Glass (m)", Range(0, 2)) = 0.15
		_Band ("Wave Spacing (m)", Range(0.1, 3)) = 0.9
		_Roll ("Waves Roll Up (m a second)", Range(0, 1)) = 0.05
		_Pulse ("Pulses a Second", Range(0, 3)) = 0.4
		_PulseLength ("Pulse Length (m)", Range(0.2, 5)) = 1.6
		[HDR] _Star ("Star Colour", Vector) = (1.1,1.2,1.1,1)
		_Tint ("Reflection Tint", Vector) = (1,1,1,1)
		[HideInInspector] [NoScaleOffset] _MirrorTexL ("Left Eye's Reflection", 2D) = "black" {}
		[HideInInspector] [NoScaleOffset] _MirrorTexR ("Right Eye's Reflection", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
		Pass {
			Name "Unlit"
			Tags { "LightMode"="UniversalForward" }
			ZWrite On
			Cull Back
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MirrorTexL); SAMPLER(sampler_MirrorTexL);
			TEXTURE2D(_MirrorTexR); SAMPLER(sampler_MirrorTexR);

			CBUFFER_START(UnityPerMaterial)
				float4 _Spark;
				float _Cell;
				float _Depth;
				float _Band;
				float _Roll;
				float _Pulse;
				float _PulseLength;
				float4 _Star;
				float4 _Tint;
				float4 _MirrorTexL_ST;
				float4 _MirrorTexR_ST;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float3 tangentWS : TEXCOORD3;
				float3 bitangentWS : TEXCOORD4;
				float fogFactor : TEXCOORD5;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
				o.positionCS = p.positionCS;
				o.positionWS = p.positionWS;
				o.screenPos = ComputeScreenPos(p.positionCS);
				o.normalWS = n.normalWS;
				o.tangentWS = n.tangentWS;
				o.bitangentWS = n.bitangentWS;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			float hash21(float2 p) {
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			float specks(float2 uv, float cell) {
				float2 g = uv / cell;
				float2 id = floor(g);
				float2 f = frac(g) - 0.5;
				float h = hash21(id);
				float2 off = float2(hash21(id + 7.1), hash21(id + 3.3)) - 0.5;
				float d = length(f - off * 0.6);
				return smoothstep(0.18, 0.0, d) * step(0.55, h) * h;
			}

			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float2 suv = i.screenPos.xy / i.screenPos.w;
				float3 refl;
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				if (unity_StereoEyeIndex == 0)
					refl = SAMPLE_TEXTURE2D(_MirrorTexL, sampler_MirrorTexL, float2(1 - suv.x, suv.y)).rgb;
				else
					refl = SAMPLE_TEXTURE2D(_MirrorTexR, sampler_MirrorTexR, float2(1 - suv.x, suv.y)).rgb;
				#else
				refl = SAMPLE_TEXTURE2D(_MirrorTexL, sampler_MirrorTexL, float2(1 - suv.x, suv.y)).rgb;
				#endif
				refl *= _Tint.rgb;

				// parallax specks behind glass
				float3 N = normalize(i.normalWS);
				float3 T = normalize(i.tangentWS);
				float3 B = normalize(i.bitangentWS);
				float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
				float3 vT = float3(dot(V, T), dot(V, B), max(dot(V, N), 0.05));
				float2 planeUV = float2(dot(i.positionWS, T), dot(i.positionWS, B));
				float2 deepUV = planeUV - vT.xy / vT.z * _Depth;

				float t = _Time.y;
				float s = specks(deepUV, _Cell);
				float s2 = specks(deepUV * 0.5 + 11.7, _Cell * 3.0);

				float wave = 0.5 + 0.5 * sin((i.positionWS.y - t * _Roll) * 6.2831853 / _Band);
				float pulsePos = frac(t * _Pulse) * (_PulseLength + 4.0) - 2.0;
				float pulse = saturate(1.0 - abs(frac((i.positionWS.y - pulsePos) / _PulseLength + 0.5) - 0.5) * 2.0);
				float glow = wave * 0.6 + pulse * pulse;

				float3 col = refl + _Spark.rgb * s * glow + _Star.rgb * s2 * (0.6 + 0.4 * sin(t * 3.0 + s2 * 20.0));
				col = MixFog(col, i.fogFactor);
				return half4(col, 1);
			}
			ENDHLSL
		}
		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }
			ZWrite On
			ColorMask R
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
			Varyings vert(Attributes v) {
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				return o;
			}
			half4 frag(Varyings i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return 0;
			}
			ENDHLSL
		}
	}
}
