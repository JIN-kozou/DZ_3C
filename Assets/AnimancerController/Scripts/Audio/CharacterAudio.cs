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
    [SerializeField] private bool startBreathingOnEnable = true;
    [SerializeField, Min(0f)] private float intenseBreathingDelaySeconds = 2f;

    [Header("Auto Player Movement")]
    [SerializeField] private bool autoDetectPlayerMovement = true;
    [SerializeField, Min(0.05f)] private float walkFootstepInterval = 0.46f;
    [SerializeField, Min(0.05f)] private float runFootstepInterval = 0.32f;
    [SerializeField, Min(0f)] private float groundedMoveSpeedThreshold = 0.05f;

    [Header("State")]
    [SerializeField] private GameObject death;
    [SerializeField] private GameObject respawn;

    [Header("Core")]
    [SerializeField] private GameObject corePickup;
    [SerializeField] private GameObject coreInstall;
    [SerializeField] private GameObject coreCharge;
    [SerializeField] private GameObject coreConsume;

    [Header("AI Noise")]
    [SerializeField] private AINoiseAudioBridge aiNoiseBridge;
    [SerializeField, Min(0f)] private float footstepNoiseLoudness = 0.5f;
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
    private float intenseBreathingTimer;

    private void Awake()
    {
        player = GetComponent<Player>() ?? GetComponentInParent<Player>();
    }

    private void OnEnable()
    {
        CaptureGroundedSnapshot();
        if (startBreathingOnEnable)
        {
            StartBreathing();
        }
    }

    private void Update()
    {
        TickBreathingFollow();
        TickAutoPlayerMovementAudio();
        TickAutoBreathingIntensity();
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
        AudioPrefabPlayer.Play(footstep, transform.position);
        EmitAINoise(footstepNoiseLoudness, footstepNoiseDuration);
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
        StartBreathing();
        EmitAINoise(respawnNoiseLoudness, respawnNoiseDuration);
    }

    public void OnCorePickup() => PlayAtSelf(corePickup);
    public void OnCoreInstall() => PlayAtSelf(coreInstall);
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

    private void TickAutoBreathingIntensity()
    {
        if (player == null)
        {
            return;
        }

        bool armed = player.ReusableData != null && player.ReusableData.armedModeActive;
        bool notMoving = player.InputService == null || player.InputService.MoveDiscrete == Vector2.zero;
        intenseBreathingTimer = armed || notMoving ? intenseBreathingTimer + Time.deltaTime : 0f;
        SetBreathingIntensity(intenseBreathingTimer >= intenseBreathingDelaySeconds ? 1f : 0f);
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
        return running ? runFootstepInterval : walkFootstepInterval;
    }

    private GameObject GetRandomFootstep()
    {
        if (footsteps == null || footsteps.Length == 0)
        {
            return null;
        }

        return footsteps[Random.Range(0, footsteps.Length)];
    }

    private void PlayAtSelf(GameObject soundPrefab)
    {
        AudioPrefabPlayer.Play(soundPrefab, transform.position);
    }

    private void EmitAINoise(float loudness, float duration)
    {
        if (aiNoiseBridge != null)
        {
            aiNoiseBridge.EmitNoise(loudness, duration);
        }
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
        }
    }
}
