using UnityEngine;

public enum PlayerBuffType
{
    Haste = 0,
    Slow = 1,
    Regeneration = 2,
    DizzyCamera = 3,
}

public enum PlayerBuffStackRule
{
    RefreshDuration = 0,
    IndependentDuration = 1,
    Override = 2,
}

public enum PlayerBuffSourceType
{
    Self = 0,
    Trap = 1,
    Monster = 2,
    Other = 3,
}

public enum RecoverTargetType
{
    Health = 0,
    Stamina = 1,
    ReverseSystem = 2,
}

public struct PlayerBuffSourceContext
{
    public PlayerBuffSourceType sourceType;
    public GameObject sourceObject;

    public PlayerBuffSourceContext(PlayerBuffSourceType sourceType, GameObject sourceObject = null)
    {
        this.sourceType = sourceType;
        this.sourceObject = sourceObject;
    }
}

public struct PlayerBuffRuntimeSnapshot
{
    public float moveSpeedMultiplier;
    public float moveSpeedAdditive;
    public float dizzyAmplitude;
    public float dizzyFrequency;
    public float dizzyFovOffset;
    public float dizzyShakePitchAmplitude;
    public float dizzyShakeYawAmplitude;
    public float dizzyShakeRollAmplitude;
    public float dizzyShakePitchOffset;
    public float dizzyShakeYawOffset;
    public float dizzyShakeRollOffset;
    public float dizzyShakePitchFrequencyScale;
    public float dizzyShakeYawFrequencyScale;
    public float dizzyShakeRollFrequencyScale;

    public static PlayerBuffRuntimeSnapshot Default =>
        new PlayerBuffRuntimeSnapshot
        {
            moveSpeedMultiplier = 1f,
            moveSpeedAdditive = 0f,
            dizzyAmplitude = 0f,
            dizzyFrequency = 0f,
            dizzyFovOffset = 0f,
            dizzyShakePitchAmplitude = 0f,
            dizzyShakeYawAmplitude = 0f,
            dizzyShakeRollAmplitude = 0f,
            dizzyShakePitchOffset = 0f,
            dizzyShakeYawOffset = 0f,
            dizzyShakeRollOffset = 0f,
            dizzyShakePitchFrequencyScale = 1f,
            dizzyShakeYawFrequencyScale = 1f,
            dizzyShakeRollFrequencyScale = 1f,
        };
}
