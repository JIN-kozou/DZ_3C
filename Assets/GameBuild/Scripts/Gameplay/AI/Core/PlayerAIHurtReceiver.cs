using DZ_3C.Reverse;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    public class PlayerAIHurtReceiver : MonoBehaviour, IAIHurtReceiver
    {
        [SerializeField] private ReverseCoreStack reverseCoreStack;
        [SerializeField] private Player player;
        [Header("Hit Visual Buff (optional)")]
        [SerializeField] private PlayerBuffConfigSO hurtVisualBuffConfig;
        [SerializeField] private PlayerBuffSourceType hurtBuffSourceType = PlayerBuffSourceType.Monster;

        [Header("Damage direction HUD (3D world UI, optional)")]
        [SerializeField, Tooltip("拖入 DamageDirectionHint 预制体；留空则不显示受击方向提示。")]
        private GameObject damageDirectionHintWorldPrefab;

        [SerializeField, Tooltip("提示 Canvas 的父节点；留空则用玩家根 Transform。")]
        private Transform damageDirectionHintAnchor;

        [SerializeField, Min(0.0001f), Tooltip("世界空间 UI 根 RectTransform 的 localScale 分量。")]
        private float damageDirectionHintWorldScale = 0.0025f;

        [SerializeField, Min(0.05f), Tooltip("每次受击后提示显示时长（秒）。")]
        private float damageDirectionHintVisibleSeconds = 1.2f;

        private DamageDirectionHudView _damageDirectionHud;

        private void Awake()
        {
            if (reverseCoreStack == null) reverseCoreStack = GetComponent<ReverseCoreStack>();
            if (player == null) player = GetComponent<Player>();
            if (damageDirectionHintWorldPrefab != null)
            {
                var anchor = damageDirectionHintAnchor != null ? damageDirectionHintAnchor : transform;
                var go = Instantiate(damageDirectionHintWorldPrefab, anchor);
                go.name = "DamageDirectionHint_World";
                var v = go.GetComponent<DamageDirectionHudView>();
                if (v == null)
                    v = go.AddComponent<DamageDirectionHudView>();
                v.Configure(anchor, damageDirectionHintWorldScale, null);
                _damageDirectionHud = v;
            }
        }

        private void OnDestroy()
        {
            if (_damageDirectionHud != null)
            {
                Destroy(_damageDirectionHud.gameObject);
                _damageDirectionHud = null;
            }
        }

        public void ReceiveAIDamage(float damage, string buffId, object attacker)
        {
            if (damage <= 0f) return;

            if (reverseCoreStack != null)
            {
                reverseCoreStack.ApplyDamage(damage);
                ApplyHurtVisualBuff(attacker);
                _damageDirectionHud?.ShowFromAttacker(ResolveSourceObject(attacker), damageDirectionHintVisibleSeconds);
                return;
            }

            if (player != null && player.ReusableData != null)
            {
                player.ReusableData.health.Value = Mathf.Max(0f, player.ReusableData.health.Value - damage);
                ApplyHurtVisualBuff(attacker);
                _damageDirectionHud?.ShowFromAttacker(ResolveSourceObject(attacker), damageDirectionHintVisibleSeconds);
            }
        }

        private void ApplyHurtVisualBuff(object attacker)
        {
            if (hurtVisualBuffConfig == null || player == null) return;
            GameObject sourceObject = ResolveSourceObject(attacker);
            player.ApplyBuff(hurtVisualBuffConfig, new PlayerBuffSourceContext(hurtBuffSourceType, sourceObject));
        }

        private static GameObject ResolveSourceObject(object attacker)
        {
            if (attacker is GameObject go) return go;
            if (attacker is Component component) return component.gameObject;
            return null;
        }
    }
}
