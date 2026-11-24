Shader "Hidden/Gilla/UberPost" {
	Properties {
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		ZWrite Off ZTest Always Cull Off Blend Off

		Pass {
			Name "UberPost"

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			half4 Frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;
				half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

				// Mild vignette
				float2 d = uv - 0.5;
				half vig = saturate(1.0 - dot(d, d) * 0.6);
				col.rgb *= vig;

				// Gentle contrast curve
				col.rgb = saturate((col.rgb - 0.5h) * 1.05h + 0.5h);

				return col;
			}
			ENDHLSL
		}
	}
	Fallback Off
}
