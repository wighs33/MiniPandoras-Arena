#ifndef MINIPANDORAS_TOON_INPUT_INCLUDED
#define MINIPANDORAS_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// SRP Batcher로 묶이려면 모든 패스가 같은 상수 버퍼를 써야 해서 한 파일에 둔다.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadeColor;
    half _ShadeThreshold;
    half _ShadeSoftness;
    half _AmbientWeight;
    half4 _RimColor;
    half _RimWidth;
    half4 _EmissionColor;
    half4 _OutlineColor;
    float _OutlineWidth;
    float _DepthPull;
    half _Flash;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// 옷처럼 몸에 붙은 메시의 깊이만 카메라 쪽으로 _DepthPull(m)만큼 당긴다(화면 위 자리는 그대로다).
// 자세에 따라 피부가 옷을 몇 mm 뚫고 나오는 것을 가리려는 것이다.
float4 PullDepth(float4 positionCS, float3 positionWS)
{
    float3 towardCamera = normalize(GetCameraPositionWS() - positionWS);
    float4 pulledCS = TransformWorldToHClip(positionWS + towardCamera * _DepthPull);
    positionCS.z = pulledCS.z / pulledCS.w * positionCS.w;
    return positionCS;
}

#endif
