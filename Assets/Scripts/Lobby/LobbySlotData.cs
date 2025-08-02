using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;
using Unity.Services.Lobbies.Models;
using System.Linq;

public class LobbySlotData : MonoBehaviour
{
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Text lobbyNameText;
    [SerializeField] private Text playerCountText;
    private string lobbyId;
    private LobbyManager lobbyManager;
    private bool hasJoined;
    private bool isHost;

    private void Awake()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();
        joinLobbyButton.onClick.AddListener(JoinLobby);
        startGameButton.onClick.AddListener(StartGame);
    }

    public void Initialize(ISessionInfo lobby)
    {
        lobbyId = lobby.Id;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.MaxPlayers - lobby.AvailableSlots}/{lobby.MaxPlayers}";
        isHost = lobby.HostId == GameInitializer.PlayerId;
        
        // Host is automatically considered "joined" to their own session
        if (isHost)
        {
            hasJoined = true;
        }
        
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        joinLobbyButton.gameObject.SetActive(!hasJoined);
        startGameButton.gameObject.SetActive(isHost && hasJoined);
    }

    private async void JoinLobby()
    {
        if (LobbyManager.Instance == null || hasJoined) return;
        
        if (LobbyManager.Instance.currentSession != null)
        {
            Debug.LogWarning("Already in a session, cannot join another");
            return;
        }
        
        try
        {
            var session = await MultiplayerService.Instance.JoinSessionByIdAsync(lobbyId);
            LobbyManager.Instance.currentSession = session;
            hasJoined = true;
            UpdateButtonStates();
            Debug.Log($"Successfully joined lobby: {session.Name}");
            
            // Start polling for game start if we're not the host
            if (!isHost)
            {
                _ = LobbyManager.Instance.StartPollingForGameStart();
                Debug.Log("Started polling for game start as client");
            }
        }
        catch (SessionException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
            hasJoined = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error joining lobby: {e.Message}");
            hasJoined = false;
        }
    }

    private void StartGame()
    {
        if (!isHost || LobbyManager.Instance == null) return;
        LobbyManager.Instance.HostStartGame();
    }
}
