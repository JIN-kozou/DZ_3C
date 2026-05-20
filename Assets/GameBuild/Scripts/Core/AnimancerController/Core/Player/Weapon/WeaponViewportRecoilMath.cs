using UnityEngine;

/// <summary>
/// Maps cumulative viewport aim offset (relative to screen center 0.5,0.5) to Cinemachine Recomposer tilt/pan degrees.
/// </summary>
public static class WeaponViewportRecoilMath
{
    /// <param name="viewportOffsetFromCenter">(dx, dy) in viewport units; same space as <see cref="GunConfigSO.recoilPattern"/> deltas.</param>
    /// <param name="verticalFovDegrees">Vertical field of view in degrees (actual lens).</param>
    /// <param name="aspect">Width / height.</param>
    public static void ViewportOffsetToTiltPanDegrees(
        Vector2 viewportOffsetFromCenter,
        float verticalFovDegrees,
        float aspect,
        out float tiltDeg,
        out float panDeg)
    {
        verticalFovDegrees = Mathf.Clamp(verticalFovDegrees, 0.01f, 179f);
        aspect = Mathf.Max(aspect, 0.01f);

        float v = verticalFovDegrees * Mathf.Deg2Rad;
        float tanHalfV = Mathf.Tan(v * 0.5f);
        float tanHalfH = tanHalfV * aspect;
        panDeg = Mathf.Rad2Deg * (viewportOffsetFromCenter.x * 2f * tanHalfH);
        tiltDeg = Mathf.Rad2Deg * (-viewportOffsetFromCenter.y * 2f * tanHalfV);
    }
}
