using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
/// <summary>
/// Manages multiplayer lobby creation, joining, and relay connectivity.
/// Compatible with Unity 6 Multiplayer Services package - uses the unified Sessions approach.
/// Uses ISession interface for session management instead of deprecated Lobby API.
/// 
/// REFACTORED FOR BETTER MAINTAINABILITY:
/// - Broken down large methods into smaller, focused functions
/// - Grouped related functionality into logical regions
/// - Added comprehensive documentation
/// - Improved error handling with dedicated methods
/// </summary>
public class LobbyManager : ThreadSafeSingleton<LobbyManager>
{
    #region Singleton & Events - Singleton handled by ThreadSafeSingleton base class
    public event Action OnSessionsUpdated;
    #endregion

    #region Public Properties
    public List<ISessionInfo> availableSessions { get; private set; } = new List<ISessionInfo>();
    public ISession currentSession;
    #endregion

    #region Private Fields
    private bool shouldRefreshSessions = true;
    private int maxPlayers = GameConstants.Networking.DEFAULT_MAX_PLAYERS;
    private DateTime lastFetchTime = DateTime.MinValue;
    private const int MIN_FETCH_INTERVAL_SECONDS = 5;
    #endregion

    #region Serialized Fields
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string gameSceneName = GameConstants.Graphics.SCENE_RACE_LEVEL;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private GameObject creatingLobbyText;
    [SerializeField] private GameObject startingGameText;
    [SerializeField] private string previousSceneName = GameConstants.Graphics.SCENE_CHARACTER_CUSTOMIZER;
    #endregion

    #region UI Management
    private LobbyUIManager uiManager;
    #endregion

    #region Services
    private LobbySessionService sessionService;
    #endregion

    #region Unity Lifecycle

    protected override void Initialize()
    {
        base.Initialize();
        SetupUIManager();
        SetupServices();
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager initialized");
    }

    /// <summary>
    /// Sets up the UI manager component and configures it with current UI references.
    /// This auto-attaches LobbyUIManager if it doesn't exist and ensures proper state reset.
    /// </summary>
    private void SetupUIManager()
    {
        // Auto-attach LobbyUIManager component if it doesn't exist
        uiManager = GetComponent<LobbyUIManager>();
        if (uiManager == null)
        {
            uiManager = gameObject.AddComponent<LobbyUIManager>();
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "Auto-attached LobbyUIManager component");
        }

