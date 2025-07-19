using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LobbySceneManager : NetworkBehaviour
{
    public static LobbySceneManager Instance { get; private set; }
    private void Awake()
         {
           if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
             {
                 Instance = this;
                 DontDestroyOnLoad(gameObject);
             }
     }


    public static void StartHostAndSwitchScene(string sceneName)
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    } 

}
