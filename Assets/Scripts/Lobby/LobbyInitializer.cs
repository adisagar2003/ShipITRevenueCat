using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class GameInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; } = false;
    public static string PlayerId => AuthenticationService.Instance.IsSignedIn
      ? AuthenticationService.Instance.PlayerId
      : null;

    private async void Start()
    {
        await InitializeServicesAndSignIn();
        IsInitialized = true;
        Debug.Log("GameInitializer: Unity Services and Authentication ready.");
    }

    private async Task InitializeServicesAndSignIn()
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
}
