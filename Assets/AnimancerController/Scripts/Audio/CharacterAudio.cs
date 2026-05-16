using DZ_3C.AI.Core;
using DZ_3C.AI.Perception;
using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private GameObject[] footsteps;
    [SerializeField] private GameObject breathing;
    [SerializeField] private GameObject jumpExhale;
    [SerializeField] private GameObject jumpWhoosh;
    [SerializeField] private GameObject land;

    [Header("Breathing")]
    [SerializeField, Min(0f)] private float normalBreathingVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float intenseBreathingVolumeMultiplier = 1.4f;
    [Tooltip("开镜输入保持且已进入有效瞄姿态后，累计满该秒数才开始播呼吸循环。")]
    [SerializeField, Min(0f)] private float adsBreathingHoldSeconds = 3f;
    [Tooltip("除转动视角外无任何打断性操作、且未开镜时，累计满该秒数开始播呼吸；有打断操作则停止。")]
    [SerializeField, Min(0f)] private float idleCalmBreathingHoldSeconds = 3f;

    [Header("Auto Player Movement")]
    [SerializeField] private bool autoDetectPlayerMovement = true;
    [SerializeField, Min(0.05f)] private float walkFootstepInterval = 0.46f;
    [SerializeField, Min(0.05f)] private float runFootstepInterval = 0.32f;
    [SerializeField, Min(0f)] private float groundedMoveSpeedThreshold = 0.05f;

    [Header("Stealth")]
    [SerializeField, Range(0f, 1f)] private float crouchFootstepVolumeMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)] private float crouchNoiseStandThreshold = 0.99f;
    [SerializeField] private bool crouchFootstepsAreSilentToAI = true;
    [SerializeField, Min(0.05f)] private float crouchWalkFootstepInterval = 0.58f;
    [SerializeField, Min(0.05f)] private float crouchRunFootstepInterval = 0.42f;

    [Header("State")]
    [SerializeField] private GameObject death;
    [SerializeField] private GameObject respawn;

    [Header("Core")]
    [SerializeField] private GameObject corePickup;
    [SerializeField, Min(0f)] private float corePickupStartSeconds;
    [SerializeField] private GameObject coreInstall;
    [SerializeField, Min(0f)] private float coreInstallStartSeconds;
    [SerializeField] private GameObject coreCharge;
    [SerializeField] private GameObject coreConsume;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float footstepNoiseLoudness = 12f;
    [SerializeField, Min(0f)] private float footstepNoiseDuration = 0.15f;
    [SerializeField, Min(0f)] private float jumpNoiseLoudness = 0.7f;
    [SerializeField, Min(0f)] private float jumpNoiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float landingNoiseLoudness = 1.2f;
    [SerializeField, Min(0f)] private float landingNoiseDuration = 0.3f;
    [SerializeField, Min(0f)] private float deathNoiseLoudness;
    [SerializeField, Min(0f)] private float deathNoiseDuration = 0.3f;
    [SerializeField, Min(0f)] private float respawnNoiseLoudness;
    [SerializeField, Min(0f)] private float respawnNoiseDuration = 0.2f;

    private AudioSource breathingSource;
    private Player player;
    private bool hasGroundedSnapshot;
    private bool wasGrounded;
    private bool wasMovingOnGround;
    private float nextFootstepTime;
    private float continuousEffectiveAdsSeconds;
    private float idleCalmSeconds;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        continuousEffectiveAdsSeconds = 0f;
        idleCalmSeconds = 0f;
        CaptureGroundedSnapshot();
    }

    private void Update()
    {
        TickBreathingFollow();
        TickAutoPlayerMovementAudio();
        TickBreathingTriggers();
    }

    private void OnDisable()
    {
        StopBreathing();
    }

    private void OnDestroy()
    {
        StopBreathing();
    }

    public void OnFootstep()
    {
        GameObject footstep = GetRandomFootstep();
        AudioPrefabPlayer.Play(footstep, transform.position, null, false, GetFootstepVolumeMultiplier());
        if (!ShouldMuteFootstepNoiseForAI())
        {
            EmitAINoise(footstepNoiseLoudness, footstepNoiseDuration);
        }
    }

    public void OnJump()
    {
        PlayAtSelf(jumpExhale);
        PlayAtSelf(jumpWhoosh);
        EmitAINoise(jumpNoiseLoudness, jumpNoiseDuration);
    }

    public void OnLand()
    {
        PlayAtSelf(land);
        EmitAINoise(landingNoiseLoudness, landingNoiseDuration);
    }

    public void OnDeath()
    {
        StopBreathing();
        PlayAtSelf(death);
        EmitAINoise(deathNoiseLoudness, deathNoiseDuration);
    }

    public void OnRespawn()
    {
        PlayAtSelf(respawn);
        EmitAINoise(respawnNoiseLoudness, respawnNoiseDuration);
    }

    public void OnCorePickup() => PlayAtSelf(corePickup, corePickupStartSeconds);
    public void OnCoreInstall() => PlayAtSelf(coreInstall, coreInstallStartSeconds);
    public void OnCoreCharge() => PlayAtSelf(coreCharge);
    public void OnCoreConsume() => PlayAtSelf(coreConsume);
    public void SetBreathingNormal() => SetBreathingIntensity(0f);
    public void SetBreathingIntense() => SetBreathingIntensity(1f);

    public void SetBreathingIntensity(float intensity01)
    {
        if (breathingSource == null)
        {
            return;
        }

        float multiplier = Mathf.Lerp(normalBreathingVolumeMultiplier, intenseBreathingVolumeMultiplier, Mathf.Clamp01(intensity01));
        breathingSource.volume = Mathf.Clamp01(multiplier);
    }

    public void StartBreathing()
    {
        if (breathingSource != null || breathing == null)
        {
            return;
        }

        breathingSource = AudioPrefabPlayer.Play(breathing, transform.position, transform, true);
        SetBreathingIntensity(0f);
    }

    public void StopBreathing()
    {
        AudioPrefabPlayer.Stop(breathingSource);
        breathingSource = null;
    }

    private void TickBreathingFollow()
    {
        if (breathingSource == null)
        {
            return;
        }

        if (!breathingSource.isPlaying)
        {
            breathingSource = null;
            return;
        }

        breathingSource.transform.position = transform.position;
    }

    private void TickAutoPlayerMovementAudio()
    {
        if (!autoDetectPlayerMovement || player == null)
        {
            return;
        }

        bool grounded = player.isOnGround.Value;
        if (!hasGroundedSnapshot)
        {
            wasGrounded = grounded;
            hasGroundedSnapshot = true;
        }

        if (wasGrounded && !grounded && player.verticalSpeed > 0.05f)
        {
            OnJump();
        }
        else if (!wasGrounded && grounded)
        {
            OnLand();
        }

        wasGrounded = grounded;
        TickAutoFootsteps(grounded);
    }

    private void TickAutoFootsteps(bool grounded)
    {
        bool moving = grounded && IsMovingOnGround();
        if (!moving)
        {
            wasMovingOnGround = false;
            return;
        }

        if (!wasMovingOnGround)
        {
            nextFootstepTime = 0f;
        }

        wasMovingOnGround = true;
        if (Time.time < nextFootstepTime)
        {
            return;
        }

        OnFootstep();
        nextFootstepTime = Time.time + GetFootstepInterval();
    }

    private void TickBreathingTriggers()
    {
        ResolveReferences();
        if (player == null)
        {
            return;
        }

        if (HasBreathingInterruptingActivity())
        {
            continuousEffectiveAdsSeconds = 0f;
            idleCalmSeconds = 0f;
            StopBreathing();
            return;
        }

        InputService input = player.InputService;
        bool inAds = IsPlayerInEffectiveAds();
        if (inAds)
        {
            continuousEffectiveAdsSeconds += Time.deltaTime;
        }
        else
        {
            continuousEffectiveAdsSeconds = 0f;
        }

        bool idleCalmEligible = input != null && !input.ADSHeld && !inAds;
        if (idleCalmEligible)
        {
            idleCalmSeconds += Time.deltaTime;
        }
        else
        {
            idleCalmSeconds = 0f;
        }

        bool shouldPlay =
            continuousEffectiveAdsSeconds >= adsBreathingHoldSeconds ||
            idleCalmSeconds >= idleCalmBreathingHoldSeconds;

        if (shouldPlay)
        {
            StartBreathing();
        }
        else
        {
            StopBreathing();
        }
    }

    /// <summary>
    /// 打断性操作（不含仅转视角）；命中则停止呼吸并重置 ADS / 静止累计。
    /// </summary>
    private bool HasBreathingInterruptingActivity()
    {
        InputService input = player.InputService;
        if (input == null)
        {
            return false;
        }

        if (input.MoveDiscrete.sqrMagnitude > 1e-6f)
        {
            return true;
        }

        if (input.Shift)
        {
            return true;
        }

        if (input.FireHeld || input.FireWasPressedThisFrame)
        {
            return true;
        }

        if (input.CrouchHeld)
        {
            return true;
        }

        if (input.Interactive || input.InteractiveWasPressedThisFrame)
        {
            return true;
        }

        if (input.ToggleWeaponWasPressedThisFrame ||
            input.HolsterWeaponWasPressedThisFrame ||
            input.ToggleOverShoulderWasPressedThisFrame)
        {
            return true;
        }

        if (input.Scroll.sqrMagnitude > 1e-6f)
        {
            return true;
        }

        return false;
    }

    private bool IsPlayerInEffectiveAds()
    {
        if (player.WeaponRuntime == null || !player.WeaponRuntime.IsAds)
        {
            return false;
        }

        PlayerArmedPresentation armed = player.ArmedPresentation;
        if (armed != null && armed.UsesAnimatedAdsCameraGates)
        {
            return armed.IsAdsCameraAimActive;
        }

        return true;
    }

    private bool IsMovingOnGround()
    {
        if (player.InputService != null && player.InputService.MoveDiscrete != Vector2.zero)
        {
            return true;
        }

        Vector3 velocity = player.AnimationVelocity;
        velocity.y = 0f;
        return velocity.sqrMagnitude > groundedMoveSpeedThreshold * groundedMoveSpeedThreshold;
    }

    private float GetFootstepInterval()
    {
        bool running = player.InputService != null && player.InputService.Shift;
        if (IsCrouchFootstepStance())
        {
            return running ? crouchRunFootstepInterval : crouchWalkFootstepInterval;
        }

        return running ? runFootstepInterval : walkFootstepInterval;
    }

    private bool IsCrouchFootstepStance()
    {
        if (player == null || player.ReusableData == null || player.ReusableData.standValueParameter == null)
        {
            return false;
        }

        var stand = player.ReusableData.standValueParameter;
        return stand.CurrentValue < crouchNoiseStandThreshold || stand.TargetValue < crouchNoiseStandThreshold;
    }

    private GameObject GetRandomFootstep()
    {
        if (footsteps == null || footsteps.Length == 0)
        {
            return null;
        }

        return footsteps[Random.Range(0, footsteps.Length)];
    }

    private void PlayAtSelf(GameObject soundPrefab, float startTimeSeconds = 0f)
    {
        AudioPrefabPlayer.Play(
            soundPrefab,
            transform.position,
            null,
            false,
            1f,
            1f,
            Mathf.Max(0f, startTimeSeconds));
    }

    private void EmitAINoise(float loudness, float duration)
    {
        ResolveReferences();
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = GetComponent<Player>() ?? GetComponentInParent<Player>();
        }

        if (aiNoiseBridge == null)
        {
            aiNoiseBridge = GetComponent<AINoiseAudioBridge>() ?? GetComponentInParent<AINoiseAudioBridge>();
        }
    }

    private float GetFootstepVolumeMultiplier()
    {
        if (player == null || player.ReusableData == null || player.ReusableData.standValueParameter == null)
        {
            return 1f;
        }

        float stand01 = Mathf.Clamp01(player.ReusableData.standValueParameter.CurrentValue);
        return Mathf.Lerp(crouchFootstepVolumeMultiplier, 1f, stand01);
    }

    private bool ShouldMuteFootstepNoiseForAI()
    {
        if (!crouchFootstepsAreSilentToAI || player == null || player.ReusableData == null || player.ReusableData.standValueParameter == null)
        {
            return false;
        }

        return player.ReusableData.standValueParameter.CurrentValue < crouchNoiseStandThreshold ||
               player.ReusableData.standValueParameter.TargetValue < crouchNoiseStandThreshold;
    }

    private void CaptureGroundedSnapshot()
    {
        if (player == null)
        {
            hasGroundedSnapshot = false;
            return;
        }

        wasGrounded = player.isOnGround.Value;
        hasGroundedSnapshot = true;
        wasMovingOnGround = false;
        nextFootstepTime = 0f;
    }

}

internal static class CharacterAudioAutoBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToPlayers()
    {
        Player[] players = Object.FindObjectsOfType<Player>();
        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            if (player != null && player.GetComponent<CharacterAudio>() == null)
            {
                player.gameObject.AddComponent<CharacterAudio>();
            }

            EnsureAINoiseComponents(player);
        }
    }

    private static void EnsureAINoiseComponents(Player player)
    {
        if (player == null)
        {
            return;
        }

        AITargetable targetable = player.GetComponent<AITargetable>();
        if (targetable == null)
        {
            targetable = player.gameObject.AddComponent<AITargetable>();
        }

        targetable.SetAlive(true);

        AINoiseEmitter noiseEmitter = player.GetComponent<AINoiseEmitter>();
        if (noiseEmitter == null)
        {
            noiseEmitter = player.gameObject.AddComponent<AINoiseEmitter>();
        }

        noiseEmitter.isEmitting = false;
        noiseEmitter.loudness = 0f;

        if (player.GetComponent<AINoiseAudioBridge>() == null)
        {
            player.gameObject.AddComponent<AINoiseAudioBridge>();
        }
    }
}
