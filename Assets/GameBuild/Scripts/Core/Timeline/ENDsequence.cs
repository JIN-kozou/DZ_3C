using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class ENDsequence : MonoBehaviour
{
    private float timer = 0;
    public PlayableDirector director;

    [Header("±³¾°ÒôÀÖ")]
    [Tooltip("½¥³ö±¶ÂÊ")]
    public float VolumeDevay = 1f;

    public AudioSource BGM;
    public AudioClip BGM_Clip;

    private float bgmStartVolume;

    private void Awake()
    {
        if (BGM != null && BGM_Clip != null)
        {
            BGM.clip = BGM_Clip;
            BGM.loop = false;
            BGM.Play();

            bgmStartVolume = BGM.volume;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        FadeOutBGM();
        LoadGameScene();
    }

    void FadeOutBGM()
    {
        double fadeStartTime = director.duration - 3f;

        if (timer >= fadeStartTime && BGM != null)
        {
            float t = (float)((timer - fadeStartTime) / 3f);

            BGM.volume = Mathf.Lerp(
                bgmStartVolume,
                0f,
                t * VolumeDevay
            );
        }
    }

    public void LoadGameScene()
    {
        if (timer >= director.duration)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}