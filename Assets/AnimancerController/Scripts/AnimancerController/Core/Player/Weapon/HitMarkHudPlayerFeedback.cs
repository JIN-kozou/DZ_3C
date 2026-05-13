using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在 HUD HitMark 根物体（与 <see cref="Animator"/>、<see cref="CanvasGroup"/> 同物体）上：
/// 未命中时隐藏（alpha 0、Animator 关闭）；玩家射击命中并造成伤害时显示并重播 <c>HitMark_Appear</c>，播完再回到隐藏。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class HitMarkHudPlayerFeedback : MonoBehaviour
{
    private const string AppearStateName = "HitMark_Appear";

    [SerializeField] private Animator animator;

    [SerializeField, Tooltip("与 HitMark 根物体上的 CanvasGroup 相同；留空则 GetComponent。")]
    private CanvasGroup canvasGroup;

    private static readonly int AppearStateHash = Animator.StringToHash(AppearStateName);

    private Coroutine _playRoutine;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        ApplyHiddenIdle();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private void OnEnable()
    {
        WeaponHitHudSignal.PlayerDealtDamageToReceiver += OnPlayerHit;
    }

    private void OnDisable()
    {
        WeaponHitHudSignal.PlayerDealtDamageToReceiver -= OnPlayerHit;
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        ApplyHiddenIdle();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private void ApplyHiddenIdle()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnPlayerHit()
    {
        if (animator == null)
        {
            return;
        }

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
        }

        _playRoutine = StartCoroutine(CoPlayAppear());
    }

    private IEnumerator CoPlayAppear()
    {
        animator.enabled = true;
        animator.Play(AppearStateHash, 0, 0f);
        yield return null;

        float len = animator.GetCurrentAnimatorStateInfo(0).length;
        if (len < 0.0001f)
        {
            len = 0.2f;
        }

        yield return new WaitForSeconds(len);

        ApplyHiddenIdle();
        animator.enabled = false;
        _playRoutine = null;
    }
}
