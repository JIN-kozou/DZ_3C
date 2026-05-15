using UnityEngine;

public class ItemAudio : MonoBehaviour
{
    [SerializeField] private GameObject pickupItem;
    [SerializeField, Min(0f)] private float pickupItemStartSeconds;
    [SerializeField] private GameObject dropItem;
    [SerializeField] private GameObject submitItem;
    [SerializeField, Min(0f)] private float submitItemStartSeconds;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float pickupNoiseLoudness = 0.3f;
    [SerializeField, Min(0f)] private float pickupNoiseDuration = 0.15f;
    [SerializeField, Min(0f)] private float dropNoiseLoudness = 0.6f;
    [SerializeField, Min(0f)] private float dropNoiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float submitNoiseLoudness = 0.5f;
    [SerializeField, Min(0f)] private float submitNoiseDuration = 0.2f;

    public void PlayPickup()
    {
        PlayAtSelf(pickupItem, pickupItemStartSeconds);
        EmitAINoise(pickupNoiseLoudness, pickupNoiseDuration);
    }

    public void PlayDrop()
    {
        PlayAtSelf(dropItem);
        EmitAINoise(dropNoiseLoudness, dropNoiseDuration);
    }

    public void PlaySubmit()
    {
        PlayAtSelf(submitItem, submitItemStartSeconds);
        EmitAINoise(submitNoiseLoudness, submitNoiseDuration);
    }

    private void PlayAtSelf(GameObject soundPrefab, float startTimeSeconds = 0f)
    {
        AudioPrefabPlayer.Play(
            soundPrefab,
            transform.position,
            null,
            false,
            1f,
            1f,
            Mathf.Max(0f, startTimeSeconds));
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }
}
