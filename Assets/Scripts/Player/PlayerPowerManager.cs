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

    public void StartSuperJump(float jumpForce, float jumpDuration)
    {
#if debug
        Debug.Log($"<color=#00FFAA><b>[PlayerPowerManager]</b></color> <color=yellow>Starting gradual super jump for {gameObject.name}: force={jumpForce}, duration={jumpDuration}s.</color>");
#endif
        StartCoroutine(SuperJumpCoroutine(jumpForce, jumpDuration));
    }

    private IEnumerator SuperJumpCoroutine(float jumpForce, float duration)
    {
        float elapsed = 0f;
        var rb = GetComponent<Rigidbody>();
        while (elapsed < duration)
        {
            if (rb != null)
            {
                rb.AddForce(Vector3.up * (jumpForce * Time.fixedDeltaTime / duration), ForceMode.Force);
            }
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
#if debug
        Debug.Log($"<color=#00FFAA><b>[PlayerPowerManager]</b></color> <color=yellow>Super jump finished for {gameObject.name}.</color>");
#endif
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

    // Step 1: Simple method to start dash
    private void StartDash(NetworkThirdPersonController controller, float targetSpeed)
    {
        Debug.Log($"<color=green>[PlayerPowerManager]</color> Starting dash!");
        controller.SetDashState(true);
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
    }

    // Helper method to apply speed in forward direction only
    private void ApplyDashSpeed(float speed)
    {
        Vector3 dashVelocity = transform.forward * speed;
        dashVelocity.y = rb.linearVelocity.y; // Keep the same up/down movement (gravity)
        rb.linearVelocity = dashVelocity;
    }

    // Simple method to start particle effect
    private void StartDashParticles(ParticleSystem particles)
    {
        if (particles != null)
        {
            particles.Play();
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Started dash particles!");
        }
        else
        {
            Debug.Log($"<color=yellow>[PlayerPowerManager]</color> No particles assigned for dash.");
        }
    }

    // Simple method to stop particle effect
    private void StopDashParticles(ParticleSystem particles)
    {
        if (particles != null)
        {
            particles.Stop();
            Debug.Log($"<color=green>[PlayerPowerManager]</color> Stopped dash particles!");
        }
    }


[Rpc(SendTo.ClientsAndHost)]
    public void ActivateDashPowerClientRpc(ulong networkObjectOwnerClientId)
    {
        Debug.Log("Dash Power activated successfully");

    }
}
