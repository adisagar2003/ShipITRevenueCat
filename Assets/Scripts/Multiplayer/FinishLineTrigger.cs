#define debug
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class FinishLineTrigger : NetworkBehaviour
{
    private NetworkVariable<ulong> winnerClientId = new NetworkVariable<ulong>(ulong.MaxValue);

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only server determines winner

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return; // Only consider objects with NetworkObject

        if (winnerClientId.Value != ulong.MaxValue) return; // Winner already determined

        winnerClientId.Value = netObj.OwnerClientId;
        
#if debug
        Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>Player with ClientId {netObj.OwnerClientId} has finished first!</color>");
#endif

        NotifyClientsWinnerRpc(netObj.OwnerClientId);
    }

    [Rpc(SendTo.NotServer)]
    private void NotifyClientsWinnerRpc(ulong winnerId)
    {
        if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
#if debug
            Debug.Log($"<color=#00FF00><b>[FinishLineTrigger]</b></color> <color=green><size=20>🏆 YOU WIN! 🏆</size></color>");
#endif
            // TODO: Switch to victory camera, disable player movement, show win UI
        }
        else
        {
#if debug
            Debug.Log($"<color=#FF0000><b>[FinishLineTrigger]</b></color> <color=red><size=18>💔 You Lose! 💔</size></color>");
#endif
            // TODO: Switch to lose camera, disable player movement, show lose UI
        }

        // Only the server calls ForceResetScene
        if (IsServer)
        {
#if debug
            Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=orange>Server initiating return to lobby...</color>");
#endif
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.PutPlayersBackToLobby();
            }
#if debug
            else
            {
                Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=red>GameManager not found!</color>");
            }
#endif
        }
    }
}
