using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Development UI for testing network operations.
/// Should only be active in development builds.
/// </summary>
public class NetworkManagerHUD : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showInRelease = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    
    private bool isVisible = true;
    private string statusMessage = "";
    private float statusMessageTimer = 0f;
    private const float STATUS_MESSAGE_DURATION = 3f;
    
    private void Update()
    {
        // Toggle visibility with key
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
        
        // Update status message timer
        if (statusMessageTimer > 0)
        {
            statusMessageTimer -= Time.deltaTime;
            if (statusMessageTimer <= 0)
            {
                statusMessage = "";
            }
        }
    }
    
    private void OnGUI()
    {
        // Only show in development builds or if explicitly enabled
        if (!Debug.isDebugBuild && !showInRelease) return;
        if (!isVisible) return;
        
        // Validate NetworkManager availability
        if (NetworkManager.Singleton == null)
        {
            DrawErrorMessage("NetworkManager.Singleton is null");
            return;
        }
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        
        DrawNetworkStatus();
        DrawNetworkControls();
        DrawStatusMessage();
        
        GUILayout.EndArea();
    }
    
    private void DrawNetworkStatus()
    {
        GUILayout.Label("=== Network Status ===");
        GUILayout.Label($"Is Client: {NetworkManager.Singleton.IsClient}");
        GUILayout.Label($"Is Server: {NetworkManager.Singleton.IsServer}");
        GUILayout.Label($"Is Host: {NetworkManager.Singleton.IsHost}");
        
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            GUILayout.Label($"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        }
        
        GUILayout.Space(10);
    }
    
    private void DrawNetworkControls()
    {
        GUILayout.Label("=== Network Controls ===");
        
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host"))
            {
                StartNetworkOperation(() => NetworkManager.Singleton.StartHost(), "Starting Host");
            }
            
            if (GUILayout.Button("Start Client"))
            {
                StartNetworkOperation(() => NetworkManager.Singleton.StartClient(), "Starting Client");
            }
            
            if (GUILayout.Button("Start Server"))
            {
                StartNetworkOperation(() => NetworkManager.Singleton.StartServer(), "Starting Server");
            }
        }
        else
        {
            if (GUILayout.Button("Shutdown"))
            {
                ShutdownNetwork();
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"Toggle with {toggleKey}");
    }
    
    private void DrawStatusMessage()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(10);
            GUI.color = Color.yellow;
            GUILayout.Label(statusMessage);
            GUI.color = Color.white;
        }
    }
    
    private void DrawErrorMessage(string error)
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUI.color = Color.red;
        GUILayout.Label("ERROR:");
        GUILayout.Label(error);
        GUI.color = Color.white;
        GUILayout.EndArea();
    }
    
    private void StartNetworkOperation(Func<bool> operation, string operationName)
    {
        try
        {
            bool success = operation.Invoke();
            
            if (success)
            {
                SetStatusMessage($"{operationName} successful");
                Debug.Log($"NetworkManagerHUD: {operationName} successful");
            }
            else
            {
                SetStatusMessage($"{operationName} failed");
                Debug.LogWarning($"NetworkManagerHUD: {operationName} failed");
            }
        }
        catch (Exception e)
        {
            string errorMsg = $"{operationName} error: {e.Message}";
            SetStatusMessage(errorMsg);
            Debug.LogError($"NetworkManagerHUD: {errorMsg}");
        }
    }
    
    private void ShutdownNetwork()
    {
        try
        {
            NetworkManager.Singleton.Shutdown();
            SetStatusMessage("Network shutdown successful");
            Debug.Log("NetworkManagerHUD: Network shutdown successful");
        }
        catch (Exception e)
        {
            string errorMsg = $"Shutdown error: {e.Message}";
            SetStatusMessage(errorMsg);
            Debug.LogError($"NetworkManagerHUD: {errorMsg}");
        }
    }
    
    private void SetStatusMessage(string message)
    {
        statusMessage = message;
        statusMessageTimer = STATUS_MESSAGE_DURATION;
    }
}
