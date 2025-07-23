

using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    private NetworkVariable<float> countdownTimer = new NetworkVariable<float>(
    3f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    [SerializeField] private string lobbySceneName = "LobbyandHost";
    private void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
        }
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        Debug.Log($"Scene '{sceneName}' loaded in {loadSceneMode}. Reassigning player positions to clients:");

        foreach (var clientId in clientsCompleted)
        {
            Debug.Log($"Client {clientId} completed scene load.");
        }

        if (clientsTimedOut.Count > 0)
        {
            foreach (var clientId in clientsTimedOut)
            {
                Debug.LogWarning($"Client {clientId} timed out while loading the scene.");
            }
        }


        // assign spawn location to each provided client
        //foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
        //{
        //    if (!clientsCompleted.Contains(networkClient.ClientId)) continue;

        //    var playerObject = networkClient.PlayerObject;
        //    if (playerObject != null)
        //    {
        //        var setSpawnLocation = playerObject.GetComponent<SetSpawnLocation>();
        //        if (setSpawnLocation != null)
        //        {
        //            setSpawnLocation.AssignNewSpawnPosition();
        //        }
        //    }
        //}
    }

    [ContextMenu("Test Force Reset Scene")]
    public void ForceResetScene()
    {
        if (IsServer)
        {
            StartCoroutine(DelayedSceneReset());
        }
    }

    [ContextMenu("Put players back to lobby")]
    public void PutPlayersBackToLobby()
    {
        if (IsServer)
        {
            StartCoroutine(BackToLobbyCoroutine());
        }
    }

    public IEnumerator BackToLobbyCoroutine()
    {
        yield return new WaitForSeconds(2.0f);
        Debug.Log("<color=orange>Player will go back to lobby now</color>");
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client waiting for host to load the lobby scene.");
        }

        NetworkManager.Singleton.Shutdown();
    }

    public IEnumerator DelayedSceneReset()
    {
        while (countdownTimer.Value > 0f)
        {
            yield return new WaitForSeconds(1f);
            countdownTimer.Value -= 1f;
        }
        NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }
}
