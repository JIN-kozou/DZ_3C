using UnityEngine;

public class PlayerBuffDebugView : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private Vector2 anchor = new Vector2(20f, 20f);

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void OnGUI()
    {
        if (!showOnScreen || player == null || player.BuffSystem == null)
        {
            return;
        }

        var buffs = player.BuffSystem.ActiveBuffs;
        var rect = new Rect(anchor.x, anchor.y, 420f, 24f + buffs.Count * 20f);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label($"HP: {player.ReusableData.health.Value:F1}  ST: {player.ReusableData.stamina.Value:F1}");
        GUILayout.Label($"Buff Count: {buffs.Count}");
        for (int i = 0; i < buffs.Count; i++)
        {
            var buff = buffs[i];
            GUILayout.Label($"{buff.config.BuffId} x{buff.stackCount} ({buff.remainDuration:F1}s)");
        }
        GUILayout.EndArea();
    }
}
