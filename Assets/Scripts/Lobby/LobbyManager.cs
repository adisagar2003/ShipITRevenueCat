using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    #region Singleton & Events
    public static LobbyManager Instance { get; private set; }
    public event Action OnLobbiesUpdated;
    #endregion

    #region Public Properties
    public List<Lobby> availableLobbies { get; private set; } = new List<Lobby>();
    public Lobby currentLobby;
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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
            await FetchAvailableLobbies();
            await Task.Delay((int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 3000));
        }
    }

    public async Task FetchAvailableLobbies()
    {
        try
        {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
            availableLobbies = response.Results;
            OnLobbiesUpdated?.Invoke();
            // Hide "Creating Lobby..." text after player has created a lobby
            if (creatingLobbyText != null && !createLobbyButton.interactable)
                creatingLobbyText.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to fetch lobbies: {e}");
        }
    }


    #endregion

    #region Lobby Operations
    public async void CreateLobby(string lobbyName = "MyLobby")
    {
        try
        {
            // Show "Creating Lobby..." text
            if (creatingLobbyText != null)
                creatingLobbyText.SetActive(true);

            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "gameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, "") } // join code for relay
                }
            };
            currentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Created lobby: {currentLobby.Name}");

            // disable lobby button to prevent multiple creations
            if (createLobbyButton != null)
            {
                createLobbyButton.interactable = false;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"Joined lobby: {currentLobby.Name}");
            shouldRefreshLobbies = false;
            StartPollingForGameStart();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e}");
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

        try
        {
            // Show "Starting Game..." text
            if (startingGameText != null)
                startingGameText.SetActive(true);

            // Create Relay allocation
            Debug.Log("Creating relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            // Get the join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Got join code: {joinCode}");

            // Configure the transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Update the lobby with game started status and join code
            var updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "gameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true") },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);
            Debug.Log("Starting host with relay...");
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay service error: {e.Message}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby: {e}");
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
        while (currentLobby != null)
        {
            try
            {
                currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
                if (currentLobby != null && 
                    currentLobby.Data.TryGetValue("gameStarted", out DataObject gameStartedData) && 
                    gameStartedData.Value == "true" &&
                    currentLobby.Data.TryGetValue("joinCode", out DataObject joinCodeData) &&
                    !string.IsNullOrEmpty(joinCodeData.Value))
                {
                    Debug.Log($"Game started by host, joining via relay with code: {joinCodeData.Value}");
                    await JoinRelayAsClient(joinCodeData.Value);
                    break;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Polling failed: {e}");
                break;
            }
            await Task.Delay((int)(GameConstants.Networking.LOBBY_POLLING_INTERVAL * 1000));
        }
    }
    
    private async Task JoinRelayAsClient(string joinCode)
    {
        try
        {
            Debug.Log($"Joining relay with code: {joinCode}");
            
            // Join the relay with the given join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
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
                await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("Left lobby successfully");
            }
            catch (LobbyServiceException e)
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
