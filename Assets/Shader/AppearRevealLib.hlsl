#ifndef APPEAR_REVEAL_LIB_INCLUDED
#define APPEAR_REVEAL_LIB_INCLUDED

// For Shader Graph → Custom Function (File).
// In Graph Inspector set Name to: AppearReveal_UVRevealMask (do NOT add _float yourself — SG appends suffixes).
// If Name was set to AppearReveal_UVRevealMask_float, Unity looks for AppearReveal_UVRevealMask_float_float — aliases below.

// Multiply mask into Surface Alpha after Appear / Alpha Clip.
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

// Mangled name when "Name" field wrongly includes _float (2 float slots in signature from SG).
void AppearReveal_UVRevealMask_float_float(float u, float reveal, out float mask)
{
    AppearReveal_UVRevealMask_float(u, reveal, 0.04f, mask);
}

// Same mangling but all three inputs wired (some SG versions still emit _float_float with 3 args).
void AppearReveal_UVRevealMask_float_float(float u, float reveal, float revealSoft, out float mask)
{
    AppearReveal_UVRevealMask_float(u, reveal, revealSoft, mask);
}

#endif
