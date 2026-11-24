Shader "Hidden/Gilla/WaterSim" {
	Properties {
		_MainTex ("", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		ZWrite Off ZTest Always Cull Off Blend Off

		Pass {
			Name "WaterSim"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
			CBUFFER_END

			// Height-field wave simulation: R = current height, G = previous height
			float4 Frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;
				float2 texel = _BlitTexture_TexelSize.xy;

				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
				float l = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0)).r;
				float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0)).r;
				float d = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, texel.y)).r;
				float u = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, texel.y)).r;

				float h = (l + r + d + u) * 0.5 - c.g;
				h *= 0.985;

				// Optional injected disturbance from _MainTex
				float2 muv = TRANSFORM_TEX(uv, _MainTex);
				h += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, muv).r;

				return float4(h, c.r, 0, 1);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
