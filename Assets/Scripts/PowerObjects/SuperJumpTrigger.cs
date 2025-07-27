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
        if (playerManager != null && superJumpPower != null)
        {
#if debug
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=yellow>Player entered trigger. Activating SuperJumpPower.</color>");
#endif
            playerManager.OnServerPowerObjectCollision(superJumpPower);
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=red>PlayerManager or SuperJumpPower missing on trigger enter.</color>");
        }
#endif
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return; // Only the server should handle power deactivation

        var playerManager = other.GetComponent<PlayerPowerManager>();
        if (playerManager != null)
        {
#if debug
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=yellow>Player exited trigger. Deactivating SuperJumpPower.</color>");
#endif
            playerManager.DeactivateCurrentPowerServerRpc();
        }
#if debug
        else
        {
            Debug.Log("<color=#00FFAA><b>[SuperJumpTrigger]</b></color> <color=red>PlayerManager missing on trigger exit.</color>");
        }
#endif
    }
}
