using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
public class NetworkUIController : MonoBehaviour
{
    [SerializeField] private Button startServer;
    [SerializeField] private Button startClient;
    [SerializeField] private Button startHost;

    private void Awake()
    {
        startServer.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        startClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

        startHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
