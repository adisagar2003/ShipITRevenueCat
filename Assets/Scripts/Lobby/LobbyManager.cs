using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public List<Lobby> availableLobbies = new List<Lobby>();

    [ContextMenu("Get all availableLobbies")]
    public async Task GetAvailableLobbiesAsync()
    {
        try
        {
            // Set up filters or options if needed (optional)
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    // Example: new QueryFilter(field: "availableSlots", op: QueryFilter.Op.GT, value: "0")
                },
                Order = new List<QueryOrder>
                {
                    // Example: new QueryOrder(true, "created")
                }
            };

            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
            availableLobbies = response.Results;

            Debug.Log($"Found {availableLobbies.Count} lobbies.");
            foreach (Lobby lobby in availableLobbies)
            {
                Debug.Log($"Lobby Name: {lobby.Name}, Lobby Code: {lobby.LobbyCode}");
                await GetAvailableLobbiesAsync();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to get lobbies: {e}");
        }
    }
    
    public async void CreateLobbyAsync(string lobbyName = "MyLobby", int maxPlayers = 2)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
            {
                { "gameMode", new DataObject(DataObject.VisibilityOptions.Public, "casual") }
            }
            };

            Lobby createdLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            Debug.Log($"Lobby created successfully!");
            Debug.Log($"Lobby Name: {createdLobby.Name}");
            Debug.Log($"Lobby ID: {createdLobby.Id}");
            Debug.Log($"Lobby Code: {createdLobby.LobbyCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    [ContextMenu("Create Button on click")]
    public void OnCreateLobbyButtonClicked()
    {
        _ = CreateLobbyAsync();
    }




}
