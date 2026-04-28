using System.Text;
using UnityEngine;

namespace DZ_3C.Reverse
{
    /// <summary>
    /// OnGUI 临时调试面板。显示锚 / 各核心血量、阵列槽位、视野半径。
    /// 不是正式 HUD，仅用于第一版手感验收。
    /// </summary>
    [DisallowMultipleComponent]
    public class ReverseDebugView : MonoBehaviour
    {
        [SerializeField] private ReverseCoreStack coreStack;
        [SerializeField] private ReverseArrayRegistry registry;
        [SerializeField] private ReverseAnchor anchor;

        [SerializeField] private Vector2 screenAnchor = new Vector2(10f, 10f);
        [SerializeField] private float panelWidth = 320f;
        [SerializeField] private int fontSize = 14;

        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private readonly StringBuilder sb = new StringBuilder(256);
        private string lastEvent = string.Empty;

        private void Awake()
        {
            if (coreStack == null) coreStack = GetComponent<ReverseCoreStack>();
            if (anchor == null && coreStack != null) anchor = coreStack.Anchor;
            if (registry == null && coreStack != null) registry = coreStack.Registry;
        }

        private void OnEnable()
        {
            if (coreStack != null)
            {
                coreStack.OnDeath += () => lastEvent = "Death";
                coreStack.OnRespawned += pos => lastEvent = $"Respawn @ {pos:F1}";
                coreStack.OnGameOver += () => lastEvent = "GameOver";
                coreStack.OnViewRadiusChanged += r => lastEvent = $"View={r:F1}m";
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float h = 30f + 18f * (1 + (coreStack != null ? coreStack.Cores.Count : 0) + (registry != null ? registry.DeployedCount : 0) + 4);
            float panelX = screenAnchor.x;
            float panelY = Mathf.Max(0f, Screen.height - h - screenAnchor.y); // 左下角锚点：y 表示距底边偏移
            GUI.Box(new Rect(panelX, panelY, panelWidth, h), GUIContent.none, boxStyle);
            sb.Clear();
            sb.AppendLine("[Reverse Debug]");
            if (anchor != null) sb.AppendLine($"Anchor HP: {anchor.CurrentHealth:F1} / {anchor.MaxHealth:F1}");
            if (coreStack != null)
            {
                sb.AppendLine($"Cores ({coreStack.Cores.Count}, full={coreStack.FullCoreCount}):");
                for (int i = 0; i < coreStack.Cores.Count; i++)
                {
                    var c = coreStack.Cores[i];
                    if (c == null) { sb.AppendLine($"  [{i}] <null>"); continue; }
                    string tag = c.IsFull ? "FULL" : c.IsEmpty ? "EMPTY" : "PART";
                    sb.AppendLine($"  [{i}] {c.Health:F1}/{c.MaxHealth:F1} {tag}");
                }
                sb.AppendLine($"View Radius: {coreStack.CurrentViewRadius:F1} m");
                sb.AppendLine($"Invincible: {(coreStack.IsInvincible ? "Y" : "N")}");
            }
            if (registry != null)
            {
                sb.AppendLine($"Arrays ({registry.DeployedCount}):");
                for (int i = 0; i < registry.DeployedArrays.Count; i++)
                {
                    var a = registry.DeployedArrays[i];
                    if (a == null) continue;
                    sb.AppendLine($"  slot={a.SlotIndex} seq={a.DeploymentSequence}");
                }
            }
            if (!string.IsNullOrEmpty(lastEvent)) sb.AppendLine($"Event: {lastEvent}");
            GUI.Label(new Rect(panelX + 8f, panelY + 6f, panelWidth - 16f, h - 12f), sb.ToString(), labelStyle);
        }

        private void EnsureStyles()
        {
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
            }
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };
                labelStyle.normal.textColor = Color.white;
            }
        }
    }
}
