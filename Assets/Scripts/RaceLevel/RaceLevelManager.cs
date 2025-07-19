using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RaceLevelManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                GameObject player = Instantiate(playerPrefab);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.Key);
            }
        }
    }
}
