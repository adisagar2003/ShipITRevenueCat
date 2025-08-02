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
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🎯 Initialize called for session: <color=yellow>{lobby.Name}</color> (ID: {lobby.Id})");
        
        lobbyId = lobby.Id;
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🆔 Set lobbyId: {lobbyId}");
        
        if (lobbyNameText != null)
        {
            lobbyNameText.text = lobby.Name;
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Set lobby name text: {lobby.Name}");
        }
        else
        {
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ lobbyNameText is null!");
        }
        
        int currentPlayers = lobby.MaxPlayers - lobby.AvailableSlots;
        string playerCountString = $"{currentPlayers}/{lobby.MaxPlayers}";
        if (playerCountText != null)
        {
            playerCountText.text = playerCountString;
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Set player count text: {playerCountString}");
        }
        else
        {
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ playerCountText is null!");
        }
        
        isHost = lobby.HostId == GameInitializer.PlayerId;
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 👑 isHost: {isHost} (HostId: {lobby.HostId}, PlayerId: {GameInitializer.PlayerId})");
        
        // Host is automatically considered "joined" to their own session
        if (isHost)
        {
            hasJoined = true;
            Debug.Log("<color=green><b>[LOBBY SLOT]</b></color> ✅ Host automatically marked as joined");
        }
        
        Debug.Log("<color=purple><b>[LOBBY SLOT]</b></color> 🔄 Calling UpdateButtonStates...");
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🔘 UpdateButtonStates: hasJoined={hasJoined}, isHost={isHost}");
        
        if (joinLobbyButton != null)
        {
            bool showJoinButton = !hasJoined;
            joinLobbyButton.gameObject.SetActive(showJoinButton);
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Join button active: {showJoinButton}");
        }
        else
        {
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ joinLobbyButton is null!");
        }
        
        if (startGameButton != null)
        {
            bool showStartButton = isHost && hasJoined;
            startGameButton.gameObject.SetActive(showStartButton);
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Start button active: {showStartButton}");
        }
        else
        {
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ startGameButton is null!");
        }
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
