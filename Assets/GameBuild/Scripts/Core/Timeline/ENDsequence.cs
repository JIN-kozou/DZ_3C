using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class ENDsequence : MonoBehaviour
{
    private float timer;
    public PlayableDirector director;

    [Header("Background music")]
    [Tooltip("Fade-out multiplier")]
    public float VolumeDevay = 1f;

    [SerializeField] private FMODSoundEvent timelineEndBgm;

    private FMODLoopHandle bgmHandle;
    private float bgmStartVolume = 1f;

    private void Awake()
    {
        GameAudio.EnsureBanksLoaded();

        if (timelineEndBgm != null)
        {
            bgmHandle = GameAudio.StartLoop2D(timelineEndBgm);
            if (bgmHandle != null && bgmHandle.IsValid)
            {
                bgmStartVolume = 1f;
                bgmHandle.SetVolume(bgmStartVolume);
            }
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
            SceneManager.LoadScene("MainMenu");
        }
    }
}
