Shader "Gilla/MonsterWounded"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _SmoothFromAlpha ("Smoothness From the Albedo's Alpha", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _UseBump ("Has a Normal Map", Float) = 0
        [NoScaleOffset] _EmissionMap ("Glow", 2D) = "white" {}
        [HDR] _EmissionColor ("Glow Colour", Vector) = (0,0,0,1)
        [Header(Wounds)] [NoScaleOffset] _FleshMap ("Raw Flesh Photo", 2D) = "red" {}
        _FleshScale ("Flesh Repeats (per unit of the body)", Float) = 5
        [NoScaleOffset] _WoundNoise ("What Roughens the Edges", 2D) = "gray" {}
        _Wound0 ("Wound", Vector) = (0,0,0,0)
        _Wound1 ("Wound", Vector) = (0,0,0,0)
        _Wound2 ("Wound", Vector) = (0,0,0,0)
        _Wound3 ("Wound", Vector) = (0,0,0,0)
        _Wound4 ("Wound", Vector) = (0,0,0,0)
        _Wound5 ("Wound", Vector) = (0,0,0,0)
        _Axis0 ("Way In, Length", Vector) = (0,0,1,1)
        _Axis1 ("Way In, Length", Vector) = (0,0,1,1)
        _Axis2 ("Way In, Length", Vector) = (0,0,1,1)
        _Axis3 ("Way In, Length", Vector) = (0,0,1,1)
        _Axis4 ("Way In, Length", Vector) = (0,0,1,1)
        _Axis5 ("Way In, Length", Vector) = (0,0,1,1)
        _Side0 ("Round From, Shape", Vector) = (1,0,0,0)
        _Side1 ("Round From, Shape", Vector) = (1,0,0,0)
        _Side2 ("Round From, Shape", Vector) = (1,0,0,0)
        _Side3 ("Round From, Shape", Vector) = (1,0,0,0)
        _Side4 ("Round From, Shape", Vector) = (1,0,0,0)
        _Side5 ("Round From, Shape", Vector) = (1,0,0,0)
        _Spot00 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot01 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot02 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot10 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot11 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot12 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot20 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot21 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot22 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot30 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot31 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot32 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot40 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot41 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot42 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot50 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot51 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _Spot52 ("Blotch: Where, How Big", Vector) = (0,0,0,0)
        _OffL ("Left Arm Off", Float) = 0
        _OffR ("Right Arm Off", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        TEXTURE2D(_FleshMap);    SAMPLER(sampler_FleshMap);
        TEXTURE2D(_WoundNoise);  SAMPLER(sampler_WoundNoise);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Smoothness;
            float _SmoothFromAlpha;
            float _Metallic;
            float _BumpScale;
            float _UseBump;
            float4 _EmissionColor;
            float _FleshScale;
            float4 _Wound0, _Wound1, _Wound2, _Wound3, _Wound4, _Wound5;
            float4 _Axis0, _Axis1, _Axis2, _Axis3, _Axis4, _Axis5;
            float4 _Side0, _Side1, _Side2, _Side3, _Side4, _Side5;
            float4 _Spot00, _Spot01, _Spot02;
            float4 _Spot10, _Spot11, _Spot12;
            float4 _Spot20, _Spot21, _Spot22;
            float4 _Spot30, _Spot31, _Spot32;
            float4 _Spot40, _Spot41, _Spot42;
            float4 _Spot50, _Spot51, _Spot52;
            float _OffL;
            float _OffR;
        CBUFFER_END

        float3 TriWeights(float3 n)
        {
            float3 w = abs(n);
            w = pow(w, 4.0);
            return w / max(w.x + w.y + w.z, 1e-4);
        }

        float4 SampleTri(TEXTURE2D_PARAM(tex, samp), float3 p, float3 n, float scale)
        {
            float3 w = TriWeights(n);
            float4 a = SAMPLE_TEXTURE2D(tex, samp, p.yz * scale);
            float4 b = SAMPLE_TEXTURE2D(tex, samp, p.xz * scale);
            float4 c = SAMPLE_TEXTURE2D(tex, samp, p.xy * scale);
            return a * w.x + b * w.y + c * w.z;
        }

        float WoundMask(float3 p, float4 W, float4 A, float4 S, float noise)
        {
            if (W.w <= 0.0) return 0.0;
            float3 v = p - W.xyz;
            float3 ax = normalize(A.xyz + float3(0, 0, 1e-5));
            float depth = dot(v, ax);
            float3 radial = v - ax * depth;
            float3 sd = S.xyz - ax * dot(S.xyz, ax);
            sd = (dot(sd, sd) > 1e-6) ? normalize(sd) : float3(1, 0, 0);
            float3 od = cross(ax, sd);
            float stretch = 1.0 + max(S.w, 0.0);
            float2 q = float2(dot(radial, sd) / stretch, dot(radial, od));
            float d = length(q);
            float len = max(A.w, 1e-3);
            float depthFade = saturate(1.0 - abs(depth) / (len + W.w));
            float r = W.w * (1.0 + (noise - 0.5) * 0.6);
            return saturate((r - d) / (0.15 * W.w + 1e-4)) * depthFade;
        }

        float SpotMask(float3 p, float4 s, float noise)
        {
            if (s.w <= 0.0) return 0.0;
            float d = length(p - s.xyz) / s.w;
            return saturate((1.0 - d + (noise - 0.5) * 0.4) * 4.0);
        }

        void ArmClip(float3 p)
        {
            if (_OffL > 0.5 && p.x < -0.3) clip(-1);
            if (_OffR > 0.5 && p.x >  0.3) clip(-1);
        }

        // returns x = wound, y = blood spot
        float2 Wounds(float3 p, float3 n)
        {
            float noise = SampleTri(TEXTURE2D_ARGS(_WoundNoise, sampler_WoundNoise), p, n, _FleshScale * 0.5).r;
            float w = 0;
            w = max(w, WoundMask(p, _Wound0, _Axis0, _Side0, noise));
            w = max(w, WoundMask(p, _Wound1, _Axis1, _Side1, noise));
            w = max(w, WoundMask(p, _Wound2, _Axis2, _Side2, noise));
            w = max(w, WoundMask(p, _Wound3, _Axis3, _Side3, noise));
            w = max(w, WoundMask(p, _Wound4, _Axis4, _Side4, noise));
            w = max(w, WoundMask(p, _Wound5, _Axis5, _Side5, noise));
            float s = 0;
            s = max(s, SpotMask(p, _Spot00, noise)); s = max(s, SpotMask(p, _Spot01, noise)); s = max(s, SpotMask(p, _Spot02, noise));
            s = max(s, SpotMask(p, _Spot10, noise)); s = max(s, SpotMask(p, _Spot11, noise)); s = max(s, SpotMask(p, _Spot12, noise));
            s = max(s, SpotMask(p, _Spot20, noise)); s = max(s, SpotMask(p, _Spot21, noise)); s = max(s, SpotMask(p, _Spot22, noise));
            s = max(s, SpotMask(p, _Spot30, noise)); s = max(s, SpotMask(p, _Spot31, noise)); s = max(s, SpotMask(p, _Spot32, noise));
            s = max(s, SpotMask(p, _Spot40, noise)); s = max(s, SpotMask(p, _Spot41, noise)); s = max(s, SpotMask(p, _Spot42, noise));
            s = max(s, SpotMask(p, _Spot50, noise)); s = max(s, SpotMask(p, _Spot51, noise)); s = max(s, SpotMask(p, _Spot52, noise));
            return float2(w, s);
        }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float3 normalOS   : TEXCOORD5;
                float  fog        : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs pi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs ni = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = pi.positionCS;
                o.positionWS = pi.positionWS;
                o.normalWS = ni.normalWS;
                o.tangentWS = float4(ni.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionOS = v.positionOS.xyz;
                o.normalOS = v.normalOS;
                o.fog = ComputeFogFactor(pi.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                ArmClip(i.positionOS);

                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float smooth = _SmoothFromAlpha > 0.5 ? albedo.a * _Smoothness : _Smoothness;

                float3 nWS = normalize(i.normalWS);
                if (_UseBump > 0.5)
                {
                    float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                    float3 t = normalize(i.tangentWS.xyz);
                    float3 b = cross(nWS, t) * i.tangentWS.w;
                    nWS = normalize(mul(nTS, float3x3(t, b, nWS)));
                }

                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;

                float2 wm = Wounds(i.positionOS, i.normalOS);
                float3 flesh = SampleTri(TEXTURE2D_ARGS(_FleshMap, sampler_FleshMap), i.positionOS, i.normalOS, _FleshScale).rgb;
                float3 blood = float3(0.25, 0.01, 0.01);

                float3 col = albedo.rgb;
                col = lerp(col, col * blood * 3.0, wm.y * 0.85);
                col = lerp(col, flesh, wm.x);
                float metallic = lerp(_Metallic, 0.0, max(wm.x, wm.y));
                smooth = lerp(smooth, 0.8, max(wm.x, wm.y * 0.7));
                emission *= 1.0 - wm.x;

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = nWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fog;
                inputData.bakedGI = SampleSH(nWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = col;
                s.metallic = metallic;
                s.smoothness = smooth;
                s.normalTS = float3(0, 0, 1);
                s.emission = emission;
                s.occlusion = 1;
                s.alpha = 1;
                s.specular = 0;

                half4 c = UniversalFragmentPBR(inputData, s);
                c.rgb = MixFog(c.rgb, i.fog);
                c.a = 1;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 ld = normalize(_LightPosition - pWS);
            #else
                float3 ld = _LightDirection;
            #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(pWS, nWS, ld));
            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = cs;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                ArmClip(i.positionOS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                ArmClip(i.positionOS);
                return i.positionCS.z;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