        // Configure UI references in the manager
        uiManager.SetupUIReferences(createLobbyButton, creatingLobbyText, startingGameText);
    }

    /// <summary>
    /// Sets up service dependencies and event subscriptions.
    /// </summary>
    private void SetupServices()
    {
        sessionService = LobbySessionService.Instance;
        
        // Subscribe to session service events for UI updates
        sessionService.OnSessionCreated += OnSessionCreatedByService;
        sessionService.OnSessionJoined += OnSessionJoinedByService;
        sessionService.OnSessionLeft += OnSessionLeftByService;
        sessionService.OnSessionError += OnSessionErrorByService;
        
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Lobby services initialized");
    }

    #endregion

    #region Service Event Handlers
    /// <summary>
    /// Handles session creation success from the service.
    /// </summary>
    private void OnSessionCreatedByService(ISession session)
    {
        currentSession = session;
        uiManager?.ShowLobbyCreatedState();
        shouldRefreshSessions = false; // Stop polling when we have a session
        GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Session created successfully: {session.Name}");
    }

    /// <summary>
    /// Handles session join success from the service.
    /// </summary>
    private void OnSessionJoinedByService(ISession session)
    {
        currentSession = session;
        shouldRefreshSessions = false; // Stop polling when we have a session
        _ = StartPollingForGameStart(); // Start polling for game start
        GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Joined session successfully: {session.Name}");
    }

    /// <summary>
    /// Handles session leave from the service.
    /// </summary>
    private void OnSessionLeftByService()
    {
        currentSession = null;
        shouldRefreshSessions = true; // Resume session browsing
        uiManager?.ResetUIToLobbyState();
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Left session successfully");
    }

    /// <summary>
    /// Handles session errors from the service.
    /// </summary>
    private void OnSessionErrorByService(string errorMessage)
    {
        uiManager?.ShowLobbyCreationFailedState();
        GameLogger.LogError(GameLogger.LogCategory.Network, $"Session service error: {errorMessage}");
    }

    protected override void OnSingletonDestroyed()
    {
        // Stop lobby operations
        shouldRefreshSessions = false;

        // Unsubscribe from service events to prevent memory leaks
        if (sessionService != null)
        {
            sessionService.OnSessionCreated -= OnSessionCreatedByService;
            sessionService.OnSessionJoined -= OnSessionJoinedByService;
            sessionService.OnSessionLeft -= OnSessionLeftByService;
            sessionService.OnSessionError -= OnSessionErrorByService;
            
            // Clean up session service
            sessionService.Cleanup();
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager disposed");
        base.OnSingletonDestroyed();
    }


    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Temporarily stop lobby refresh to save battery/data
            shouldRefreshSessions = false;
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Lobby operations paused");
        }
        else
        {
            // Resume lobby operations if we were refreshing
            if (!sessionService.HasActiveSession) // Only resume if not in a specific session
            {
                shouldRefreshSessions = true;
                _ = RefreshSessionsLoop();
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "Session operations resumed");
            }
        }
    }

    private async void Start()
    {
        await WaitForGameInitializer();
#if debug
        Debug.Log("LobbyManager ready.");
#endif
        StartCoroutine(WaitForNetworkManagerReady());
        _ = RefreshSessionsLoop();
    }

    private void OnEnable()
    {
        // Reset UI state when returning to lobby scene
        // This fixes the "Creating Lobby..." persistence issue
        if (uiManager != null)
        {
            uiManager.ResetUIToLobbyState();
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI state reset on scene enable");
        }
    }

    #endregion

    #region Lobby Management
    private async Task WaitForGameInitializer()
    {
        while (!GameInitializer.IsInitialized)
            await Task.Delay(100);
    }

    private async Task RefreshSessionsLoop()
    {
        while (shouldRefreshSessions && !sessionService.HasActiveSession)
        {
            try
            {
                await FetchAvailableSessions();
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, $"Error in lobby refresh loop: {ex.Message}");

                // Use exponential backoff on errors to avoid spam
                await Task.Delay(30000); // 30 second backoff on error
                continue;
            }

            // Reduced interval to 5 seconds for responsive lobby discovery
            await Task.Delay(5000);
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "RefreshSessionsLoop stopped - either shouldRefreshSessions=false or session exists");
    }

    public async Task FetchAvailableSessions(bool bypassRateLimit = false)
    {
        // Rate limiting safeguard - prevent calls within 5 seconds (unless bypassed)
        if (!bypassRateLimit)
        {
            var timeSinceLastFetch = DateTime.Now - lastFetchTime;
            if (timeSinceLastFetch.TotalSeconds < MIN_FETCH_INTERVAL_SECONDS)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, $"FetchAvailableSessions called too soon, skipping. Time since last call: {timeSinceLastFetch.TotalSeconds:F1}s");
                return;
            }
        }

        lastFetchTime = DateTime.Now;

        try
        {
            var response = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

            if (response?.Sessions != null)
            {
                availableSessions = response.Sessions.ToList();
                OnSessionsUpdated?.Invoke();

                // Ensure UI state is properly synchronized after session fetch
                if (uiManager != null && createLobbyButton != null && !createLobbyButton.interactable)
                    uiManager.ShowLobbyCreatedState();
            }
            else
            {
#if debug
                Debug.LogWarning("Query response or results is null");
#endif
                availableSessions?.Clear();
            }
        }
        catch (SessionException)
        {
            // Clear sessions on error to avoid showing stale data
            availableSessions?.Clear();
            OnSessionsUpdated?.Invoke();
        }
        catch (Exception)
        {
            // Clear sessions on error to avoid showing stale data
            availableSessions?.Clear();
            OnSessionsUpdated?.Invoke();
        }
    }


    #endregion

    #region Manual Refresh
    [ContextMenu("Manual Refresh Sessions")]
    public async void ManualRefreshSessions()
    {
#if debug
        Debug.Log("<color=yellow><b>[MANUAL REFRESH]</b></color> 🔄 Manual session refresh requested");
#endif
        await FetchAvailableSessions(bypassRateLimit: true);
    }
    #endregion

    #region Lobby Operations
    public async void CreateSession(string lobbyName = "MyLobby")
    {
#if debug
        Debug.Log($"<color=cyan><b>[LOBBY WORKFLOW]</b></color> 🚀 CreateSession called with name: <color=yellow>{lobbyName}</color>");
#endif

        if (!await PrepareForSessionCreation())
            return;

        // Show creating UI state
        ShowCreatingLobbyUI();

        // Use service to create session - UI updates handled by event handlers
        bool success = await sessionService.CreateSessionAsync(lobbyName, maxPlayers);
        
        if (success)
        {
            // Refresh available sessions after creation
            await FetchAvailableSessions(bypassRateLimit: true);
        }
    }


    private async Task<bool> PrepareForSessionCreation()
    {
        if (!IsNetworkManagerClean())
        {
#if debug
            Debug.LogWarning("<color=orange><b>[LOBBY WORKFLOW]</b></color> ⚠️ NetworkManager not in clean state, performing cleanup...");
#endif
            bool cleanupSuccess = await SafeShutdownNetworkManager();
            if (!cleanupSuccess)
            {
#if debug
                Debug.LogError("<color=red><b>[LOBBY WORKFLOW ERROR]</b></color> ❌ Failed to cleanup NetworkManager before session creation");
#endif
                return false;
            }
        }

#if debug
        Debug.Log("<color=cyan><b>[LOBBY WORKFLOW]</b></color> ✅ All validations passed, NetworkManager clean, starting session creation...");
#endif
        return true;
    }

    private void ShowCreatingLobbyUI()
    {
#if debug
        Debug.Log("<color=cyan><b>[LOBBY WORKFLOW]</b></color> 🎭 Showing 'Creating Lobby...' state");
#endif
        uiManager?.ShowCreatingLobbyState();
    }




    #endregion

    #region Session Joining Operations
    /// <summary>
    /// Joins a session by its ID using the session service.
    /// </summary>
    private async void JoinSessionById(string lobbyId)
    {
        await sessionService.JoinSessionAsync(lobbyId);
    }

    #endregion

    #region Network State Management
    /// <summary>
    /// Checks if NetworkManager is in a clean state for starting new operations
    /// </summary>
    private bool IsNetworkManagerClean()
    {
        if (NetworkManager.Singleton == null) return false;

        bool isClean = !NetworkManager.Singleton.IsClient &&
                      !NetworkManager.Singleton.IsServer &&
                      !NetworkManager.Singleton.IsHost;

#if debug
        Debug.Log($"<color=cyan><b>[NETWORK STATE]</b></color> NetworkManager clean check - IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}, IsHost: {NetworkManager.Singleton.IsHost}, Clean: {isClean}");
#endif

        return isClean;
    }

    /// <summary>
    /// Safely shuts down NetworkManager with proper cleanup
    /// </summary>
    private async Task<bool> SafeShutdownNetworkManager(float timeoutSeconds = 5f)
    {
        if (NetworkManager.Singleton == null) return true;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
#if debug
            Debug.Log("<color=cyan><b>[NETWORK STATE]</b></color> NetworkManager already clean, no shutdown needed");
#endif
            return true;
        }

