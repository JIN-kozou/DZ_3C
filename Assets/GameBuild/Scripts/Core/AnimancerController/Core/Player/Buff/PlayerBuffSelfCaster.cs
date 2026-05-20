using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBuffSelfCaster : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private List<PlayerBuffConfigSO> selfBuffs = new List<PlayerBuffConfigSO>();
    [SerializeField] private Key triggerKey = Key.B;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }
    }

    private void Update()
    {
        if (player == null || Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current[triggerKey].wasPressedThisFrame)
        {
            return;
        }

        var sourceContext = new PlayerBuffSourceContext(PlayerBuffSourceType.Self, player.gameObject);
        for (int i = 0; i < selfBuffs.Count; i++)
        {
            player.ApplyBuff(selfBuffs[i], sourceContext);
        }
    }
}
