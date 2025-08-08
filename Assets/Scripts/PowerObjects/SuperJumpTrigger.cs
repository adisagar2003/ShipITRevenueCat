#define debug

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attach this script to a GameObject with a BoxCollider set as Trigger.
/// Assign the SuperJumpPower ScriptableObject in the Inspector.
/// When a player enters, super jump power is activated. When they exit, it is deactivated.
/// </summary>
public class SuperJumpTrigger : NetworkBehaviour
{
    [SerializeField] private SuperJumpPower superJumpPower;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // server-authoritative

        var playerManager = other.GetComponent<PlayerPowerManager>();
        var networkObject = other.GetComponentInParent<NetworkObject>();
        if (playerManager != null && superJumpPower != null && networkObject != null)
        {
#if debug
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=yellow>Player entered trigger. Activating SuperJumpPower.</color>");
#endif
            playerManager.OnServerPowerObjectCollision(superJumpPower);
            
            // Notify only the client that owns this player (similar to dash trigger)
            // Note: Super jump is server-authoritative, so no client RPC needed for physics
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=red>PlayerManager, SuperJumpPower, or NetworkObject missing on trigger enter.</color>");
        }
#endif
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return; // Only the server should handle power deactivation
        
        // Get the network controller to check if super jump is active
        var networkController = other.GetComponent<NetworkThirdPersonController>();
        if (networkController != null && networkController.IsSuperJumping())
        {
            // Force end super jump if player exits trigger during super jump
            networkController.SetSuperJumpState(false);
            
#if debug
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=yellow>Player exited trigger during super jump - force stopping super jump.</color>");
#endif
        }
#if debug
        else if (networkController == null)
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=red>NetworkController missing on trigger exit.</color>");
        }
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=yellow>Player exited trigger (was not super jumping).</color>");
        }
#endif
    }
}
