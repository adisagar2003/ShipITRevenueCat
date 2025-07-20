using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public List<Lobby> availableLobbies { get; private set; } = new List<Lobby>();
    public Lobby currentLobby;
    private bool shouldRefreshLobbies = true;
    private int maxPlayers = 2;
    public event Action OnLobbiesUpdated;

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

    public async void CreateLobby(string lobbyName = "MyLobby")
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
               { "gameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false") }
            }
            };
            currentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options); // 2== Max players allowed
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
            var updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { "gameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true") }
            }
            };

            await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);

            networkManager.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
                Debug.Log(currentLobby);
                if (currentLobby != null && currentLobby.Data.TryGetValue("gameStarted", out DataObject data) && data.Value == "true")
                {
                    Debug.Log("Game started by host, joining as client.");
                    networkManager.StartClient();
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
