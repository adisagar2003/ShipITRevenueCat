#define debug
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// RACE LEVEL MANAGER - Core Race Scene Controller & Player Spawning System
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// COMPLETE MULTIPLAYER RACING GAME FLOW:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 1. 🏁 LOBBY PHASE:
///    LobbyManager → CreateSession() → HostStartGame() → NetworkManager.SceneManager.LoadScene()
/// 
/// 2. 🎯 RACE SCENE INITIALIZATION (THIS SCRIPT):
///    Scene Loads → RaceLevelManager.OnNetworkSpawn() → Wait for Players → Spawn Players → Fire OnAllPlayersReady
/// 
/// 3. 🚀 COUNTDOWN PHASE:
///    OnAllPlayersReady → StartRaceCountdown → 3-2-1-GO → OnPlayerPossessionEvent → Players Can Move
/// 
/// 4. 🏃 RACE ACTIVE PHASE:
///    Players Race → FinishLineTrigger → RaceResultsManager → Transition to Leaderboard Scene
/// 
/// 5. 📊 POST-RACE PHASE:
///    Leaderboard Display → BackToLobby Button → GameManager.BackToLobbyCoroutine() → Return to Lobby
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// RACE LEVEL MANAGER RESPONSIBILITIES:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 🎮 PLAYER MANAGEMENT:
/// - Waits for expected number of players to connect (with intelligent fallback)
/// - Spawns NetworkObject player prefabs at designated positions
/// - Manages player connection timeouts and minimum player requirements
/// 
/// 🌐 NETWORK COORDINATION:
/// - Server-authoritative player spawning and game flow control
/// - Synchronizes "Waiting for Players" UI across all clients via RPC
/// - Triggers game start when conditions are met
/// 
/// 📡 EVENT BROADCASTING:
/// - Fires OnAllPlayersReady static event consumed by StartRaceCountdown
/// - Coordinates with other race systems through event-driven architecture
/// 
/// ⚙️ FALLBACK SYSTEMS:
/// - Intelligent timeout system (60s max wait, 10s minimum for partial games)
/// - Minimum player requirements with graceful degradation
/// - Handles network disconnections during waiting phase
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// DEPENDENCIES & INTEGRATION:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 📦 REQUIRED COMPONENTS:
/// - playerPrefab: Must have NetworkObject component for network spawning
/// - waitingForPlayersUI: UI GameObject shown during player waiting phase
/// 
/// 🔗 CONNECTS TO:
/// - StartRaceCountdown: Consumes OnAllPlayersReady event for countdown initiation
/// - GameConstants.Networking: Uses DEFAULT_MAX_PLAYERS and PLAYER_WAIT_POLLING_INTERVAL
/// - NetworkManager: Uses ConnectedClients for player count and spawning operations
/// 
/// 📋 CONFIGURATION:
/// - maxWaitTime: Maximum seconds to wait for players (default: 60s)
/// - minPlayersToStart: Minimum players needed to start race (default: 1 for testing)
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// </summary>
public class RaceLevelManager : NetworkBehaviour
{
    #region Serialized Fields
    [Header("Required Components")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject waitingForPlayersUI;

    [Header("Player Waiting Configuration")]
    [SerializeField] private float maxWaitTime = 60f; // Maximum time to wait for players
    [SerializeField] private int minPlayersToStart = 1; // Allow starting with fewer players for testing
    #endregion

    #region Events
    /// <summary>
    /// Event fired when all players are ready and the race can begin.
    /// Consumed by StartRaceCountdown to initiate the countdown sequence.
    /// </summary>
    public static event System.Action OnAllPlayersReady;
    #endregion

    #region Private Fields
    private float waitStartTime;
    private bool hasGameStarted = false;
    #endregion

    #region Network Lifecycle
    /// <summary>
    /// Called when this NetworkBehaviour spawns on the network.
    /// Only the server will initiate the player waiting and spawning process.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
#if debug
        Debug.Log($"<color=#4CAF50><b>[RACE LEVEL MANAGER]</b></color> <color=white>Network spawned - Server: {IsServer}, Client: {IsClient}</color>");
        if (IsServer)
        {
            Debug.Log($"<color=#4CAF50><b>[RACE LEVEL MANAGER]</b></color> <color=cyan>Server initializing race scene - Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}</color>");
        }
#endif

        // Validate required components
        if (playerPrefab == null)
        {
#if debug
            Debug.LogError($"<color=#F44336><b>[RACE LEVEL MANAGER ERROR]</b></color> <color=white>playerPrefab is null! Cannot spawn players.</color>");
#endif
            return;
        }

        if (waitingForPlayersUI == null)
        {
#if debug
            Debug.LogWarning($"<color=#FF9800><b>[RACE LEVEL MANAGER WARNING]</b></color> <color=white>waitingForPlayersUI is null! Players won't see waiting status.</color>");
#endif
        }

        // Only server manages the game flow
        if (IsServer)
        {
#if debug
            Debug.Log($"<color=#4CAF50><b>[RACE LEVEL MANAGER]</b></color> <color=yellow>Starting player waiting and spawning process...</color>");
#endif
            waitStartTime = Time.time;
            StartCoroutine(WaitForPlayersAndSpawn());
        }
        else
        {
#if debug
            Debug.Log($"<color=#4CAF50><b>[RACE LEVEL MANAGER]</b></color> <color=white>Client connected to race scene - waiting for server to manage game flow</color>");
#endif
        }
    }
    #endregion

