using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;

using Unity.Services.Lobbies.Models;
using System.Linq;
using TMPro;

public class LobbySlotData : MonoBehaviour
{
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    private string lobbyId;
    private LobbyManager lobbyManager;
        private LobbySessionService lobbySessionService;
    private bool hasJoined;
    private bool isHost;
    private bool isJoining; // Prevent double-clicks while joining
    private bool isStarting; // Prevent double-clicks while starting

    private void Awake()
    {
        // Add null checks to prevent crashes
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager not found! Make sure LobbyManager exists in scene.");
            return;
        }
        
        // Safely get session service (might be null during initialization)
        lobbySessionService = lobbyManager.sessionService;
        
        // Add button listeners with null checks
        if (joinLobbyButton != null)
            joinLobbyButton.onClick.AddListener(JoinLobby);
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
    }

    public void Initialize(ISessionInfo lobby)
    {
#if debug
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🎯 Initialize called for session: <color=yellow>{lobby.Name}</color> (ID: {lobby.Id})");
#endif
        lobbyId = lobby.Id;
#if debug
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🆔 Set lobbyId: {lobbyId}");
#endif

        if (lobbyNameText != null)
        {
            lobbyNameText.text = lobby.Name;
#if debug
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Set lobby name text: {lobby.Name}");
#endif
        }
        else
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ lobbyNameText is null!");
#endif
        }

        int currentPlayers = lobby.MaxPlayers - lobby.AvailableSlots;
        string playerCountString = $"{currentPlayers}/{lobby.MaxPlayers}";
        if (playerCountText != null)
        {
            playerCountText.text = playerCountString;
#if debug
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Set player count text: {playerCountString}");
#endif
        }
        else
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ playerCountText is null!");
#endif
        }

        isHost = lobby.HostId == GameInitializer.PlayerId;
#if debug
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 👑 isHost: {isHost} (HostId: {lobby.HostId}, PlayerId: {GameInitializer.PlayerId})");
#endif

        // Check if we're already in this session (either as host or joined client)
        bool hasLobbyManager = LobbyManager.Instance != null;
        bool hasCurrentSession = hasLobbyManager && LobbyManager.Instance.currentSession != null;
        string currentSessionId = hasCurrentSession ? LobbyManager.Instance.currentSession.Id : "NULL";
        bool isCurrentSession = hasCurrentSession && LobbyManager.Instance.currentSession.Id == lobby.Id;

#if debug
        Debug.Log($"<color=purple><b>[LOBBY SLOT DEBUG]</b></color> 🔍 Session check: LobbyManager={hasLobbyManager}, CurrentSession={hasCurrentSession}, CurrentId='{currentSessionId}', LobbyId='{lobby.Id}', Match={isCurrentSession}");
#endif

        if (isCurrentSession)
        {
            hasJoined = true;
#if debug
            Debug.Log("<color=green><b>[LOBBY SLOT]</b></color> ✅ Already in this session, marked as joined");
#endif
        }
        else
        {
            hasJoined = false;
#if debug
            Debug.Log("<color=purple><b>[LOBBY SLOT]</b></color> ⚫ Not in this session, marked as not joined");
#endif
        }

#if debug
        Debug.Log("<color=purple><b>[LOBBY SLOT]</b></color> 🔄 Calling UpdateButtonStates...");
