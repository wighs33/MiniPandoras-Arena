// 스킬 효과용 툰 셰이더. 모양 그림(알파)에 흐르는 노이즈를 곱한 값을 속색·겉색 두 단으로 끊어서,
// 인물의 셀셰이딩과 어울리는 단단한 색의 불꽃·기운을 그린다(부드러운 빛 번짐 대신).
// 입자 색의 알파가 줄면 투명해지는 대신 가장자리부터 타들어 가며 사라진다(입자 색 쓰기가 켜졌을 때).
// 섞기: 더하기 = One One, 덮기 = One OneMinusSrcAlpha(색에 알파를 미리 곱해 내보낸다).
Shader "MiniPandoras/ToonFx"
{
    Properties
    {
        [MainTexture] _MainTex ("Shape (알파가 모양)", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling (xy)", Vector) = (1, 1, 0, 0)
        _NoiseScroll ("Noise Scroll (xy, 초당)", Vector) = (0, -0.8, 0, 0)
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.7
        _WorldNoise ("World Noise (바닥 장판은 1: 월드 XZ로 노이즈를 읽는다)", Range(0, 1)) = 0
        [HDR] _CoreColor ("Core Color (속)", Color) = (2.6, 2.1, 1.1, 1)
        [HDR] _EdgeColor ("Edge Color (겉)", Color) = (2.2, 0.6, 0.12, 1)
        _CoreThreshold ("Core Threshold", Range(0, 1.5)) = 0.62
        _EdgeThreshold ("Edge Threshold", Range(0, 1.5)) = 0.28
        _Softness ("Softness (단의 경계)", Range(0.001, 0.2)) = 0.015
        _Erosion ("Erosion (0~1, 스크립트가 사라지게 할 때 올린다)", Range(0, 1)) = 0
        _ParticleColor ("Particle Color (입자 색과 알파 쓰기)", Range(0, 1)) = 1
        _RimFade ("Rim Fade (메시 가장자리가 타들어 감, 0이면 끔)", Range(0, 8)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ToonFx"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTiling;
                float4 _NoiseScroll;
                half _NoiseStrength;
                half _WorldNoise;
                half4 _CoreColor;
                half4 _EdgeColor;
                half _CoreThreshold;
                half _EdgeThreshold;
                half _Softness;
                half _Erosion;
                half _ParticleColor;
                half _RimFade;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                // xy: UV, zw: 입자마다 다른 난수(파티클의 StableRandom.xy 꼭짓점 흐름). 메시는 0이다.
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = lerp(half4(1, 1, 1, 1), input.color, _ParticleColor);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half shape = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv.xy, _MainTex)).a;

                // 바닥 장판은 월드 자리로 노이즈를 읽어 여러 장판이 이어져 보이고, 입자는 입자마다 다른 곳을 읽는다.
                float2 noiseUV = lerp(input.uv.xy, input.positionWS.xz, _WorldNoise) * _NoiseTiling.xy;
                noiseUV += input.uv.zw * 7.31 + _Time.y * _NoiseScroll.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                half value = shape * lerp(1.0h, noise * 1.6h, _NoiseStrength);

                if (_RimFade > 0)
                {
                    // 공처럼 둥근 메시는 가장자리가 먼저 타들어 가 속이 밝은 불덩이가 된다.
                    half facing = saturate(dot(normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS)));
                    value *= pow(facing, _RimFade);
                }

                half erosion = (1.0h - input.color.a) + _Erosion;
                half edge = _EdgeThreshold + erosion;
                half core = _CoreThreshold + erosion;
                half visible = smoothstep(edge - _Softness, edge + _Softness, value);
                half inner = smoothstep(core - _Softness, core + _Softness, value);

                half3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, inner) * input.color.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color * visible, visible);
            }
            ENDHLSL
        }
    }
}
