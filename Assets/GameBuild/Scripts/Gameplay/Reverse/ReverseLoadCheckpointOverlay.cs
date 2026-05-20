using System;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// Checkpoint respawn splash (LoadPanel). Shown when the player dies with array or battery respawn.
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseLoadCheckpointOverlay : MonoBehaviour
    {
        private static ReverseLoadCheckpointOverlay instance;

        [SerializeField] private CanvasGroup canvasGroup;

        [Min(0.1f)]
        [SerializeField] private float fadeDuration = 3f;

        [SerializeField] private bool pauseTimeDuringFade = true;

        private Coroutine runningFade;

        public static bool TryPlayThen(float duration, Action onComplete)
        {
            ReverseLoadCheckpointOverlay overlay = instance != null
                ? instance
                : FindObjectOfType<ReverseLoadCheckpointOverlay>(true);

            if (overlay == null)
            {
                return false;
            }

            return overlay.TryPlayFadeThen(duration, onComplete);
        }

        private void Awake()
        {
            instance = this;
            EnsureInitialized();
            ApplyHiddenVisuals();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void HideImmediate()
        {
            ReverseUiFadeUtil.HideImmediate(this, canvasGroup, ref runningFade);
        }

        private void ApplyHiddenVisuals()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private bool TryPlayFadeThen(float duration, Action onComplete)
        {
            EnsureInitialized();
            return ReverseUiFadeUtil.TryPlay(
                this,
                canvasGroup,
                ref runningFade,
                duration,
                fadeDuration,
                pauseTimeDuringFade,
                onComplete);
        }

        private void EnsureInitialized()
        {
            if (canvasGroup == null)
            {
                canvasGroup = ReverseUiFadeUtil.EnsureCanvasGroup(this);
            }
        }
    }
}
