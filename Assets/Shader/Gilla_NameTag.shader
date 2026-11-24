Shader "Gilla/NameTag" {
	Properties {
		[MainTexture] _BaseMap ("Letters", 2D) = "white" {}
		[MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
		_TagColour ("Title's Colour (NameTag sets it)", Vector) = (1,1,1,1)
		_TagLight ("Title's Lighter Colour (NameTag sets it)", Vector) = (1,1,1,1)
		_Period ("Texels To A Band", Float) = 34
		_Speed ("Bands A Second", Float) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
		Pass {
			Name "NameTag"
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

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseMap_ST;
				float4 _BaseMap_TexelSize;
				float4 _BaseColor;
				float4 _TagColour;
				float4 _TagLight;
				float _Period;
				float _Speed;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float fogFactor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = p.positionCS;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.color = v.color;
				o.fogFactor = ComputeFogFactor(p.positionCS.z);
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

				// Diagonal moving bands measured in texels
				float2 texel = i.uv * _BaseMap_TexelSize.zw;
				float period = max(_Period, 1.0);
				float phase = (texel.x + texel.y) / period - _Time.y * _Speed;
				float band = 0.5 + 0.5 * sin(phase * 6.2831853);
				band = smoothstep(0.35, 0.65, band);

				float3 title = lerp(_TagColour.rgb, _TagLight.rgb, band);
				half4 col;
				col.rgb = tex.rgb * _BaseColor.rgb * title * i.color.rgb;
				col.a = saturate(tex.a * _BaseColor.a * _TagColour.a * i.color.a);
				col.rgb = MixFog(col.rgb, i.fogFactor);
				return col;
			}
			ENDHLSL
		}
	}
	FallBack Off
}
