#define debug
using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

// Assumes SpecialPower.cs defines:
// - enum ActivationType { Passive, Active }
// - class SpecialPower : ScriptableObject { ActivationType activationType; void ApplyEffect(GameObject player); void OnEffectAppliedRpc(GameObject player); }

public class PlayerPowerManager : NetworkBehaviour
{
    [SerializeField] private SpecialPower[] availablePowers;
    private NetworkVariable<int> currentPowerIndex = new NetworkVariable<int>(0);
    private Rigidbody rb;
    
    [Header("Particle Spawn Settings")]
    [SerializeField] private Transform particlesSpawnLocation;
    
    [Header("Trail Settings")]
    [SerializeField] private TrailRenderer dashTrailRenderer;
    
    // Track instantiated particles so we can destroy them
    private ParticleSystem currentDashParticles = null;
    private ParticleSystem currentSuperJumpParticles = null;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>Awake called for {gameObject.name}.</color>");
#endif
    }

    private void Start()
    {
#if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>Start called for {gameObject.name}.</color>");
#endif
    }

    private void Update()
    {
        if (!IsServer) return;

        SpecialPower currentPower = GetCurrentPower();
        if (currentPower == null) return;

        if (currentPower.activationType == ActivationType.Active && Input.GetKeyDown(KeyCode.Space))
        {
#if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Activating current active power for {gameObject.name}.</color>");
#endif
            ActivateCurrentPowerRpc(OwnerClientId);
        }
    }

    private SpecialPower GetCurrentPower()
    {
        if (availablePowers == null || availablePowers.Length == 0) return null;
        int idx = Mathf.Clamp(currentPowerIndex.Value, 0, availablePowers.Length - 1);
        if (idx < 0 || idx >= availablePowers.Length) return null;
        return availablePowers[idx];
    }

    public void OnServerPowerObjectCollision(SpecialPower power)
    {
        if (!IsServer) return;
        if (power.activationType == ActivationType.Passive)
        {
        #if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Passive power collision detected. Setting and activating power for {gameObject.name}.</color>");
        #endif
            SetCurrentPower(power);
            ActivateCurrentPowerRpc(OwnerClientId);
        }
    }

    // Sets the current power and syncs index
    private void SetCurrentPower(SpecialPower power)
    {
        int idx = System.Array.IndexOf(availablePowers, power);
        if (idx >= 0)
        {
        #if debug
            Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=yellow>Setting current power index to {idx} for {gameObject.name}.</color>");
        #endif
            currentPowerIndex.Value = idx;
        }
        #if debug
            else
            {
                Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=red>Power not found in availablePowers for {gameObject.name}.</color>");
            }
        #endif
    }

    private void ActivateCurrentPowerRpc(ulong playerClientId)
    {
        if (!IsServer) return;

        SpecialPower currentPower = GetCurrentPower();
        if (currentPower == null) return;

    #if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=green>Applying effect for current power on {gameObject.name}.</color>");
    #endif
        currentPower.ApplyEffect(gameObject);
        currentPower.OnEffectAppliedRpc(gameObject);
    }

    /// <summary>
    /// ServerRpc to deactivate the current power.
    /// Resets the currentPowerIndex so no power is active.
    /// </summary>


    public void ActivateDashPowerRpc(ulong targetClientId)
    {
        // Only run on the client that owns this player
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

    #if debug
        Debug.Log($"<color=#00FF00><b>[PlayerPowerManager]</b></color> <color=cyan>DashPower activated on client {targetClientId} for {gameObject.name}.</color>");
    #endif
        SpecialPower currentPower = GetCurrentPower();

        currentPower.ApplyEffect(gameObject);
    }

    // Simple method to start a smooth super jump (3 configurable parameters + optional particles)
    public void StartSmoothSuperJump(float jumpForce, float jumpDuration, float airControl = 0.5f, ParticleSystem particles = null)
    {
#if debug
        Debug.Log($"<color=#00FFAA><b>[PlayerPowerManager]</b></color> <color=yellow>Starting smooth super jump for {gameObject.name}.</color>");
#endif
        StartCoroutine(SuperJumpSequence(jumpForce, jumpDuration, airControl, particles));
    }

    // Main super jump sequence - broken into simple steps
    private IEnumerator SuperJumpSequence(float jumpForce, float duration, float airControl, ParticleSystem particles = null)
    {
        var networkController = GetComponent<NetworkThirdPersonController>();
        if (networkController == null) yield break;

        // Step 1: Start the super jump
        StartSuperJumpHelper(networkController, jumpForce);
        StartSuperJumpParticles(particles);

        // Step 2: Apply upward force over time (server authoritative)
        yield return StartCoroutine(ApplySuperJumpForce(jumpForce, duration));

        // Step 3: End the super jump
        EndSuperJump(networkController);
        StopSuperJumpParticles(particles);
    }

    // Step 1: Simple method to start super jump
    private void StartSuperJumpHelper(NetworkThirdPersonController controller, float jumpForce)
    {
        Debug.Log($"<color=purple>[PlayerPowerManager]</color> Starting super jump!");
        controller.SetSuperJumpState(true);
    }

    // Step 2: Apply upward force over time (server authoritative)
    private IEnumerator ApplySuperJumpForce(float jumpForce, float duration)
    {
        Debug.Log($"<color=purple>[PlayerPowerManager]</color> Applying super jump force over {duration}s!");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration; // Goes from 0 to 1
            // Start strong, get weaker over time (but not too weak)
            float currentForce = Mathf.Lerp(jumpForce, jumpForce * 0.5f, progress);

            ApplySuperJumpUpwardForce(currentForce);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Step 3: Simple method to end super jump
    private void EndSuperJump(NetworkThirdPersonController controller)
    {
        Debug.Log($"<color=purple>[PlayerPowerManager]</color> Ending super jump!");
        controller.SetSuperJumpState(false);
    }

    // Helper method to apply upward force (server authoritative)
    private void ApplySuperJumpUpwardForce(float force)
    {
        if (!IsServer) return; // Server authoritative - only server applies physics forces

        // Apply force directly without Time.fixedDeltaTime scaling - that was making it too weak!
        Vector3 upwardForce = Vector3.up * force;
        rb.AddForce(upwardForce, ForceMode.Force);

#if debug
        Debug.Log($"<color=purple>[PlayerPowerManager]</color> Applied upward force: {upwardForce.magnitude}");
#endif
    }

    // Simple method to start particle effect by instantiating it
    private void StartSuperJumpParticles(ParticleSystem particlesPrefab)
    {
        if (particlesPrefab != null && particlesSpawnLocation != null)
        {
            // Instantiate the particle system as a child of the spawn location
            currentSuperJumpParticles = Instantiate(particlesPrefab, particlesSpawnLocation.position, particlesSpawnLocation.rotation, particlesSpawnLocation);
            currentSuperJumpParticles.Play();
            Debug.Log($"<color=purple>[PlayerPowerManager]</color> Instantiated and started super jump particles!");
        }
        else
        {
            Debug.Log($"<color=yellow>[PlayerPowerManager]</color> No particles prefab or spawn location assigned for super jump.");
        }
    }

    // Simple method to stop particle effect and destroy the instance
    private void StopSuperJumpParticles(ParticleSystem particlesPrefab)
    {
        if (currentSuperJumpParticles != null)
        {
            currentSuperJumpParticles.Stop();
            // Destroy after the particle system finishes playing
            Destroy(currentSuperJumpParticles.gameObject, currentSuperJumpParticles.main.duration + currentSuperJumpParticles.main.startLifetime.constantMax);
            Debug.Log($"<color=purple>[PlayerPowerManager]</color> Stopped and scheduled destruction of super jump particles!");
            currentSuperJumpParticles = null;
        }
    }

    // Backward compatibility method for existing SuperJumpPower usage
    public void StartSuperJump(float jumpForce, float jumpDuration)
    {
        StartSmoothSuperJump(jumpForce, jumpDuration, 0.5f, null);
    }

    // Simple method to start a smooth dash (4 configurable parameters + optional particles)
    public void StartSmoothDash(float dashSpeed, float accelerationTime, float dashDuration, float decelerationTime, ParticleSystem particles = null)
    {
#if debug
        Debug.Log($"<color=#00FFAA><b>[PlayerPowerManager]</b></color> <color=yellow>Starting smooth dash for {gameObject.name}.</color>");
#endif
        StartCoroutine(DashSequence(dashSpeed, accelerationTime, dashDuration, decelerationTime, particles));
    }

    // Main dash sequence - broken into simple, easy-to-understand steps
    private IEnumerator DashSequence(float targetSpeed, float accelTime, float dashTime, float decelTime, ParticleSystem particles = null)
    {
        var networkController = GetComponent<NetworkThirdPersonController>();
        if (networkController == null) yield break;

        // Step 1: Start the dash
        StartDash(networkController, targetSpeed);
        StartDashParticles(particles);

        // Step 2: Speed up smoothly
        yield return StartCoroutine(AccelerateDash(targetSpeed, accelTime));

        // Step 3: Keep dash speed
        yield return StartCoroutine(MaintainDash(targetSpeed, dashTime - accelTime));

        // Step 4: Slow down smoothly
        yield return StartCoroutine(DecelerateDash(targetSpeed, decelTime));

        // Step 5: End the dash
        EndDash(networkController);
        StopDashParticles(particles);
    }

    private static float particleYOffset = .3f;
    // Step 1: Simple method to start dash
    private void StartDash(NetworkThirdPersonController controller, float targetSpeed)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Starting dash!");
        controller.SetDashState(true);
        StartDashTrail();
    }

    // Step 2: Gradually increase speed (smooth acceleration)
    private IEnumerator AccelerateDash(float targetSpeed, float accelTime)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Accelerating to {targetSpeed}!");

        float startSpeed = rb.linearVelocity.magnitude;
        float elapsed = 0f;

        while (elapsed < accelTime)
        {
            float progress = elapsed / accelTime; // Goes from 0 to 1
            float currentSpeed = Mathf.Lerp(startSpeed, targetSpeed, progress);

            ApplyDashSpeed(currentSpeed);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Step 3: Keep the same speed (maintain phase)
    private IEnumerator MaintainDash(float targetSpeed, float maintainTime)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Maintaining speed {targetSpeed}!");

        float elapsed = 0f;
        while (elapsed < maintainTime)
        {
            ApplyDashSpeed(targetSpeed);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Step 4: Gradually decrease speed (smooth deceleration)
    private IEnumerator DecelerateDash(float startSpeed, float decelTime)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Decelerating from {startSpeed}!");

        float endSpeed = 5f; // Back to normal movement speed
        float elapsed = 0f;

        while (elapsed < decelTime)
        {
            float progress = elapsed / decelTime; // Goes from 0 to 1
            float currentSpeed = Mathf.Lerp(startSpeed, endSpeed, progress);

            ApplyDashSpeed(currentSpeed);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    // Step 5: Simple method to end dash
    private void EndDash(NetworkThirdPersonController controller)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Ending dash!");
        controller.SetDashState(false);
        StopDashTrail();
    }

    // Helper method to apply speed in forward direction only
    private void ApplyDashSpeed(float speed)
    {
        Vector3 dashVelocity = transform.forward * speed;
        dashVelocity.y = rb.linearVelocity.y; // Keep the same up/down movement (gravity)
        rb.linearVelocity = dashVelocity;
    }

    // Simple method to start particle effect by instantiating it
    private void StartDashParticles(ParticleSystem particlesPrefab)
    {
        if (particlesPrefab != null && particlesSpawnLocation != null)
        {
            // Instantiate the particle system as a child of the spawn location
            currentDashParticles = Instantiate(particlesPrefab, particlesSpawnLocation.position, particlesSpawnLocation.rotation, particlesSpawnLocation);
            currentDashParticles.Play();
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Instantiated and started dash particles!");
        }
        else
        {
            Debug.Log($"<color=yellow>[PlayerPowerManager]</color> No particles prefab or spawn location assigned for dash.");
        }
    }

    // Simple method to stop particle effect and destroy the instance
    private void StopDashParticles(ParticleSystem particlesPrefab)
    {
        if (currentDashParticles != null)
        {
            currentDashParticles.Stop();
            // Destroy after the particle system finishes playing
            Destroy(currentDashParticles.gameObject, currentDashParticles.main.duration + currentDashParticles.main.startLifetime.constantMax);
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Stopped and scheduled destruction of dash particles!");
            currentDashParticles = null;
        }
    }

    // Simple method to start the dash trail effect
    private void StartDashTrail()
    {
        if (dashTrailRenderer != null)
        {
            dashTrailRenderer.enabled = true;
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Started dash trail effect!");
        }
        else
        {
            Debug.Log($"<color=yellow>[PlayerPowerManager]</color> No trail renderer assigned for dash.");
        }
    }

    // Simple method to stop the dash trail effect
    private void StopDashTrail()
    {
        if (dashTrailRenderer != null)
        {
            dashTrailRenderer.enabled = false;
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Stopped dash trail effect!");
        }
    }


[Rpc(SendTo.ClientsAndHost)]
    public void ActivateDashPowerClientRpc(ulong networkObjectOwnerClientId)
    {
        Debug.Log("Dash Power activated successfully");

    }
}
