using System.Collections;
using UnityEngine;

public class StatusAlertAudioController : MonoBehaviour
{
    public enum StatusAlertState
    {
        None,
        Low,
        Danger
    }

    [Header("Status Beeps")]
    [SerializeField] private GameObject lowStatusBeep;
    [SerializeField] private GameObject dangerStatusBeep;
    [SerializeField, Min(0.01f)] private float lowStatusInterval = 1.25f;
    [SerializeField, Min(0.01f)] private float dangerStatusInterval = 0.45f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool playAtTransformPosition;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField] private bool statusBeepEmitsAINoise;
    [SerializeField, Min(0f)] private float lowStatusNoiseLoudness;
    [SerializeField, Min(0f)] private float lowStatusNoiseDuration = 0.1f;
    [SerializeField, Min(0f)] private float dangerStatusNoiseLoudness;
    [SerializeField, Min(0f)] private float dangerStatusNoiseDuration = 0.1f;

    private StatusAlertState currentState = StatusAlertState.None;
    private Coroutine alertRoutine;

    public StatusAlertState CurrentState => currentState;

    public void SetStatusNormal() => SetStatusAlertState(StatusAlertState.None);
    public void SetStatusLow() => SetStatusAlertState(StatusAlertState.Low);
    public void SetStatusDanger() => SetStatusAlertState(StatusAlertState.Danger);

    public void SetStatusAlertState(StatusAlertState state)
    {
        if (currentState == state)
        {
            return;
        }

        StopAlertLoop();
        currentState = state;

        switch (currentState)
        {
            case StatusAlertState.Low:
                alertRoutine = StartCoroutine(AlertLoop(lowStatusBeep, lowStatusInterval, lowStatusNoiseLoudness, lowStatusNoiseDuration));
                break;
            case StatusAlertState.Danger:
                alertRoutine = StartCoroutine(AlertLoop(dangerStatusBeep, dangerStatusInterval, dangerStatusNoiseLoudness, dangerStatusNoiseDuration));
                break;
        }
    }

    public void StopAlert()
    {
        StopAlertLoop();
        currentState = StatusAlertState.None;
    }

    public void PlayLowStatusOnce() => PlayStatusBeep(lowStatusBeep, lowStatusNoiseLoudness, lowStatusNoiseDuration);
    public void PlayDangerStatusOnce() => PlayStatusBeep(dangerStatusBeep, dangerStatusNoiseLoudness, dangerStatusNoiseDuration);
    private void OnDisable() => StopAlert();
    private void OnDestroy() => StopAlert();

    private IEnumerator AlertLoop(GameObject beep, float interval, float noiseLoudness, float noiseDuration)
    {
        float waitDuration = Mathf.Max(0.01f, interval);
        while (true)
        {
            PlayStatusBeep(beep, noiseLoudness, noiseDuration);
            yield return useUnscaledTime ? new WaitForSecondsRealtime(waitDuration) : new WaitForSeconds(waitDuration);
        }
    }

    private void StopAlertLoop()
    {
        if (alertRoutine == null)
        {
            return;
        }

        StopCoroutine(alertRoutine);
        alertRoutine = null;
    }

    private void PlayStatusBeep(GameObject beep, float noiseLoudness, float noiseDuration)
    {
        AudioPrefabPlayer.Play(beep, playAtTransformPosition ? transform.position : Vector3.zero);
        if (statusBeepEmitsAINoise && aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(noiseLoudness, noiseDuration);
        }
    }
}
