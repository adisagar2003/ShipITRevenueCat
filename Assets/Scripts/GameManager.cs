
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
public class GameManager : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "LobbyandHost";
    
    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            StartCoroutine(WaitForNetworkManagerAndSubscribe());
            return;
        }
        SubscribeToNetworkEvents();
    }

    public override void OnDestroy()
    {
        UnsubscribeFromNetworkEvents();
        base.OnDestroy();
    }

    private IEnumerator WaitForNetworkManagerAndSubscribe()
    {
        while (NetworkManager.Singleton == null)
            yield return null;
        
        SubscribeToNetworkEvents();
    }

    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"<color=cyan>[SCENE LOAD COMPLETE]</color> Scene: {sceneName}, IsServer: {IsServer}");
        LogNetworkManagerState($"Scene Load Complete - {sceneName}");
        
        if (!IsHost) 
        {
            Debug.LogWarning($"<color=yellow>[SCENE LOAD COMPLETE]</color> IsServer is FALSE for scene: {sceneName}");
            return;
        }

        if (IsRaceScene(sceneName))
            InitializeRaceScene();
    }

    [ContextMenu("Put players back to lobby")]
    public void PutPlayersBackToLobby()
    {
        if (IsHost)
            StartCoroutine(BackToLobbyCoroutine());
    }

    public IEnumerator BackToLobbyCoroutine()
    {
        yield return new WaitForSeconds(2.0f);
        
        if (!IsHost) yield break;

        RequestClientDisconnectRpc();
        
        float timeout = 5f;
        float elapsed = 0f;
        
        while (NetworkManager.Singleton.ConnectedClientsList.Count > 1 && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    [Rpc(SendTo.NotServer)]
    private void RequestClientDisconnectRpc()
    {
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        DisconnectClient();
    }

    [ContextMenu("Disconnect Client")]
    public void DisconnectClient()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // First leave any active session to clean up properly
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.LeaveSession();
            }
            
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private bool IsRaceScene(string sceneName)
    {
        return sceneName.Contains("Race") || 
               sceneName == "RaceLevel" || 
               sceneName == "MultiplayerTestLevel";
    }

    private void InitializeRaceScene()
    {
        if (!IsHost) return;

        var finishTriggers = FindObjectsByType<FinishLineTrigger>(FindObjectsSortMode.None);
        foreach (var trigger in finishTriggers)
        {
            trigger.ResetRace();
        }
    }
    
    /// <summary>
    /// Logs comprehensive NetworkManager state for debugging
    /// </summary>
    private void LogNetworkManagerState(string context)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError($"<color=red>[NETWORK STATE - {context}]</color> NetworkManager.Singleton is NULL!");
            return;
        }
        
        var nm = NetworkManager.Singleton;
        Debug.Log($"<color=yellow>[NETWORK STATE - {context}]</color> " +
                 $"IsServer: {IsServer}, " +
                 $"IsClient: {nm.IsClient}, " +
                 $"IsHost: {nm.IsHost}, " +
                 $"IsConnectedClient: {nm.IsConnectedClient}, " +
                 $"ConnectedClients.Count: {nm.ConnectedClients.Count}, " +
                 $"NetworkManager.name: {nm.name}, " +
                 $"GameObject.instanceID: {nm.GetInstanceID()}");
    }
}
