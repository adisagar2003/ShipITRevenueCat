using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
    private int maxPlayers = GameConstants.Networking.DEFAULT_MAX_PLAYERS;
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

    #region Scene Management
    private string currentSceneName;
    private bool isSceneTransitioning = false;
    #endregion

    #region UI Management
    private LobbyUIManager uiManager;
    #endregion

    #region Services
    public LobbySessionService sessionService;
    private LobbyNetworkService networkService;
    private LobbyPollingService pollingService;
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
        networkService = LobbyNetworkService.Instance;
        pollingService = LobbyPollingService.Instance;
        
        // Subscribe to session service events for UI updates
        sessionService.OnSessionCreated += OnSessionCreatedByService;
        sessionService.OnSessionJoined += OnSessionJoinedByService;
        sessionService.OnSessionLeft += OnSessionLeftByService;
        sessionService.OnSessionError += OnSessionErrorByService;
        
        // Subscribe to network service events
        networkService.OnNetworkHostStarted += OnNetworkHostStartedByService;
        networkService.OnNetworkClientStarted += OnNetworkClientStartedByService;
        networkService.OnNetworkShutdown += OnNetworkShutdownByService;
        networkService.OnNetworkError += OnNetworkErrorByService;
        
        // Subscribe to polling service events
        pollingService.OnSessionsUpdated += OnSessionsUpdatedByService;
        pollingService.OnGameStartDetected += OnGameStartDetectedByService;
        pollingService.OnPollingError += OnPollingErrorByService;
        
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
        pollingService.StopSessionPolling(); // Stop polling when we have a session
        GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Session created successfully: {session.Name}");
    }

    /// <summary>
    /// Handles session join success from the service.
    /// </summary>
    private void OnSessionJoinedByService(ISession session)
    {
        currentSession = session;
        pollingService.StopSessionPolling(); // Stop session discovery polling
        _ = pollingService.StartPollingForGameStartAsync(sessionService); // Start polling for game start
        GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Joined session successfully: {session.Name}");
    }

    /// <summary>
    /// Handles session leave from the service.
    /// </summary>
    private void OnSessionLeftByService()
    {
        currentSession = null;
        pollingService.StopPollingForGameStart(); // Stop game start polling
        _ = pollingService.StartSessionPollingAsync(sessionService); // Resume session browsing
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

    /// <summary>
    /// Handles network host startup from the service.
    /// </summary>
    private void OnNetworkHostStartedByService()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Network host started successfully");
    }

    /// <summary>
    /// Handles network client startup from the service.
    /// </summary>
    private void OnNetworkClientStartedByService()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Network client started successfully");
    }

    /// <summary>
    /// Handles network shutdown from the service.
    /// </summary>
    private void OnNetworkShutdownByService()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Network shutdown completed");
    }

    /// <summary>
    /// Handles network errors from the service.
    /// </summary>
    private void OnNetworkErrorByService(string errorMessage)
    {
        GameLogger.LogError(GameLogger.LogCategory.Network, $"Network service error: {errorMessage}");
        uiManager?.ShowLobbyCreationFailedState();
    }

    /// <summary>
    /// Handles sessions list updates from the polling service.
    /// </summary>
    private void OnSessionsUpdatedByService(List<ISessionInfo> sessions)
    {
        availableSessions = sessions;
        OnSessionsUpdated?.Invoke();
        GameLogger.LogDebug(GameLogger.LogCategory.Network, $"Sessions updated: {sessions.Count} available");
    }

    /// <summary>
    /// Handles game start detection from the polling service.
    /// </summary>
    private void OnGameStartDetectedByService(string joinCode)
    {
        GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Game start detected, joining with code: {joinCode}");
        _ = networkService.JoinAsClientAsync(joinCode);
    }

    /// <summary>
    /// Handles polling errors from the service.
    /// </summary>
    private void OnPollingErrorByService(string errorMessage)
    {
        GameLogger.LogWarning(GameLogger.LogCategory.Network, $"Polling service error: {errorMessage}");
    }

    protected override void OnSingletonDestroyed()
    {
        // Stop lobby operations - handled by service cleanup

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

        if (networkService != null)
        {
            networkService.OnNetworkHostStarted -= OnNetworkHostStartedByService;
            networkService.OnNetworkClientStarted -= OnNetworkClientStartedByService;
            networkService.OnNetworkShutdown -= OnNetworkShutdownByService;
            networkService.OnNetworkError -= OnNetworkErrorByService;
            
            // Clean up network service
            networkService.Cleanup();
        }

        if (pollingService != null)
        {
            pollingService.OnSessionsUpdated -= OnSessionsUpdatedByService;
            pollingService.OnGameStartDetected -= OnGameStartDetectedByService;
            pollingService.OnPollingError -= OnPollingErrorByService;
            
            // Clean up polling service
            pollingService.Cleanup();
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager disposed");
        base.OnSingletonDestroyed();
    }


    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Temporarily stop lobby refresh to save battery/data
            pollingService.PausePolling();
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Lobby operations paused");
        }
        else
        {
            // Resume lobby operations if we were refreshing
            _ = pollingService.ResumePollingAsync(sessionService);
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Session operations resumed");
        }
    }

    private async void Start()
    {
        await WaitForGameInitializer();
#if debug
        Debug.Log("LobbyManager ready.");
#endif
        StartCoroutine(WaitForNetworkManagerReady());
        _ = pollingService.StartSessionPollingAsync(sessionService);
    }

    private void OnEnable()
    {
        // Detect scene changes and refresh UI accordingly
        DetectSceneChange();
        
        // Refresh UI references when returning to lobby scene
        // This fixes the DontDestroyOnLoad stale reference issue
        RefreshUIReferences();
        
        // Validate UI references after refresh
        ValidateAndLogUIState();
        
        // Reset UI state when returning to lobby scene
        // This fixes the "Creating Lobby..." persistence issue
        if (uiManager != null)
        {
            uiManager.ResetUIToLobbyState();
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI state reset on scene enable");
        }
    }
    
    /// <summary>
    /// Detects scene changes and sets appropriate flags for UI management.
    /// </summary>
    private void DetectSceneChange()
    {
        string newSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (currentSceneName != newSceneName)
        {
            string previousScene = currentSceneName ?? "Unknown";
            currentSceneName = newSceneName;
            isSceneTransitioning = false; // Reset transition flag
            
            GameLogger.LogInfo(GameLogger.LogCategory.UI, 
                $"Scene change detected: {previousScene} -> {newSceneName}");
                
            // If we're entering a lobby-related scene, mark for UI refresh
            if (IsLobbyScene(newSceneName))
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, "Entering lobby scene - will refresh UI");
            }
        }
    }
    
    /// <summary>
    /// Determines if the current scene is a lobby-related scene.
    /// </summary>
    private bool IsLobbyScene(string sceneName)
    {
        return sceneName.Contains("Lobby", System.StringComparison.OrdinalIgnoreCase) ||
               sceneName.Contains("Host", System.StringComparison.OrdinalIgnoreCase) ||
               sceneName == "LobbyandHost"; // Specific to this project
    }
    
    /// <summary>
    /// Validates UI state and logs current status for debugging.
    /// </summary>
    private void ValidateAndLogUIState()
    {
        if (uiManager != null)
        {
            bool isValid = uiManager.ValidateUIReferences();
            if (!isValid)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.UI, 
                    "UI references validation failed - some UI elements may not work correctly");
            }
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "UIManager is null");
        }
    }

    #endregion

    #region UI Reference Management
    /// <summary>
    /// Refreshes UI references when returning to the lobby scene.
    /// This fixes the DontDestroyOnLoad stale reference issue.
    /// </summary>
    private void RefreshUIReferences()
    {
        // Find UI elements by name in the current scene
        Button foundCreateButton = FindUIElementByName<Button>("CreateLobbyButton");
        Transform foundCreatingTextTransform = FindUIElementByName<Transform>("CreatingLobbyText");
        Transform foundStartingTextTransform = FindUIElementByName<Transform>("StartingGameText");
        
        // Convert transforms to GameObjects
        GameObject foundCreatingText = foundCreatingTextTransform?.gameObject;
        GameObject foundStartingText = foundStartingTextTransform?.gameObject;
        
        // Update references if found
        bool referencesUpdated = false;
        
        if (foundCreateButton != null && createLobbyButton != foundCreateButton)
        {
            createLobbyButton = foundCreateButton;
            referencesUpdated = true;
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "Updated createLobbyButton reference");
        }
        
        if (foundCreatingText != null && creatingLobbyText != foundCreatingText)
        {
            creatingLobbyText = foundCreatingText;
            referencesUpdated = true;
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "Updated creatingLobbyText reference");
        }
        
        if (foundStartingText != null && startingGameText != foundStartingText)
        {
            startingGameText = foundStartingText;
            referencesUpdated = true;
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "Updated startingGameText reference");
        }
        
        // Update UI manager references if any were updated
        if (referencesUpdated && uiManager != null)
        {
            uiManager.SetupUIReferences(createLobbyButton, creatingLobbyText, startingGameText);
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "UI references refreshed successfully");
        }
        else if (!referencesUpdated)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "No UI reference updates needed");
        }
    }
    
    /// <summary>
    /// Finds a UI element by name in the current scene.
    /// Uses multiple search strategies for robust discovery.
    /// </summary>
    private T FindUIElementByName<T>(string elementName) where T : Component
    {
        // Strategy 1: Find by exact GameObject name
        GameObject foundObject = GameObject.Find(elementName);
        if (foundObject != null)
        {
            if (foundObject.TryGetComponent<T>(out T component))
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found {elementName} by exact name search");
                return component;
            }
        }
        
        // Strategy 2: Find in all objects of type T and match by name
        T[] allComponents = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T component in allComponents)
        {
            if (component.gameObject.name.Contains(elementName) || 
                component.gameObject.name.Equals(elementName, System.StringComparison.OrdinalIgnoreCase))
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found {elementName} by component search");
                return component;
            }
        }
        
        // Strategy 3: Find by tag (if element has appropriate tag)
        string tagName = GetTagForUIElement(elementName);
        if (!string.IsNullOrEmpty(tagName))
        {
            GameObject taggedObject = GameObject.FindGameObjectWithTag(tagName);
            if (taggedObject != null)
            {
                if (taggedObject.TryGetComponent<T>(out T component))
                {
                    GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found {elementName} by tag search: {tagName}");
                    return component;
                }
            }
        }
        
        GameLogger.LogWarning(GameLogger.LogCategory.UI, $"Could not find UI element: {elementName}");
        return null;
    }
    
    /// <summary>
    /// Maps UI element names to their expected tags for fallback search.
    /// </summary>
    private string GetTagForUIElement(string elementName)
    {
        return elementName switch
        {
            "CreateLobbyButton" => "CreateLobbyButton",
            "CreatingLobbyText" => "CreatingLobbyText",
            "StartingGameText" => "StartingGameText",
            _ => null
        };
    }
    
    #endregion

    #region Lobby Management
    private async Task WaitForGameInitializer()
    {
        while (!GameInitializer.IsInitialized)
            await Task.Delay(100);
    }

    #endregion

    #region Manual Refresh
    [ContextMenu("Manual Refresh Sessions")]
    public async void ManualRefreshSessions()
    {
#if debug
        Debug.Log("<color=yellow><b>[MANUAL REFRESH]</b></color> 🔄 Manual session refresh requested");
#endif
        await pollingService.ManualRefreshSessionsAsync();
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
            await pollingService.FetchAvailableSessionsAsync(bypassRateLimit: true);
        }
    }


    private async Task<bool> PrepareForSessionCreation()
    {
        if (!networkService.IsNetworkManagerClean)
        {
#if debug
            Debug.LogWarning("<color=orange><b>[LOBBY WORKFLOW]</b></color> ⚠️ NetworkManager not in clean state, performing cleanup...");
#endif
            bool cleanupSuccess = await networkService.SafeShutdownNetworkManagerAsync();
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
            // Use network service to start host with relay
            string joinCode = await networkService.StartHostWithRelayAsync(maxPlayers);
            
            // Update session with game start info
            await UpdateSessionForGameStart(joinCode);
            
            // Load game scene
            LoadGameScene();
        }
        catch (Exception ex)
        {
            HandleHostStartError(ex);
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


    private void LoadGameScene()
    {
#if debug
        Debug.Log($"<color=cyan><b>[SCENE TRANSITION]</b></color> Loading game scene: {gameSceneName}");
        networkService.LogNetworkManagerState("Before LoadScene Call");
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
        if (!networkService.IsNetworkManagerClean)
        {
#if debug
            Debug.Log("<color=orange><b>[LEAVE SESSION]</b></color> Cleaning up NetworkManager state...");
#endif
            _ = networkService.SafeShutdownNetworkManagerAsync(); // Fire and forget cleanup
        }
    }

    #endregion
}
#endregion
