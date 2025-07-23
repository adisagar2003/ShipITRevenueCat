using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class NetworkManagerSingletonEnforcer : MonoBehaviour
{
    private void Awake()
    {
        NetworkManager currentNetworkManager = GetComponent<NetworkManager>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton != currentNetworkManager)
        {
            Debug.LogWarning("[NetworkManagerSingletonEnforcer] Duplicate NetworkManager detected, destroying this instance.");
            Destroy(gameObject);
            return;
        }

        // Set Singleton explicitly for clarity 
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("[NetworkManagerSingletonEnforcer] Setting this NetworkManager as Singleton.");
            // NetworkManager automatically sets itself as Singleton on Awake,
            // but we can ensure explicitly for safety if needed:
            // NetworkManager.Singleton = currentNetworkManager;
        }

        DontDestroyOnLoad(gameObject);
    }
}
