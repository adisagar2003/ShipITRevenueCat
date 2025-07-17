using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services;
using System.Net.Http;
using System.Threading.Tasks;

public class SignInPlayerAnonymously : MonoBehaviour
{
    private string playerId = "Not signed in yet.";
    private string playerName;

 
    async System.Threading.Tasks.Task SignInCachedUserAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
            playerId = AuthenticationService.Instance.PlayerId;
            playerName = await CallARandomAPIToGenerateRandomUsername();
           await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log($"PlayerID: {playerId}");
            Debug.Log($"PlayerName: {playerName}");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
            playerId = $"AuthException: {ex.Message}";
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            playerId = $"RequestFailed: {ex.Message}";
        }
    }

    async Task<string> CallARandomAPIToGenerateRandomUsername()
    {
        using HttpClient client = new HttpClient();

        await Task.Delay(500); // simulate network delay
        return "Player_" + Random.Range(1000, 9999);
    }


    public static bool IsSignedIn()
    {
        return AuthenticationService.Instance.IsSignedIn;
    }
#if ONGUI
    void OnGUI()
    {
        // Prepare style
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 28;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;

        // Background panel
        Color originalColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.6f); // semi-transparent black
        GUI.Box(new Rect(90, 90, 520, 40), GUIContent.none);
        GUI.color = originalColor;

        // Label text
        GUI.Label(new Rect(100, 100, 500, 30), $"PlayerID: {playerId}", style);
        GUI.Label(new Rect(100, 200, 500, 30), $"Player name: {playerName}", style);
    }
#endif
}
