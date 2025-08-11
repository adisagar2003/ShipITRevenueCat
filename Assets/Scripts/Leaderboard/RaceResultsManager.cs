#define debug
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;

/// <summary>
/// Manages race results for 2-player races. Tracks winner/loser with timestamps.
/// Persists across scene transitions to provide data to the leaderboard.
/// Attach to GameManager GameObject in race scenes.
/// </summary>
public class RaceResultsManager : NetworkBehaviour
{
    public static RaceResultsManager Instance { get; private set; }

    [Header("Race Settings")]
    [SerializeField] private float leaderboardDelaySeconds = 3f;  // Delay before showing leaderboard
    
    [Header("Scene Management")]
    [SerializeField] private string leaderboardSceneName = "Leaderboard"; // Scene to transition to after race

    // This is a custom object synced across clients and server for reading and writing.
    [System.Serializable]
    public struct PlayerRaceResult : INetworkSerializable
    {
        public ulong clientId;
        public string playerName;
        public float finishTime;
        public bool isWinner;
        public bool hasFinished;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref finishTime);
            serializer.SerializeValue(ref isWinner);
            serializer.SerializeValue(ref hasFinished);
        }
    }

    // Race state
    private PlayerRaceResult[] raceResults = new PlayerRaceResult[2]; // Fixed for 2 players
    private int finishedPlayerCount = 0;
    private float raceStartTime;
    private bool raceCompleted = false;
    private bool isTransitioningToLeaderboard = false; // Prevent duplicate transitions

    // Network synchronization
    private NetworkVariable<bool> isRaceActive = new NetworkVariable<bool>(false);

    private void Awake()
    {
        // Singleton pattern with scene persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            InitializeRace();
        }

#if debug
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=cyan>Network spawned - Server: {IsServer}</color>");
#endif
    }

    /// <summary>
    /// Initializes a new race. Call this when the race scene starts.
    /// </summary>
    public void InitializeRace()
    {
        if (!IsServer) return;

        raceStartTime = Time.time;
        finishedPlayerCount = 0;
        raceCompleted = false;
        isTransitioningToLeaderboard = false;
        isRaceActive.Value = true;

        // Clear previous results
        for (int i = 0; i < raceResults.Length; i++)
        {
            raceResults[i] = new PlayerRaceResult
            {
                clientId = 0,
                playerName = "",
                finishTime = 0f,
                isWinner = false,
                hasFinished = false
            };
        }

#if debug
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=green>Race initialized at time {raceStartTime}</color>");
#endif
    }

    /// <summary>
    /// Records when a player finishes the race. Call this from FinishLineTrigger.
    /// </summary>
    /// <param name="clientId">Player's network client ID</param>
    /// <param name="playerName">Player's display name</param>
    public void RecordPlayerFinish(ulong clientId, string playerName)
    {
        if (!IsServer) return;
        if (raceCompleted) return;
        if (finishedPlayerCount >= 2) return;

        float finishTime = Time.time - raceStartTime;
        bool isWinner = finishedPlayerCount == 0; // First to finish wins

        raceResults[finishedPlayerCount] = new PlayerRaceResult
        {
            clientId = clientId,
            playerName = playerName,
            finishTime = finishTime,
            isWinner = isWinner,
            hasFinished = true
        };

        finishedPlayerCount++;

#if debug
        string winStatus = isWinner ? "WINNER" : "RUNNER-UP";
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=yellow>Player {playerName} finished as {winStatus} in {finishTime:F2}s</color>");
#endif

        // Check if we have a winner or both players finished
        if (isWinner && !isTransitioningToLeaderboard)
        {
            // We have a winner, start countdown to leaderboard
            isTransitioningToLeaderboard = true;
            StartCoroutine(DelayedLeaderboardTransition());
        }
        else if (finishedPlayerCount >= 2)
        {
            // Both players finished, go to leaderboard immediately
            TransitionToLeaderboard();
        }
    }

    /// <summary>
    /// Forces race completion if needed (e.g., timeout scenario)
    /// </summary>
    public void ForceRaceCompletion()
    {
        if (!IsServer) return;
        if (raceCompleted) return;

        // Find unfinished players and mark them as DNF
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        // Get list of already finished client IDs
        var finishedClientIds = new System.Collections.Generic.HashSet<ulong>();
        for (int i = 0; i < finishedPlayerCount; i++)
        {
            finishedClientIds.Add(raceResults[i].clientId);
        }

        // Mark remaining connected clients as DNF
        foreach (var client in connectedClients)
        {
            if (!finishedClientIds.Contains(client.ClientId) && finishedPlayerCount < 2)
            {
                raceResults[finishedPlayerCount] = new PlayerRaceResult
                {
                    clientId = client.ClientId,
                    playerName = $"Player{client.ClientId}",
                    finishTime = float.MaxValue, // DNF marker
                    isWinner = false,
                    hasFinished = false // DNF - did not finish
                };
                finishedPlayerCount++;
            }
        }

        TransitionToLeaderboard();
    }

    /// <summary>
    /// Waits for delay then transitions to leaderboard
    /// </summary>
    private IEnumerator DelayedLeaderboardTransition()
    {
#if debug
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=orange>Starting {leaderboardDelaySeconds}s countdown to leaderboard...</color>");
#endif

        yield return new WaitForSeconds(leaderboardDelaySeconds);

        // If second player still hasn't finished, mark them as DNF
        if (finishedPlayerCount < 2)
        {
            ForceRaceCompletion();
        }
        else
        {
            TransitionToLeaderboard();
        }
    }

    /// <summary>
    /// Transitions all players to the leaderboard scene
    /// </summary>
    private void TransitionToLeaderboard()
    {
        if (!IsServer) return;
        if (raceCompleted) return;

        raceCompleted = true;
        isRaceActive.Value = false;
        // Send data to all clients
        SendRaceResultsClientRpc(raceResults);
#if debug
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=green>Transitioning to leaderboard scene...</color>");
#endif
        // Use NetworkSceneManager to transition all clients
        NetworkManager.SceneManager.LoadScene(leaderboardSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>
    /// Gets the current race results. Used by LeaderboardManager.
    /// </summary>
    /// <returns>Array of race results, sorted by finish order</returns>
    public PlayerRaceResult[] GetResults()
    {
        return raceResults;
    }

    /// <summary>
    /// Gets the winner's result data
    /// </summary>
    /// <returns>Winner's PlayerRaceResult, or default if no winner</returns>
    public PlayerRaceResult GetWinner()
    {
        for (int i = 0; i < raceResults.Length; i++)
        {
            if (raceResults[i].isWinner && raceResults[i].hasFinished)
            {
                return raceResults[i];
            }
        }
        return new PlayerRaceResult(); // Default empty result
    }

    /// <summary>
    /// Clears race results. Call this when returning to lobby.
    /// </summary>
    public void ClearResults()
    {
        if (!IsServer) return;

        InitializeRace();
        isRaceActive.Value = false;

#if debug
        Debug.Log($"<color=#FF6B35><b>[RaceResultsManager]</b></color> <color=yellow>Race results cleared</color>");
#endif
    }

    /// <summary>
    /// Checks if the race is currently active
    /// </summary>
    /// <returns>True if race is in progress</returns>
    public bool IsRaceActive()
    {
        return isRaceActive.Value;
    }

    // Push race results to client before transitioning to the leaderboard.
    [ClientRpc]
    private void SendRaceResultsClientRpc(PlayerRaceResult[] results)
    {
        // Store locally for the leaderboard
        raceResults = results;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
