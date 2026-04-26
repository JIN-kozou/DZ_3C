using UnityEngine;

public class PlayerBuffRuntimeValidator : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PlayerBuffPresetLibrarySO presetLibrary;

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    [ContextMenu("Validate Buff Presets")]
    public void ValidatePresetLibrary()
    {
        if (presetLibrary == null)
        {
            Debug.LogWarning("Buff preset library is missing.");
            return;
        }

        ValidateConfig("Haste", presetLibrary.haste);
        ValidateConfig("Slow", presetLibrary.slow);
        ValidateConfig("Regeneration", presetLibrary.regeneration);
        ValidateConfig("DizzyCamera", presetLibrary.dizzyCamera);
    }

    private void ValidateConfig(string label, PlayerBuffConfigSO config)
    {
        if (config == null)
        {
            Debug.LogWarning($"{label} preset is not assigned.");
            return;
        }

        if (config.Duration <= 0f)
        {
            Debug.LogWarning($"{label} duration must be > 0.");
        }
    }
}
