#define DEBUG
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RaceLevelManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject waitingForPlayersUI;

    public static event System.Action OnAllPlayersReady;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) Debug.Log($"OnNetworkSpawn called. IsServer: {IsServer}, Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

        if (IsServer)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    private IEnumerator WaitForPlayersAndSpawn()
    {
        int expectedPlayers = GameConstants.Networking.DEFAULT_MAX_PLAYERS;

        while (NetworkManager.Singleton.ConnectedClients.Count < expectedPlayers)
        {
            Debug.Log($"Waiting for players: {NetworkManager.Singleton.ConnectedClients.Count}/{expectedPlayers}");
            yield return new WaitForSeconds(GameConstants.Networking.PLAYER_WAIT_POLLING_INTERVAL);
        }

        Debug.Log("All players connected! Spawning...");
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.Key);

            // sets random position
        #if PRODUCTION
            player.GetComponent<Rigidbody>().MovePosition(SpawnManager.Instance.GetRandomAvailableSpawnPoint().position);
        #endif
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

    //[ClientRpc]
    //private void EnableMovementEventClientRPC()
    //{
    //    OnPlayerPossesionEvent?.Invoke();
    //} 
}

