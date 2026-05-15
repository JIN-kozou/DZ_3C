using UnityEngine;

[CreateAssetMenu(menuName = "Audio/AudioEvent")]
public class AudioEventSO : ScriptableObject
{
    [SerializeField] private GameObject soundPrefab;

    public GameObject SoundPrefab => soundPrefab;

    public AudioSource Play()
    {
        return AudioPrefabPlayer.Play(soundPrefab, Vector3.zero);
    }

    public AudioSource PlayAt(Vector3 position)
    {
        return AudioPrefabPlayer.Play(soundPrefab, position);
    }
}
