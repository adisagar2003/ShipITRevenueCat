using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System;

/// <summary>
/// Handles anonymous player sign-in for Unity Services.
/// This component initializes Unity Services and creates an anonymous player session.
/// </summary>
public class SignInPlayerAnonymously : ThreadSafeSingleton<SignInPlayerAnonymously>
{
    [Header("Authentication Settings")]
    [SerializeField, Tooltip("Automatically sign in on Start")]
    private bool autoSignIn = true;
    
    [SerializeField, Tooltip("Retry count for failed sign-in attempts")]
    private int maxRetries = 3;
    
    [SerializeField, Tooltip("Delay between retry attempts in seconds")]
    private float retryDelay = 2f;
    
    private string playerId = "Not signed in yet.";
    private string playerName = "";
    private volatile bool isSigningIn = false; // volatile for thread safety
    private int currentRetryCount = 0;
    private readonly object signInLock = new object();
    public bool IsSignedIn => AuthenticationService.Instance?.IsSignedIn ?? false;
    public string PlayerId => playerId;
    public string PlayerName => playerName;
    
    protected override void Initialize()
    {
        base.Initialize();
        ValidateConfiguration();
    }
    
    protected override void OnSingletonDestroyed()
    {
        // Cancel any ongoing sign-in operations
        lock (signInLock)
        {
            isSigningIn = false;
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.Authentication, "SignInPlayerAnonymously disposed");
        base.OnSingletonDestroyed();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Authentication, "Application paused during authentication");
        }
    }
    
    private async void Start()
    {
        if (autoSignIn)
        {
            await SignInCachedUserAsync();
        }
    }
    
    private void ValidateConfiguration()
    {
        if (maxRetries <= 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Authentication, $"Invalid maxRetries value: {maxRetries}, setting to 3");
            maxRetries = 3;
        }
        
        if (retryDelay <= 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Authentication, $"Invalid retryDelay value: {retryDelay}, setting to 2.0");
            retryDelay = 2f;
        }
    }
    
    /// <summary>
    /// Attempts to sign in the user anonymously with retry logic.
    /// </summary>
    public async Task SignInCachedUserAsync()
    {
        lock (signInLock)
        {
            if (isSigningIn)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Authentication, "Sign-in already in progress");
                return;
            }
            
            isSigningIn = true;
            currentRetryCount = 0;
        }
        
        while (currentRetryCount < maxRetries)
        {
            try
            {
                await AttemptSignIn();
                isSigningIn = false;
                return; // Success
            }
            catch (Exception ex)
            {
                currentRetryCount++;
                Debug.LogWarning($"Sign-in attempt {currentRetryCount} failed: {ex.Message}");
                
                if (currentRetryCount >= maxRetries)
                {
                    HandleSignInFailure(ex);
                    break;
                }
                
                // Wait before retry
                await Task.Delay(TimeSpan.FromSeconds(retryDelay));
            }
        }
        
        lock (signInLock)
        {
            isSigningIn = false;
        }
    }
    
    private async Task AttemptSignIn()
    {
        // Initialize Unity Services
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
        }
        
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            throw new InvalidOperationException($"Unity Services initialization failed. State: {UnityServices.State}");
        }
        
        // Check if already signed in
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Player already signed in");
            playerId = AuthenticationService.Instance.PlayerId;
            return;
        }
        
        // Sign in anonymously
        Debug.Log("Signing in anonymously...");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            throw new InvalidOperationException("Sign-in completed but user is not marked as signed in");
        }
        
        playerId = AuthenticationService.Instance.PlayerId;
        
        if (string.IsNullOrEmpty(playerId))
        {
            throw new InvalidOperationException("Sign-in succeeded but PlayerId is null or empty");
        }
        
        Debug.Log("Sign in anonymously succeeded!");
        
        // Generate and set player name
        try
        {
            playerName = await CallARandomAPIToGenerateRandomUsername();
            
            if (!string.IsNullOrEmpty(playerName))
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
                Debug.Log($"Player name updated to: {playerName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to update player name: {ex.Message}");
            playerName = $"Player_{playerId.Substring(0, 4)}";
        }
        
        Debug.Log($"PlayerID: {playerId}");
        Debug.Log($"PlayerName: {playerName}");
    }
    
    private void HandleSignInFailure(Exception finalException)
    {
        string errorMessage = finalException switch
        {
            AuthenticationException authEx => $"Authentication failed: {authEx.Message}",
            RequestFailedException reqEx => $"Request failed: {reqEx.Message}",
            InvalidOperationException invEx => $"Invalid operation: {invEx.Message}",
            _ => $"Unexpected error: {finalException.Message}"
        };
        
        Debug.LogError($"Sign-in failed after {maxRetries} attempts: {errorMessage}");
        playerId = errorMessage;
        playerName = "Sign-in failed";
    }

    /// <summary>
    /// Generates a random username. In a real implementation, this could call an external API.
    /// </summary>
    private async Task<string> CallARandomAPIToGenerateRandomUsername()
    {
        try
        {
            // Simulate API call delay
            await Task.Delay(500);
            
            // Generate a random username
            string username = $"Player_{Random.Range(1000, 9999)}";
            
            // Validate generated username
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Generated username is null or empty");
            }
            
            return username;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to generate username: {ex.Message}");
            // Fallback to simple random name
            return $"Player_{UnityEngine.Random.Range(100, 999)}";
        }
    }


    /// <summary>
    /// Static helper to check if any player is currently signed in.
    /// </summary>
    public static bool IsPlayerSignedIn()
    {
        try
        {
            return AuthenticationService.Instance?.IsSignedIn ?? false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error checking sign-in status: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Public method to manually trigger sign-in.
    /// </summary>
    public async void TriggerSignIn()
    {
        await SignInCachedUserAsync();
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
