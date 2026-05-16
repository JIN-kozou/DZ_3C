using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineLoadScene : MonoBehaviour
{
    private float timer = 0;
    public PlayableDirector director;

    [Header("±³¾°ÒôÀÖºÍÅäÒô")]
    [Tooltip("½¥³ö±¶ÂÊ")]
    public float VolumeDevay;
    public AudioSource BGM;
    public AudioClip BGM_Clip;
    public AudioSource Dialogue;
    public AudioClip Dialogue_Clip;

    private void Awake()
    {
        if (BGM.clip != null)
        {
            BGM.clip = BGM_Clip;
            BGM.loop = false;
            BGM.Play();
        }

        if (Dialogue.clip != null)
        {
            Dialogue.clip = Dialogue_Clip;
            Dialogue.loop = false;
            Dialogue.Play();
        }
    }

    private void Update()
    {
        timer += Time.deltaTime * 1f;

        LoadGameScene();
    }

    public void LoadGameScene()
    {
        if (timer >= director.duration)
        {
            SceneManager.LoadScene("Level test");
        }
    }
}
