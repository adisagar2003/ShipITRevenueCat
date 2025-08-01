using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbySceneManager : ThreadSafeNetworkSingleton<LobbySceneManager>
{
    protected override void Initialize()
    {
        base.Initialize();
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "LobbySceneManager initialized");
    }


    public static void StartHostAndSwitchScene(string sceneName)
    {
        if (NetworkManager.Singleton == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "NetworkManager.Singleton is null, cannot start host");
            return;
        }
        
        if (string.IsNullOrEmpty(sceneName))
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Scene name is null or empty");
            return;
        }
        
        try
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Starting host and switching to scene: {sceneName}");
            
            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, "Failed to start host");
                return;
            }
            
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (System.Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"Error starting host and switching scene: {ex.Message}");
        }
    } 

}
