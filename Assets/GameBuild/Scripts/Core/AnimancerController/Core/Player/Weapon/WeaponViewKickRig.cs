using UnityEngine;

/// <summary>
/// Extra tilt/pan/dutch (degrees) and forward Z kick, decayed each frame by <see cref="StepDecay"/>.
/// </summary>
public class WeaponViewKickRig : MonoBehaviour
{
    public float TiltKick { get; private set; }
    public float PanKick { get; private set; }
    public float DutchKick { get; private set; }
    public float ForwardZKick { get; private set; }

    private float _recoveryHalfLife = 0.06f;

    public void ApplyKick(Vector3 eulerKickDeg, float forwardZOffset, float recoveryHalfLife)
    {
        TiltKick += eulerKickDeg.x;
        PanKick += eulerKickDeg.y;
        DutchKick += eulerKickDeg.z;
        ForwardZKick += forwardZOffset;
        _recoveryHalfLife = Mathf.Max(0.001f, recoveryHalfLife);
    }

    public void StepDecay(float deltaTime)
    {
        float t = 1f - Mathf.Exp(-deltaTime / _recoveryHalfLife);
        TiltKick = Mathf.Lerp(TiltKick, 0f, t);
        PanKick = Mathf.Lerp(PanKick, 0f, t);
        DutchKick = Mathf.Lerp(DutchKick, 0f, t);
        ForwardZKick = Mathf.Lerp(ForwardZKick, 0f, t);
    }
}
