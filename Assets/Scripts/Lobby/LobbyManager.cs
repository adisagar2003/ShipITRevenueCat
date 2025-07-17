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
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to get lobbies: {e}");
        }
    }

}
