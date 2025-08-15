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
    [SerializeField] private PlayerRankSlot playerRankSlotPrefab;
    [SerializeField] private Button backToLobbyButton;

    [Header("Leaderboard Settings")]
    [SerializeField] private float slotSpacing = -70f;         // Y spacing between slots
    [SerializeField] private float populationDelay = 0.5f;     // Delay before populating UI

    
    [Header("Position Settings")]
    [SerializeField] private Vector3 slotOffset = Vector3.zero; // Additional transform offset for slots
    [SerializeField] private float firstSlotYPosition = 0f;     // Starting Y position for first slot

    
    [Header("Winner Celebration")]
    [SerializeField] private GameObject winnerCelebrationEffect; // Optional winner celebration
    [SerializeField] private AudioClip victorySound;           // Optional victory sound
    [SerializeField] private AudioSource audioSource;          // Audio source for sounds
    
    [Header("Scene Management")]
    [SerializeField] private string lobbySceneName = "LobbyandHost"; // Scene to return to when leaving

    // Leaderboard state
    private bool isLeaderboardPopulated = false;
    private PlayerRankSlot[] instantiatedSlots;
    private bool isButtonListenerAdded = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Set up button listener (only once)
        SetupButtonListener();

        // Start leaderboard population process
        StartCoroutine(WaitAndPopulateLeaderboard());

#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Network spawned - Starting leaderboard setup</color>");
#endif
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Sets up the button listener safely (only once)
    /// </summary>
    private void SetupButtonListener()
    {
        if (!isButtonListenerAdded && backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveListener(BackToLobby); // Remove any existing listeners first
            backToLobbyButton.onClick.AddListener(BackToLobby);
            isButtonListenerAdded = true;
            
#if debug
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=green>Button listener added successfully</color>");
#endif
        }
        else if (backToLobbyButton == null)
        {
#if debug
            Debug.LogWarning($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=yellow>backToLobbyButton is null - cannot add listener</color>");
#endif
        }
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
            yield return new WaitForSeconds(0.2f);
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

        ClearLeaderboard();

        // Count valid results first to properly size array
        int validResultsCount = 0;
        foreach (var result in raceResults)
        {
            if (result.hasFinished || result.isWinner) validResultsCount++;
        }
        
        // Create array to track instantiated slots
        instantiatedSlots = new PlayerRankSlot[validResultsCount];

        // Sort results: Winner first, then by finish time
        var sortedResults = SortRaceResults(raceResults);

        // Instantiate PlayerRankSlot for each valid result
        int validSlotPosition = 0; // Separate counter for valid slots only
        for (int i = 0; i < sortedResults.Length; i++)
        {
            var result = sortedResults[i];
            
            // Skip empty results but don't affect positioning
        //            if (result.clientId == 0) continue;

            // Instantiate slot prefab
            GameObject slotObject = Instantiate(playerRankSlotPrefab.gameObject, contentParent);
            PlayerRankSlot slot = slotObject.GetComponent<PlayerRankSlot>();

            // Position the slot with proper offset and spacing
            Vector3 position = new Vector3(
                slotOffset.x, 
                firstSlotYPosition + (validSlotPosition * slotSpacing) + slotOffset.y, 
                slotOffset.z
            );
            slotObject.transform.localPosition = position;

            // Determine rank (1 for winner, 2 for loser in 2-player race)
            int rank = result.isWinner ? 1 : 2;
            
            // Format player name and time
            string playerName = result.playerName.ToString();
            string displayName = FormatPlayerDisplay(playerName, result);

            // Set slot data
            slot.SetData(rank, displayName);

            // Store reference using validSlotPosition index
            instantiatedSlots[validSlotPosition] = slot;
            
            // Increment valid slot position counter
            validSlotPosition++;

#if debug
            string status = result.isWinner ? "WINNER" : (result.hasFinished ? "FINISHED" : "DNF");
            Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Slot {validSlotPosition}: {playerName} - {status} ({result.finishTime:F2}s)</color>");
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
            return $"{playerName} (Did Not Finish)"; // Did Not Finish
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
    /// Handles client disconnect vs server shutdown scenarios properly.
    /// </summary>
    public void BackToLobby()
    {
        ulong requestingClientId = NetworkManager.Singleton.LocalClientId;
        
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=orange>BackToLobby pressed by client {requestingClientId} (IsHost: {IsHost}, IsServer: {NetworkManager.Singleton.IsHost})</color>");
#endif

        if (IsHost || NetworkManager.Singleton.IsHost)
        {
            // Host/Server wants to end session - disconnect all players
            HandleHostBackToLobby();
        }
        else
        {
            // Client wants to leave - only disconnect this client
            HandleClientBackToLobby();
        }
    }
    
    /// <summary>
    /// Handles when host/server wants to end the session - disconnects all clients
    /// </summary>
    private void HandleHostBackToLobby()
    {
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=red>Host disconnecting - shutting down session for all players</color>");
#endif

        // Clear race results
        if (RaceResultsManager.Instance != null)
        {
            RaceResultsManager.Instance.ClearResults();
        }

        // Use GameManager to properly disconnect all clients
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.PutPlayersBackToLobby();
        }
        else
        {
#if debug
            Debug.LogError($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=red>GameManager not found!</color>");
#endif
            // Fallback: Direct scene load and shutdown
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    
    /// <summary>
    /// Handles when a client wants to leave - only disconnects the requesting client
    /// </summary>
    private void HandleClientBackToLobby()
    {
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=yellow>Client requesting to leave session</color>");
#endif
        // If the scene is already shut down ,then return to lobby safelty
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) 
        {
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);            
        }

        // Client disconnects immediately and returns to lobby
        StartCoroutine(DisconnectClientAndReturnToLobby());
    }
    
    /// <summary>
    /// Coroutine to handle clean client disconnect and return to lobby
    /// </summary>
    private System.Collections.IEnumerator DisconnectClientAndReturnToLobby()
    {
        // Small delay to ensure any final network messages are sent
        yield return new WaitForSeconds(0.1f);
        
#if debug
        Debug.Log($"<color=#9B59B6><b>[LeaderboardManager]</b></color> <color=cyan>Client disconnecting and returning to lobby</color>");
#endif

        // Disconnect from network session
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // Return to lobby scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
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