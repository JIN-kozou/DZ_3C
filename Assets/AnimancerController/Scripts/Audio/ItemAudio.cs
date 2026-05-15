using UnityEngine;

public class ItemAudio : MonoBehaviour
{
    [SerializeField] private GameObject pickupItem;
    [SerializeField] private GameObject dropItem;
    [SerializeField] private GameObject submitItem;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float pickupNoiseLoudness = 0.3f;
    [SerializeField, Min(0f)] private float pickupNoiseDuration = 0.15f;
    [SerializeField, Min(0f)] private float dropNoiseLoudness = 0.6f;
    [SerializeField, Min(0f)] private float dropNoiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float submitNoiseLoudness = 0.5f;
    [SerializeField, Min(0f)] private float submitNoiseDuration = 0.2f;

    public void PlayPickup() { PlayAtSelf(pickupItem); EmitAINoise(pickupNoiseLoudness, pickupNoiseDuration); }
    public void PlayDrop() { PlayAtSelf(dropItem); EmitAINoise(dropNoiseLoudness, dropNoiseDuration); }
    public void PlaySubmit() { PlayAtSelf(submitItem); EmitAINoise(submitNoiseLoudness, submitNoiseDuration); }

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
}
