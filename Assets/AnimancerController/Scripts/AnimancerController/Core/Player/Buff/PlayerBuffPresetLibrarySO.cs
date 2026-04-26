using UnityEngine;

[CreateAssetMenu(menuName = "Asset/Buff/Player Buff Preset Library")]
public class PlayerBuffPresetLibrarySO : ScriptableObject
{
    [Header("Core Presets")]
    public PlayerBuffConfigSO haste;
    public PlayerBuffConfigSO slow;
    public PlayerBuffConfigSO regeneration;
    public PlayerBuffConfigSO dizzyCamera;
}
