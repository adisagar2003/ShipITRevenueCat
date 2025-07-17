using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GameInitializer : MonoBehaviour
{
    public List<Lobby> availableLobbies = new List<Lobby>();

    async void Start()
    {
        await InitializeServicesAndSignIn();
        await GetAvailableLobbiesAsync();
    }

    async Task InitializeServicesAndSignIn()
    {
        try
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in as PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Initialization or sign-in failed: {e}");
        }
    }

    async Task GetAvailableLobbiesAsync()
    {
        try
        {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
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
