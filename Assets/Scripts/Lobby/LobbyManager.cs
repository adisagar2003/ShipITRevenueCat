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
    
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public List<Lobby> availableLobbies { get; private set; } = new List<Lobby>();
    public Lobby currentLobby;
    private bool shouldRefreshLobbies = true;
    private int maxPlayers = 2;
    public event Action OnLobbiesUpdated;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string gameSceneName = "RaceLevel";

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
            await Task.Delay(3000);
        }
    }

    public async Task FetchAvailableLobbies()
    {
        try
        {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
            availableLobbies = response.Results;
            OnLobbiesUpdated?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to fetch lobbies: {e}");
        }
    }

    [Obsolete]
    private void AddPlayerPrefab()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        // Register the prefab with Netcode
        NetworkManager.Singleton.AddNetworkPrefab(playerPrefab);
        Debug.Log("Player prefab added via AddNetworkPrefab.");

        // Assign it as the player prefab
        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
        Debug.Log("Player prefab set in NetworkManager configuration.");
    }

    public async void CreateLobby(string lobbyName = "MyLobby")
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "gameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, "") }
                }
            };
            currentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Created lobby: {currentLobby.Name}");
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

    public async void HostStartGame()
    {
        if (currentLobby == null)
        {
            Debug.LogWarning("No lobby to start the game.");
            return;
        }

        try
        {
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
            await Task.Delay(1000);
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
}
