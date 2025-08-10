#define debug
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the leaderboard display in the Leaderboard scene.
/// Receives race results from RaceResultsManager and populates UI with PlayerRankSlot prefabs.
/// Attach to a dedicated GameObject in the Leaderboard scene.
/// </summary>
public class LeaderboardManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;           // ScrollView Content Transform
    [SerializeField] private PlayerRankSlot playerRankSlotPrefab; // PlayerRankSlot prefab
    [SerializeField] private Button backToLobbyButton;          // Back to Lobby button
    
    [Header("Leaderboard Settings")]
    [SerializeField] private float slotSpacing = -70f;         // Y spacing between slots
    [SerializeField] private float populationDelay = 0.5f;     // Delay before populating UI
    
    [Header("Winner Celebration")]
    [SerializeField] private GameObject winnerCelebrationEffect; // Optional winner celebration
    [SerializeField] private AudioClip victorySound;           // Optional victory sound
    [SerializeField] private AudioSource audioSource;          // Audio source for sounds

    // Leaderboard state
    private bool isLeaderboardPopulated = false;
    private PlayerRankSlot[] instantiatedSlots;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Set up button listener
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(BackToLobby);
        }

        // Start leaderboard population process
        StartCoroutine(WaitAndPopulateLeaderboard());

#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Network spawned - Starting leaderboard setup</color>");
#endif
    }

    public override void OnNetworkDespawn()
    {
        // Clean up button listener
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveListener(BackToLobby);
        }
        
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Waits for RaceResultsManager to be available then populates leaderboard
    /// </summary>
    private IEnumerator WaitAndPopulateLeaderboard()
    {
        // Wait for RaceResultsManager singleton to be available
        while (RaceResultsManager.Instance == null)
        {
#if debug
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=yellow>Waiting for RaceResultsManager...</color>");
#endif
            yield return new WaitForSeconds(0.1f);
        }

        // Small delay for UI to settle
        yield return new WaitForSeconds(populationDelay);

        PopulateLeaderboard();
    }

    /// <summary>
    /// Populates the leaderboard with race results from RaceResultsManager
    /// </summary>
    private void PopulateLeaderboard()
    {
        if (isLeaderboardPopulated) return;
        if (RaceResultsManager.Instance == null) 
        {
#if debug
            Debug.LogError($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=red>RaceResultsManager.Instance is null!</color>");
#endif
            return;
        }

        var raceResults = RaceResultsManager.Instance.GetResults();
        if (raceResults == null || raceResults.Length == 0)
        {
#if debug
            Debug.LogWarning($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=yellow>No race results available</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=green>Populating leaderboard with {raceResults.Length} results</color>");
#endif

        // Clear any existing slots
        ClearLeaderboard();

        // Create array to track instantiated slots
        instantiatedSlots = new PlayerRankSlot[raceResults.Length];

        // Sort results: Winner first, then by finish time
        var sortedResults = SortRaceResults(raceResults);

        // Instantiate PlayerRankSlot for each result
        for (int i = 0; i < sortedResults.Length; i++)
        {
            var result = sortedResults[i];
            
            // Skip empty results
            if (result.clientId == 0) continue;

            // Instantiate slot prefab
            GameObject slotObject = Instantiate(playerRankSlotPrefab.gameObject, contentParent);
            PlayerRankSlot slot = slotObject.GetComponent<PlayerRankSlot>();

            // Position the slot
            Vector3 position = new Vector3(0, i * slotSpacing, 0);
            slotObject.transform.localPosition = position;

            // Determine rank (1 for winner, 2 for loser in 2-player race)
            int rank = result.isWinner ? 1 : 2;
            
            // Format player name and time
            string playerName = result.playerName.ToString();
            string displayName = FormatPlayerDisplay(playerName, result);

            // Set slot data
            slot.SetData(rank, displayName);

            // Store reference
            instantiatedSlots[i] = slot;

#if debug
            string status = result.isWinner ? "WINNER" : (result.hasFinished ? "FINISHED" : "DNF");
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Slot {i + 1}: {playerName} - {status} ({result.finishTime:F2}s)</color>");
#endif
        }

        isLeaderboardPopulated = true;

        // Trigger winner celebration if available
        TriggerWinnerCelebration(sortedResults);
    }

    /// <summary>
    /// Sorts race results with winner first, then by finish time
    /// </summary>
    private RaceResultsManager.PlayerRaceResult[] SortRaceResults(RaceResultsManager.PlayerRaceResult[] results)
    {
        var sortedResults = new RaceResultsManager.PlayerRaceResult[results.Length];
        System.Array.Copy(results, sortedResults, results.Length);

        // Sort: Winner first, then by finish time (ascending)
        System.Array.Sort(sortedResults, (a, b) => 
        {
            // Skip empty results
            if (a.clientId == 0) return 1;
            if (b.clientId == 0) return -1;
            
            // Winner always comes first
            if (a.isWinner && !b.isWinner) return -1;
            if (b.isWinner && !a.isWinner) return 1;
            
            // If both winners or both not winners, sort by finish time
            return a.finishTime.CompareTo(b.finishTime);
        });

        return sortedResults;
    }

    /// <summary>
    /// Formats player name with finish time for display
    /// </summary>
    private string FormatPlayerDisplay(string playerName, RaceResultsManager.PlayerRaceResult result)
    {
        if (!result.hasFinished)
        {
            return $"{playerName} (DNF)"; // Did Not Finish
        }
        else if (result.finishTime < float.MaxValue)
        {
            return $"{playerName} ({result.finishTime:F2}s)";
        }
        else
        {
            return playerName;
        }
    }

    /// <summary>
    /// Triggers winner celebration effects
    /// </summary>
    private void TriggerWinnerCelebration(RaceResultsManager.PlayerRaceResult[] sortedResults)
    {
        if (sortedResults.Length == 0) return;

        var winner = sortedResults[0];
        if (!winner.isWinner || !winner.hasFinished) return;

        // Activate celebration effect
        if (winnerCelebrationEffect != null)
        {
            winnerCelebrationEffect.SetActive(true);
        }

        // Play victory sound
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=gold>🎉 Winner celebration for {winner.playerName}! 🎉</color>");
#endif
    }

    /// <summary>
    /// Clears existing leaderboard slots
    /// </summary>
    private void ClearLeaderboard()
    {
        if (instantiatedSlots != null)
        {
            foreach (var slot in instantiatedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
        }

        // Clear all children from content parent as backup
        if (contentParent != null)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                var child = contentParent.GetChild(i);
                if (child.GetComponent<PlayerRankSlot>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        instantiatedSlots = null;
        isLeaderboardPopulated = false;
    }

    /// <summary>
    /// Public method called by BackToLobby button.
    /// Returns all players to the lobby scene.
    /// </summary>
    public void BackToLobby()
    {
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=orange>BackToLobby button pressed</color>");
#endif

        // Only server can initiate scene transitions
        if (!IsServer)
        {
#if debug
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=yellow>Requesting server to return to lobby...</color>");
#endif
            RequestBackToLobbyServerRpc();
            return;
        }

        // Server logic: Clear results and return to lobby
        if (RaceResultsManager.Instance != null)
        {
            RaceResultsManager.Instance.ClearResults();
        }

        // Find GameManager and call existing lobby return method
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
#if debug
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=green>Initiating return to lobby via GameManager</color>");
#endif
            gameManager.PutPlayersBackToLobby();
        }
        else
        {
#if debug
            Debug.LogError($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=red>GameManager not found!</color>");
#endif
            // Fallback: Direct scene load
            NetworkManager.SceneManager.LoadScene("LobbyandHost", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Server RPC to handle back to lobby requests from clients
    /// </summary>
    [Rpc(SendTo.Server)]
    private void RequestBackToLobbyServerRpc()
    {
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Server received BackToLobby request</color>");
#endif
        BackToLobby();
    }

    /// <summary>
    /// Validates UI references in the editor
    /// </summary>
    private void OnValidate()
    {
        if (contentParent == null)
        {
            Debug.LogWarning($"[LeaderboardManager] contentParent is not assigned on {gameObject.name}", this);
        }

        if (playerRankSlotPrefab == null)
        {
            Debug.LogWarning($"[LeaderboardManager] playerRankSlotPrefab is not assigned on {gameObject.name}", this);
        }

        if (backToLobbyButton == null)
        {
            Debug.LogWarning($"[LeaderboardManager] backToLobbyButton is not assigned on {gameObject.name}", this);
        }
    }
}