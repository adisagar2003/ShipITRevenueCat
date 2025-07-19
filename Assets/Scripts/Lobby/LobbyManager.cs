using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public List<Lobby> availableLobbies = new List<Lobby>();
    public Lobby currentLobby;

    public delegate void LobbiesUpdatedHandler();
    public event LobbiesUpdatedHandler OnLobbiesUpdated;

    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string gameSceneName = "RaceLevel";

    private Coroutine pollingCoroutine;

    [ContextMenu("Debug Current State")]
    private void DebugCurrentState()
    {
        Debug.Log($"=== LobbyManager Debug Info ===");
        Debug.Log($"Available Lobbies: {availableLobbies?.Count ?? 0}");
        Debug.Log($"Current Lobby: {(currentLobby != null ? currentLobby.Name : "None")}");
        Debug.Log($"Network Manager: {(networkManager != null ? "Assigned" : "NULL!")}");
        Debug.Log($"Game Scene Name: {gameSceneName}");
        Debug.Log($"Polling Active: {pollingCoroutine != null}");
        Debug.Log($"Player ID: {Unity.Services.Authentication.AuthenticationService.Instance?.PlayerId ?? "Not Authenticated"}");
        Debug.Log("================================");
    }

    private void OnEnable()
    {
        Debug.Log("LobbyManager enabled - starting polling");
        pollingCoroutine = StartCoroutine(PollLobbiesRoutine());
    }

    private void OnDisable()
    {
        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);
    }

    private IEnumerator PollLobbiesRoutine()
    {
        while (true)
        {
            // Start the async task without yielding it directly
            _ = GetAvailableLobbiesAsync();
            yield return new WaitForSeconds(3f); // Poll every 3 seconds
        }
    }

    [ContextMenu("Get all availableLobbies")]
    public async Task GetAvailableLobbiesAsync()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>(),
                Order = new List<QueryOrder>()
            };

            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
            availableLobbies = response.Results;
            OnLobbiesUpdated?.Invoke();

            Debug.Log($"Found {availableLobbies.Count} lobbies.");
            foreach (Lobby lobby in availableLobbies)
            {
                Debug.Log($"Lobby Name: {lobby.Name}, Lobby Code: {lobby.LobbyCode}");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to get lobbies: {e}");

            // Rate limit backoff
            if (e.Reason == LobbyExceptionReason.RateLimited)
            {
                Debug.LogWarning("Rate limit hit, backing off for 15 seconds.");
                await Task.Delay(15000);
            }
        }
    }

    
    public async void CreateLobbyAsync(string lobbyName = "MyLobby", int maxPlayers = 2)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
            {
                { "gameMode", new DataObject(DataObject.VisibilityOptions.Public, "casual") }
            }
            };

            Lobby createdLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            currentLobby = createdLobby; 

            Debug.Log($"Lobby created successfully!");
            Debug.Log($"Lobby Name: {createdLobby.Name}");
            Debug.Log($"Lobby ID: {createdLobby.Id}");
            Debug.Log($"Lobby Code: {createdLobby.LobbyCode}");
            await GetAvailableLobbiesAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    [ContextMenu("Create Button on click")]
    public void OnCreateLobbyButtonClicked()
    {
        CreateLobbyAsync();
    }

    public async Task JoinLobbyByIdAsync(string lobbyId)
    {
        try
        {
            currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"Joined lobby by ID: {currentLobby.Name}, Code: {currentLobby.LobbyCode}");
            
            // Start polling if not the host
            if (currentLobby.HostId != Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
            {
                StartPollingForGameStart();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by ID: {e}");
        }
    }

    public async Task JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"Joined lobby by Code: {currentLobby.Name}, Code: {currentLobby.LobbyCode}");
            
            // Start polling if not the host
            if (currentLobby.HostId != Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
            {
                StartPollingForGameStart();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by Code: {e}");
        }
    }

    public async Task LeaveLobbyAsync()
    {
        if (currentLobby == null)
        {
            Debug.LogWarning("No lobby to leave.");
            return;
        }
        try
        {
            await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);
            Debug.Log($"Left lobby: {currentLobby.Name}");
            currentLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to leave lobby: {e}");
        }
    }


    public async void StartGameFromLobby()
    {
        if (currentLobby == null) return;
        if (currentLobby.HostId != Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
        {
            Debug.Log("Only the host can start the game!");
            return;
        }

        Debug.Log("Starting game as host...");
        networkManager.StartHost();
        await Task.Delay(1000);
        await UpdateLobbyWithGameInfo();
        
        SceneManager.LoadScene(gameSceneName);
    }

    private async Task UpdateLobbyWithGameInfo()
    {
        try
        {
            // Get the host's IP address (you'll need to implement this)
            string hostIP = GetLocalIPAddress();
            
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "gameStarted", new DataObject(DataObject.VisibilityOptions.Public, "true") },
                    { "hostIP", new DataObject(DataObject.VisibilityOptions.Public, hostIP) },
                    { "gamePort", new DataObject(DataObject.VisibilityOptions.Public, "7777") } // Default NGO port
                }
            };
            
            await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, options);
            Debug.Log($"Updated lobby with game info: {hostIP}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby: {e}");
        }
    }

    private string GetLocalIPAddress()
    {
        // Get local IP address for other players to connect to
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not get local IP: {e.Message}");
        }
        return "127.0.0.1"; // Fallback to localhost
    }

    public async void StartPollingForGameStart()
    {
        if (currentLobby == null) return;
        
        if (currentLobby.HostId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
        {
            return;
        }
        
        Debug.Log("Started polling for game start...");
        
        while (currentLobby != null)
        {
            try
            {
                currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);
                
                if (currentLobby.Data.ContainsKey("gameStarted") && 
                    currentLobby.Data["gameStarted"].Value == "true")
                {
                    Debug.Log("Game has started! Connecting to host...");
                    await JoinHostGame();
                    break;
                }
                
                await Task.Delay(1000); // Check every second
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to poll lobby: {e}");
                break;
            }
        }
    }

    private async Task JoinHostGame()
    {
        try
        {
            string hostIP = currentLobby.Data["hostIP"].Value;
            string gamePort = currentLobby.Data["gamePort"].Value;
            
            Debug.Log($"Connecting to host at {hostIP}:{gamePort}");
            
            // Get the UnityTransport component
            var transport = networkManager.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(hostIP, ushort.Parse(gamePort));
                networkManager.StartClient();
                await Task.Delay(2000);
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("UnityTransport component not found on NetworkManager!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join host game: {e}");
        }
    }
}
