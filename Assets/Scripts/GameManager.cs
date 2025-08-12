
#define debug
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GAME MANAGER - Scene Lifecycle & Network Management System
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// COMPLETE SCENE MANAGEMENT FLOW:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 1. 🌐 SCENE TRANSITIONS:
///    LobbyManager → NetworkManager.SceneManager.LoadScene() → OnSceneLoadComplete() 
///    GameManager listens to all scene load events and coordinates post-load initialization
/// 
/// 2. 🏁 RACE SCENE INITIALIZATION:
///    Race Scene Loads → InitializeRaceScene() → Setup RaceResultsManager → Reset FinishLineTriggers
///    Ensures race systems are properly configured when players enter race scenes
/// 
/// 3. 📊 POST-RACE MANAGEMENT:
///    Race Ends → RaceResultsManager → Leaderboard → BackToLobby Button → BackToLobbyCoroutine()
///    Handles coordinated return to lobby with proper network cleanup
/// 
/// 4. 🔌 NETWORK LIFECYCLE:
///    Manages NetworkManager state throughout game lifecycle
///    Handles client disconnections and server shutdown procedures
///    Ensures clean transitions between networked and offline states
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// GAME MANAGER RESPONSIBILITIES:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 🎬 SCENE ORCHESTRATION:
/// - Monitors all NetworkManager scene load events
/// - Initializes scene-specific systems (race, leaderboard, etc.)
/// - Handles player spawn position assignment (legacy/disabled)
/// - Coordinates scene-specific setup and teardown
/// 
/// 🌐 NETWORK COORDINATION:
/// - Manages NetworkManager lifecycle and cleanup
/// - Handles graceful client disconnection procedures
/// - Coordinates server shutdown and scene transitions
/// - Ensures proper network state during scene changes
/// 
/// 🔄 LOBBY RETURN SYSTEM:
/// - Provides BackToLobby functionality from any scene
/// - Orchestrates client disconnection sequence
/// - Manages server shutdown and cleanup
/// - Loads offline lobby scene after network cleanup
/// 
/// 🏁 RACE SYSTEM INTEGRATION:
/// - Detects race scenes and initializes race-specific systems
/// - Ensures RaceResultsManager is available for leaderboard flow
/// - Resets FinishLineTriggers for new races
/// - Validates race system dependencies
/// 
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// INTEGRATION & DEPENDENCIES:
/// ═══════════════════════════════════════════════════════════════════════════════════════
/// 
/// 🔗 CONNECTS TO:
/// - NetworkManager.SceneManager: Listens to OnLoadEventCompleted
/// - RaceResultsManager: Validates presence in race scenes
/// - FinishLineTrigger: Resets race state for new races
/// - LobbyManager: Target scene for BackToLobby functionality
/// 
/// 📋 SCENE DETECTION:
/// - Race Scenes: Names containing "Race", "RaceLevel", "MultiplayerTestLevel"
/// - Lobby Scene: Configurable via lobbySceneName (default: "LobbyandHost")
/// 
/// ⚙️ NETWORK SETTINGS:
/// - Uses NetworkVariable for countdown timer (legacy)
/// - Server authority for scene management decisions
/// - Client coordination for graceful disconnections
/// </summary>
public class GameManager : NetworkBehaviour
{
    #region Network Variables
    /// <summary>
    /// Legacy countdown timer - kept for compatibility but not actively used.
    /// Modern countdown handled by StartRaceCountdown component.
    /// </summary>
    private NetworkVariable<float> _countdownTimer = new NetworkVariable<float>(3f);
    #endregion

    #region Serialized Fields
    [Header("Scene Configuration")]
    [SerializeField] private string lobbySceneName = "LobbyandHost";
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initialize GameManager and subscribe to NetworkManager scene events.
    /// Handles cases where NetworkManager may not be immediately available.
    /// </summary>
    private void Start()
    {
#if debug
        Debug.Log($"<color=#4CAF50><b>[GAME MANAGER]</b></color> <color=white>GameManager initializing...</color>");
#endif

        if (NetworkManager.Singleton == null)
        {
#if debug
            Debug.LogWarning($"<color=#FF9800><b>[GAME MANAGER WARNING]</b></color> <color=white>NetworkManager.Singleton is null at Start() - will wait for initialization</color>");
#endif
            StartCoroutine(WaitForNetworkManagerAndSubscribe());
            return;
        }

        SubscribeToNetworkEvents();
    }

