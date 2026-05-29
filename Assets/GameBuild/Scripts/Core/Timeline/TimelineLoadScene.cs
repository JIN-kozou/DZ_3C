using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineLoadScene : MonoBehaviour
{
    private float timer;
    public PlayableDirector director;

    [Header("Background music and dialogue")]
    [Tooltip("Fade-out multiplier")]
    public float VolumeDevay = 1f;

    [SerializeField] private FMODSoundEvent timelineBgm;
    [SerializeField] private FMODSoundEvent timelineDialogue;

    private FMODLoopHandle bgmHandle;
    private FMODLoopHandle dialogueHandle;
    private float bgmStartVolume = 1f;

    private void Awake()
    {
        GameAudio.EnsureBanksLoaded();

        if (timelineBgm != null)
        {
            bgmHandle = GameAudio.StartLoop2D(timelineBgm);
            if (bgmHandle != null && bgmHandle.IsValid)
            {
                bgmStartVolume = 1f;
                bgmHandle.SetVolume(bgmStartVolume);
            }
        }

        if (timelineDialogue != null)
        {
            dialogueHandle = GameAudio.StartLoop2D(timelineDialogue);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        FadeOutBgm();
        LoadGameScene();
    }

    private void OnDestroy()
    {
        if (bgmHandle != null)
        {
            GameAudio.Stop(bgmHandle);
            bgmHandle = null;
        }

        if (dialogueHandle != null)
        {
            GameAudio.Stop(dialogueHandle);
            dialogueHandle = null;
        }
    }

    private void FadeOutBgm()
    {
        if (director == null || bgmHandle == null || !bgmHandle.IsValid)
        {
            return;
        }

        double fadeStartTime = director.duration - 3f;
        if (timer < fadeStartTime)
        {
            return;
        }

        float t = (float)((timer - fadeStartTime) / 3f);
        bgmHandle.SetVolume(Mathf.Lerp(bgmStartVolume, 0f, t * VolumeDevay));
    }

    public void LoadGameScene()
    {
        if (director != null && timer >= director.duration)
        {
            SceneManager.LoadScene("Level test");
        }
    }
}
