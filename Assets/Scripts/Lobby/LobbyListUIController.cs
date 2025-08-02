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
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
            lobbyManager.OnLobbiesUpdated += PopulateLobbyList;
    }

    void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnLobbiesUpdated -= PopulateLobbyList;
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
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        List<Lobby> lobbies = lobbyManager.availableLobbies;
        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies[i];
            GameObject slot = Instantiate(lobbySlotPrefab, content.transform);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -i * slotSpacing);

            LobbySlotData slotData = slot.GetComponent<LobbySlotData>();
            slotData.Initialize(lobby);
        }
    }
}
