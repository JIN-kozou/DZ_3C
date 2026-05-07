using Animancer;
using UnityEngine;

[System.Serializable]
public class PlayerMoveStartData
{
    [field: SerializeField] public TransitionAsset moveStart_F { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L45 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L90 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L135 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L180 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R45 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R90 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R135 { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R180 { get; private set; }

    [Tooltip("【蹲姿可选】若指定，蹲姿下任意方向起步都使用该过渡（忽略分向蹲姿字段）。")]
    [field: SerializeField] public TransitionAsset moveStartCrouchOmni { get; private set; }

    [field: SerializeField] public TransitionAsset moveStart_F_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L45_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L90_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L135_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_L180_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R45_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R90_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R135_Crouch { get; private set; }
    [field: SerializeField] public TransitionAsset moveStart_R180_Crouch { get; private set; }

    static TransitionAsset Pick(TransitionAsset stand, TransitionAsset crouch, bool crouchIntent)
    {
        if (crouchIntent && crouch != null && crouch.IsValid)
        {
            return crouch;
        }

        return stand;
    }

    /// <summary>按起步朝向角（度）与是否蹲姿意图，解析应播放的过渡；蹲姿未配时退回站姿同向。</summary>
    public TransitionAsset ResolveMoveStart(float targetAngleDeg, bool crouchIntent)
    {
        if (crouchIntent && moveStartCrouchOmni != null && moveStartCrouchOmni.IsValid)
        {
            return moveStartCrouchOmni;
        }

        if (targetAngleDeg < 22.5f && targetAngleDeg >= 0f || targetAngleDeg >= -22.5f && targetAngleDeg <= 0f)
        {
            return Pick(moveStart_F, moveStart_F_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= 22.5f && targetAngleDeg < 67.5f)
        {
            return Pick(moveStart_R45, moveStart_R45_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= 67.5f && targetAngleDeg < 112.5f)
        {
            return Pick(moveStart_R90, moveStart_R90_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= 112.5f && targetAngleDeg < 157.5f)
        {
            return Pick(moveStart_R135, moveStart_R135_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= 157.5f || targetAngleDeg < -157.5f)
        {
            return Pick(moveStart_R180, moveStart_R180_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= -157.5f && targetAngleDeg < -112.5f)
        {
            return Pick(moveStart_L135, moveStart_L135_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= -112.5f && targetAngleDeg < -67.5f)
        {
            return Pick(moveStart_L90, moveStart_L90_Crouch, crouchIntent);
        }

        if (targetAngleDeg >= -67.5f && targetAngleDeg < -22.5f)
        {
            return Pick(moveStart_L45, moveStart_L45_Crouch, crouchIntent);
        }

        return Pick(moveStart_F, moveStart_F_Crouch, crouchIntent);
    }
}
