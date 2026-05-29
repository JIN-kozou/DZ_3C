using UnityEngine;

[CreateAssetMenu(fileName = "GameAudioSettings", menuName = "Audio/Game Audio Settings")]
public class GameAudioSettingsSO : ScriptableObject
{
    public enum QualityPreset
    {
        Low,
        Medium,
        High
    }

    [SerializeField] private QualityPreset quality = QualityPreset.Medium;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool waitForBanksOnBoot = true;

    public QualityPreset Quality => quality;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool WaitForBanksOnBoot => waitForBanksOnBoot;

    public bool EnableReflections => quality == QualityPreset.High;
    public bool EnableEnvironmentReverb => quality != QualityPreset.Low;
}
