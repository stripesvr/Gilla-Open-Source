Shader "Gilla/PixelGlow" {
	Properties {
		[HDR] _Color ("Colour", Vector) = (2.2,2.4,2.1,1)
		_Density ("Density at the Surface (per metre)", Range(0, 60)) = 20
		_Falloff ("Thins Out Over (metres up)", Range(0.01, 0.5)) = 0.09
		_DotSize ("Dot Size (screen pixels)", Range(1, 4)) = 2
		_Fizz ("Fizz (times each dot changes a second)", Range(0, 30)) = 12
		_FadeStart ("Starts Fizzling Out At (metres)", Range(0, 30)) = 3
		_FadeEnd ("Gone By (metres)", Range(0.1, 40)) = 6
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "PixelGlow"
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
				float4 _Color;
				float _Density;
				float _Falloff;
				float _DotSize;
				float _Fizz;
				float _FadeStart;
				float _FadeEnd;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float fogCoord : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float Hash13(float3 p)
			{
				p = frac(p * 0.1031);
				p += dot(p, p.zyx + 31.32);
				return frac((p.x + p.y) * p.z);
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
				o.fogCoord = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 cell = floor(i.positionCS.xy / max(_DotSize, 1.0));
				float tStep = floor(_Time.y * _Fizz);
				float eye = 0;
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
				eye = unity_StereoEyeIndex;
				#endif
				float rnd = Hash13(float3(cell, tStep + eye * 97.0));

				float3 originWS = TransformObjectToWorld(float3(0,0,0));
				float h = max(i.positionWS.y - originWS.y, 0.0);
				float dens = _Density * exp(-h / max(_Falloff, 0.001));
				float prob = saturate(dens * 0.02);

				float dist = distance(GetCameraPositionWS(), i.positionWS);
				float fade = 1.0 - saturate((dist - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));

				float dotOn = step(rnd, prob * fade);
				float3 col = _Color.rgb * dotOn * _Color.a;
				col = MixFogColor(col, half3(0,0,0), i.fogCoord);
				return half4(col, 1);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
