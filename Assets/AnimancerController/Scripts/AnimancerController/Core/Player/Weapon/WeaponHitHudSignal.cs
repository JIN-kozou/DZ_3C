using System;
using UnityEngine;

/// <summary>
/// 玩家武器对有效受击体结算伤害时发出，供 HUD（如 HitMark）订阅。
/// </summary>
public static class WeaponHitHudSignal
{
    public static event Action PlayerDealtDamageToReceiver;

    public static void RaisePlayerDealtDamageToReceiver()
    {
        PlayerDealtDamageToReceiver?.Invoke();
    }
}
