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
       
        if (IsServer) Debug.Log($"OnNetworkSpawn called. IsServer: {IsServer}, Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

        if (IsServer)
        {
            StartCoroutine(WaitForPlayersAndSpawn());
        }
    }

    private IEnumerator WaitForPlayersAndSpawn()
    {
        int expectedPlayers = 2;

        while (NetworkManager.Singleton.ConnectedClients.Count < expectedPlayers)
        {
            Debug.Log($"Waiting for players: {NetworkManager.Singleton.ConnectedClients.Count}/{expectedPlayers}");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("All players connected! Spawning...");
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.Key);
            // sets random position
            player.GetComponent<Rigidbody>().MovePosition(SpawnManager.Instance.GetRandomAvailableSpawnPoint().position);
        }
        waitingForPlayersUI.SetActive(false); // this would only set server's UI false, calling clientRPC at bottom.
        DisableUIClientRpc();
        OnAllPlayersReady?.Invoke();
        //EnableMovementEventClientRPC();
        //OnPlayerPossesionEvent?.Invoke();  // migrating this to a new start race script. 
    }

    [ClientRpc]
    private void DisableUIClientRpc()
    {
        waitingForPlayersUI.SetActive(false); 
    }

    //[ClientRpc]
    //private void EnableMovementEventClientRPC()
    //{
    //    OnPlayerPossesionEvent?.Invoke();
    //} 
}
