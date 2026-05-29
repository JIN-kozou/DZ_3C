#ifndef APPEAR_REVEAL_LIB_INCLUDED
#define APPEAR_REVEAL_LIB_INCLUDED

#include "AppearRevealUVLib.hlsl"

#ifndef APPEAR_REVEAL_SPHERE_GLOBALS_DECLARED
#include "AppearRevealSphereGlobals.hlsl"
#endif

float AppearReveal_SphereMaskSingleXZ_float(float3 worldPos, float3 center, float radius, float edgeSoft)
{
    if (radius <= 1e-5)
        return 0.0;

    float dx = worldPos.x - center.x;
    float dz = worldPos.z - center.z;
    float d = sqrt(dx * dx + dz * dz);
    float rInner = max(0.0, radius - edgeSoft);
    return 1.0 - smoothstep(rInner, radius, d);
}

float AppearReveal_SphereMaskXZ_float(float3 worldPos, float edgeSoft)
{
    float m = 0.0;
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position0, _Radius0, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position1, _Radius1, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position2, _Radius2, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position3, _Radius3, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position4, _Radius4, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position5, _Radius5, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position6, _Radius6, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position7, _Radius7, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position8, _Radius8, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position9, _Radius9, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position10, _Radius10, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position11, _Radius11, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position12, _Radius12, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position13, _Radius13, edgeSoft));
    m = max(m, AppearReveal_SphereMaskSingleXZ_float(worldPos, _Position14, _Radius14, edgeSoft));
    return saturate(m);
}

void AppearReveal_CombinedMask_float(float3 worldPos, float u, float reveal, float revealSoft, float sphereEdgeSoft, out float mask)
{
    float uvMask;
    float sphereMask;
    AppearReveal_UVRevealMask_float(u, reveal, revealSoft, uvMask);
    sphereMask = AppearReveal_SphereMaskXZ_float(worldPos, sphereEdgeSoft);
    mask = saturate(uvMask * sphereMask);
}

void AppearReveal_CombinedMask_float_float(float3 worldPos, float u, float reveal, out float mask)
{
    AppearReveal_CombinedMask_float(worldPos, u, reveal, 0.04f, 0.08f, mask);
}

void AppearReveal_CombinedMask_float_float(float3 worldPos, float u, float reveal, float revealSoft, out float mask)
{
    AppearReveal_CombinedMask_float(worldPos, u, reveal, revealSoft, 0.08f, mask);
}

void AppearReveal_GhostAlpha_float(float combinedMask, float baseAlpha, out float alpha)
{
    alpha = baseAlpha * (1.0 - saturate(combinedMask));
}

void AppearReveal_GhostAlpha_float_float(float combinedMask, float baseAlpha, out float alpha)
{
    AppearReveal_GhostAlpha_float(combinedMask, baseAlpha, alpha);
}

void AppearReveal_ApplyGhostLayerAlpha_float(
    float3 worldPos, float u, float reveal, float revealSoft, float sphereEdgeSoft, float baseAlpha, out float alpha)
{
    float combined;
    AppearReveal_CombinedMask_float(worldPos, u, reveal, revealSoft, sphereEdgeSoft, combined);
    AppearReveal_GhostAlpha_float(combined, baseAlpha, alpha);
}

void AppearReveal_ApplyGhostLayerAlpha_float_float(
    float3 worldPos, float u, float reveal, float revealSoft, float baseAlpha, out float alpha)
{
    AppearReveal_ApplyGhostLayerAlpha_float(worldPos, u, reveal, revealSoft, 0.08f, baseAlpha, alpha);
}

void AppearReveal_OpaqueSurfaceAlpha_float(
    float3 worldPos, float u, float reveal, float revealSoft, float sphereEdgeSoft, out float alpha)
{
    AppearReveal_CombinedMask_float(worldPos, u, reveal, revealSoft, sphereEdgeSoft, alpha);
}

void AppearReveal_OpaqueSurfaceAlpha_float_float(
    float3 worldPos, float u, float reveal, float revealSoft, out float alpha)
{
    AppearReveal_OpaqueSurfaceAlpha_float(worldPos, u, reveal, revealSoft, 0.08f, alpha);
}

#endif