    #region Player Management
    /// <summary>
    /// Server-only coroutine that waits for players to connect and then spawns them.
    /// Implements intelligent fallback system with timeout and minimum player requirements.
    /// </summary>
    private IEnumerator WaitForPlayersAndSpawn()
    {
        int expectedPlayers = GameConstants.Networking.DEFAULT_MAX_PLAYERS;
        
#if debug
        Debug.Log($"<color=#FFEB3B><b>[PLAYER WAITING]</b></color> <color=white>Waiting for players to join race - Expected: {expectedPlayers}, Current: {NetworkManager.Singleton.ConnectedClients.Count}</color>");
        Debug.Log($"<color=#FFEB3B><b>[PLAYER WAITING]</b></color> <color=yellow>Timeout: {maxWaitTime}s, Minimum players: {minPlayersToStart}, Quick start after: 10s</color>");
#endif

        // Main waiting loop with intelligent conditions
        while (NetworkManager.Singleton.ConnectedClients.Count < expectedPlayers)
        {
            float elapsedTime = Time.time - waitStartTime;
            int currentPlayerCount = NetworkManager.Singleton.ConnectedClients.Count;
            float timeRemaining = maxWaitTime - elapsedTime;

            // Timeout check to prevent infinite waiting
            if (elapsedTime >= maxWaitTime)
            {
#if debug
                Debug.LogWarning($"<color=#FF9800><b>[PLAYER WAITING TIMEOUT]</b></color> <color=white>Maximum wait time reached ({maxWaitTime}s)! Starting with {currentPlayerCount}/{expectedPlayers} players</color>");
#endif
                break;
            }

            // Quick start: Allow starting with minimum players after reasonable wait
            if (currentPlayerCount >= minPlayersToStart && elapsedTime >= 10f)
            {
#if debug
                Debug.Log($"<color=#FFEB3B><b>[PLAYER WAITING]</b></color> <color=lime>Quick start conditions met! {currentPlayerCount}/{expectedPlayers} players, waited {elapsedTime:F1}s (minimum: 10s)</color>");
#endif
                break;
            }

            // Regular status update every polling interval
#if debug
            if (Mathf.RoundToInt(elapsedTime) % 5 == 0) // Log every 5 seconds to avoid spam
            {
                Debug.Log($"<color=#FFEB3B><b>[PLAYER WAITING]</b></color> <color=cyan>Status: {currentPlayerCount}/{expectedPlayers} players connected, {timeRemaining:F1}s remaining</color>");
            }
#endif

            yield return new WaitForSeconds(GameConstants.Networking.PLAYER_WAIT_POLLING_INTERVAL);
        }

        // Player spawning phase
        yield return StartCoroutine(SpawnAllPlayers());
        
        // Game start phase
        StartGame();
    }

