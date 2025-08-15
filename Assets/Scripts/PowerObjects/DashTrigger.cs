
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
            Debug.Log($"<color=#FF00FF><b>[DashTrigger]</b></color> {other.gameObject.name} <color=yellow>Player. Activating DashPower.</color>");
            if (!IsHost) return; // host-authoritative

            var playerManager = other.GetComponent<PlayerPowerManager>();
            var networkObject = other.GetComponentInParent<NetworkObject>();
            if (playerManager != null && dashPower != null && networkObject != null)
            {
    #if debug
                Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=yellow>Player entered trigger. Activating DashPower.</color>");
    #endif
                // Play activation sound at the trigger location
                AudioClip activationSound = dashPower.GetActivationSound();
                if (activationSound != null)
                {
                    AudioSource.PlayClipAtPoint(activationSound, other.transform.position);
                    Debug.Log($"<color=#FF00FF><b>[DashTrigger]</b></color> <color=cyan>Playing dash activation sound at {other.transform.position}.</color>");
                }

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
        if (!IsHost) return; // Only the host should handle power deactivation

        // Get the network controller to check if dash is active
        var networkController = other.GetComponent<NetworkThirdPersonController>();
        if (networkController != null && networkController.IsDashing())
        {
            // Force end dash if player exits trigger during dash
            networkController.SetDashState(false);

#if debug
            Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=yellow>Player exited trigger during dash - force stopping dash.</color>");
#endif
        }
#if debug
        else if (networkController == null)
        {
            Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=red>NetworkController missing on trigger exit.</color>");
        }
        else
        {
            Debug.Log("<color=#FF00FF><b>[DashTrigger]</b></color> <color=yellow>Player exited trigger (was not dashing).</color>");
        }
#endif
    }
}
