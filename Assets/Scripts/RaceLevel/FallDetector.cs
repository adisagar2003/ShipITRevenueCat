using UnityEngine;
using Unity.Netcode;

public class FallDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!NetworkManager.Singleton.IsServer) return;
            Debug.Log($"{other.name} has fallen, triggering respawn.");
            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                Debug.Log("Checking for if respawn component is detected");
                respawn.RequestRespawn();
            }
        }
    }
}
