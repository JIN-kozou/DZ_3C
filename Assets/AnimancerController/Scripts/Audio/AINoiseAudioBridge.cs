using System.Collections;
using System.Collections.Generic;
using DZ_3C.AI.Core;
using DZ_3C.AI.Perception;
using UnityEngine;

[DisallowMultipleComponent]
public class AINoiseAudioBridge : MonoBehaviour
{
    private sealed class NoisePulse
    {
        public int Id;
        public float Loudness;
    }

    [SerializeField] private AINoiseEmitter noiseEmitter;
    [SerializeField] private AITargetable targetable;
    [SerializeField] private bool stopNoiseOnAwake = true;

    private readonly List<NoisePulse> activePulses = new List<NoisePulse>();
    private bool continuousNoiseActive;
    private float continuousLoudness;
    private int nextPulseId;
    private bool warnedMissingRequirements;

    private void Awake()
    {
        ResolveReferences();
        WarnMissingRequirementsOnce();

        if (stopNoiseOnAwake && IsReady())
        {
            noiseEmitter.isEmitting = false;
            noiseEmitter.loudness = 0f;
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        WarnMissingRequirementsOnce();
    }

    public bool IsReady()
    {
        return noiseEmitter != null && targetable != null;
    }

    public void EmitNoise(float loudness, float duration)
    {
        if (!IsReady() || loudness <= 0f || duration <= 0f)
        {
            return;
        }

        NoisePulse pulse = new NoisePulse
        {
            Id = nextPulseId++,
            Loudness = Mathf.Max(0f, loudness)
        };

        activePulses.Add(pulse);
        ApplyNoiseState();
        StartCoroutine(StopPulseAfterDelay(pulse.Id, duration));
    }

    public void StartContinuousNoise(float loudness)
    {
        if (!IsReady() || loudness <= 0f)
        {
            return;
        }

        continuousNoiseActive = true;
        continuousLoudness = Mathf.Max(0f, loudness);
        ApplyNoiseState();
    }

    public void StopContinuousNoise()
    {
        continuousNoiseActive = false;
        continuousLoudness = 0f;
        ApplyNoiseState();
    }

    private IEnumerator StopPulseAfterDelay(int pulseId, float duration)
    {
        yield return new WaitForSeconds(duration);

        for (int i = activePulses.Count - 1; i >= 0; i--)
        {
            if (activePulses[i].Id == pulseId)
            {
                activePulses.RemoveAt(i);
                break;
            }
        }

        ApplyNoiseState();
    }

    private void ApplyNoiseState()
    {
        if (!IsReady())
        {
            return;
        }

        float loudestNoise = continuousNoiseActive ? continuousLoudness : 0f;
        for (int i = 0; i < activePulses.Count; i++)
        {
            loudestNoise = Mathf.Max(loudestNoise, activePulses[i].Loudness);
        }

        bool hasNoise = continuousNoiseActive || activePulses.Count > 0;
        noiseEmitter.loudness = loudestNoise;
        noiseEmitter.isEmitting = hasNoise;
    }

    private void ResolveReferences()
    {
        if (noiseEmitter == null)
        {
            noiseEmitter = GetComponent<AINoiseEmitter>();
        }

        if (noiseEmitter == null)
        {
            noiseEmitter = GetComponentInParent<AINoiseEmitter>();
        }

        if (targetable == null)
        {
            targetable = GetComponent<AITargetable>();
        }

        if (targetable == null)
        {
            targetable = GetComponentInParent<AITargetable>();
        }
    }

    private void WarnMissingRequirementsOnce()
    {
        if (warnedMissingRequirements || IsReady())
        {
            return;
        }

        warnedMissingRequirements = true;
        Debug.LogWarning($"{nameof(AINoiseAudioBridge)} on {name} requires {nameof(AINoiseEmitter)} and {nameof(AITargetable)} on this GameObject or a parent.", this);
    }
}
