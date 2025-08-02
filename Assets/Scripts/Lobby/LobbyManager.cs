using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
/// <summary>
/// Manages multiplayer lobby creation, joining, and relay connectivity.
/// Compatible with Unity 6 Multiplayer Services package - uses the unified Sessions approach.
/// Uses ISession interf ace for session management instead of deprecated Lobby API.
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

    #region Unity Lifecycle

    protected override void Initialize()
    {
        base.Initialize();
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager initialized");
    }

    protected override void OnSingletonDestroyed()
    {
        // Stop lobby operations
        shouldRefreshSessions = false;

        // Leave current session if in one
        if (currentSession != null)
        {
            _ = LeaveSessionOnDestroy();
        }

        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager disposed");
        base.OnSingletonDestroyed();
    }

    private async Task LeaveSessionOnDestroy()
    {
        try
        {
            if (currentSession != null && AuthenticationService.Instance.IsSignedIn)
            {
                await currentSession.LeaveAsync();
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "Left session during cleanup");
            }
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, $"Failed to leave lobby during cleanup: {ex.Message}");
        }
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
            if (currentSession == null) // Only resume if not in a specific session
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
        Debug.Log("LobbyManager ready.");
        StartCoroutine(WaitForNetworkManagerReady());
        _ = RefreshSessionsLoop();
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
        while (shouldRefreshSessions)
        {
            try
            {
                await FetchAvailableSessions();
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, $"Error in lobby refresh loop: {ex.Message}");

                // Use exponential backoff on errors to avoid spam
                await Task.Delay((int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 6000));
                continue;
            }

            await Task.Delay((int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 3000));
        }
    }

    public async Task FetchAvailableSessions()
    {
        try
        {
            var response = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

            if (response?.Sessions != null)
            {
                availableSessions = response.Sessions.ToList();
                OnSessionsUpdated?.Invoke();

                // Hide "Creating Lobby..." text after player has created a lobby
                if (creatingLobbyText != null && createLobbyButton != null && !createLobbyButton.interactable)
                    creatingLobbyText.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Query response or results is null");
                availableSessions?.Clear();
            }
        }
        catch (SessionException e)
        {
            Debug.LogError($"Failed to fetch sessions: {e.Message}");

            // Clear sessions on error to avoid showing stale data
            availableSessions?.Clear();
            OnSessionsUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error fetching sessions: {e.Message}");

            // Clear sessions on error to avoid showing stale data
            availableSessions?.Clear();
            OnSessionsUpdated?.Invoke();
        }
    }


    #endregion

    #region Lobby Operations
    public async void CreateSession(string lobbyName = "MyLobby")
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogError("Lobby name cannot be null or empty");
            return;
        }

        if (currentSession != null)
        {
            Debug.LogWarning("Already in a lobby, cannot create another");
            return;
        }

        try
        {
            // Show "Creating Lobby..." text
            if (creatingLobbyText != null)
                creatingLobbyText.SetActive(true);

            // Lobby options will be set through the simplified API
            var sessionOptions = new SessionOptions(){ Name = lobbyName, MaxPlayers = maxPlayers };
            currentSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            Debug.Log($"Created lobby: {currentSession.Name}");

            // disable lobby button to prevent multiple creations
            if (createLobbyButton != null)
            {
                createLobbyButton.interactable = false;
            }
        }
        catch (SessionException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");

            // Reset UI state on failure
            if (creatingLobbyText != null)
                creatingLobbyText.SetActive(false);
            if (createLobbyButton != null)
                createLobbyButton.interactable = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error creating lobby: {e.Message}");

            // Reset UI state on failure
            if (creatingLobbyText != null)
                creatingLobbyText.SetActive(false);
            if (createLobbyButton != null)
                createLobbyButton.interactable = true;
        }
    }

    private async void JoinSessionById(string lobbyId)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogError("Lobby ID cannot be null or empty");
            return;
        }

        if (currentSession != null)
        {
            Debug.LogWarning("Already in a lobby, leave current lobby first");
            return;
        }

        try
        {
            currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(lobbyId);
            Debug.Log($"Joined lobby: {currentSession.Name}");
            shouldRefreshSessions = false;
            StartPollingForGameStart();
        }
        catch (SessionException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");

            // Reset state if join failed
            currentSession = null;
            shouldRefreshSessions = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error joining lobby: {e.Message}");

            // Reset state if join failed
            currentSession = null;
            shouldRefreshSessions = true;
        }
    }

    #endregion
    #region Relay & Networking
        public async void HostStartGame()
        {
            if (currentSession == null)
            {
                Debug.LogWarning("No session available to start the game.");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null, cannot start host.");
                return;
            }

            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("Game scene name is not set.");
                return;
            }

            try
            {
                startingGameText?.SetActive(true);

                // Step 1: Create a Relay Allocation
                Debug.Log("Creating relay allocation...");
#pragma warning disable CS0436 // Type conflicts with imported type
                var allocation = await Unity.Services.Relay.Relay.Instance.CreateAllocationAsync(maxPlayers - 1);
#pragma warning restore CS0436

                if (allocation == null)
                    throw new InvalidOperationException("Failed to create relay allocation.");

                // Step 2: Get the Join Code
                // Use reflection to avoid compile-time ambiguity for RelayService
                var relayServiceType = System.Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay");
                var relayServiceInstance = relayServiceType?.GetProperty("Instance")?.GetValue(null);
                var getJoinCodeMethod = relayServiceType?.GetMethod("GetJoinCodeAsync", new[] { typeof(System.Guid) });
                var joinCodeTask = getJoinCodeMethod?.Invoke(relayServiceInstance, new object[] { allocation.AllocationId });
                string joinCode = await (System.Threading.Tasks.Task<string>)joinCodeTask;

                if (string.IsNullOrEmpty(joinCode))
                    throw new InvalidOperationException("Failed to retrieve join code.");

                Debug.Log($"Join code: {joinCode}");

                // Step 3: Configure Unity Transport for Relay
                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                if (transport == null)
                    throw new InvalidOperationException("UnityTransport component not found.");

                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );
                var hostSession = currentSession.AsHost();

                hostSession.SetProperty("GameStarted", new SessionProperty("true", VisibilityPropertyOptions.Public));
                hostSession.SetProperty("JoinCode", new SessionProperty(joinCode, VisibilityPropertyOptions.Public));

                await hostSession.SavePropertiesAsync();
                Debug.Log("Session updated with game started and join code.");


                Debug.Log("Session updated with game started and join code.");

                // Step 5: Start host and load the game scene
                if (!NetworkManager.Singleton.StartHost())
                    throw new InvalidOperationException("Failed to start host.");

                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
            catch (SessionException se)
            {
                Debug.LogError($"Session error: {se.Message}");
            }
            catch (System.Exception re) when (re.GetType().Name == "RelayServiceException")
            {
                Debug.LogError($"Relay error: {re.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error starting host: {ex.Message}");
            }
            finally
            {
                startingGameText?.SetActive(false);
            }
        }

        private async void StartPollingForGameStart()
        {
            int failureCount = 0;
            const int MAX_FAILURES = 5;

            while (currentSession != null && failureCount < MAX_FAILURES)
            {
                try
                {
                    List<string> currentLobbiesPlayerIsIn = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
                    currentSession = MultiplayerService.Instance.Sessions[currentLobbiesPlayerIsIn[0]];
                    // Reset failure count on successful request
                    failureCount = 0;

                    if (currentSession != null && currentSession.Properties.TryGetValue("GameStarted", out var gameStarted) && gameStarted.Value == "true")
                    {
                        GameLogger.LogNetwork("GameStartDetected", "Joining game as client");
                        if (currentSession.Properties.TryGetValue("joinCode", out var joinCode))
                        {
                            await JoinRelayAsClient(joinCode.Value);
                        }
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    failureCount++;
                    GameLogger.LogError(GameLogger.LogCategory.Network, $"Polling failed (attempt {failureCount}/{MAX_FAILURES}): {e.Message}");

                    if (failureCount >= MAX_FAILURES)
                    {
                        GameLogger.LogError(GameLogger.LogCategory.Network, "Max polling failures reached, stopping polling");
                        break;
                    }

                    // Use exponential backoff for retries
                    int backoffDelay = (int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 1000 * Math.Pow(2, failureCount));
                    await Task.Delay(Math.Min(backoffDelay, 30000)); // Cap at 30 seconds
                    continue;
                }

                await Task.Delay((int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 1000));
            }

            if (failureCount >= MAX_FAILURES)
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, "Lobby polling stopped due to repeated failures");
            }
        }

        private async Task JoinRelayAsClient(string joinCode)
        {
            try
            {
                Debug.Log($"Joining relay with code: {joinCode}");

                // Join the relay with the given join code
                // Use reflection to avoid compile-time ambiguity
                var relayServiceType = System.Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay");
                if (relayServiceType != null)
                {
                    var instance = relayServiceType.GetProperty("Instance")?.GetValue(null);
                    var method = relayServiceType.GetMethod("JoinAllocationAsync", new[] { typeof(string) });
                    if (instance != null && method != null)
                    {
                        var taskResult = method.Invoke(instance, new object[] { joinCode });
                        if (taskResult is System.Threading.Tasks.Task task)
                        {
                            await task.ConfigureAwait(false);
                            
                            // Get the result from the task
                            var resultProperty = task.GetType().GetProperty("Result");
                            var joinAllocation = resultProperty?.GetValue(task);
                            
                            if (joinAllocation != null)
                            {
                                // Use reflection to access properties since we can't cast to the ambiguous type
                                var relayServerProp = joinAllocation.GetType().GetProperty("RelayServer");
                                var relayServer = relayServerProp?.GetValue(joinAllocation);
                                
                                var ipProp = relayServer?.GetType().GetProperty("IpV4");
                                var portProp = relayServer?.GetType().GetProperty("Port");
                                var allocIdProp = joinAllocation.GetType().GetProperty("AllocationIdBytes");
                                var keyProp = joinAllocation.GetType().GetProperty("Key");
                                var connDataProp = joinAllocation.GetType().GetProperty("ConnectionData");
                                var hostConnDataProp = joinAllocation.GetType().GetProperty("HostConnectionData");
                                
                                // Configure the transport
                                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                                transport.SetClientRelayData(
                                    (string)ipProp?.GetValue(relayServer),
                                    (ushort)(int)portProp?.GetValue(relayServer),
                                    (byte[])allocIdProp?.GetValue(joinAllocation),
                                    (byte[])keyProp?.GetValue(joinAllocation),
                                    (byte[])connDataProp?.GetValue(joinAllocation),
                                    (byte[])hostConnDataProp?.GetValue(joinAllocation)
                                );
                            }
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException("Could not resolve RelayService type");
                }

                // Start the client
                NetworkManager.Singleton.StartClient();
                Debug.Log("Started client with relay");
            }
            catch (SessionException e)
            {
                Debug.LogError($"Failed to join relay: {e.Message}");
            }
        }

        private IEnumerator WaitForNetworkManagerReady()
        {
            while (NetworkManager.Singleton == null)
            {
                Debug.Log("[LobbyManager] Waiting for NetworkManager.Singleton to initialize...");
                yield return null; // check every frame
            }
            Debug.Log("[LobbyManager] NetworkManager.Singleton is ready.");
        }

        #endregion


    #region Scene Navigation
    public void BackToPreviousScene()
    {
        if (currentSession != null)
        {
            LeaveSession();
        }

        if (createLobbyButton != null)
        {
            createLobbyButton.interactable = true;
        }

        if (creatingLobbyText != null)
            creatingLobbyText.SetActive(false);

        if (startingGameText != null)
            startingGameText.SetActive(false);

        Debug.Log($"Returning to previous scene: {previousSceneName}");
        SceneManager.LoadScene(previousSceneName, LoadSceneMode.Single);
    }

    public async void LeaveSession()
    {
        if (currentSession != null)
        {
            try
            {
                await currentSession.LeaveAsync();
                Debug.Log("Left lobby successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to leave lobby: {e}");
            }
            finally
            {
                currentSession = null;
                shouldRefreshSessions = true; // Re-enable lobby browsing
            }
        }
    }
    #endregion
}
