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
using UnityEngine.UI;

/// <summary>
/// Manages multiplayer lobby creation, joining, and relay connectivity.
/// Compatible with Unity 6 Multiplayer Services package - uses individual Lobby/Relay services
/// which are wrapped by the unified package for backward compatibility.
/// </summary>
public class LobbyManager : ThreadSafeSingleton<LobbyManager>
{
    #region Singleton & Events - Singleton handled by ThreadSafeSingleton base class
    public event Action OnLobbiesUpdated;
    #endregion

    #region Public Properties
    public List<ILobby> availableLobbies { get; private set; } = new List<ILobby>();
    public ILobby currentLobby;
    #endregion

    #region Private Fields
    private bool shouldRefreshLobbies = true;
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
        shouldRefreshLobbies = false;
        
        // Leave current lobby if in one
        if (currentLobby != null)
        {
            _ = LeaveLobbyOnDestroy();
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbyManager disposed");
        base.OnSingletonDestroyed();
    }
    
    private async Task LeaveLobbyOnDestroy()
    {
        try
        {
            if (currentLobby != null && AuthenticationService.Instance.IsSignedIn)
            {
                await MultiplayerService.Instance.LeaveLobbyAsync(currentLobby.Id);
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "Left lobby during cleanup");
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
            shouldRefreshLobbies = false;
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Lobby operations paused");
        }
        else
        {
            // Resume lobby operations if we were refreshing
            if (currentLobby == null) // Only resume if not in a specific lobby
            {
                shouldRefreshLobbies = true;
                _ = RefreshLobbiesLoop();
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "Lobby operations resumed");
            }
        }
    }

    private async void Start()
    {
        await WaitForGameInitializer();
        Debug.Log("LobbyManager ready.");
        StartCoroutine(WaitForNetworkManagerReady());
        _ = RefreshLobbiesLoop();
    }

    #endregion

    #region Lobby Management
    private async Task WaitForGameInitializer()
    {
        while (!GameInitializer.IsInitialized)
            await Task.Delay(100);
    }

    private async Task RefreshLobbiesLoop()
    {
        while (shouldRefreshLobbies)
        {
            try
            {
                await FetchAvailableLobbies();
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

    public async Task FetchAvailableLobbies()
    {
        try
        {
            var response = await MultiplayerService.Instance.GetAvailableLobbiesAsync();
            
            if (response?.Results != null)
            {
                availableLobbies = response.Results;
                OnLobbiesUpdated?.Invoke();
                
                // Hide "Creating Lobby..." text after player has created a lobby
                if (creatingLobbyText != null && createLobbyButton != null && !createLobbyButton.interactable)
                    creatingLobbyText.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Query response or results is null");
                availableLobbies?.Clear();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to fetch lobbies: {e.Message} (Reason: {e.Reason})");
            
            // Clear lobbies on error to avoid showing stale data
            availableLobbies?.Clear();
            OnLobbiesUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error fetching lobbies: {e.Message}");
            
            // Clear lobbies on error to avoid showing stale data
            availableLobbies?.Clear();
            OnLobbiesUpdated?.Invoke();
        }
    }


    #endregion

    #region Lobby Operations
    public async void CreateLobby(string lobbyName = "MyLobby")
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogError("Lobby name cannot be null or empty");
            return;
        }

        if (currentLobby != null)
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
            currentLobby = await MultiplayerService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            Debug.Log($"Created lobby: {currentLobby.Name}");

            // disable lobby button to prevent multiple creations
            if (createLobbyButton != null)
            {
                createLobbyButton.interactable = false;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message} (Reason: {e.Reason})");
            
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

    public async void JoinLobbyById(string lobbyId)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogError("Lobby ID cannot be null or empty");
            return;
        }

        if (currentLobby != null)
        {
            Debug.LogWarning("Already in a lobby, leave current lobby first");
            return;
        }

        try
        {
            currentLobby = await MultiplayerService.Instance.JoinLobbyAsync(lobbyId);
            Debug.Log($"Joined lobby: {currentLobby.Name}");
            shouldRefreshLobbies = false;
            StartPollingForGameStart();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message} (Reason: {e.Reason})");
            
            // Reset state if join failed
            currentLobby = null;
            shouldRefreshLobbies = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error joining lobby: {e.Message}");
            
            // Reset state if join failed
            currentLobby = null;
            shouldRefreshLobbies = true;
        }
    }

    #endregion

    #region Relay & Networking
    public async void HostStartGame()
    {
        if (currentLobby == null)
        {           
            Debug.LogWarning("No lobby to start the game.");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null, cannot start host");
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Game scene name is not set");
            return;
        }

        try
        {
            // Show "Starting Game..." text
            if (startingGameText != null)
                startingGameText.SetActive(true);

            // Create Relay allocation
            Debug.Log("Creating relay allocation...");
            var allocation = await MultiplayerService.Instance.CreateRelayAllocationAsync(maxPlayers - 1);

            if (allocation == null)
            {
                throw new InvalidOperationException("Failed to create relay allocation");
            }

            // Get the join code
            string joinCode = await MultiplayerService.Instance.GetRelayJoinCodeAsync(allocation.AllocationId);
            
            if (string.IsNullOrEmpty(joinCode))
            {
                throw new InvalidOperationException("Failed to get join code from relay");
            }
            
            Debug.Log($"Got join code: {joinCode}");

            // Configure the transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                throw new InvalidOperationException("UnityTransport component not found on NetworkManager");
            }
            
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Update the lobby with game started status and join code

            await MultiplayerService.Instance.UpdateLobbyAsync(currentLobby.Id, joinCode, true);
            Debug.Log("Starting host with relay...");
            
            if (!NetworkManager.Singleton.StartHost())
            {
                throw new InvalidOperationException("Failed to start NetworkManager as host");
            }
            
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay service error: {e.Message} (Reason: {e.Reason})");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby: {e.Message} (Reason: {e.Reason})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected error starting host game: {e.Message}");
        }
        finally
        {
            // Hide "Starting Game..." text after attempt (success or fail)
            if (startingGameText != null)
                startingGameText.SetActive(false);
        }
    }

    private async void StartPollingForGameStart()
    {
        int failureCount = 0;
        const int MAX_FAILURES = 5;
        
        while (currentLobby != null && failureCount < MAX_FAILURES)
        {
            try
            {
                currentLobby = await MultiplayerService.Instance.GetLobbyAsync(currentLobby.Id);
                
                // Reset failure count on successful request
                failureCount = 0;
                
                if (currentLobby != null && currentLobby.IsGameStarted)
                {
                    GameLogger.LogNetwork("GameStartDetected", "Joining game as client");
                    await JoinRelayAsClient(currentLobby.JoinCode);
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
            var joinAllocation = await MultiplayerService.Instance.JoinRelayAllocationAsync(joinCode);
            
            // Configure the transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            // Start the client
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started client with relay");
        }
        catch (RelayServiceException e)
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
        if (currentLobby != null)
        {
            LeaveLobby();
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

    public async void LeaveLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                await MultiplayerService.Instance.LeaveLobbyAsync(currentLobby.Id);
                Debug.Log("Left lobby successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to leave lobby: {e}");
            }
            finally
            {
                currentLobby = null;
                shouldRefreshLobbies = true; // Re-enable lobby browsing
            }
        }
    }
    #endregion
}
