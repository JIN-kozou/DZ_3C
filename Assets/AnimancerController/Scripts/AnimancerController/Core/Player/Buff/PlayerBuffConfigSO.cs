using UnityEngine;

[CreateAssetMenu(menuName = "Asset/Buff/Player Buff Config")]
public class PlayerBuffConfigSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string buffId = "buff.default";
    [SerializeField] private PlayerBuffType buffType = PlayerBuffType.Haste;
    [SerializeField, Min(0.01f)] private float duration = 3f;
    [SerializeField, Min(1)] private int maxStack = 1;
    [SerializeField] private PlayerBuffStackRule stackRule = PlayerBuffStackRule.RefreshDuration;
    [SerializeField] private bool isDispellable = true;

    [Header("Stat Modifier")]
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float moveSpeedAdditive = 0f;

    [Header("Regeneration")]
    [SerializeField, Min(0f)] private float tickInterval = 1f;
    [SerializeField] private float recoverValue = 0f;
    [SerializeField] private RecoverTargetType recoverTargetType = RecoverTargetType.Health;

    [Header("Camera Dizzy")]
    [SerializeField, Min(0f)] private float dizzyAmplitude = 0f;
    [SerializeField, Min(0f)] private float dizzyFrequency = 0f;
    [SerializeField] private float dizzyFovOffset = 0f;
    [Header("Camera Shake (Tilt=Pitch, Pan=Yaw, Dutch=Roll)")]
    [SerializeField, Min(0f)] private float dizzyShakePitchAmplitude = 0f;
    [SerializeField, Min(0f)] private float dizzyShakeYawAmplitude = 0f;
    [SerializeField, Min(0f)] private float dizzyShakeRollAmplitude = 0f;
    [SerializeField] private float dizzyShakePitchOffset = 0f;
    [SerializeField] private float dizzyShakeYawOffset = 0f;
    [SerializeField] private float dizzyShakeRollOffset = 0f;
    [SerializeField, Min(0.01f)] private float dizzyShakePitchFrequencyScale = 1f;
    [SerializeField, Min(0.01f)] private float dizzyShakeYawFrequencyScale = 1f;
    [SerializeField, Min(0.01f)] private float dizzyShakeRollFrequencyScale = 1f;

    [Header("Source Constraint")]
    [SerializeField] private PlayerBuffSourceType sourceType = PlayerBuffSourceType.Other;

    public string BuffId => string.IsNullOrWhiteSpace(buffId) ? name : buffId;
    public PlayerBuffType BuffType => buffType;
    public float Duration => duration;
    public int MaxStack => Mathf.Max(1, maxStack);
    public PlayerBuffStackRule StackRule => stackRule;
    public bool IsDispellable => isDispellable;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float MoveSpeedAdditive => moveSpeedAdditive;
    public float TickInterval => tickInterval;
    public float RecoverValue => recoverValue;
    public RecoverTargetType RecoverTargetType => recoverTargetType;
    public float DizzyAmplitude => dizzyAmplitude;
    public float DizzyFrequency => dizzyFrequency;
    public float DizzyFovOffset => dizzyFovOffset;
    public float DizzyShakePitchAmplitude => dizzyShakePitchAmplitude;
    public float DizzyShakeYawAmplitude => dizzyShakeYawAmplitude;
    public float DizzyShakeRollAmplitude => dizzyShakeRollAmplitude;
    public float DizzyShakePitchOffset => dizzyShakePitchOffset;
    public float DizzyShakeYawOffset => dizzyShakeYawOffset;
    public float DizzyShakeRollOffset => dizzyShakeRollOffset;
    public float DizzyShakePitchFrequencyScale => dizzyShakePitchFrequencyScale;
    public float DizzyShakeYawFrequencyScale => dizzyShakeYawFrequencyScale;
    public float DizzyShakeRollFrequencyScale => dizzyShakeRollFrequencyScale;
    public PlayerBuffSourceType SourceType => sourceType;
}