#if debug
        Debug.Log("<color=orange><b>[NETWORK STATE]</b></color> Shutting down NetworkManager...");
#endif

        try
        {
            NetworkManager.Singleton.Shutdown();

            // Wait for clean shutdown with timeout
            float elapsed = 0f;
            while ((NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost) && elapsed < timeoutSeconds)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }

            bool success = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost;

#if debug
            if (success)
            {
                Debug.Log("<color=green><b>[NETWORK STATE]</b></color> NetworkManager shutdown successful");
            }
            else
            {
                Debug.LogWarning($"<color=red><b>[NETWORK STATE]</b></color> NetworkManager shutdown timeout after {timeoutSeconds}s");
            }
#endif

            return success;
        }
        catch (Exception ex)
        {
#if debug
            Debug.LogError($"<color=red><b>[NETWORK STATE ERROR]</b></color> Failed to shutdown NetworkManager: {ex.Message}");
#endif
            return false;
        }
    }

    /// <summary>
    /// Prepares NetworkManager for hosting by ensuring clean state
    /// </summary>
    private async Task<bool> PrepareNetworkManagerForHost()
    {
        if (NetworkManager.Singleton == null)
        {
#if debug
            Debug.LogError("<color=red><b>[NETWORK STATE ERROR]</b></color> NetworkManager.Singleton is null");
#endif
            return false;
        }

        if (IsNetworkManagerClean())
        {
#if debug
            Debug.Log("<color=green><b>[NETWORK STATE]</b></color> NetworkManager already clean, ready for host");
#endif
            return true;
        }

#if debug
        Debug.Log("<color=orange><b>[NETWORK STATE]</b></color> NetworkManager not clean, performing shutdown...");
#endif

        return await SafeShutdownNetworkManager();
    }
    #endregion

    #region Relay & Networking
    public async void HostStartGame()
    {
#if debug
        Debug.Log("<color=magenta><b>[HOST START]</b></color> 🚀 HostStartGame called");
#endif

        if (!ValidateHostStartConditions())
            return;

        uiManager?.ShowStartingGameState();

        try
        {
            if (!await PrepareNetworkManagerForHost())
            {
#if debug
                Debug.LogError("<color=red><b>[HOST START ERROR]</b></color> Failed to prepare NetworkManager for hosting");
#endif
                return;
            }

            var (allocation, joinCode) = await CreateRelayAllocation();
            ConfigureHostTransport(allocation);
            await UpdateSessionForGameStart(joinCode);
            StartNetworkHost();
            LoadGameScene();
        }
        catch (Exception ex)
        {
            HandleHostStartError(ex);
        }
        finally
        {
            // UI state will be reset when needed - remove this direct manipulation
        }
    }

    private bool ValidateHostStartConditions()
    {
        if (!sessionService.HasActiveSession)
        {
#if debug
            Debug.LogWarning("<color=red><b>[HOST START ERROR]</b></color> No session available to start the game");
#endif
            return false;
        }

#if debug
        Debug.Log("<color=magenta><b>[HOST START]</b></color> ✅ Session validation passed, preparing NetworkManager...");
#endif
        return true;
    }

    private async Task<(Unity.Services.Relay.Models.Allocation allocation, string joinCode)> CreateRelayAllocation()
    {
#if debug
        Debug.Log("<color=magenta><b>[HOST START]</b></color> 🌐 Creating relay allocation...");
#endif

        var allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

#if debug
        Debug.Log($"<color=magenta><b>[HOST START]</b></color> ✅ Relay allocation created. Join code: {joinCode}");
#endif

        return (allocation, joinCode);
    }

    private void ConfigureHostTransport(Unity.Services.Relay.Models.Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );
    }

    private async Task UpdateSessionForGameStart(string joinCode)
    {
#if debug
        Debug.Log("<color=magenta><b>[HOST START]</b></color> 🔧 Transport configured, updating session properties...");
#endif

        // Use session service to set properties
        await sessionService.SetSessionPropertyAsync("GameStarted", "true");
        await sessionService.SetSessionPropertyAsync("JoinCode", joinCode);

#if debug
        Debug.Log("<color=magenta><b>[HOST START]</b></color> 💾 Session properties saved, starting NetworkManager host...");
#endif
    }

    private void StartNetworkHost()
    {
        if (!NetworkManager.Singleton.StartHost())
        {
#if debug
            Debug.LogError("<color=red><b>[HOST START ERROR]</b></color> ❌ NetworkManager.StartHost() returned false");
#endif
            return;
        }

#if debug
        Debug.Log("<color=green><b>[HOST START SUCCESS]</b></color> 🎉 Host started successfully, loading game scene...");
        LogNetworkManagerState("Before Scene Load");
#endif
    }

    private void LoadGameScene()
    {
#if debug
        Debug.Log($"<color=cyan><b>[SCENE TRANSITION]</b></color> Loading game scene: {gameSceneName}");
        LogNetworkManagerState("Before LoadScene Call");
#endif

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

#if debug
        Debug.Log($"<color=cyan><b>[SCENE TRANSITION]</b></color> LoadScene call completed for: {gameSceneName}");
#endif
    }

    private void HandleHostStartError(Exception ex)
    {
#if debug
        Debug.LogError($"<color=red><b>[HOST START ERROR]</b></color> Exception during host startup: {ex.Message}");
        Debug.LogError($"<color=red><b>[HOST START ERROR]</b></color> Stack trace: {ex.StackTrace}");
#endif
    }


    public async Task StartPollingForGameStart()
    {
        int failureCount = 0;
        const int MAX_FAILURES = 5;

        while (ShouldContinuePolling(failureCount))
        {
            try
            {
                await RefreshSessionData();
                failureCount = 0; // Reset on success

                if (TryGetGameStartInfo(out string joinCode))
                {
                    await HandleGameStartDetected(joinCode);
                    break;
                }
            }
            catch (System.Exception ex)
            {
                failureCount = await HandlePollingError(ex, failureCount, MAX_FAILURES);
                if (failureCount >= MAX_FAILURES)
                    break;
                continue;
            }

            await Task.Delay(10000); // 10 second polling interval
        }

        if (failureCount >= MAX_FAILURES)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Lobby polling stopped due to repeated failures");
        }
    }

    private bool ShouldContinuePolling(int failureCount)
    {
        return sessionService.HasActiveSession && failureCount < 5;
    }

    private async Task RefreshSessionData()
    {
        await sessionService.RefreshSessionAsync();
    }

    private bool TryGetGameStartInfo(out string joinCode)
    {
        joinCode = null;

        string gameStarted = sessionService.GetSessionProperty("GameStarted");
        if (gameStarted == "true")
        {
            joinCode = sessionService.GetSessionProperty("JoinCode");
            if (!string.IsNullOrEmpty(joinCode))
            {
                return true;
            }
            else
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, "Game started but no JoinCode found in session properties");
            }
        }

        return false;
    }

    private async Task HandleGameStartDetected(string joinCode)
    {
        GameLogger.LogNetwork("GameStartDetected", "Joining game as client");
        await JoinRelayAsClient(joinCode);
    }

    private async Task<int> HandlePollingError(System.Exception ex, int currentFailureCount, int maxFailures)
    {
        int newFailureCount = currentFailureCount + 1;
        GameLogger.LogError(GameLogger.LogCategory.Network, $"Polling failed (attempt {newFailureCount}/{maxFailures}): {ex.Message}");

        if (newFailureCount >= maxFailures)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Max polling failures reached, stopping polling");
            return newFailureCount;
        }

        // Exponential backoff for retries
        int backoffDelay = (int)(10000 * Math.Pow(2, newFailureCount));
        await Task.Delay(Math.Min(backoffDelay, 60000)); // Cap at 60 seconds

        return newFailureCount;
    }


    #region Relay Client Operations
    /// <summary>
    /// Joins a relay server as a client using the provided join code
    /// </summary>
    /// <param name="joinCode">The relay join code from the host</param>
    private async Task JoinRelayAsClient(string joinCode)
    {
        try
        {
#if debug
            Debug.Log($"Joining relay with code: {joinCode}");
#endif
            var joinAllocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
            ConfigureClientTransport(joinAllocation);
            StartNetworkClient();
        }
        catch (SessionException)
        {
#if debug
            Debug.LogError("Failed to join relay");
#endif
        }
    }

    /// <summary>
    /// Configures the Unity Transport for client relay connection
    /// </summary>
    private void ConfigureClientTransport(Unity.Services.Relay.Models.JoinAllocation joinAllocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );
    }

    /// <summary>
    /// Starts the NetworkManager as a client
    /// </summary>
    private void StartNetworkClient()
    {
        if (!NetworkManager.Singleton.StartClient())
        {
#if debug
            Debug.LogError("Failed to start client.");
#endif
        }
#if debug
        Debug.Log("Started client with relay");
#endif
    }
    #endregion

    #region Unity Coroutines
    /// <summary>
    /// Waits for NetworkManager.Singleton to be initialized before proceeding
    /// </summary>
    private IEnumerator WaitForNetworkManagerReady()
    {
        while (NetworkManager.Singleton == null)
        {
#if debug
            Debug.Log("[LobbyManager] Waiting for NetworkManager.Singleton to initialize...");
#endif
            yield return null;
        }
#if debug
        Debug.Log("[LobbyManager] NetworkManager.Singleton is ready.");
#endif
    }
    #endregion

    #region Scene Navigation
    public void BackToPreviousScene()
    {
        if (sessionService.HasActiveSession)
        {
            LeaveSession();
        }

        // Reset UI to clean state before leaving
        uiManager?.ResetUIToLobbyState();

#if debug
        Debug.Log($"Returning to previous scene: {previousSceneName}");
#endif
        SceneManager.LoadScene(previousSceneName, LoadSceneMode.Single);
    }

    public async void LeaveSession()
    {
        await sessionService.LeaveSessionAsync();

        // Also ensure NetworkManager is clean when leaving session
        if (!IsNetworkManagerClean())
        {
#if debug
            Debug.Log("<color=orange><b>[LEAVE SESSION]</b></color> Cleaning up NetworkManager state...");
#endif
            _ = SafeShutdownNetworkManager(); // Fire and forget cleanup
        }
    }

    /// <summary>
    /// Logs comprehensive NetworkManager state for debugging scene transitions
    /// </summary>
    private void LogNetworkManagerState(string context)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError($"<color=red>[NETWORK STATE - {context}]</color> NetworkManager.Singleton is NULL!");
            return;
        }

        var nm = NetworkManager.Singleton;
        Debug.Log($"<color=yellow>[NETWORK STATE - {context}]</color> " +
                 $"IsServer: {nm.IsServer}, " +
                 $"IsClient: {nm.IsClient}, " +
                 $"IsHost: {nm.IsHost}, " +
                 $"IsConnectedClient: {nm.IsConnectedClient}, " +
                 $"ConnectedClients.Count: {nm.ConnectedClients.Count}, " +
                 $"NetworkManager.name: {nm.name}, " +
                 $"GameObject.instanceID: {nm.GetInstanceID()}, " +
                 $"DontDestroyOnLoad: {(nm.gameObject.scene.name == "DontDestroyOnLoad" ? "true" : "false")}");
    }
    #endregion
}
#endregion
