#define ONGUI
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using System.Linq;

public class LobbySlotData : MonoBehaviour
{
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    
    private string lobbyId;
    private LobbyManager lobbyManager;
    private bool hasJoined = false;
    private bool isHost = false;
    
    void Awake()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();
        joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }
    
    public void Initialize(Lobby lobby)
    {
        lobbyId = lobby.Id;
        lobbyNameText.text = lobby.Name;
        UpdatePlayerCount(lobby.Players.Count, lobby.MaxPlayers);
        
        // Check if current user is the host
        isHost = lobby.HostId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        
        // Check if current user is in this lobby (either as host or player)
        hasJoined = lobby.Players.Any(p => p.Id == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);
        
        UpdateButtonVisibility();
    }
    
    private void UpdateButtonVisibility()
    {
        if (isHost && hasJoined)
        {
            joinLobbyButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            joinLobbyButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
        }
    }
    
    public string GetLobbyId()
    {
        return lobbyId;
    }
    
    private void OnJoinLobbyClicked()
    {
        if (hasJoined)
        {
            Debug.LogWarning("You have already joined a lobby!");
            return;
        }
        
        if (lobbyManager.currentLobby != null)
        {
            Debug.LogWarning("You are already in a lobby. Leave the current lobby first.");
            return;
        }
        
        JoinLobby();
    }
    
    private void OnStartGameClicked()
    {
        if (!isHost)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }
        
        lobbyManager.StartGameFromLobby();
    }
    
    private async void JoinLobby()
    {
        try
        {
            await lobbyManager.JoinLobbyByIdAsync(lobbyId);
            hasJoined = true;
            joinLobbyButton.interactable = false;
            joinLobbyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Joined";
            
            UpdateButtonVisibility();
            
            Debug.Log($"<color=green>Successfully joined lobby: {lobbyNameText.text}</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }
    
    public void UpdatePlayerCount(int currentPlayers, int maxPlayers)
    {
        playerCountText.text = $"{currentPlayers}/{maxPlayers}";
    }
    
    public void ResetJoinState()
    {
        hasJoined = false;
        isHost = false;
        joinLobbyButton.interactable = true;
        joinLobbyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Join";
        UpdateButtonVisibility();
    }
    
#if ONGUI
    private void OnGUI()
    {
        if (lobbyId != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y;
            
            GUI.color = hasJoined ? Color.green : Color.white;
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 20, 200, 20), $"Lobby: {lobbyNameText.text}");
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 200, 20), $"ID: {lobbyId.Substring(0, 8)}...");
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y + 20, 200, 20), $"Joined: {hasJoined}");
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y + 40, 200, 20), $"Host: {isHost}");
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y + 60, 200, 20), $"Current: {lobbyManager?.currentLobby?.Id == lobbyId}");
        }
    }
#endif
} 