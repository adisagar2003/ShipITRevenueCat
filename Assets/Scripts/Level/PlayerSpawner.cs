using Unity.Netcode;
using UnityEngine;

// DISABLED: This script conflicts with NetworkManager's automatic player spawning
// NetworkManager will automatically spawn the PlayerPrefab when clients connect
// The SetSpawnLocation script will handle positioning the spawned players
/*
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Transform spawnPoint = SpawnManager.Instance.GetRandomAvailableSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("No available spawn points for player!");
            return;
        }

        GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }
} 
*/ 