using System;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Manages background polling operations for lobby including session refresh,
/// game start detection, and error handling with retry logic.
/// Extracted from LobbyManager to provide focused polling functionality.
/// </summary>
public class LobbyPollingService : ThreadSafeSimpleSingleton<LobbyPollingService>
{
    #region Events
    public event Action<List<ISessionInfo>> OnSessionsUpdated;
    public event Action<string> OnGameStartDetected; // Join code parameter
    public event Action<string> OnPollingError;
    #endregion

    #region Properties
    public List<ISessionInfo> AvailableSessions { get; private set; } = new List<ISessionInfo>();
    public bool IsPollingActive { get; private set; } = false;
    public bool IsPollingForGameStart { get; private set; } = false;
    #endregion

    #region Private Fields
    private bool shouldRefreshSessions = true;
    private DateTime lastFetchTime = DateTime.MinValue;
    private const int MIN_FETCH_INTERVAL_SECONDS = 5;
    private const int MAX_POLLING_FAILURES = 5;
    private const int POLLING_INTERVAL_MS = 5000; // 5 seconds
    private const int GAME_START_POLLING_INTERVAL_MS = 10000; // 10 seconds
    private const int ERROR_BACKOFF_MS = 30000; // 30 seconds
    #endregion

    #region Session Discovery Polling
    /// <summary>
    /// Starts the session refresh polling loop.
    /// This runs continuously until stopped or a session is joined.
    /// </summary>
    public async Task StartSessionPollingAsync(LobbySessionService sessionService)
    {
        if (IsPollingActive)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, "Session polling already active");
            return;
        }

        IsPollingActive = true;
        shouldRefreshSessions = true;
        
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Starting session discovery polling");
        
        await RefreshSessionsLoop(sessionService);
    }

    /// <summary>
    /// Stops the session refresh polling.
    /// </summary>
    public void StopSessionPolling()
    {
        shouldRefreshSessions = false;
        IsPollingActive = false;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Session polling stopped");
    }

    /// <summary>
    /// Main session refresh polling loop with error handling and backoff.
    /// </summary>
    private async Task RefreshSessionsLoop(LobbySessionService sessionService)
    {
        while (shouldRefreshSessions && !sessionService.HasActiveSession)
        {
            try
            {
                await FetchAvailableSessionsAsync();
            }
            catch (Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, $"Error in lobby refresh loop: {ex.Message}");
                OnPollingError?.Invoke($"Session refresh error: {ex.Message}");

                // Use exponential backoff on errors to avoid spam
                await Task.Delay(ERROR_BACKOFF_MS);
                continue;
            }

            // Regular polling interval
            await Task.Delay(POLLING_INTERVAL_MS);
        }

        IsPollingActive = false;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "RefreshSessionsLoop stopped - either disabled or session exists");
    }

    /// <summary>
    /// Fetches available sessions from the multiplayer service.
    /// </summary>
    /// <param name="bypassRateLimit">Whether to bypass the rate limiting</param>
    public async Task FetchAvailableSessionsAsync(bool bypassRateLimit = false)
    {
        // Rate limiting safeguard - prevent calls within 5 seconds (unless bypassed)
        if (!bypassRateLimit)
        {
            var timeSinceLastFetch = DateTime.Now - lastFetchTime;
            if (timeSinceLastFetch.TotalSeconds < MIN_FETCH_INTERVAL_SECONDS)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, 
                    $"FetchAvailableSessions called too soon, skipping. Time since last call: {timeSinceLastFetch.TotalSeconds:F1}s");
                return;
            }
        }

        lastFetchTime = DateTime.Now;

        try
        {
            var response = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

            if (response?.Sessions != null)
            {
                AvailableSessions = response.Sessions.ToList();
                OnSessionsUpdated?.Invoke(AvailableSessions);
                
                GameLogger.LogDebug(GameLogger.LogCategory.Network, $"Found {AvailableSessions.Count} available sessions");
            }
            else
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, "Query response or sessions is null");
                AvailableSessions.Clear();
                OnSessionsUpdated?.Invoke(AvailableSessions);
            }
        }
        catch (SessionException ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"Session query failed: {ex.Message}");
            AvailableSessions.Clear();
            OnSessionsUpdated?.Invoke(AvailableSessions);
            OnPollingError?.Invoke($"Session query failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, $"Unexpected error fetching sessions: {ex.Message}");
            AvailableSessions.Clear();
            OnSessionsUpdated?.Invoke(AvailableSessions);
            OnPollingError?.Invoke($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Manual refresh trigger for UI or external systems.
    /// </summary>
    public async Task ManualRefreshSessionsAsync()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Manual session refresh requested");
        await FetchAvailableSessionsAsync(bypassRateLimit: true);
    }
    #endregion

    #region Game Start Detection Polling
    /// <summary>
    /// Starts polling for game start in the current session.
    /// </summary>
    public async Task StartPollingForGameStartAsync(LobbySessionService sessionService)
    {
        if (IsPollingForGameStart)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, "Game start polling already active");
            return;
        }

        if (!sessionService.HasActiveSession)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Cannot start game polling - no active session");
            return;
        }

        IsPollingForGameStart = true;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Starting game start detection polling");
        
        await GameStartPollingLoop(sessionService);
    }

    /// <summary>
    /// Stops polling for game start.
    /// </summary>
    public void StopPollingForGameStart()
    {
        IsPollingForGameStart = false;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Game start polling stopped");
    }

    /// <summary>
    /// Main game start polling loop with failure handling.
    /// </summary>
    private async Task GameStartPollingLoop(LobbySessionService sessionService)
    {
        int failureCount = 0;

        while (IsPollingForGameStart && ShouldContinuePolling(sessionService, failureCount))
        {
            try
            {
                await sessionService.RefreshSessionAsync();
                failureCount = 0; // Reset on success

                if (TryGetGameStartInfo(sessionService, out string joinCode))
                {
                    GameLogger.LogInfo(GameLogger.LogCategory.Network, $"Game start detected with join code: {joinCode}");
                    OnGameStartDetected?.Invoke(joinCode);
                    break;
                }
            }
            catch (Exception ex)
            {
                failureCount = await HandlePollingError(ex, failureCount);
                if (failureCount >= MAX_POLLING_FAILURES)
                    break;
                continue;
            }

            await Task.Delay(GAME_START_POLLING_INTERVAL_MS);
        }

        IsPollingForGameStart = false;

        if (failureCount >= MAX_POLLING_FAILURES)
        {
            var errorMsg = "Game start polling stopped due to repeated failures";
            GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
            OnPollingError?.Invoke(errorMsg);
        }
    }

    /// <summary>
    /// Checks if polling should continue based on session state and failure count.
    /// </summary>
    private bool ShouldContinuePolling(LobbySessionService sessionService, int failureCount)
    {
        return sessionService.HasActiveSession && failureCount < MAX_POLLING_FAILURES;
    }

    /// <summary>
    /// Attempts to get game start information from session properties.
    /// </summary>
    private bool TryGetGameStartInfo(LobbySessionService sessionService, out string joinCode)
    {
        joinCode = null;

        string gameStarted = sessionService.GetSessionProperty("GameStarted");
        if (gameStarted == "true")
        {
            joinCode = sessionService.GetSessionProperty("JoinCode");
            if (!string.IsNullOrEmpty(joinCode))
            {
                return true;
            }
            else
            {
                GameLogger.LogError(GameLogger.LogCategory.Network, "Game started but no JoinCode found in session properties");
                OnPollingError?.Invoke("Game started but no join code available");
            }
        }

        return false;
    }

    /// <summary>
    /// Handles polling errors with exponential backoff.
    /// </summary>
    private async Task<int> HandlePollingError(Exception ex, int currentFailureCount)
    {
        int newFailureCount = currentFailureCount + 1;
        var errorMsg = $"Game start polling failed (attempt {newFailureCount}/{MAX_POLLING_FAILURES}): {ex.Message}";
        
        GameLogger.LogError(GameLogger.LogCategory.Network, errorMsg);
        OnPollingError?.Invoke(errorMsg);

        if (newFailureCount >= MAX_POLLING_FAILURES)
        {
            GameLogger.LogError(GameLogger.LogCategory.Network, "Max polling failures reached, stopping polling");
            return newFailureCount;
        }

        // Exponential backoff for retries
        int backoffDelay = (int)(GAME_START_POLLING_INTERVAL_MS * Math.Pow(2, newFailureCount));
        await Task.Delay(Math.Min(backoffDelay, 60000)); // Cap at 60 seconds

        return newFailureCount;
    }
    #endregion

    #region Lifecycle Management
    /// <summary>
    /// Pauses all polling operations (useful for app pause scenarios).
    /// </summary>
    public void PausePolling()
    {
        shouldRefreshSessions = false;
        IsPollingForGameStart = false;
        GameLogger.LogInfo(GameLogger.LogCategory.Network, "Polling operations paused");
    }

    /// <summary>
    /// Resumes polling operations if no active session exists.
    /// </summary>
    public async Task ResumePollingAsync(LobbySessionService sessionService)
    {
        if (!sessionService.HasActiveSession)
        {
            shouldRefreshSessions = true;
            await StartSessionPollingAsync(sessionService);
            GameLogger.LogInfo(GameLogger.LogCategory.Network, "Polling operations resumed");
        }
    }

    /// <summary>
    /// Cleanup method for service destruction.
    /// </summary>
    public void Cleanup()
    {
        StopSessionPolling();
        StopPollingForGameStart();
        AvailableSessions.Clear();
    }
    #endregion
}