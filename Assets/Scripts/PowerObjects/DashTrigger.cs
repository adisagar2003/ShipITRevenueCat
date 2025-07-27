using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attach this script to a GameObject with a BoxCollider set as Trigger.
/// Assign the DashPower ScriptableObject in the Inspector.
/// When a player enters, dash power is activated. When they exit, it is deactivated.
/// </summary>
public class DashTrigger : NetworkBehaviour
{
    [SerializeField] private DashPower dashPower;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return; // server-authoritative

            var playerManager = other.GetComponent<PlayerPowerManager>();
            var networkObject = other.GetComponent<NetworkObject>();
            if (playerManager != null && dashPower != null && networkObject != null)
            {
    #if debug
                Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=yellow>Player entered trigger. Activating DashPower.</color>");
    #endif
                playerManager.OnServerPowerObjectCollision(dashPower);

                // Notify only the client that owns this player
                playerManager.ActivateDashPowerClientRpc(networkObject.OwnerClientId);
            }
    #if debug
            else
            {
                Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=red>PlayerManager, DashPower, or NetworkObject missing on trigger enter.</color>");
            }
    #endif
        }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return; // Only the server should handle power deactivation
        // Deactivate the dash power when the player exits the trigger  
        var playerManager = other.GetComponent<PlayerPowerManager>();
        if (playerManager != null)
        {
#if debug
            Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=yellow>Player exited trigger. Deactivating DashPower.</color>");
#endif
            playerManager.DeactivateCurrentPowerServerRpc();
        }
#if debug
        else
        {
            Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=red>PlayerManager missing on trigger exit.</color>");
        }
#endif
    }
}
