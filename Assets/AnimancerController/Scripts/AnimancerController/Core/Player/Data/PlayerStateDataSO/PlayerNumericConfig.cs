using UnityEngine;

[System.Serializable]
public class PlayerNumericConfig
{
    [Header("Move")]
    [Min(0.1f)] public float moveSpeedMultiplier = 1f;
    public float walkSpeedParameter = 1f;
    public float runSpeedParameter = 2f;
    public float inAirMoveSpeed = 2f;

    [Header("Gravity & Ground")]
    public float gravity = -12f;
    public Vector2 velocityLimit = new Vector2(-20f, 60f);
    public float groundDetectedOffset = -0.06f;
    public float groundRadius = 0.5f;

    [Header("Jump")]
    [Min(0f)] public float defaultJumpHeight = 0.8f;
    [Min(0f)] public float outPlaceJumpHeight = 0.8f;
    [Min(0f)] public float platformerJumpHeight = 15f;

    [Header("Climb Detection")]
    [Min(0f)] public float climbDetectionAngle = 45f;
    [Min(0f)] public float climbDetectionDistance = 1f;
    [Min(0f)] public float vaultMaxDistance = 0.45f;
    [Min(0f)] public float canClimbMaxHeight = 3.2f;
    [Min(0f)] public float canClimbMinHeight = 0.3f;
    [Min(1)] public int climbDetectionSamplingCount = 30;

    [Header("Climb Obstacle Height Ranges")]
    [Min(0f)] public float lowClimbMaxHeight = 0.35f;
    [Min(0f)] public float lowMediumClimbMaxHeight = 1f;
    [Min(0f)] public float mediumClimbMaxHeight = 1.7f;
    [Min(0f)] public float mediumHighClimbMinHeight = 2f;
    [Min(0f)] public float mediumHighClimbMaxHeight = 2.5f;

    [Header("Ledge Probe (In Air)")]
    [Min(0f)] public float ledgeProbeMinY = 1.35f;
    [Min(0f)] public float ledgeProbeMaxY = 2.05f;
    [Min(0f)] public float ledgeProbeDistance = 1.5f;
    [Min(1)] public int ledgeProbeSamplingCount = 12;
}
