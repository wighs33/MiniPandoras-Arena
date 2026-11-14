// 인물·투기장·소품이 함께 쓰는 셀셰이딩 + 외곽선 셰이더.
// 빛은 밝은 면과 그늘 두 단으로 끊고, 그늘은 검게 누르지 않고 재질마다 정한 색을 곱한다(그림체가 탁해지지 않게).
// 꼭짓점 색이 있는 메시(Luna의 드레스와 머리 장식)는 그 색을 바탕색에 곱한다. 색이 없는 메시는 유니티가 흰색을 준다.
// 외곽선은 뒷면을 법선 방향으로 밀어 그리는 방식이고, 미는 방향은 가져오기 때 접선 칸에 구운 부드러운 법선이다
// (Editor/OutlineNormalBaker). 로우폴리의 각진 면에서 외곽선이 갈라지지 않게 하려고 그렇게 한다.
Shader "MiniPandoras/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Color (곱하는 색)", Color) = (0.68, 0.64, 0.84, 1)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.0
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.5)) = 0.03
        _AmbientWeight ("Ambient Weight", Range(0, 1)) = 0.25
        _RimColor ("Rim Color (a = 세기)", Color) = (1, 1, 1, 0.2)
        _RimWidth ("Rim Width", Range(0, 1)) = 0.3
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _OutlineColor ("Outline Color", Color) = (0.12, 0.10, 0.16, 1)
        _OutlineWidth ("Outline Width (1080p 화면의 픽셀)", Range(0, 8)) = 2.5
        _DepthPull ("Depth Pull (m, 옷을 몸 앞에 그린다)", Range(0, 0.05)) = 0
        _Flash ("Flash (맞은 순간 하얗게)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half3 vertexColor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = PullDepth(position.positionCS, position.positionWS);
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                // 블렌더가 sRGB 값으로 내보낸 꼭짓점 색(Tools/Blender/export_luna.py)을 선형으로 되돌린다.
                output.vertexColor = SRGBToLinear(input.color.rgb);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb * input.vertexColor;
                float3 normalWS = normalize(input.normalWS);
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                // 면의 방향으로 정한 밝기와 그림자를 같은 두 단으로 끊는다. 그림자도 그늘색이 되어 한 그림체로 보인다.
                half facing = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, dot(normalWS, light.direction));
                half shadow = smoothstep(0.35, 0.65, light.shadowAttenuation);
                half lit = facing * shadow;

                half3 lightColor = light.color * light.distanceAttenuation;
                half3 color = albedo * lerp(_ShadeColor.rgb, 1.0, lit) * lightColor;
                color += albedo * SampleSH(normalWS) * _AmbientWeight;

                half rim = step(1.0 - _RimWidth, 1.0 - saturate(dot(normalWS, viewWS))) * lit;
                color += _RimColor.rgb * (_RimColor.a * rim);
                color += _EmissionColor.rgb;
                color = lerp(color, 1.0, _Flash);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            // URP는 이 태그의 패스를 본 패스와 함께 그린다. 앞면을 지우고 부풀린 뒷면만 남겨 테두리가 된다.
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "ToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 접선 칸에 구운 부드러운 법선. 굽지 않은 메시는 접선이 비어 있어 원래 법선을 쓴다.
                float3 pushOS = dot(input.tangentOS.xyz, input.tangentOS.xyz) > 0.01 ? input.tangentOS.xyz : input.normalOS;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = PullDepth(TransformWorldToHClip(positionWS), positionWS);
                float3 pushCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(pushOS));
                float2 direction = normalize(pushCS.xy + float2(1e-5, 0));
                // 화면 높이 1080을 기준으로 한 픽셀 두께라서 해상도가 달라도 그림체가 같다. 가로는 화면비로 줄여 둥글게 민다.
                float2 offset = direction * (_OutlineWidth * 2.0 / 1080.0);
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                positionCS.xy += offset * positionCS.w;

                output.positionCS = positionCS;
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                clip(_OutlineWidth - 0.001);
                return half4(MixFog(_OutlineColor.rgb, input.fogFactor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "ToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // URP가 그림자를 그릴 때마다 채워 주는 값. URP의 ShadowCasterPass.hlsl과 같은 이름이어야 한다.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "ToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half Frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "ToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    return half4(PackFloat2To888(remappedOctNormalWS), 0.0);
                #else
                    return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
