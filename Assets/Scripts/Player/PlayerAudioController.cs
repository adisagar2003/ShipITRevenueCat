using UnityEngine;
using Unity.Netcode;

/// <summary>
/// PlayerAudioController handles player-specific audio events with 3D spatial audio.
/// Modified to use local AudioSources for spatial positioning instead of global AudioManager.
/// Attach this to your PlayerMultiplayer prefab along with PlayerAudioIntegration.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAudioController : NetworkBehaviour
{
    [Header("Audio Clip Settings")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] landingClips;

    [Header("Spatial Audio Settings")]
    [SerializeField] private float maxAudioDistance = 15f;
    [SerializeField] private float minAudioDistance = 1f;
    [SerializeField] private AnimationCurve audioFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolumeMultiplier = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpVolumeMultiplier = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float landingVolumeMultiplier = 0.8f;

    [Header("Footstep Settings")]
    [SerializeField] private float footstepCooldown = 0.3f;
    [SerializeField] private float minWalkSpeed = 0.1f;

    [Header("Component References")]
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private PlayerMovement playerMovement;

    // 3D Audio Sources for spatial audio (NEW)
    private AudioSource footstepAudioSource;
    private AudioSource jumpAudioSource;
    private AudioSource landingAudioSource;

    // Internal state
    private float lastFootstepTime;
    private bool wasGrounded;
    private bool isMoving;
    private Vector3 lastPosition;
    private float currentSpeed;

    private void Awake()
    {
        InitializeComponents();
        CreateSpatialAudioSources(); // NEW
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!IsOwner && !IsServer) return;

        UpdateMovementState();
        UpdateGroundState();
        HandleFootsteps();
        UpdateAudioSourceVolumes();
    }

    private void InitializeComponents()
    {
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Create spatial audio sources for footsteps, jump, and landing sounds.
    /// Necessary for 3D spatial audio effects.
    /// </summary>
    private void CreateSpatialAudioSources()
    {
        GameObject footstepObj = new GameObject("FootstepAudioSource");
        footstepObj.transform.SetParent(transform);
        footstepObj.transform.localPosition = Vector3.zero;
        footstepAudioSource = footstepObj.AddComponent<AudioSource>();
        ConfigureSpatialAudioSource(footstepAudioSource);

        GameObject jumpObj = new GameObject("JumpAudioSource");
        jumpObj.transform.SetParent(transform);
        jumpObj.transform.localPosition = Vector3.zero;
        jumpAudioSource = jumpObj.AddComponent<AudioSource>();
        ConfigureSpatialAudioSource(jumpAudioSource);

        GameObject landingObj = new GameObject("LandingAudioSource");
        landingObj.transform.SetParent(transform);
        landingObj.transform.localPosition = Vector3.zero;
        landingAudioSource = landingObj.AddComponent<AudioSource>();
        ConfigureSpatialAudioSource(landingAudioSource);
    }


    private void ConfigureSpatialAudioSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = 1f; // FULLY 3D
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        audioSource.minDistance = minAudioDistance;
        audioSource.maxDistance = maxAudioDistance;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioFalloffCurve);

        audioSource.dopplerLevel = 0.3f;
        audioSource.spread = 45f;
    }

    /// <summary>
    /// nfigure the volumes of the audio sources based on AudioManager settings.
    /// </summary>
    private void UpdateAudioSourceVolumes()
    {
        if (AudioManager.Instance != null)
        {
            float masterVolume = AudioManager.Instance.GetMasterVolmue();
            float sfxVolume = AudioManager.Instance.sfxVolume;

            if (footstepAudioSource != null)
                footstepAudioSource.volume = masterVolume * sfxVolume * footstepVolumeMultiplier;

            if (jumpAudioSource != null)
                jumpAudioSource.volume = masterVolume * sfxVolume * jumpVolumeMultiplier;

            if (landingAudioSource != null)
                landingAudioSource.volume = masterVolume * sfxVolume * landingVolumeMultiplier;
        }
    }

    /// <summary>
    /// Chore: Same implementation in PlayerAnimationController, unify logic later.
    /// </summary>
    private void UpdateMovementState()
    {
        Vector3 currentPosition = transform.position;
        Vector3 velocity = (currentPosition - lastPosition) / Time.deltaTime;
        currentSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;

        isMoving = currentSpeed > minWalkSpeed;
        lastPosition = currentPosition;
    }

    /// <summary>
    /// Coupled with PlayerMovement's ground check.
    /// </summary>
    private void UpdateGroundState()
    {
        bool previousGrounded = wasGrounded;

        // Use PlayerMovement's ground check instead of duplicating logic
        bool isGrounded = playerMovement.isGrounded;

        // Detect jump
        if (wasGrounded && !isGrounded)
        {
            PlayJumpSound();
        }

        // Detect landing
        if (!previousGrounded && isGrounded)
        {
            PlayLandingSound();
        }

        wasGrounded = isGrounded;
    }

    private void HandleFootsteps()
    {
        if (playerMovement.isGrounded && isMoving && Time.time > lastFootstepTime + footstepCooldown)
        {
            PlayFootstepSound();
            lastFootstepTime = Time.time; // Reset cooldown timer
        }
    }

    #region Public Methods

    /// <summary>
    /// Call this method when the player jumps
    /// </summary>
    public void PlayJumpSound()
    {
        if (jumpClips != null && jumpClips.Length > 0 && jumpAudioSource != null)
        {
            AudioClip clipToPlay = jumpClips[Random.Range(0, jumpClips.Length)];
            jumpAudioSource.PlayOneShot(clipToPlay);

            if (IsOwner)
            {
                int clipIndex = System.Array.IndexOf(jumpClips, clipToPlay);
                PlayJumpSoundRpc(clipIndex);
            }
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>
    /// Force play a footstep sound (useful for animation events)
    /// </summary>
    public void PlayFootstepSound()
    {
        if (footstepClips != null && footstepClips.Length > 0 && footstepAudioSource != null)
        {
            AudioClip clipToPlay = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepAudioSource.PlayOneShot(clipToPlay);

            if (IsOwner)
            {
                int clipIndex = System.Array.IndexOf(footstepClips, clipToPlay);
                PlayFootstepSoundRpc(clipIndex);
            }
        }
    }

    /// <summary>
    /// Play landing sound when hitting the ground
    /// </summary>
    public void PlayLandingSound()
    {
        // MODIFIED: Use local 3D AudioSource instead of global AudioManager
        if (landingClips != null && landingClips.Length > 0 && landingAudioSource != null)
        {
            AudioClip clipToPlay = landingClips[Random.Range(0, landingClips.Length)];
            landingAudioSource.PlayOneShot(clipToPlay);

            // Network the sound for multiplayer
            if (IsOwner)
            {
                int clipIndex = System.Array.IndexOf(landingClips, clipToPlay);
                PlayLandingSoundRpc(clipIndex);
            }
        }
    }

    /// <summary>
    /// Enable or disable automatic footsteps
    /// </summary>
    /// <param name="enabled">Whether automatic footsteps should play</param>
    public void SetAutomaticFootsteps(bool enabled)
    {
        this.enabled = enabled;
    }

    #endregion

    #region Network RPCs (Netcode 1.8.1 Syntax) - MODIFIED for clip synchronization
    // !isOwner is used to not play sounds twice on the owner client.

    [Rpc(SendTo.Server)]
    private void PlayJumpSoundRpc(int clipIndex)
    {
        PlayJumpSoundRpc(clipIndex);
    }

    [Rpc(SendTo.NotServer)]
    private void PlayJumpSoundRpc(int clipIndex)
    {
        if (!IsOwner && jumpClips != null && clipIndex >= 0 && clipIndex < jumpClips.Length && jumpAudioSource != null)
        {
            AudioClip clipToPlay = jumpClips[clipIndex];
            jumpAudioSource.PlayOneShot(clipToPlay);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayFootstepSoundRpc(int clipIndex)
    {
        PlayFootstepSoundRpc(clipIndex);
    }

    [Rpc(SendTo.NotServer)]
    private void PlayFootstepSoundRpc(int clipIndex)
    {
        if (!IsOwner && footstepClips != null && clipIndex >= 0 && clipIndex < footstepClips.Length && footstepAudioSource != null)
        {
            AudioClip clipToPlay = footstepClips[clipIndex];
            footstepAudioSource.PlayOneShot(clipToPlay);
        }
    }

    [Rpc(SendTo.Server)]
    private void PlayLandingSoundRpc(int clipIndex)
    {
        PlayLandingSoundRpc(clipIndex);
    }

    [Rpc(SendTo.NotServer)]
    private void PlayLandingSoundRpc(int clipIndex)
    {
        if (!IsOwner && landingClips != null && clipIndex >= 0 && clipIndex < landingClips.Length && landingAudioSource != null)
        {
            AudioClip clipToPlay = landingClips[clipIndex];
            landingAudioSource.PlayOneShot(clipToPlay);
        }
    }

    #endregion

}
