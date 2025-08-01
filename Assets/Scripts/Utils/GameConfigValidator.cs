using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Validates game configuration and settings at runtime.
/// Ensures all constants and settings are within acceptable ranges and provides fallbacks.
/// </summary>
public static class GameConfigValidator
{
    public struct ValidationResult
    {
        public bool IsValid;
        public List<string> Warnings;
        public List<string> Errors;
        public int WarningCount => Warnings?.Count ?? 0;
        public int ErrorCount => Errors?.Count ?? 0;
    }

    /// <summary>
    /// Validate all game configuration and return detailed results.
    /// </summary>
    public static ValidationResult ValidateAllConfiguration()
    {
        var result = new ValidationResult
        {
            Warnings = new List<string>(),
            Errors = new List<string>()
        };

        // Validate different configuration categories
        ValidateNetworkingConfig(result);
        ValidateGraphicsConfig(result);
        ValidateMovementConfig(result);
        ValidateDebugConfig(result);
        ValidateUnitySettings(result);
        ValidateSystemRequirements(result);

        result.IsValid = result.ErrorCount == 0;

        // Log validation results
        LogValidationResults(result);

        return result;
    }

    private static void ValidateNetworkingConfig(ValidationResult result)
    {
        // Validate max players
        if (GameConstants.Networking.DEFAULT_MAX_PLAYERS <= 0 || GameConstants.Networking.DEFAULT_MAX_PLAYERS > 10)
        {
            result.Errors.Add($"Invalid DEFAULT_MAX_PLAYERS: {GameConstants.Networking.DEFAULT_MAX_PLAYERS}. Must be between 1 and 10.");
        }

        // Validate polling intervals
        if (GameConstants.Networking.LOBBY_POLLING_INTERVAL <= 0 || GameConstants.Networking.LOBBY_POLLING_INTERVAL > 10)
        {
            result.Warnings.Add($"LOBBY_POLLING_INTERVAL ({GameConstants.Networking.LOBBY_POLLING_INTERVAL}s) may cause performance issues. Recommended: 0.5-5.0s");
        }

        if (GameConstants.Networking.PLAYER_WAIT_POLLING_INTERVAL <= 0)
        {
            result.Errors.Add($"Invalid PLAYER_WAIT_POLLING_INTERVAL: {GameConstants.Networking.PLAYER_WAIT_POLLING_INTERVAL}");
        }

        // Check if relay connections match max players
        int expectedRelayConnections = GameConstants.Networking.DEFAULT_MAX_PLAYERS - 1;
        if (GameConstants.Networking.RELAY_MAX_CONNECTIONS != expectedRelayConnections)
        {
            result.Warnings.Add($"RELAY_MAX_CONNECTIONS ({GameConstants.Networking.RELAY_MAX_CONNECTIONS}) doesn't match DEFAULT_MAX_PLAYERS-1 ({expectedRelayConnections})");
        }
    }

