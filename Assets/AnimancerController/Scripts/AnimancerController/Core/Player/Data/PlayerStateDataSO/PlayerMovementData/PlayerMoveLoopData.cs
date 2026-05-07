using Animancer;
using UnityEngine;

[System.Serializable]
public class PlayerMoveLoopData
{
    [field: SerializeField] public TransitionAsset moveLoop { get; private set; }

    [Tooltip("蹲姿移动循环；未配置时沿用站姿 moveLoop。")]
    [field: SerializeField] public TransitionAsset moveLoopCrouch { get; private set; }

    /// <summary>站姿或蹲姿下应播放的循环过渡（蹲姿未配则退回站姿）。</summary>
    public TransitionAsset ResolveMoveLoop(bool crouchIntent)
    {
        if (crouchIntent && moveLoopCrouch != null && moveLoopCrouch.IsValid)
        {
            return moveLoopCrouch;
        }

        return moveLoop;
    }
}
