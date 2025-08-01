using UnityEngine;
using System;
using System.IO;
using System.Text;

/// <summary>
/// Centralized logging system for game events, performance metrics, and debugging.
/// Provides structured logging with categories, levels, and optional file output.
/// </summary>
public static class GameLogger
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public enum LogCategory
    {
        General,
        Network,
        Audio,
        Input,
        Performance,
        Purchase,
        Authentication,
        UI,
        Gameplay
    }

    private static LogLevel currentLogLevel = LogLevel.Debug;
    private static bool enableFileLogging = false;
    private static string logFilePath = "";
    private static readonly object logLock = new object();

    /// <summary>
    /// Initialize the logging system with specified settings.
    /// </summary>
    public static void Initialize(LogLevel minLogLevel = LogLevel.Debug, bool enableFile = false)
    {
        currentLogLevel = minLogLevel;
        enableFileLogging = enableFile;

        if (enableFileLogging)
        {
            try
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                Directory.CreateDirectory(logDirectory);
                logFilePath = Path.Combine(logDirectory, $"game_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                LogInfo(LogCategory.General, "GameLogger initialized with file logging enabled");
                LogInfo(LogCategory.General, $"Log file: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize file logging: {ex.Message}");
                enableFileLogging = false;
            }
        }
        else
        {
            LogInfo(LogCategory.General, "GameLogger initialized with console logging only");
        }
    }

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void LogDebug(LogCategory category, string message, UnityEngine.Object context = null)
    {
        Log(LogLevel.Debug, category, message, context);
    }

    /// <summary>
    /// Log an informational message.
    /// </summary>
    public static void LogInfo(LogCategory category, string message, UnityEngine.Object context = null)
    {
        Log(LogLevel.Info, category, message, context);
    }

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogWarning(LogCategory category, string message, UnityEngine.Object context = null)
    {
        Log(LogLevel.Warning, category, message, context);
    }

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void LogError(LogCategory category, string message, UnityEngine.Object context = null)
    {
        Log(LogLevel.Error, category, message, context);
    }

    /// <summary>
    /// Log a critical error message.
    /// </summary>
    public static void LogCritical(LogCategory category, string message, UnityEngine.Object context = null)
    {
        Log(LogLevel.Critical, category, message, context);
    }

    /// <summary>
    /// Log performance metrics.
    /// </summary>
    public static void LogPerformance(string operationName, float durationMs, string additionalInfo = "")
    {
        string message = $"PERF: {operationName} took {durationMs:F2}ms";
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            message += $" | {additionalInfo}";
        }
        Log(LogLevel.Info, LogCategory.Performance, message);
    }

    /// <summary>
    /// Log network events with connection info.
    /// </summary>
    public static void LogNetwork(string eventName, string details = "", LogLevel level = LogLevel.Info)
    {
        string message = $"NET: {eventName}";
        if (!string.IsNullOrEmpty(details))
        {
            message += $" | {details}";
        }
        Log(level, LogCategory.Network, message);
    }

    /// <summary>
    /// Log user interactions and UI events.
    /// </summary>
    public static void LogUserAction(string action, string details = "")
    {
        string message = $"USER: {action}";
        if (!string.IsNullOrEmpty(details))
        {
            message += $" | {details}";
        }
        Log(LogLevel.Info, LogCategory.UI, message);
    }

    /// <summary>
    /// Core logging method that handles all log output.
    /// </summary>
    private static void Log(LogLevel level, LogCategory category, string message, UnityEngine.Object context = null)
    {
        if (level < currentLogLevel)
            return;

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string categoryStr = category.ToString().ToUpper();
            string levelStr = level.ToString().ToUpper();
            string formattedMessage = $"[{timestamp}] [{levelStr}] [{categoryStr}] {message}";

            // Console output with Unity's logging system
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(formattedMessage, context);
                    break;
            }

            // File output if enabled
            if (enableFileLogging && !string.IsNullOrEmpty(logFilePath))
            {
                WriteToFile(formattedMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameLogger internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Write log message to file (thread-safe).
    /// </summary>
    private static void WriteToFile(string message)
    {
        try
        {
            lock (logLock)
            {
                File.AppendAllText(logFilePath, message + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to write to log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current log file path (if file logging is enabled).
    /// </summary>
    public static string GetLogFilePath()
    {
        return enableFileLogging ? logFilePath : null;
    }

    /// <summary>
    /// Change the minimum log level at runtime.
    /// </summary>
    public static void SetLogLevel(LogLevel newLevel)
    {
        LogLevel oldLevel = currentLogLevel;
        currentLogLevel = newLevel;
        LogInfo(LogCategory.General, $"Log level changed from {oldLevel} to {newLevel}");
    }

    /// <summary>
    /// Performance helper - measures execution time of an action.
    /// </summary>
    public static void MeasurePerformance(string operationName, System.Action action, string additionalInfo = "")
    {
        if (action == null)
        {
            LogError(LogCategory.Performance, $"Cannot measure performance of null action: {operationName}");
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            LogError(LogCategory.Performance, $"Exception during performance measurement of {operationName}: {ex.Message}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogPerformance(operationName, stopwatch.ElapsedMilliseconds, additionalInfo);
        }
    }
}