#endif
        UpdateButtonStates();
        
        // Set initial button texts
        SetJoinButtonState(!isHost && !hasJoined, "Join Lobby");
        SetStartButtonState(isHost && hasJoined, "Start Game");
    }

    private void UpdateButtonStates()
    {
#if debug
        Debug.Log($"<color=purple><b>[LOBBY SLOT]</b></color> 🔘 UpdateButtonStates: hasJoined={hasJoined}, isHost={isHost}");
#endif

        // Join button logic: Show for clients who haven't joined this session
        if (joinLobbyButton != null)
        {
            bool showJoinButton = !isHost && !hasJoined;  // Only non-hosts who haven't joined
            joinLobbyButton.gameObject.SetActive(showJoinButton);
#if debug
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Join button active: {showJoinButton} (isHost={isHost}, hasJoined={hasJoined})");
#endif
        }
        else
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ joinLobbyButton is null!");
#endif
        }

        // Start button logic: Only show for hosts who are in this session
        if (startGameButton != null)
        {
            bool showStartButton = isHost && hasJoined;  // Only hosts in their own session
            startGameButton.gameObject.SetActive(showStartButton);
#if debug
            Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Start button active: {showStartButton} (isHost={isHost}, hasJoined={hasJoined})");
#endif
        }
        else
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY SLOT ERROR]</b></color> ❌ startGameButton is null!");
#endif
        }
    }

    /// <summary>
    /// Sets the join button state and text
    /// </summary>
    private void SetJoinButtonState(bool interactable, string buttonText)
    {
        if (joinLobbyButton == null) return;
        
        joinLobbyButton.interactable = interactable;
        
        // Update TextMeshProUGUI component
        var textComponent = joinLobbyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = buttonText;
        }
        
#if debug
        Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Join button: interactable={interactable}, text='{buttonText}'");
#endif
    }

    /// <summary>
    /// Sets the start button state and text
    /// </summary>
    private void SetStartButtonState(bool interactable, string buttonText)
    {
        if (startGameButton == null) return;
        
        startGameButton.interactable = interactable;
        
        // Update TextMeshProUGUI component
        var textComponent = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = buttonText;
        }
        
#if debug
        Debug.Log($"<color=green><b>[LOBBY SLOT UI]</b></color> ✅ Start button: interactable={interactable}, text='{buttonText}'");
#endif
    }

    private async void JoinLobby()
    {
        // Prevent double-clicks and multiple join attempts
        if (LobbyManager.Instance == null || hasJoined || isJoining) return;

        // Ensure we have session service (in case it wasn't ready during Awake)
        if (lobbySessionService == null)
        {
            lobbySessionService = LobbyManager.Instance.sessionService;
            if (lobbySessionService == null)
            {
                Debug.LogError("LobbySessionService not available yet!");
                return;
            }
        }

        if (lobbySessionService.HasActiveSession)
        {
#if debug
            Debug.LogWarning("Already in a session, cannot join another");
#endif
            return;
        }

        // Set joining state and disable button immediately
        isJoining = true;
        SetJoinButtonState(false, "Joining...");

        try
        {
            // Use proper service architecture instead of direct API call
            bool joinSuccess = await lobbySessionService.JoinSessionAsync(lobbyId);
            
            if (joinSuccess)
            {
                hasJoined = true;
                SetJoinButtonState(false, "Joined"); // Keep disabled with "Joined" text
#if debug
                Debug.Log($"Successfully joined lobby: {lobbyId}");
#endif
                // Note: Polling for game start is now handled by LobbyManager.OnSessionJoinedByService event
            }
            else
            {
                // Join failed through service
#if debug
                Debug.LogError("Failed to join lobby through session service");
#endif
                hasJoined = false;
                SetJoinButtonState(true, "Join Lobby"); // Re-enable on failure
            }
        }
        catch (System.Exception e)
        {
#if debug
            Debug.LogError($"Unexpected error joining lobby: {e.Message}");
#endif
            hasJoined = false;
            SetJoinButtonState(true, "Join Lobby"); // Re-enable on failure
        }
        finally
        {
            isJoining = false;
        }
    }

    private void StartGame()
    {
        // Prevent double-clicks and multiple start attempts
        if (!isHost || LobbyManager.Instance == null || isStarting) return;

        // Set starting state and disable button immediately
        isStarting = true;
        SetStartButtonState(false, "Starting...");

        try
        {
            LobbyManager.Instance.HostStartGame();
#if debug
            Debug.Log("Game start initiated by host");
#endif
            // Keep button disabled after starting
            SetStartButtonState(false, "Started");
        }
        catch (System.Exception e)
        {
#if debug
            Debug.LogError($"Failed to start game: {e.Message}");
#endif
            isStarting = false;
            SetStartButtonState(true, "Start Game"); // Re-enable on failure
        }
    }
}
