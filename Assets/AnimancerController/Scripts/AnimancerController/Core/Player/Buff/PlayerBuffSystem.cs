using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffInstance
{
    public PlayerBuffConfigSO config;
    public int stackCount;
    public float remainDuration;
    public float tickTimer;
    public PlayerBuffSourceContext sourceContext;

    public PlayerBuffInstance(PlayerBuffConfigSO config, PlayerBuffSourceContext sourceContext)
    {
        this.config = config;
        this.sourceContext = sourceContext;
        stackCount = 1;
        remainDuration = config.Duration;
        tickTimer = 0f;
    }
}

public class PlayerBuffSystem
{
    private readonly Player player;
    private readonly List<PlayerBuffInstance> activeBuffs = new List<PlayerBuffInstance>();
    private readonly Dictionary<string, PlayerBuffInstance> uniqueBuffMap = new Dictionary<string, PlayerBuffInstance>();
    private PlayerBuffRuntimeSnapshot runtimeSnapshot = PlayerBuffRuntimeSnapshot.Default;

    public PlayerBuffSystem(Player player)
    {
        this.player = player;
    }

    public PlayerBuffRuntimeSnapshot RuntimeSnapshot => runtimeSnapshot;
    public IReadOnlyList<PlayerBuffInstance> ActiveBuffs => activeBuffs;

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        bool needRebuild = false;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var instance = activeBuffs[i];
            instance.remainDuration -= deltaTime;
            ProcessRegeneration(instance, deltaTime);
            if (instance.remainDuration > 0f)
            {
                continue;
            }

