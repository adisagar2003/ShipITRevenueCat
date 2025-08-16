#define debug
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class FinishLineTrigger : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas finishUICanvas;
    [SerializeField] private GameObject winUIPanel;
    [SerializeField] private GameObject loseUIPanel;
    [SerializeField] private GameObject finishUIPanel;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI loseText;
    [SerializeField] private TextMeshProUGUI finishText;
    
    [Header("UI Settings")]
    [SerializeField] private float uiDisplayDuration = 3f;
    [SerializeField] private float sceneTransitionDelay = 4f;
    
    // Track which clients have already finished to prevent duplicate entries
    private HashSet<ulong> finishedClients = new HashSet<ulong>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsHost) return; // Only host determines finish order

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>(); // parent third person component has NetworkObject.
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
                // Disable player movement and show win UI
                DisablePlayerMovement();
                ShowWinUI();
            }
            else
            {
#if debug
                Debug.Log($"<color=#FFA500><b>[FinishLineTrigger]</b></color> <color=orange><size=18>🏃 You finished {GetOrdinalNumber(finishPosition)}! 🏃</size></color>");
#endif
                // Disable player movement and show finish UI
                DisablePlayerMovement();
                ShowFinishUI(finishPosition);
            }
        }
        else
        {
            if (isWinner)
            {
#if debug
                Debug.Log($"<color=#FF0000><b>[FinishLineTrigger]</b></color> <color=red><size=18>💔 {playerName} Won! 💔</size></color>");
#endif
                // Disable player movement and show lose UI
                DisablePlayerMovement();
                ShowLoseUI(playerName);
            }
            else
            {
#if debug
                Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>{playerName} finished {GetOrdinalNumber(finishPosition)}!</color>");
#endif
            }
        }

        // Start scene transition coroutine
        StartCoroutine(TransitionToLeaderboardAfterDelay());
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
        if (IsHost)
        {
            finishedClients.Clear();
#if debug
            Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=cyan>Race reset - cleared finished clients</color>");
#endif
        }
    }
    
    /// <summary>
    /// Disables local player movement by finding and calling DisableMovement on NetworkThirdPersonController
    /// </summary>
    private void DisablePlayerMovement()
    {
        // Find local player's NetworkThirdPersonController
        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
        {
            var controller = localPlayer.GetComponentInChildren<NetworkThirdPersonController>();
            if (controller != null)
            {
                controller.DisableMovement();
#if debug
                Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=cyan>Disabled local player movement</color>");
#endif
            }
            else
            {
#if debug
                Debug.LogWarning($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>NetworkThirdPersonController not found on local player</color>");
#endif
            }
        }
        else
        {
#if debug
            Debug.LogWarning($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>Local player object not found</color>");
#endif
        }
    }
    
    /// <summary>
    /// Shows the win UI with celebration text
    /// </summary>
    private void ShowWinUI()
    {
        if (finishUICanvas != null) finishUICanvas.gameObject.SetActive(true);
        
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(true);
            if (winText != null)
            {
                winText.text = "🏆 YOU WIN! 🏆";
            }
        }
        
        // Hide UI after duration
        StartCoroutine(HideUIAfterDelay(winUIPanel));
        
#if debug
        Debug.Log($"<color=#00FF00><b>[FinishLineTrigger]</b></color> <color=green>Showing win UI</color>");
#endif
    }
    
    /// <summary>
    /// Shows the finish UI for non-winner completion
    /// </summary>
    private void ShowFinishUI(int finishPosition)
    {
        if (finishUICanvas != null) finishUICanvas.gameObject.SetActive(true);
        
        if (finishUIPanel != null)
        {
            finishUIPanel.SetActive(true);
            if (finishText != null)
            {
                finishText.text = $"🏃 You finished {GetOrdinalNumber(finishPosition)}! 🏃";
            }
        }
        
        // Hide UI after duration
        StartCoroutine(HideUIAfterDelay(finishUIPanel));
        
#if debug
        Debug.Log($"<color=#FFA500><b>[FinishLineTrigger]</b></color> <color=orange>Showing finish UI for position {finishPosition}</color>");
#endif
    }
    
    /// <summary>
    /// Shows the lose UI when another player wins
    /// </summary>
    private void ShowLoseUI(string winnerName)
    {
        if (finishUICanvas != null) finishUICanvas.gameObject.SetActive(true);
        
        if (loseUIPanel != null)
        {
            loseUIPanel.SetActive(true);
            if (loseText != null)
            {
                loseText.text = $"💔 {winnerName} Won! 💔";
            }
        }
        
        // Hide UI after duration
        StartCoroutine(HideUIAfterDelay(loseUIPanel));
        
#if debug
        Debug.Log($"<color=#FF0000><b>[FinishLineTrigger]</b></color> <color=red>Showing lose UI - winner: {winnerName}</color>");
#endif
    }
    
    /// <summary>
    /// Hides UI panel after the specified duration
    /// </summary>
    private IEnumerator HideUIAfterDelay(GameObject uiPanel)
    {
        yield return new WaitForSeconds(uiDisplayDuration);
        
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
        
        // Hide entire canvas if all panels are inactive
        if (finishUICanvas != null && 
            (winUIPanel == null || !winUIPanel.activeInHierarchy) &&
            (loseUIPanel == null || !loseUIPanel.activeInHierarchy) &&
            (finishUIPanel == null || !finishUIPanel.activeInHierarchy))
        {
            finishUICanvas.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Transitions to leaderboard scene after a delay to allow UI feedback
    /// </summary>
    private IEnumerator TransitionToLeaderboardAfterDelay()
    {
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        // Only transition if we're the host (to avoid multiple scene loads)
        if (IsHost)
        {
#if debug
            Debug.Log($"<color=#FFD700><b>[FinishLineTrigger]</b></color> <color=yellow>Transitioning to Leaderboard scene</color>");
#endif
            
            // Load the Leaderboard scene directly
            // Note: Using NetworkManager's SceneManager for proper multiplayer scene transitions
            NetworkManager.Singleton.SceneManager.LoadScene("Leaderboard", LoadSceneMode.Single);
        }
    }
}
