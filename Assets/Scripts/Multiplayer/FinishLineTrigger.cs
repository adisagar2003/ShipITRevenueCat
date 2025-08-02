#define OnGUI
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class FinishLineTrigger : NetworkBehaviour
{
    private NetworkVariable<ulong> winnerClientId = new NetworkVariable<ulong>(ulong.MaxValue);

    private string resultString = "";

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only server determines winner

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return; // Only consider objects with NetworkObject

        if (winnerClientId.Value != ulong.MaxValue) return; // Winner already determined

        winnerClientId.Value = netObj.OwnerClientId;
        Debug.Log($"Player with ClientId {netObj.OwnerClientId} has finished first!");

        NotifyClientsWinnerRpc(netObj.OwnerClientId);
       
    }

    [Rpc(SendTo.NotServer)]
    private void NotifyClientsWinnerRpc(ulong winnerId)
    {
        if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            resultString = "<color=green>You Win!</color>";
            // TODO: Switch to victory camera, disable player movement, show win UI
        }
        else
        {
            resultString = "<color=red>You Lose!</color>";
            // TODO: Switch to lose camera, disable player movement, show lose UI
        }

        // Only the server calls ForceResetScene
        if (IsServer)
        {
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.PutPlayersBackToLobby();
            }
        }
    }

#if OnGUI
    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(resultString))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 25, 300, 50), resultString, style);
        }
    }
#endif
}
