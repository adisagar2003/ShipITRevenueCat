using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Multiplayer;

public class LobbyListUIController : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject lobbySlotPrefab;
    [SerializeField] private float slotSpacing = 50f;
    private LobbyManager lobbyManager;

    void OnEnable()
    {
        Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 🔄 LobbyListUIController OnEnable called");
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.OnSessionsUpdated += PopulateLobbyList;
            Debug.Log("<color=green><b>[LOBBY UI]</b></color> ✅ Subscribed to OnSessionsUpdated event");
        }
        else
        {
            Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ LobbyManager not found!");
        }
    }

    void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnSessionsUpdated -= PopulateLobbyList;
    }

    void OnDestroy()
    {
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void PopulateLobbyList()
    {
        Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 🎨 PopulateLobbyList called!");
        
        // Clear existing slots
        Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🧹 Clearing existing slots (count: {content.transform.childCount})");
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        if (lobbyManager == null)
        {
            Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ lobbyManager is null in PopulateLobbyList!");
            return;
        }

        List<ISessionInfo> lobbies = lobbyManager.availableSessions;
        Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 📋 Available sessions count: <color=yellow>{lobbies.Count}</color>");
        
        if (lobbies.Count == 0)
        {
            Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 📭 No available sessions to display");
            return;
        }

        for (int i = 0; i < lobbies.Count; i++)
        {
            ISessionInfo lobby = lobbies[i];
            Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🏗️ Creating slot {i} for session: <color=yellow>{lobby.Name}</color> (ID: {lobby.Id})");
            
            if (lobbySlotPrefab == null)
            {
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ lobbySlotPrefab is null!");
                return;
            }
            
            if (content == null)
            {
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ content GameObject is null!");
                return;
            }

            GameObject slot = Instantiate(lobbySlotPrefab, content.transform);
            Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ Instantiated lobby slot GameObject: {slot.name}");
            
            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -i * slotSpacing);
                Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ Positioned slot at Y: {-i * slotSpacing}");
            }

            LobbySlotData slotData = slot.GetComponent<LobbySlotData>();
            if (slotData != null)
            {
                Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🎯 Initializing LobbySlotData for session: {lobby.Name}");
                slotData.Initialize(lobby);
                Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ LobbySlotData initialized successfully");
            }
            else
            {
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ LobbySlotData component not found on prefab!");
            }
        }
        
        Debug.Log($"<color=green><b>[LOBBY UI SUCCESS]</b></color> 🎉 PopulateLobbyList completed! Created {lobbies.Count} lobby slots");
    }
}
