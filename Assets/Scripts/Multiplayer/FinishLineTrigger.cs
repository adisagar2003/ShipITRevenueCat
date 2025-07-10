using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class FinishLineTrigger : NetworkBehaviour
{
    // register winner here
    private NetworkVariable<ulong> winnerClientId = new NetworkVariable<ulong>(ulong.MaxValue);

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only server determines winner

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return; // Only consider objects with NetworkObject

        if (winnerClientId.Value != ulong.MaxValue) return; // Winner already determined

        winnerClientId.Value = netObj.OwnerClientId;
        Debug.Log($"Player with ClientId {netObj.OwnerClientId} has finished first!");

        NotifyClientsWinnerClientRpc(netObj.OwnerClientId);
    }

    [ClientRpc]
    private void NotifyClientsWinnerClientRpc(ulong winnerId)
    {
        if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            Debug.Log("<color=green>You win!</color>");
            // TODO: Switch to victory camera, disable player movement, show win UI
        }
        else
        {
            Debug.Log("<color=red>You lose!</color>");
            // TODO: Switch to lose camera, disable player movement, show lose UI
        }
    }
}
