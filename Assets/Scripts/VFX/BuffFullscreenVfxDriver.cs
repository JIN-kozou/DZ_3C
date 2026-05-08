using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class BuffFullscreenVfxDriver : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private BuffFullscreenVfxBindingsSO bindingsConfig;
    [SerializeField] private bool deactivateAllFeaturesOnEnable = true;

    private sealed class RuntimeBindingState
    {
        public BuffFullscreenVfxBinding binding;
        public int instanceCount;
        public bool activeContribution;
        public float lastActivatedAt;
        public Coroutine pendingOn;
        public Coroutine pendingOff;
    }

    private readonly Dictionary<string, List<RuntimeBindingState>> statesByBuffId = new Dictionary<string, List<RuntimeBindingState>>();
    private readonly Dictionary<ScriptableRendererFeature, int> featureRefCounts = new Dictionary<ScriptableRendererFeature, int>();
    private bool subscribed;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        BuildRuntimeStates();
    }

    private void OnEnable()
    {
        if (deactivateAllFeaturesOnEnable) DeactivateAllKnownFeatures();
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
    }

    private void OnDisable()
    {
        if (player != null && player.BuffSystem != null && subscribed)
        {
            player.BuffSystem.BuffApplied -= HandleBuffApplied;
            player.BuffSystem.BuffRemoved -= HandleBuffRemoved;
        }

        subscribed = false;
        StopAllCoroutines();
        DeactivateAllKnownFeatures();
        featureRefCounts.Clear();

        foreach (var kv in statesByBuffId)
        {
            var states = kv.Value;
            for (int i = 0; i < states.Count; i++)
            {
                states[i].instanceCount = 0;
                states[i].activeContribution = false;
                states[i].pendingOn = null;
                states[i].pendingOff = null;
                states[i].lastActivatedAt = 0f;
            }
        }
    }

    private void BuildRuntimeStates()
    {
        statesByBuffId.Clear();
        if (bindingsConfig == null || bindingsConfig.bindings == null) return;

        for (int i = 0; i < bindingsConfig.bindings.Count; i++)
        {
            var binding = bindingsConfig.bindings[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.buffId)) continue;

            if (!statesByBuffId.TryGetValue(binding.buffId, out var list))
            {
                list = new List<RuntimeBindingState>();
                statesByBuffId.Add(binding.buffId, list);
            }

            list.Add(new RuntimeBindingState { binding = binding });
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (player == null || player.BuffSystem == null) return;

        player.BuffSystem.BuffApplied += HandleBuffApplied;
        player.BuffSystem.BuffRemoved += HandleBuffRemoved;
        subscribed = true;
    }

    private void HandleBuffApplied(PlayerBuffLifecycleEvent evt)
    {
        if (!statesByBuffId.TryGetValue(evt.buffId, out var states)) return;

        for (int i = 0; i < states.Count; i++)
        {
            RuntimeBindingState state = states[i];
            if (evt.isRefresh && !state.binding.triggerOnRefresh) continue;

            state.instanceCount++;
            if (state.pendingOff != null)
            {
                StopCoroutine(state.pendingOff);
                state.pendingOff = null;
            }

            if (!state.activeContribution && state.pendingOn == null)
            {
                state.pendingOn = StartCoroutine(ActivateAfterDelay(state));
            }
        }
    }

    private void HandleBuffRemoved(PlayerBuffLifecycleEvent evt)
    {
        if (!statesByBuffId.TryGetValue(evt.buffId, out var states)) return;

        for (int i = 0; i < states.Count; i++)
        {
            RuntimeBindingState state = states[i];
            state.instanceCount = Mathf.Max(0, state.instanceCount - 1);

            if (state.instanceCount > 0) continue;

            if (state.pendingOn != null)
            {
                StopCoroutine(state.pendingOn);
                state.pendingOn = null;
            }

            if (state.activeContribution && state.pendingOff == null)
            {
                state.pendingOff = StartCoroutine(DeactivateAfterDelay(state));
            }
        }
    }

    private IEnumerator ActivateAfterDelay(RuntimeBindingState state)
    {
        float delay = state.binding != null ? Mathf.Max(0f, state.binding.delayAfterApplySec) : 0f;
        if (delay > 0f) yield return new WaitForSeconds(delay);

        state.pendingOn = null;
        if (state.instanceCount <= 0 || state.activeContribution) yield break;

        state.activeContribution = true;
        state.lastActivatedAt = Time.time;
        SetFeaturesActive(state.binding, true);
    }

    private IEnumerator DeactivateAfterDelay(RuntimeBindingState state)
    {
        float delay = state.binding != null ? Mathf.Max(0f, state.binding.delayAfterRemoveSec) : 0f;
        float minHold = state.binding != null ? Mathf.Max(0f, state.binding.minHoldSec) : 0f;
        float minHoldRemain = Mathf.Max(0f, (state.lastActivatedAt + minHold) - Time.time);
        float wait = Mathf.Max(delay, minHoldRemain);
        if (wait > 0f) yield return new WaitForSeconds(wait);

        state.pendingOff = null;
        if (state.instanceCount > 0 || !state.activeContribution) yield break;

        state.activeContribution = false;
        SetFeaturesActive(state.binding, false);
    }

    private void SetFeaturesActive(BuffFullscreenVfxBinding binding, bool active)
    {
        if (binding == null || binding.features == null) return;

        for (int i = 0; i < binding.features.Length; i++)
        {
            ScriptableRendererFeature feature = binding.features[i];
            if (feature == null) continue;

            if (!featureRefCounts.TryGetValue(feature, out int count))
            {
                count = 0;
            }

            if (active)
            {
                count++;
                featureRefCounts[feature] = count;
                if (count == 1) feature.SetActive(true);
            }
            else
            {
                count = Mathf.Max(0, count - 1);
                if (count == 0)
                {
                    featureRefCounts.Remove(feature);
                    feature.SetActive(false);
                }
                else
                {
                    featureRefCounts[feature] = count;
                }
            }
        }
    }

    private void DeactivateAllKnownFeatures()
    {
        if (bindingsConfig == null || bindingsConfig.bindings == null) return;

        for (int i = 0; i < bindingsConfig.bindings.Count; i++)
        {
            var binding = bindingsConfig.bindings[i];
            if (binding == null || binding.features == null) continue;
            for (int j = 0; j < binding.features.Length; j++)
            {
                var feature = binding.features[j];
                if (feature != null) feature.SetActive(false);
            }
        }
    }
}