    /// <summary>
    /// Clean up event subscriptions when GameManager is destroyed.
    /// </summary>
    public override void OnDestroy()
    {
        UnsubscribeFromNetworkEvents();
        base.OnDestroy();
    }

    /// <summary>
    /// Waits for NetworkManager to become available then subscribes to events.
    /// Used when NetworkManager.Singleton is null at Start().
    /// </summary>
    private IEnumerator WaitForNetworkManagerAndSubscribe()
    {
#if debug
        Debug.Log($"<color=#FFEB3B><b>[GAME MANAGER]</b></color> <color=yellow>Waiting for NetworkManager.Singleton to become available...</color>");
#endif

        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

#if debug
        Debug.Log($"<color=#4CAF50><b>[GAME MANAGER]</b></color> <color=lime>NetworkManager.Singleton available - subscribing to events</color>");
#endif
        SubscribeToNetworkEvents();
    }

    /// <summary>
    /// Subscribe to NetworkManager scene events for coordinated scene management.
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
#if debug
            Debug.Log($"<color=#4CAF50><b>[GAME MANAGER]</b></color> <color=cyan>Subscribed to NetworkManager.SceneManager.OnLoadEventCompleted</color>");
#endif
        }
    }

    /// <summary>
    /// Unsubscribe from NetworkManager scene events during cleanup.
    /// </summary>
    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
#if debug
            Debug.Log($"<color=#FF9800><b>[GAME MANAGER]</b></color> <color=white>Unsubscribed from NetworkManager scene events</color>");
#endif
        }
    }
    #endregion

    #region Scene Management
    /// <summary>
    /// Called whenever a networked scene load completes.
    /// Handles post-load initialization and coordination for all clients.
    /// </summary>
    /// <param name="sceneName">Name of the loaded scene</param>
    /// <param name="loadSceneMode">How the scene was loaded (Single/Additive)</param>
    /// <param name="clientsCompleted">List of clients that successfully loaded the scene</param>
    /// <param name="clientsTimedOut">List of clients that timed out during scene load</param>
    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Only server handles scene coordination
        if (!IsServer)
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=white>Client received scene load complete event for '{sceneName}' - server will handle coordination</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=lime>🎬 Scene '{sceneName}' loaded in {loadSceneMode} mode</color>");
        Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=cyan>✅ Successful clients: {clientsCompleted.Count}, ⚠️ Timed out clients: {clientsTimedOut.Count}</color>");
