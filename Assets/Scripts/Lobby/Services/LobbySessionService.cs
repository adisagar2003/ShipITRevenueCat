using System;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using Unity.Services.Authentication;
using UnityEngine;

/// <summary>
/// Manages lobby session lifecycle operations using ThreadSafeSingleton pattern.
/// Handles session creation, joining, leaving and state management.
/// Extracted from LobbyManager to provide focused session management functionality.
/// </summary>
public class LobbySessionService : ThreadSafeSimpleSingleton<LobbySessionService>
{
    #region Events
    public event Action<ISession> OnSessionCreated;
    public event Action<ISession> OnSessionJoined;
    public event Action OnSessionLeft;
    public event Action<string> OnSessionError;
    #endregion

    #region Properties
    public ISession CurrentSession { get; private set; }
    public bool HasActiveSession => CurrentSession != null;
    public bool IsInSession => CurrentSession != null;
    #endregion

    #region Session Creation
    /// <summary>
    /// Creates a new multiplayer session with the specified configuration.
    /// </summary>
    /// <param name="sessionName">Name of the session to create</param>
    /// <param name="maxPlayers">Maximum number of players allowed</param>
    /// <param name="isPrivate">Whether the session should be private</param>
    /// <returns>True if session was created successfully</returns>
    public async Task<bool> CreateSessionAsync(string sessionName, int maxPlayers = 4, bool isPrivate = false)
    {
        if (!ValidateSessionCreation(sessionName))
            return false;

        try
        {
            var sessionOptions = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = false
            }.WithRelayNetwork();

            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Creating session: {sessionName} (MaxPlayers: {maxPlayers})");
            
            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Session created successfully: {CurrentSession.Name} (ID: {CurrentSession.Id})");
            OnSessionCreated?.Invoke(CurrentSession);
            
            return true;
        }
        catch (SessionException ex)
        {
            var errorMsg = $"Failed to create session '{sessionName}': {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            return false;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error creating session '{sessionName}': {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Validates session creation requirements before attempting to create.
    /// </summary>
    private bool ValidateSessionCreation(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Session name cannot be null or empty");
            return false;
        }

        if (CurrentSession != null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, $"Already in session: {CurrentSession.Name}");
            return false;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Not authenticated - cannot create session");
            return false;
        }

