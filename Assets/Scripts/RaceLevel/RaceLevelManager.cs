using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RaceLevelManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject waitingForPlayersUI;

    [Header("Player Waiting Settings")]
    [SerializeField] private float maxWaitTime = 60f; // Maximum time to wait for players
    [SerializeField] private int minPlayersToStart = 1; // Allow starting with fewer players

    public static event System.Action OnAllPlayersReady;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
#if debug
        if (IsServer) Debug.Log($"OnNetworkSpawn called. IsServer: {IsServer}, Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");
#endif

        if (IsServer)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    private IEnumerator WaitForPlayersAndSpawn()
    {
        int expectedPlayers = GameConstants.Networking.DEFAULT_MAX_PLAYERS;
        float waitStartTime = Time.time;

        while (NetworkManager.Singleton.ConnectedClients.Count < expectedPlayers)
        {
            float elapsedTime = Time.time - waitStartTime;

            // Timeout check to prevent infinite loop
            if (elapsedTime >= maxWaitTime)
            {
#if debug
                Debug.LogWarning($"Player wait timeout reached ({maxWaitTime}s). Starting with {NetworkManager.Singleton.ConnectedClients.Count} players instead of {expectedPlayers}");
#endif
                break;
            }

            // Also allow starting if we have minimum players and waited reasonable time
            if (NetworkManager.Singleton.ConnectedClients.Count >= minPlayersToStart && elapsedTime >= 10f)
            {
#if debug
                Debug.Log($"Starting with {NetworkManager.Singleton.ConnectedClients.Count} players (minimum reached)");
#endif
                break;
            }

#if debug
            Debug.Log($"Waiting for players: {NetworkManager.Singleton.ConnectedClients.Count}/{expectedPlayers} (timeout in {maxWaitTime - elapsedTime:F1}s)");
#endif
            yield return new WaitForSeconds(GameConstants.Networking.PLAYER_WAIT_POLLING_INTERVAL);
        }

#if debug
        Debug.Log($"Starting game with {NetworkManager.Singleton.ConnectedClients.Count} players! Spawning...");
#endif
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            GameObject player = Instantiate(playerPrefab);

            // Set position BEFORE spawning for proper network sync
        #if PRODUCTION
            if (SpawnManager.Instance != null)
            {
                player.transform.position = SpawnManager.Instance.GetRandomAvailableSpawnPoint().position;
            }
        #endif

            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.Key);
        }
        StartGame();
        //EnableMovementEventClientRPC();
        //OnPlayerPossesionEvent?.Invoke();  // migrating this to a new start race script.
    }

    [ContextMenu("Start Game")]
    private void StartGame()
    {
        waitingForPlayersUI.SetActive(false); // this would only set server's UI false, calling clientRPC at bottom.
        DisableUIRpc();
        OnAllPlayersReady?.Invoke();
    }

    [Rpc(SendTo.NotServer)]
    private void DisableUIRpc()
    {
        waitingForPlayersUI.SetActive(false);
    }

    //[Rpc(SendTo.NotServer)]
    //private void EnableMovementEventClientRPC()
    //{
    //    OnPlayerPossesionEvent?.Invoke();
    //}
}

