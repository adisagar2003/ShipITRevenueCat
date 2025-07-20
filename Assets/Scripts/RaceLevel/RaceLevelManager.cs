#define DEBUG
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RaceLevelManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn called. IsServer: {IsServer}, Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

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
        }
    }
}
