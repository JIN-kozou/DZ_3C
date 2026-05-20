using UnityEngine;

public class GunAudio : MonoBehaviour
{
    [SerializeField] private GameObject draw;
    [SerializeField] private GameObject holster;
    [SerializeField] private GameObject shoot;
    [SerializeField] private GameObject hit;
    [SerializeField] private GameObject bulletFly;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float drawNoiseLoudness = 0.2f;
    [SerializeField, Min(0f)] private float drawNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float holsterNoiseLoudness = 0.2f;
    [SerializeField, Min(0f)] private float holsterNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float shootNoiseLoudness = 8f;
    [SerializeField, Min(0f)] private float shootNoiseDuration = 0.5f;

    private void Awake()
    {
        LoadDefaultPrefabsIfNeeded();
    }

    public void OnDraw()
    {
        PlayAtSelf(draw);
        EmitAINoise(drawNoiseLoudness, drawNoiseDuration);
    }

    public void OnHolster()
    {
        PlayAtSelf(holster);
        EmitAINoise(holsterNoiseLoudness, holsterNoiseDuration);
    }

    public void OnShoot()
    {
        PlayAtSelf(shoot);
        PlayAtSelf(bulletFly);
        EmitAINoise(shootNoiseLoudness, shootNoiseDuration);
    }

    public void OnHit(Vector3 hitPosition)
    {
        AudioPrefabPlayer.Play(hit, hitPosition);
    }

    private void PlayAtSelf(GameObject soundPrefab)
    {
        AudioPrefabPlayer.Play(soundPrefab, transform.position);
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }

    private void LoadDefaultPrefabsIfNeeded()
    {
        if (shoot == null)
        {
            shoot = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyBlackHoleMissileSND.prefab");
        }

        if (hit == null)
        {
            hit = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Explosions/PolyBulletExplosionSND.prefab");
        }

        if (bulletFly == null)
        {
            bulletFly = AudioDefaultPrefabs.Load("Assets/Polygon Arsenal/Sound/Prefabs/Missile/PolyLaserMissileSND.prefab");
        }
    }
}
