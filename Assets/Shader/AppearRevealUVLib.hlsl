#ifndef APPEAR_REVEAL_UV_LIB_INCLUDED
#define APPEAR_REVEAL_UV_LIB_INCLUDED

// Safe for Shader Graph Custom Function include (no sphere globals).

void AppearReveal_UVRevealMask_float(float u, float reveal, float revealSoft, out float mask)
{
    if (reveal >= 0.999f)
    {
        mask = 1.0f;
        return;
    }
    float r0 = saturate(reveal - revealSoft);
    float r1 = saturate(reveal);
    mask = 1.0f - smoothstep(r0, max(r1, r0 + 1e-4f), u);
}

void AppearReveal_UVRevealMask_float_float(float u, float reveal, out float mask)
{
    AppearReveal_UVRevealMask_float(u, reveal, 0.04f, mask);
}

void AppearReveal_UVRevealMask_float_float(float u, float reveal, float revealSoft, out float mask)
{
    AppearReveal_UVRevealMask_float(u, reveal, revealSoft, mask);
}

#endif