    /// <summary>
    /// Spawns player objects for all connected clients.
    /// </summary>
    private IEnumerator SpawnAllPlayers()
    {
        int finalPlayerCount = NetworkManager.Singleton.ConnectedClients.Count;
        
#if debug
        Debug.Log($"<color=#2196F3><b>[PLAYER SPAWNING]</b></color> <color=white>Beginning player spawn sequence for {finalPlayerCount} players...</color>");
#endif

        int spawnedCount = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            spawnedCount++;
            
#if debug
            Debug.Log($"<color=#2196F3><b>[PLAYER SPAWNING]</b></color> <color=cyan>Spawning player {spawnedCount}/{finalPlayerCount} for client {client.Key}...</color>");
#endif

            // Instantiate player object
            GameObject player = Instantiate(playerPrefab);
            
            if (player == null)
            {
#if debug
                Debug.LogError($"<color=#F44336><b>[PLAYER SPAWNING ERROR]</b></color> <color=white>Failed to instantiate player for client {client.Key}!</color>");
#endif
                continue;
            }

            // Set spawn position (production only - using SpawnManager)
            #if PRODUCTION
            if (SpawnManager.Instance != null)
            {
                var spawnPoint = SpawnManager.Instance.GetRandomAvailableSpawnPoint();
                player.transform.position = spawnPoint.position;
                
#if debug
                Debug.Log($"<color=#2196F3><b>[PLAYER SPAWNING]</b></color> <color=yellow>Set spawn position for client {client.Key}: {spawnPoint.position}</color>");
#endif
            }
            #endif

            // Spawn as network player object
            var networkObject = player.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
#if debug
                Debug.LogError($"<color=#F44336><b>[PLAYER SPAWNING ERROR]</b></color> <color=white>Player prefab missing NetworkObject component for client {client.Key}!</color>");
#endif
                Destroy(player);
                continue;
            }

            networkObject.SpawnAsPlayerObject(client.Key);
            
#if debug
            Debug.Log($"<color=#2196F3><b>[PLAYER SPAWNING]</b></color> <color=lime>Successfully spawned player {spawnedCount} for client {client.Key} ✓</color>");
#endif

            // Small delay between spawns for network stability
            yield return new WaitForEndOfFrame();
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[PLAYER SPAWNING]</b></color> <color=lime>Player spawning complete! {spawnedCount}/{finalPlayerCount} players spawned successfully</color>");
#endif
    }
    #endregion

    #region Game Flow Management
    /// <summary>
    /// Initiates the race start sequence after all players are spawned.
    /// Disables waiting UI and fires OnAllPlayersReady event for StartRaceCountdown.
    /// </summary>
    [ContextMenu("Start Game")]
    private void StartGame()
    {
        if (hasGameStarted)
        {
#if debug
            Debug.LogWarning($"<color=#FF9800><b>[GAME START WARNING]</b></color> <color=white>StartGame called but game has already started! Ignoring duplicate call.</color>");
#endif
            return;
        }

        hasGameStarted = true;
        float totalWaitTime = Time.time - waitStartTime;
        
#if debug
        Debug.Log($"<color=#9C27B0><b>[GAME START]</b></color> <color=lime>🚀 RACE STARTING! Total wait time: {totalWaitTime:F1}s</color>");
        Debug.Log($"<color=#9C27B0><b>[GAME START]</b></color> <color=white>Hiding waiting UI and firing OnAllPlayersReady event...</color>");
#endif

        // Hide waiting UI on server
        if (waitingForPlayersUI != null)
        {
            waitingForPlayersUI.SetActive(false);
#if debug
            Debug.Log($"<color=#9C27B0><b>[GAME START]</b></color> <color=cyan>Server waiting UI disabled</color>");
#endif
        }

        // Hide waiting UI on all clients via RPC
        DisableUIRpc();

        // Fire event to start countdown system
#if debug
        Debug.Log($"<color=#9C27B0><b>[GAME START]</b></color> <color=yellow>Firing OnAllPlayersReady event for StartRaceCountdown system...</color>");
#endif
        OnAllPlayersReady?.Invoke();

#if debug
        Debug.Log($"<color=#9C27B0><b>[GAME START]</b></color> <color=lime>✅ Game start sequence complete! Countdown should begin now.</color>");
#endif
    }

    /// <summary>
    /// RPC to disable the waiting UI on all clients.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void DisableUIRpc()
    {
#if debug
        Debug.Log($"<color=#E91E63><b>[UI RPC]</b></color> <color=cyan>Client received DisableUIRpc - hiding waiting UI</color>");
#endif
        
        if (waitingForPlayersUI != null)
        {
            waitingForPlayersUI.SetActive(false);
#if debug
            Debug.Log($"<color=#E91E63><b>[UI RPC]</b></color> <color=lime>Client waiting UI disabled ✓</color>");
#endif
        }
        else
        {
#if debug
            Debug.LogWarning($"<color=#E91E63><b>[UI RPC WARNING]</b></color> <color=white>waitingForPlayersUI is null on client!</color>");
#endif
        }
    }
    #endregion

    #region Cleanup & Legacy Code
    // Legacy code kept for reference - movement is now handled by StartRaceCountdown
    //[Rpc(SendTo.NotServer)]
    //private void EnableMovementEventClientRPC()
    //{
    //    OnPlayerPossesionEvent?.Invoke();
    //}
    #endregion
}