#endif

        // Log successful client connections
        foreach (var clientId in clientsCompleted)
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=lime>✅ Client {clientId} successfully loaded scene</color>");
#endif
        }

        // Log and handle timed out clients
        if (clientsTimedOut.Count > 0)
        {
#if debug
            Debug.LogWarning($"<color=#FF5722><b>[SCENE MANAGEMENT ERROR]</b></color> <color=white>⚠️ {clientsTimedOut.Count} clients timed out during scene load:</color>");
#endif
            foreach (var clientId in clientsTimedOut)
            {
#if debug
                Debug.LogWarning($"<color=#FF5722><b>[SCENE MANAGEMENT ERROR]</b></color> <color=white>❌ Client {clientId} timed out - may cause synchronization issues</color>");
#endif
            }
        }

        // Scene-specific initialization
        if (IsRaceScene(sceneName))
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=yellow>🏁 Race scene detected - initializing race systems...</color>");
#endif
            InitializeRaceScene();
        }
        else
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=white>📋 Non-race scene loaded - skipping race system initialization</color>");
#endif
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[SCENE MANAGEMENT]</b></color> <color=lime>✅ Scene load coordination complete for '{sceneName}'</color>");
#endif

        // Legacy spawn position assignment (disabled)
        // This functionality has been moved to RaceLevelManager for better organization
        //foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
        //{
        //    if (!clientsCompleted.Contains(networkClient.ClientId)) continue;
        //    var playerObject = networkClient.PlayerObject;
        //    if (playerObject != null)
        //    {
        //        var setSpawnLocation = playerObject.GetComponent<SetSpawnLocation>();
        //        if (setSpawnLocation != null)
        //        {
        //            setSpawnLocation.AssignNewSpawnPosition();
        //        }
        //    }
        //}
    }

    #region Lobby Return System
    /// <summary>
    /// Context menu method to return all players to lobby.
    /// Initiates coordinated disconnection and scene transition sequence.
    /// </summary>
    [ContextMenu("Put players back to lobby")]
    public void PutPlayersBackToLobby()
    {
        if (!IsServer)
        {
#if debug
            Debug.LogWarning($"<color=#FF9800><b>[LOBBY RETURN WARNING]</b></color> <color=white>Only server can initiate lobby return</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=yellow>🔙 Initiating return to lobby sequence...</color>");
#endif
        StartCoroutine(BackToLobbyCoroutine());
    }

    /// <summary>
    /// Coordinated sequence to return all players to offline lobby.
    /// Handles client disconnection, server shutdown, and scene transition.
    /// </summary>
    public IEnumerator BackToLobbyCoroutine()
    {
        // Initial delay for any ongoing operations to complete
#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=white>⏳ Waiting 2 seconds before starting disconnect sequence...</color>");
#endif
        yield return new WaitForSeconds(2.0f);

#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=orange>📡 Sending disconnect request to all clients...</color>");
#endif
        
        if (!IsServer)
        {
#if debug
            Debug.LogError($"<color=#F44336><b>[LOBBY RETURN ERROR]</b></color> <color=white>Server authority lost during lobby return!</color>");
#endif
            yield break;
        }

        // Request all clients to disconnect
        RequestClientDisconnectRpc();

        // Wait for clients to disconnect with timeout
        float timeout = 5f;
        float elapsed = 0f;
        int initialClientCount = NetworkManager.Singleton.ConnectedClientsList.Count - 1; // Exclude server

#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=cyan>⌛ Waiting for {initialClientCount} clients to disconnect (timeout: {timeout}s)...</color>");
#endif

        while (NetworkManager.Singleton.ConnectedClientsList.Count > 1 && elapsed < timeout)
        {
            int remainingClients = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
#if debug
            if (Mathf.RoundToInt(elapsed) % 1 == 0) // Log every second
            {
                Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=yellow>⏳ Waiting for disconnection... {remainingClients} clients remaining, {timeout - elapsed:F1}s timeout</color>");
            }
#endif
            yield return null;
            elapsed += Time.deltaTime;
        }

        int finalClientCount = NetworkManager.Singleton.ConnectedClientsList.Count - 1;
        if (finalClientCount == 0)
        {
#if debug
            Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=lime>✅ All clients disconnected successfully!</color>");
#endif
        }
        else
        {
#if debug
            Debug.LogWarning($"<color=#FF5722><b>[LOBBY RETURN WARNING]</b></color> <color=white>⚠️ Timeout reached - {finalClientCount} clients still connected</color>");
#endif
        }

        // Shutdown server and cleanup
#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=orange>🔌 Shutting down server and cleaning up network...</color>");
#endif
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);

        // Load offline lobby scene
#if debug
        Debug.Log($"<color=#FF5722><b>[LOBBY RETURN]</b></color> <color=lime>🏠 Loading offline lobby scene: '{lobbySceneName}'</color>");
#endif
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// RPC sent to all clients to initiate graceful disconnection.
    /// Clients load lobby scene and disconnect from network.
    /// </summary>
    [Rpc(SendTo.NotServer)]
    private void RequestClientDisconnectRpc()
    {
#if debug
        Debug.Log($"<color=#E91E63><b>[CLIENT DISCONNECT]</b></color> <color=orange>📡 Received disconnect request from server - returning to offline lobby</color>");
#endif
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        DisconnectClient();
    }

    /// <summary>
    /// Disconnects client from server and loads lobby scene.
    /// Can be called manually via context menu or programmatically.
    /// </summary>
    [ContextMenu("Disconnect Client")]
    public void DisconnectClient()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
#if debug
            Debug.Log($"<color=#E91E63><b>[CLIENT DISCONNECT]</b></color> <color=orange>🔌 Client disconnecting from server...</color>");
#endif
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(lobbySceneName);
#if debug
            Debug.Log($"<color=#E91E63><b>[CLIENT DISCONNECT]</b></color> <color=lime>✅ Client disconnected and returned to lobby</color>");
#endif
        }
        else
        {
#if debug
            Debug.LogWarning($"<color=#E91E63><b>[CLIENT DISCONNECT WARNING]</b></color> <color=white>Not connected to server or NetworkManager is null</color>");
#endif
        }
    }
    #endregion

    #region Legacy & Development Tools
    /// <summary>
    /// Development tool to force reset current scene after countdown.
    /// Uses legacy countdown timer system.
    /// </summary>
    [ContextMenu("Test Force Reset Scene")]
    public void ForceResetScene()
    {
        if (!IsServer)
        {
#if debug
            Debug.LogWarning($"<color=#9C27B0><b>[LEGACY TOOL WARNING]</b></color> <color=white>Only server can force scene reset</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#9C27B0><b>[LEGACY TOOL]</b></color> <color=yellow>🔄 Force scene reset requested via context menu</color>");
#endif
        StartCoroutine(DelayedSceneReset());
    }

    /// <summary>
    /// Legacy method for delayed scene reset using countdown timer.
    /// Note: Modern countdown system uses StartRaceCountdown component instead.
    /// </summary>
    public IEnumerator DelayedSceneReset()
    {
#if debug
        Debug.Log($"<color=#9C27B0><b>[LEGACY SYSTEM]</b></color> <color=yellow>⏰ Starting delayed scene reset - countdown: {_countdownTimer.Value}s</color>");
#endif

        while (_countdownTimer.Value > 0f)
        {
            yield return new WaitForSeconds(1f);
            _countdownTimer.Value -= 1f;
#if debug
            Debug.Log($"<color=#9C27B0><b>[LEGACY SYSTEM]</b></color> <color=white>Countdown: {_countdownTimer.Value}s remaining</color>");
#endif
        }

#if debug
        Debug.Log($"<color=#9C27B0><b>[LEGACY SYSTEM]</b></color> <color=lime>🔄 Countdown complete - reloading current scene</color>");
#endif
        NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    /// <summary>
    /// Determines if a scene is a race scene that should use the leaderboard system.
    /// Race scenes require special initialization including RaceResultsManager and FinishLineTriggers.
    /// </summary>
    /// <param name="sceneName">Name of the scene to check</param>
    /// <returns>True if this is a race scene requiring race system initialization</returns>
    private bool IsRaceScene(string sceneName)
    {
        bool isRace = sceneName.Contains("Race") ||
                     sceneName == "RaceLevel" ||
                     sceneName == "MultiplayerTestLevel";

#if debug
        Debug.Log($"<color=#2196F3><b>[SCENE DETECTION]</b></color> <color=cyan>Scene '{sceneName}' race detection: {isRace}</color>");
#endif
        return isRace;
    }

    /// <summary>
    /// Initializes race-specific systems when a race scene loads.
    /// Sets up RaceResultsManager and resets FinishLineTriggers for new race.
    /// </summary>
    private void InitializeRaceScene()
    {
        if (!IsServer)
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=white>Client skipping race initialization - server will handle setup</color>");
#endif
            return;
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=yellow>🏁 Initializing race scene systems...</color>");
#endif

        // Validate RaceResultsManager component
        var raceResultsManager = GetComponent<RaceResultsManager>();
        if (raceResultsManager == null)
        {
#if debug
            Debug.LogWarning($"<color=#FF9800><b>[RACE INITIALIZATION WARNING]</b></color> <color=white>RaceResultsManager not found on GameManager! Leaderboard system will not work properly.</color>");
            Debug.LogWarning($"<color=#FF9800><b>[RACE INITIALIZATION WARNING]</b></color> <color=white>Please attach RaceResultsManager.cs to the GameManager GameObject in race scenes.</color>");
#endif
        }
        else
        {
#if debug
            Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=lime>✅ RaceResultsManager found - leaderboard system ready</color>");
#endif
        }

        // Reset all finish line triggers for new race
        var finishTriggers = FindObjectsByType<FinishLineTrigger>(FindObjectsSortMode.None);
#if debug
        Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=cyan>Found {finishTriggers.Length} FinishLineTrigger(s) - resetting for new race...</color>");
#endif

        foreach (var trigger in finishTriggers)
        {
            trigger.ResetRace();
#if debug
            Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=lime>✅ Reset FinishLineTrigger: {trigger.name}</color>");
#endif
        }

#if debug
        Debug.Log($"<color=#2196F3><b>[RACE INITIALIZATION]</b></color> <color=lime>✅ Race scene initialization complete!</color>");
#endif
    }
    #endregion
}
