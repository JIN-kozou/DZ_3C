using UnityEngine;

/// <summary>
/// Optional hook when a <see cref="Projectile"/> is removed (lifetime or hit-despawn).
/// Keeps <see cref="Projectile"/> free of direct references to third-party VFX scripts.
/// </summary>
public interface IProjectileDespawnVisuals
{
    void OnProjectileDespawned();
}
