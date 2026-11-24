Shader "Gilla/BloodSplat" {
	Properties {
		[MainTexture] _BaseMap ("Splats", 2D) = "white" {}
		_Probes ("Lit By Its Own Light Probes", Range(0, 1)) = 0
		_Ambient ("Never Darker Than", Range(0, 1)) = 0.12
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10" "IgnoreProjector"="True" }
		Pass {
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back
			Offset -1, -1

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseMap_ST;
				float _Probes;
				float _Ambient;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 color : TEXCOORD3;
				float fogFactor : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes IN) {
				Varyings OUT = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = p.positionCS;
				OUT.positionWS = p.positionWS;
				OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
				OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				OUT.color = IN.color;
				OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * IN.color;
				float3 n = normalize(IN.normalWS);

				// ambient: blend between flat world-up probe and own-normal probe
				half3 shUp = SampleSH(float3(0, 1, 0));
				half3 shN = SampleSH(n);
				half3 light = lerp(shUp, shN, _Probes);

				float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				light += mainLight.color * saturate(dot(n, mainLight.direction)) * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

				#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
				uint count = GetAdditionalLightsCount();
				for (uint i = 0u; i < count; i++) {
					Light l = GetAdditionalLight(i, IN.positionWS);
					light += l.color * saturate(dot(n, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
				}
				#endif

				light = max(light, _Ambient.xxx);
				half3 col = tex.rgb * light;
				col = MixFog(col, IN.fogFactor);
				return half4(col, tex.a);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
