using PolygonArsenal;
using UnityEngine;

/// <summary>
/// Drag this (or assign it on <see cref="Projectile"/>) to run
/// <see cref="PolygonProjectileScript.DestroyMissile"/> when the gameplay projectile despawns.
/// </summary>
[DisallowMultipleComponent]
public class PolygonProjectileDespawnBridge : MonoBehaviour, IProjectileDespawnVisuals
{
    [Tooltip("If empty, uses PolygonProjectileScript on this object, then in children.")]
    [SerializeField]
    private PolygonProjectileScript polygonProjectile;

    private void Awake()
    {
        if (polygonProjectile == null)
        {
            polygonProjectile = GetComponent<PolygonProjectileScript>()
                ?? GetComponentInChildren<PolygonProjectileScript>(true);
        }
    }

    public void OnProjectileDespawned()
    {
        if (polygonProjectile == null)
        {
            return;
        }

        polygonProjectile.DestroyMissile();
    }
}
