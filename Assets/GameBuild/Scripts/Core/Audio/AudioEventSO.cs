using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    [SerializeField] private FMODSoundEvent soundEvent;

    public FMODSoundEvent SoundEvent => soundEvent;

    public bool Play()
    {
        return soundEvent != null && GameAudio.Play2D(soundEvent);
    }

    public bool PlayAt(Vector3 position)
    {
        if (soundEvent == null)
        {
            return false;
        }

        if (soundEvent.Is3D)
        {
            return GameAudio.Play3D(soundEvent, position);
        }

        return GameAudio.Play2D(soundEvent);
    }
}