        return true;
    }
    #endregion

    #region Session Joining
    /// <summary>
    /// Joins an existing session by its ID.
    /// </summary>
    /// <param name="sessionId">ID of the session to join</param>
    /// <returns>True if successfully joined the session</returns>
    public async Task<bool> JoinSessionAsync(string sessionId)
    {
        if (!ValidateSessionJoin(sessionId))
            return false;

        try
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Joining session: {sessionId}");
            
            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Successfully joined session: {CurrentSession.Name}");
            OnSessionJoined?.Invoke(CurrentSession);
            
            return true;
        }
        catch (SessionException ex)
        {
            var errorMsg = $"Failed to join session '{sessionId}': {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            return false;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error joining session '{sessionId}': {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Validates session join requirements.
    /// </summary>
    private bool ValidateSessionJoin(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Session ID cannot be null or empty");
            return false;
        }

        if (CurrentSession != null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, "Already in a session - leave current session first");
            return false;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Not authenticated - cannot join session");
            return false;
        }

        return true;
    }
    #endregion

    #region Session Leaving
    /// <summary>
    /// Leaves the current session if one exists.
    /// </summary>
    /// <returns>True if successfully left the session or no session was active</returns>
    public async Task<bool> LeaveSessionAsync()
    {
        if (CurrentSession == null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "No active session to leave");
            return true;
        }

        try
        {
            var sessionName = CurrentSession.Name;
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Leaving session: {sessionName}");
            
            await CurrentSession.LeaveAsync();
            CurrentSession = null;
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Successfully left session: {sessionName}");
            OnSessionLeft?.Invoke();
            
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error leaving session: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            
            // Force clear the session even if leave failed
            CurrentSession = null;
            OnSessionLeft?.Invoke();
            
            return false;
        }
    }

    /// <summary>
    /// Forces session cleanup without calling leave (for emergency cleanup).
    /// </summary>
    public void ForceLeaveSession()
    {
        if (CurrentSession != null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, "Force leaving session without proper cleanup");
            CurrentSession = null;
            OnSessionLeft?.Invoke();
        }
    }

    /// <summary>
    /// Deletes/destroys the current session entirely (host authority).
    /// This completely removes the session from Unity Multiplayer Services.
    /// </summary>
    /// <returns>True if session was successfully deleted</returns>
    public async Task<bool> DeleteSessionAsync()
    {
        if (CurrentSession == null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "No active session to delete");
            return true;
        }

        try
        {
            var sessionName = CurrentSession.Name;
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Deleting session: {sessionName}");
            
            // Get host session and delete it entirely
            var hostSession = CurrentSession.AsHost();
            await hostSession.DeleteAsync();
            CurrentSession = null;
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Successfully deleted session: {sessionName}");
            OnSessionLeft?.Invoke();
            
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error deleting session: {ex.Message}";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnSessionError?.Invoke(errorMsg);
            
            // Force clear the session even if delete failed
            CurrentSession = null;
            OnSessionLeft?.Invoke();
            
            return false;
        }
    }
    #endregion

    #region Session State Management
    /// <summary>
    /// Updates session properties for game state transitions.
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <param name="visibility">Property visibility</param>
    /// <returns>True if property was set successfully</returns>
    public async Task<bool> SetSessionPropertyAsync(string key, string value, VisibilityPropertyOptions visibility = VisibilityPropertyOptions.Public)
    {
        if (CurrentSession == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, "Cannot set property - no active session");
            return false;
        }

        try
        {
            var hostSession = CurrentSession.AsHost();
            hostSession.SetProperty(key, new SessionProperty(value, visibility));
            await hostSession.SavePropertiesAsync();
            
            GameLogger.LogDebug(GameLogger.LogCategory.Network, $"Session property set: {key} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"Failed to set session property {key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a session property value.
    /// </summary>
    /// <param name="key">Property key to retrieve</param>
    /// <returns>Property value or null if not found</returns>
    public string GetSessionProperty(string key)
    {
        if (CurrentSession?.Properties.TryGetValue(key, out var property) == true)
        {
            return property.Value;
        }
        return null;
    }

    /// <summary>
    /// Refreshes current session data from the server.
    /// </summary>
    /// <returns>True if refresh was successful</returns>
    public async Task<bool> RefreshSessionAsync()
    {
        if (CurrentSession == null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "No session to refresh");
            return false;
        }

        try
        {
            await CurrentSession.RefreshAsync();
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "Session refreshed successfully");
            return true;
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"Failed to refresh session: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Game Integration
    /// <summary>
    /// Initializes event subscriptions for game integration.
    /// Call this during service setup.
    /// </summary>
    public void InitializeGameEvents()
    {
        // Subscribe to race level events for session cleanup
        RaceLevelManager.OnAllPlayersConnected += OnAllPlayersConnectedHandler;
        GameLogger.LogDebug(GameLogger.LogCategory.Network, "Subscribed to RaceLevelManager.OnAllPlayersConnected event");
    }

    /// <summary>
    /// Handles the event when all players are connected in the race level.
    /// Only processes if this instance is the host to avoid duplicate cleanup.
    /// Delays cleanup to allow clients to finish their join process.
    /// </summary>
    private async void OnAllPlayersConnectedHandler()
    {
        // Double-check host authority for safety
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "Ignoring session cleanup - not host");
            return;
        }

        if (CurrentSession == null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Network, "No active session to cleanup");
            return;
        }

        try
        {
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "All players connected - delaying lobby session cleanup to allow clients to finish joining...");
            
            // Wait a bit to ensure all clients have finished their join process
            // This prevents race condition where session is deleted while clients are still polling
            await Task.Delay(3000); // 3 second delay
            
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Cleanup delay complete - now destroying lobby session as host");
            bool success = await DeleteSessionAsync();
            
            if (success)
            {
                GameLogger.LogInfo(GameLogger.LogCategory.Network, "✅ Session successfully destroyed after all players joined");
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, "⚠️ Session destruction completed with warnings");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't block game - session cleanup is not critical for gameplay
            GameLogger.LogError(GameLogger.LogCategory.Network, $"❌ Error during session cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Unsubscribes from game events. Called during cleanup.
    /// </summary>
    private void CleanupGameEvents()
    {
        RaceLevelManager.OnAllPlayersConnected -= OnAllPlayersConnectedHandler;
        GameLogger.LogDebug(GameLogger.LogCategory.Network, "Unsubscribed from RaceLevelManager events");
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleanup method called during service destruction.
    /// </summary>
    public void Cleanup()
    {
        // Unsubscribe from events first
        CleanupGameEvents();
        
        if (CurrentSession != null)
        {
            // Fire-and-forget cleanup - don't wait for async operation
            _ = LeaveSessionAsync();
        }
    }
    #endregion
}