            RemoveInstance(instance, i);
            needRebuild = true;
        }

        if (needRebuild)
        {
            RebuildSnapshot();
        }
    }

    public void ApplyBuff(PlayerBuffConfigSO config, PlayerBuffSourceContext sourceContext)
    {
        if (config == null)
        {
            return;
        }
        if (config.SourceType != PlayerBuffSourceType.Other && config.SourceType != sourceContext.sourceType)
        {
            return;
        }

        if (config.StackRule == PlayerBuffStackRule.IndependentDuration)
        {
            AddIndependentBuff(config, sourceContext);
            RebuildSnapshot();
            return;
        }

        if (!uniqueBuffMap.TryGetValue(config.BuffId, out var instance))
        {
            instance = new PlayerBuffInstance(config, sourceContext);
            uniqueBuffMap[config.BuffId] = instance;
            activeBuffs.Add(instance);
            RebuildSnapshot();
            return;
        }

        switch (config.StackRule)
        {
            case PlayerBuffStackRule.Override:
                instance.stackCount = 1;
                instance.remainDuration = config.Duration;
                instance.tickTimer = 0f;
                instance.config = config;
                instance.sourceContext = sourceContext;
                break;
            case PlayerBuffStackRule.RefreshDuration:
            default:
                instance.stackCount = Mathf.Min(instance.stackCount + 1, config.MaxStack);
                instance.remainDuration = Mathf.Max(instance.remainDuration, config.Duration);
                instance.sourceContext = sourceContext;
                break;
        }

        RebuildSnapshot();
    }

    public void RemoveBuff(string buffId)
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            return;
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(activeBuffs[i].config.BuffId, buffId))
            {
                continue;
            }

            RemoveInstance(activeBuffs[i], i);
        }

        RebuildSnapshot();
    }

    public void ClearAll(bool includeUndispellable = false)
    {
        if (includeUndispellable)
        {
            activeBuffs.Clear();
            uniqueBuffMap.Clear();
        }
        else
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (!activeBuffs[i].config.IsDispellable)
                {
                    continue;
                }

                RemoveInstance(activeBuffs[i], i);
            }
        }

        RebuildSnapshot();
    }

    private void AddIndependentBuff(PlayerBuffConfigSO config, PlayerBuffSourceContext sourceContext)
    {
        var independent = new PlayerBuffInstance(config, sourceContext);
        activeBuffs.Add(independent);
    }

    private void RemoveInstance(PlayerBuffInstance instance, int index)
    {
        activeBuffs.RemoveAt(index);

        if (instance.config.StackRule != PlayerBuffStackRule.IndependentDuration &&
            uniqueBuffMap.TryGetValue(instance.config.BuffId, out var cache) &&
            cache == instance)
        {
            uniqueBuffMap.Remove(instance.config.BuffId);
        }
    }

    private void ProcessRegeneration(PlayerBuffInstance instance, float deltaTime)
    {
        if (instance.config.BuffType != PlayerBuffType.Regeneration)
        {
            return;
        }

        if (instance.config.TickInterval <= 0f || Mathf.Approximately(instance.config.RecoverValue, 0f))
        {
            return;
        }

        instance.tickTimer += deltaTime;
        while (instance.tickTimer >= instance.config.TickInterval)
        {
            instance.tickTimer -= instance.config.TickInterval;
            float recoverValue = instance.config.RecoverValue * instance.stackCount;
            player.RecoverResource(instance.config.RecoverTargetType, recoverValue);
        }
    }

    private void RebuildSnapshot()
    {
        var snapshot = PlayerBuffRuntimeSnapshot.Default;
        float multiplier = 1f;
        float additive = 0f;
        float dizzyAmplitude = 0f;
        float dizzyFrequency = 0f;
        float dizzyFovOffset = 0f;
        float shakePitchAmp = 0f;
        float shakeYawAmp = 0f;
        float shakeRollAmp = 0f;
        float shakePitchOff = 0f;
        float shakeYawOff = 0f;
        float shakeRollOff = 0f;
        float shakePitchFreq = 0f;
        float shakeYawFreq = 0f;
        float shakeRollFreq = 0f;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            var buff = activeBuffs[i];
            int stack = Mathf.Max(1, buff.stackCount);
            multiplier *= Mathf.Pow(buff.config.MoveSpeedMultiplier, stack);
            additive += buff.config.MoveSpeedAdditive * stack;
            dizzyAmplitude += buff.config.DizzyAmplitude * stack;
            dizzyFrequency = Mathf.Max(dizzyFrequency, buff.config.DizzyFrequency);
            dizzyFovOffset += buff.config.DizzyFovOffset * stack;
            shakePitchAmp += buff.config.DizzyShakePitchAmplitude * stack;
            shakeYawAmp += buff.config.DizzyShakeYawAmplitude * stack;
            shakeRollAmp += buff.config.DizzyShakeRollAmplitude * stack;
            shakePitchOff += buff.config.DizzyShakePitchOffset * stack;
            shakeYawOff += buff.config.DizzyShakeYawOffset * stack;
            shakeRollOff += buff.config.DizzyShakeRollOffset * stack;
            shakePitchFreq = Mathf.Max(shakePitchFreq, buff.config.DizzyShakePitchFrequencyScale);
            shakeYawFreq = Mathf.Max(shakeYawFreq, buff.config.DizzyShakeYawFrequencyScale);
            shakeRollFreq = Mathf.Max(shakeRollFreq, buff.config.DizzyShakeRollFrequencyScale);
        }

        snapshot.moveSpeedMultiplier = multiplier;
        snapshot.moveSpeedAdditive = additive;
        snapshot.dizzyAmplitude = dizzyAmplitude;
        snapshot.dizzyFrequency = dizzyFrequency;
        snapshot.dizzyFovOffset = dizzyFovOffset;
        snapshot.dizzyShakePitchAmplitude = shakePitchAmp;
        snapshot.dizzyShakeYawAmplitude = shakeYawAmp;
        snapshot.dizzyShakeRollAmplitude = shakeRollAmp;
        snapshot.dizzyShakePitchOffset = shakePitchOff;
        snapshot.dizzyShakeYawOffset = shakeYawOff;
        snapshot.dizzyShakeRollOffset = shakeRollOff;
        snapshot.dizzyShakePitchFrequencyScale = Mathf.Max(0.01f, shakePitchFreq);
        snapshot.dizzyShakeYawFrequencyScale = Mathf.Max(0.01f, shakeYawFreq);
        snapshot.dizzyShakeRollFrequencyScale = Mathf.Max(0.01f, shakeRollFreq);
        runtimeSnapshot = snapshot;
    }
}
