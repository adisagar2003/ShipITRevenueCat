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
#if debug
        Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 🔄 LobbyListUIController OnEnable called");
#endif
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.OnSessionsUpdated += PopulateLobbyList;
#if debug
            Debug.Log("<color=green><b>[LOBBY UI]</b></color> ✅ Subscribed to OnSessionsUpdated event");
#endif
        }
        else
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ LobbyManager not found!");
#endif
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
#if debug
        Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 🎨 PopulateLobbyList called!");
#endif

        // Clear existing slots
#if debug
        Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🧹 Clearing existing slots (count: {content.transform.childCount})");
#endif
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        if (lobbyManager == null)
        {
#if debug
            Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ lobbyManager is null in PopulateLobbyList!");
#endif
            return;
        }

        List<ISessionInfo> lobbies = lobbyManager.availableSessions;
#if debug
        Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 📋 Available sessions count: <color=yellow>{lobbies.Count}</color>");
#endif

        if (lobbies.Count == 0)
        {
#if debug
            Debug.Log("<color=magenta><b>[LOBBY UI]</b></color> 📭 No available sessions to display");
#endif
            return;
        }

        for (int i = 0; i < lobbies.Count; i++)
        {
            ISessionInfo lobby = lobbies[i];
#if debug
            Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🏗️ Creating slot {i} for session: <color=yellow>{lobby.Name}</color> (ID: {lobby.Id})");
#endif

            if (lobbySlotPrefab == null)
            {
#if debug
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ lobbySlotPrefab is null!");
#endif
                return;
            }

            if (content == null)
            {
#if debug
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ content GameObject is null!");
#endif
                return;
            }

            GameObject slot = Instantiate(lobbySlotPrefab, content.transform);
#if debug
            Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ Instantiated lobby slot GameObject: {slot.name}");
#endif

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -i * slotSpacing);
#if debug
                Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ Positioned slot at Y: {-i * slotSpacing}");
#endif
            }

            LobbySlotData slotData = slot.GetComponent<LobbySlotData>();
            if (slotData != null)
            {
#if debug
                Debug.Log($"<color=magenta><b>[LOBBY UI]</b></color> 🎯 Initializing LobbySlotData for session: {lobby.Name}");
#endif
                slotData.Initialize(lobby);
#if debug
                Debug.Log($"<color=green><b>[LOBBY UI]</b></color> ✅ LobbySlotData initialized successfully");
#endif
            }
            else
            {
#if debug
                Debug.LogError("<color=red><b>[LOBBY UI ERROR]</b></color> ❌ LobbySlotData component not found on prefab!");
#endif
            }
        }

#if debug
        Debug.Log($"<color=green><b>[LOBBY UI SUCCESS]</b></color> 🎉 PopulateLobbyList completed! Created {lobbies.Count} lobby slots");
#endif
    }
}
