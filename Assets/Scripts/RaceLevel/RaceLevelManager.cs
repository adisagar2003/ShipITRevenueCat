using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Class intentions: 
///     - Check for total connected clients.
///     - Calls disable waiting UI when all clients are connected.
///     - Start Countdown. 
///     - Trigger session cleanup when all players are ready
/// </summary>
public class RaceLevelManager : NetworkBehaviour
{
    #region Events
    /// <summary>
    /// Event triggered when all expected players are connected and ready to start.
    /// Only fired by the host.
    /// </summary>
    public static event Action OnAllPlayersConnected;
    #endregion
    private int currentPlayers;
    private int expectedPlayers = 2;

    private NetworkManager networkManager;

    // UI 
    [SerializeField] private GameObject waitingUI;
    [SerializeField] private StartRaceCountdown startRaceCountdown;
    void Start()
    {
        Debug.Log("<color=red>Race Level start called</color>");
        networkManager = NetworkManager.Singleton;
        startRaceCountdown = GetComponent<StartRaceCountdown>();

        // Comprehensive NetworkManager state debugging
        LogNetworkManagerState();

        if (!networkManager.IsHost)
        {
            Debug.Log("<color=yellow>IsHost is FALSE - cannot check for connected clients!</color>");
            return;
        }

        Debug.Log("IsHost is TRUE - starting client check coroutine");
        StartCoroutine(CheckForConnectedClients());
    }

    // poll for checking total clients
    private IEnumerator CheckForConnectedClients()
    {
        Debug.Log("Starting CheckForConnectedClients coroutine");

        // check for clients via polling
        while (currentPlayers < expectedPlayers)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is NULL!");
                yield break;
            }

            if (!networkManager.IsHost)
            {
                Debug.LogError("<color=red>[RACE LEVEL ERROR]</color> IsHost became FALSE during polling!");
                LogNetworkManagerState();
                yield break;
            }

            currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"Connected clients: {currentPlayers}/{expectedPlayers} (IsServer: {NetworkManager.Singleton.IsServer}, IsHost: {NetworkManager.Singleton.IsHost})");
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("<color=green>[RACE LEVEL]</color> All players connected! Starting game...");

        // Trigger session cleanup event (only host can trigger this)
        if (IsHost)
        {
            Debug.Log("<color=blue>[RACE LEVEL]</color> Host triggering session cleanup...");
            OnAllPlayersConnected?.Invoke();
            
            // Brief delay to allow session cleanup to complete
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1.0f);
        Debug.Log("<color=orange>[RACE LEVEL]</color> All players Start Game RPC Called...");
        StartGameRpc();
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void StartGameRpc()
    {
        Debug.Log("<color=magenta>[RACE LEVEL]</color> StartGameRpc called - Disable UI can be called, wait for 3 seconds ready to call and player can move after 3 seconds.");
        waitingUI.SetActive(false);
        startRaceCountdown.StartCountdown();
    }
    
    /// <summary>
    /// Logs comprehensive NetworkManager state for debugging
    /// </summary>
    private void LogNetworkManagerState()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("<color=red>[NETWORK STATE]</color> NetworkManager.Singleton is NULL!");
            return;
        }
        
        var nm = NetworkManager.Singleton;
        Debug.Log($"<color=yellow>[NETWORK STATE]</color> " +
                 $"IsServer: {nm.IsServer}, " +
                 $"IsClient: {nm.IsClient}, " +
                 $"IsHost: {nm.IsHost}, " +
                 $"IsConnectedClient: {nm.IsConnectedClient}, " +
                 $"ConnectedClients.Count: {nm.ConnectedClients.Count}, " +
                 $"NetworkManager.name: {nm.name}, " +
                 $"GameObject.instanceID: {nm.GetInstanceID()}");
                 
    }
}

