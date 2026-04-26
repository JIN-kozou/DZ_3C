using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffTriggerTransmitter : MonoBehaviour
{
    [SerializeField] private List<PlayerBuffConfigSO> buffs = new List<PlayerBuffConfigSO>();
    [SerializeField] private PlayerBuffSourceType sourceType = PlayerBuffSourceType.Other;
    [SerializeField] private bool triggerOnce = false;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player == null)
        {
            return;
        }

        ApplyBuffRequest(player);
    }

    public void ApplyBuffRequest(Player player)
    {
        if (player == null || buffs == null || buffs.Count == 0)
        {
            return;
        }

        var sourceContext = new PlayerBuffSourceContext(sourceType, gameObject);
        for (int i = 0; i < buffs.Count; i++)
        {
            player.ApplyBuff(buffs[i], sourceContext);
        }

        if (triggerOnce)
        {
            hasTriggered = true;
        }
    }
}
