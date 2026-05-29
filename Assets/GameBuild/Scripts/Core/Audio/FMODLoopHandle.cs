using FMOD.Studio;

/// <summary>
/// Handle for a looping FMOD event instance started via <see cref="GameAudio"/>.
/// </summary>
public sealed class FMODLoopHandle
{
    internal EventInstance Instance;
    internal bool IsValid;

    public bool IsPlaying
    {
        get
        {
            if (!IsValid || !Instance.isValid())
            {
                return false;
            }

            Instance.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING;
        }
    }

    public void SetVolume(float volume01)
    {
        if (!IsValid || !Instance.isValid())
        {
            return;
        }

        Instance.setVolume(UnityEngine.Mathf.Clamp01(volume01));
    }

    public void SetParameter(string name, float value)
    {
        if (!IsValid || !Instance.isValid() || string.IsNullOrEmpty(name))
        {
            return;
        }

        Instance.setParameterByName(name, value);
    }
}
