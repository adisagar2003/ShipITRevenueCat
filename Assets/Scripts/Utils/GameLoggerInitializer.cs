using UnityEngine;

/// <summary>
/// Initializes the GameLogger system at application start.
/// This component should be placed on a GameObject in the first scene.
/// </summary>
public class GameLoggerInitializer : MonoBehaviour
{
    [Header("Logging Configuration")]
    [SerializeField, Tooltip("Minimum log level to display and record")]
    private GameLogger.LogLevel minimumLogLevel = GameLogger.LogLevel.Debug;
    
    [SerializeField, Tooltip("Enable file logging to persistent data path")]
    private bool enableFileLogging = false;
    
    [SerializeField, Tooltip("Enable file logging only in development builds")]
    private bool fileLoggingOnlyInDevelopment = true;
    
    [SerializeField, Tooltip("Log system performance metrics")]
    private bool enablePerformanceLogging = true;

    private void Awake()
    {
        // Ensure this runs early in the application lifecycle
        InitializeLogging();
        
        // Make this persistent across scenes
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeLogging()
    {
        // Determine if file logging should be enabled
        bool shouldEnableFileLogging = enableFileLogging;
        
        if (fileLoggingOnlyInDevelopment && !Debug.isDebugBuild)
        {
            shouldEnableFileLogging = false;
        }

        // Initialize the logging system
        GameLogger.Initialize(minimumLogLevel, shouldEnableFileLogging);
        
        // Log system information
        LogSystemInfo();
        
        // Set up performance logging if enabled
        if (enablePerformanceLogging)
        {
            SetupPerformanceLogging();
        }
    }

    private void LogSystemInfo()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Game started - Version: {Application.version}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Unity Version: {Application.unityVersion}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Platform: {Application.platform}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Device Model: {SystemInfo.deviceModel}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Operating System: {SystemInfo.operatingSystem}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Memory: {SystemInfo.systemMemorySize}MB");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Graphics: {SystemInfo.graphicsDeviceName}");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Graphics Memory: {SystemInfo.graphicsMemorySize}MB");
        
        if (enableFileLogging)
        {
            string logPath = GameLogger.GetLogFilePath();
            if (!string.IsNullOrEmpty(logPath))
            {
                GameLogger.LogInfo(GameLogger.LogCategory.General, $"Log file: {logPath}");
            }
        }
    }

    private void SetupPerformanceLogging()
    {
        // Log frame rate periodically
        InvokeRepeating(nameof(LogPerformanceMetrics), 10f, 30f);
    }

    private void LogPerformanceMetrics()
    {
        if (!enablePerformanceLogging) return;

        float fps = 1.0f / Time.deltaTime;
        long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024); // MB
        
        GameLogger.LogPerformance("PeriodicMetrics", 0f, $"FPS: {fps:F1}, Memory: {memoryUsage}MB");
        
        // Log additional Unity-specific metrics
        if (Application.isPlaying)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.Performance, 
                $"Time Scale: {Time.timeScale}, Fixed Delta: {Time.fixedDeltaTime:F4}");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Application {(pauseStatus ? "paused" : "resumed")}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Application {(hasFocus ? "gained" : "lost")} focus");
    }

    /// <summary>
    /// Public method to change log level at runtime (useful for debugging).
    /// </summary>
    public void SetLogLevel(int logLevel)
    {
        if (logLevel >= 0 && logLevel <= 4)
        {
            GameLogger.SetLogLevel((GameLogger.LogLevel)logLevel);
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, $"Invalid log level: {logLevel}");
        }
    }

    /// <summary>
    /// Toggle performance logging at runtime.
    /// </summary>
    public void TogglePerformanceLogging()
    {
        enablePerformanceLogging = !enablePerformanceLogging;
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"Performance logging {(enablePerformanceLogging ? "enabled" : "disabled")}");
            
        if (enablePerformanceLogging)
        {
            SetupPerformanceLogging();
        }
        else
        {
            CancelInvoke(nameof(LogPerformanceMetrics));
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Log Test Messages")]
    private void LogTestMessages()
    {
        GameLogger.LogDebug(GameLogger.LogCategory.General, "This is a debug message");
        GameLogger.LogInfo(GameLogger.LogCategory.General, "This is an info message");
        GameLogger.LogWarning(GameLogger.LogCategory.General, "This is a warning message");
        GameLogger.LogError(GameLogger.LogCategory.General, "This is an error message");
        GameLogger.LogCritical(GameLogger.LogCategory.General, "This is a critical message");
        
        GameLogger.MeasurePerformance("Test Operation", () => {
            System.Threading.Thread.Sleep(10); // Simulate work
        });
    }
#endif
}