    private static void ValidateGraphicsConfig(ValidationResult result)
    {
        // Validate target frame rate
        if (GameConstants.Graphics.TARGET_FRAME_RATE <= 0 || GameConstants.Graphics.TARGET_FRAME_RATE > 300)
        {
            result.Warnings.Add($"TARGET_FRAME_RATE ({GameConstants.Graphics.TARGET_FRAME_RATE}) is outside typical range (30-144)");
        }

        // Validate VSync setting
        if (GameConstants.Graphics.VSYNC_DISABLED < 0 || GameConstants.Graphics.VSYNC_DISABLED > 4)
        {
            result.Errors.Add($"Invalid VSYNC_DISABLED value: {GameConstants.Graphics.VSYNC_DISABLED}. Must be 0-4.");
        }

        // Validate scene names
        if (string.IsNullOrWhiteSpace(GameConstants.Graphics.SCENE_RACE_LEVEL))
        {
            result.Errors.Add("SCENE_RACE_LEVEL cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(GameConstants.Graphics.SCENE_CHARACTER_CUSTOMIZER))
        {
            result.Errors.Add("SCENE_CHARACTER_CUSTOMIZER cannot be null or empty");
        }
    }

    private static void ValidateMovementConfig(ValidationResult result)
    {
        // Validate ground check distance
        if (GameConstants.Movement.GROUND_CHECK_RAY_DISTANCE <= 0 || GameConstants.Movement.GROUND_CHECK_RAY_DISTANCE > 10)
        {
            result.Warnings.Add($"GROUND_CHECK_RAY_DISTANCE ({GameConstants.Movement.GROUND_CHECK_RAY_DISTANCE}) may not work well. Recommended: 0.5-3.0");
        }

        // Validate movement threshold
        if (GameConstants.Movement.MOVEMENT_DIRECTION_THRESHOLD <= 0 || GameConstants.Movement.MOVEMENT_DIRECTION_THRESHOLD > 1)
        {
            result.Warnings.Add($"MOVEMENT_DIRECTION_THRESHOLD ({GameConstants.Movement.MOVEMENT_DIRECTION_THRESHOLD}) outside typical range (0.01-0.5)");
        }
    }

    private static void ValidateDebugConfig(ValidationResult result)
    {
        // Validate log settings
        if (GameConstants.Debug.LOG_LIFETIME_SECONDS <= 0 || GameConstants.Debug.LOG_LIFETIME_SECONDS > 60)
        {
            result.Warnings.Add($"LOG_LIFETIME_SECONDS ({GameConstants.Debug.LOG_LIFETIME_SECONDS}) outside typical range (1-30)");
        }

        if (GameConstants.Debug.MAX_DEBUG_LOG_LINES <= 0 || GameConstants.Debug.MAX_DEBUG_LOG_LINES > 1000)
        {
            result.Warnings.Add($"MAX_DEBUG_LOG_LINES ({GameConstants.Debug.MAX_DEBUG_LOG_LINES}) may impact performance. Recommended: 10-100");
        }

        if (GameConstants.Debug.DEBUG_FONT_SIZE <= 0 || GameConstants.Debug.DEBUG_FONT_SIZE > 50)
        {
            result.Warnings.Add($"DEBUG_FONT_SIZE ({GameConstants.Debug.DEBUG_FONT_SIZE}) outside typical range (8-24)");
        }

        if (GameConstants.Debug.DEBUG_BACKGROUND_ALPHA < 0 || GameConstants.Debug.DEBUG_BACKGROUND_ALPHA > 1)
        {
            result.Errors.Add($"DEBUG_BACKGROUND_ALPHA ({GameConstants.Debug.DEBUG_BACKGROUND_ALPHA}) must be between 0 and 1");
        }
    }

    private static void ValidateUnitySettings(ValidationResult result)
    {
        // Check Quality Settings
        if (QualitySettings.vSyncCount != GameConstants.Graphics.VSYNC_DISABLED)
        {
            result.Warnings.Add($"Unity VSync setting ({QualitySettings.vSyncCount}) doesn't match GameConstants.Graphics.VSYNC_DISABLED ({GameConstants.Graphics.VSYNC_DISABLED})");
        }

        if (Application.targetFrameRate != GameConstants.Graphics.TARGET_FRAME_RATE && Application.targetFrameRate != -1)
        {
            result.Warnings.Add($"Unity target frame rate ({Application.targetFrameRate}) doesn't match GameConstants.Graphics.TARGET_FRAME_RATE ({GameConstants.Graphics.TARGET_FRAME_RATE})");
        }

        // Check if scenes exist in build settings
        var buildScenes = new HashSet<string>();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            buildScenes.Add(sceneName);
        }

        if (!buildScenes.Contains(GameConstants.Graphics.SCENE_RACE_LEVEL))
        {
            result.Errors.Add($"Scene '{GameConstants.Graphics.SCENE_RACE_LEVEL}' not found in build settings");
        }

        if (!buildScenes.Contains(GameConstants.Graphics.SCENE_CHARACTER_CUSTOMIZER))
        {
            result.Errors.Add($"Scene '{GameConstants.Graphics.SCENE_CHARACTER_CUSTOMIZER}' not found in build settings");
        }
    }

    private static void ValidateSystemRequirements(ValidationResult result)
    {
        // Check available memory
        if (SystemInfo.systemMemorySize < 2048) // Less than 2GB
        {
            result.Warnings.Add($"Low system memory: {SystemInfo.systemMemorySize}MB. Game may experience performance issues.");
        }

        // Check graphics capabilities
        if (SystemInfo.graphicsMemorySize < 512) // Less than 512MB VRAM
        {
            result.Warnings.Add($"Low graphics memory: {SystemInfo.graphicsMemorySize}MB. Visual quality may be limited.");
        }

        // Check platform-specific requirements
        ValidatePlatformRequirements(result);
    }

    private static void ValidatePlatformRequirements(ValidationResult result)
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                if (SystemInfo.systemMemorySize < 3072) // Less than 3GB on Android
                {
                    result.Warnings.Add("Android device may struggle with multiplayer features due to limited memory");
                }
                break;

            case RuntimePlatform.IPhonePlayer:
                // iOS-specific checks could go here
                break;

            case RuntimePlatform.WebGLPlayer:
                result.Warnings.Add("WebGL platform may have limitations with networking features");
                break;
        }
    }

    private static void LogValidationResults(ValidationResult result)
    {
        if (result.IsValid && result.WarningCount == 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, "All configuration validation passed ✓");
        }
        else
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, 
                $"Configuration validation complete - Errors: {result.ErrorCount}, Warnings: {result.WarningCount}");

            foreach (string error in result.Errors)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, $"Config Error: {error}");
            }

            foreach (string warning in result.Warnings)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, $"Config Warning: {warning}");
            }
        }
    }

    /// <summary>
    /// Apply safe defaults for any invalid configuration values.
    /// </summary>
    public static void ApplySafeDefaults()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Applying safe configuration defaults");

        // Apply Unity settings that match our constants
        QualitySettings.vSyncCount = GameConstants.Graphics.VSYNC_DISABLED;
        Application.targetFrameRate = GameConstants.Graphics.TARGET_FRAME_RATE;
        
        // Additional safe defaults
        Time.fixedDeltaTime = 0.02f; // 50 Hz physics
        
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Safe defaults applied");
    }

    /// <summary>
    /// Get a summary of current configuration values.
    /// </summary>
    public static string GetConfigurationSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== Game Configuration Summary ===");
        summary.AppendLine($"Max Players: {GameConstants.Networking.DEFAULT_MAX_PLAYERS}");
        summary.AppendLine($"Lobby Polling: {GameConstants.Networking.LOBBY_POLLING_INTERVAL}s");
        summary.AppendLine($"Target FPS: {GameConstants.Graphics.TARGET_FRAME_RATE}");
        summary.AppendLine($"VSync: {(GameConstants.Graphics.VSYNC_DISABLED == 0 ? "Disabled" : "Enabled")}");
        summary.AppendLine($"Ground Check Distance: {GameConstants.Movement.GROUND_CHECK_RAY_DISTANCE}");
        summary.AppendLine($"Debug Log Lines: {GameConstants.Debug.MAX_DEBUG_LOG_LINES}");
        summary.AppendLine($"Unity Target FPS: {Application.targetFrameRate}");
        summary.AppendLine($"Unity VSync: {QualitySettings.vSyncCount}");
        summary.AppendLine($"Physics Rate: {1f / Time.fixedDeltaTime:F0} Hz");
        return summary.ToString();
    }
}

/// <summary>
/// Component to automatically validate configuration at game start.
/// </summary>
public class GameConfigValidatorInitializer : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField, Tooltip("Validate configuration on Start")]
    private bool validateOnStart = true;
    
    [SerializeField, Tooltip("Apply safe defaults if validation fails")]
    private bool applySafeDefaults = true;
    
    [SerializeField, Tooltip("Log configuration summary")]
    private bool logConfigSummary = true;

    private void Start()
    {
        if (validateOnStart)
        {
            ValidateConfiguration();
        }
        
        if (logConfigSummary)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, GameConfigValidator.GetConfigurationSummary());
        }
    }

    private void ValidateConfiguration()
    {
        var result = GameConfigValidator.ValidateAllConfiguration();
        
        if (!result.IsValid && applySafeDefaults)
        {
            GameConfigValidator.ApplySafeDefaults();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Configuration")]
    private void ValidateConfigurationFromMenu()
    {
        ValidateConfiguration();
    }

    [ContextMenu("Log Config Summary")]
    private void LogConfigSummaryFromMenu()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, GameConfigValidator.GetConfigurationSummary());
    }
#endif
}