using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "FMODSoundEvent", menuName = "Audio/FMOD Sound Event")]
public class FMODSoundEvent : ScriptableObject
{
    [EventRef]
    [SerializeField] private string eventPath;

    [SerializeField] private bool is3D = true;
    [SerializeField] private bool isLoop;

    public string EventPath => eventPath;
    public bool Is3D => is3D;
    public bool IsLoop => isLoop;

    public bool HasEvent => !string.IsNullOrWhiteSpace(eventPath);

    public EventReference EventReference => RuntimeManager.PathToEventReference(eventPath);

#if UNITY_EDITOR
    public void SetEventPath(string path, bool is3D, bool loop)
    {
        eventPath = path;
        this.is3D = is3D;
        isLoop = loop;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
