#define debug
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class FinishLineTrigger : NetworkBehaviour
{
    // Track which clients have already finished to prevent duplicate entries
    private HashSet<ulong> finishedClients = new HashSet<ulong>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only server determines finish order

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return; // Only consider objects with NetworkObject

        ulong clientId = netObj.OwnerClientId;

        // Check if this player has already finished
        if (finishedClients.Contains(clientId)) return;

        // Mark this client as finished
        finishedClients.Add(clientId);

        // Get player name - try multiple methods
        string playerName = GetPlayerName(netObj);

        // Record the finish with RaceResultsManager
        if (RaceResultsManager.Instance != null)
        {
            RaceResultsManager.Instance.RecordPlayerFinish(clientId, playerName);
            
#if debug
            string finishPosition = finishedClients.Count == 1 ? "FIRST" : "SECOND";
            Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>Player {playerName} (ClientId {clientId}) finished {finishPosition}!</color>");
#endif

            // Notify all clients about this finish
            NotifyClientsPlayerFinishedRpc(clientId, playerName, finishedClients.Count);
        }
        else
        {
#if debug
            Debug.LogError($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=red>RaceResultsManager.Instance is null!</color>");
#endif
        }
    }

    /// <summary>
    /// Notifies all clients when a player finishes the race
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyClientsPlayerFinishedRpc(ulong clientId, string playerName, int finishPosition)
    {
        bool isLocalPlayer = NetworkManager.Singleton.LocalClientId == clientId;
        bool isWinner = finishPosition == 1;

        if (isLocalPlayer)
        {
            if (isWinner)
            {
#if debug
                Debug.Log($"<color=#00FF00><b>[FinishLineTrigger]</b></color> <color=green><size=20>🏆 YOU WIN! 🏆</size></color>");
#endif
                // TODO: Switch to victory camera, disable player movement, show win UI
            }
            else
            {
#if debug
                Debug.Log($"<color=#FFA500><b>[FinishLineTrigger]</b></color> <color=orange><size=18>🏃 You finished {GetOrdinalNumber(finishPosition)}! 🏃</size></color>");
#endif
                // TODO: Switch to finish camera, disable player movement, show finish UI
            }
        }
        else
        {
            if (isWinner)
            {
#if debug
                Debug.Log($"<color=#FF0000><b>[FinishLineTrigger]</b></color> <color=red><size=18>💔 {playerName} Won! 💔</size></color>");
#endif
                // TODO: Switch to lose camera, disable player movement, show lose UI
            }
            else
            {
#if debug
                Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>{playerName} finished {GetOrdinalNumber(finishPosition)}!</color>");
#endif
            }
        }
    }

    /// <summary>
    /// Attempts to get the player's name from various possible components
    /// </summary>
    private string GetPlayerName(NetworkObject networkObject)
    {
        if (networkObject == null) return "Unknown Player";

        // Try to get name from PlayerPowerManager (which might have color info in future)
        var powerManager = networkObject.GetComponent<PlayerPowerManager>();
        if (powerManager != null)
        {
            // For now, use client ID as name. In future, could integrate with color system
            return $"Player{networkObject.OwnerClientId}";
        }

        // Try getting name from GameObject name (fallback)
        if (!string.IsNullOrEmpty(networkObject.gameObject.name))
        {
            string objName = networkObject.gameObject.name;
            // Clean up Unity's clone naming
            objName = objName.Replace("(Clone)", "").Trim();
            if (objName != "Player" && objName != "PlayerObject")
            {
                return objName;
            }
        }

        // Fallback to client ID
        return $"Player{networkObject.OwnerClientId}";
    }

    /// <summary>
    /// Converts number to ordinal (1st, 2nd, 3rd, etc.)
    /// </summary>
    private string GetOrdinalNumber(int number)
    {
        switch (number)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return $"{number}th";
        }
    }

    /// <summary>
    /// Reset the finished clients when race restarts
    /// </summary>
    public void ResetRace()
    {
        if (IsServer)
        {
            finishedClients.Clear();
#if debug
            Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=cyan>Race reset - cleared finished clients</color>");
#endif
        }
    }
}
