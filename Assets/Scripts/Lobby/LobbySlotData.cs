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
    private bool hasJoined;
    private bool isHost;

    private void Awake()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();
        joinLobbyButton.onClick.AddListener(JoinLobby);
        startGameButton.onClick.AddListener(StartGame);
    }

    public void Initialize(Lobby lobby)
    {
        lobbyId = lobby.Id;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        isHost = lobby.HostId == GameInitializer.PlayerId;
        hasJoined = lobby.Players.Any(p => p.Id == GameInitializer.PlayerId);

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        joinLobbyButton.gameObject.SetActive(!hasJoined);
        startGameButton.gameObject.SetActive(isHost && hasJoined);
    }

    private void JoinLobby()
    {
        if (LobbyManager.Instance == null || hasJoined) return;
        LobbyManager.Instance.JoinLobbyById(lobbyId);
        hasJoined = true;
        UpdateButtonStates();
    }

    private void StartGame()
    {
        if (!isHost || LobbyManager.Instance == null) return;
        LobbyManager.Instance.HostStartGame();
    }

}
