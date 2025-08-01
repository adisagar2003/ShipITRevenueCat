using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages resource lifecycle and disposal for better memory management.
/// Provides utilities for tracking and cleaning up resources across the game.
/// </summary>
public static class ResourceManager
{
    private static readonly Dictionary<string, WeakReference> trackedObjects = new Dictionary<string, WeakReference>();
    private static readonly List<Action> cleanupActions = new List<Action>();
    private static int nextId = 1;

    /// <summary>
    /// Track an object for automatic cleanup monitoring.
    /// </summary>
    public static string TrackObject(UnityEngine.Object obj, string name = null)
    {
        if (obj == null) return null;

        string id = name ?? $"TrackedObject_{nextId++}";
        trackedObjects[id] = new WeakReference(obj);
        
        GameLogger.LogDebug(GameLogger.LogCategory.General, $"Tracking object: {id} ({obj.GetType().Name})");
        
        return id;
    }

    /// <summary>
    /// Stop tracking an object.
    /// </summary>
    public static void UntrackObject(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        if (trackedObjects.Remove(id))
        {
            GameLogger.LogDebug(GameLogger.LogCategory.General, $"Stopped tracking object: {id}");
        }
    }

    /// <summary>
    /// Register a cleanup action to be called during application shutdown.
    /// </summary>
    public static void RegisterCleanupAction(Action cleanupAction)
    {
        if (cleanupAction != null)
        {
            cleanupActions.Add(cleanupAction);
        }
    }

    /// <summary>
    /// Check for objects that have been garbage collected.
    /// </summary>
    public static void CheckTrackedObjects()
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in trackedObjects)
        {
            if (!kvp.Value.IsAlive)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            trackedObjects.Remove(key);
            GameLogger.LogDebug(GameLogger.LogCategory.General, $"Tracked object was garbage collected: {key}");
        }
        
        if (keysToRemove.Count > 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Cleaned up {keysToRemove.Count} garbage collected objects");
        }
    }

    /// <summary>
    /// Get memory usage information.
    /// </summary>
    public static MemoryInfo GetMemoryInfo()
    {
        return new MemoryInfo
        {
            ManagedMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024),
            TrackedObjectsCount = trackedObjects.Count,
            CleanupActionsCount = cleanupActions.Count
        };
    }

    /// <summary>
    /// Force garbage collection and cleanup.
    /// Use sparingly as it can cause performance hitches.
    /// </summary>
    public static void ForceCleanup()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Forcing garbage collection and cleanup");
        
        CheckTrackedObjects();
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        var memInfo = GetMemoryInfo();
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"Cleanup complete - Memory: {memInfo.ManagedMemoryMB}MB, Tracked: {memInfo.TrackedObjectsCount}");
    }

    /// <summary>
    /// Execute all registered cleanup actions.
    /// Called automatically during application shutdown.
    /// </summary>
    public static void ExecuteCleanupActions()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Executing {cleanupActions.Count} cleanup actions");
        
        foreach (var action in cleanupActions)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, $"Error in cleanup action: {ex.Message}");
            }
        }
        
        cleanupActions.Clear();
    }

    /// <summary>
    /// Helper method to safely destroy Unity objects.
    /// </summary>
    public static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;

        try
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, $"Error destroying object {obj.name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a disposable wrapper for Unity objects.
    /// </summary>
    public static DisposableWrapper<T> CreateDisposable<T>(T obj) where T : UnityEngine.Object
    {
        return new DisposableWrapper<T>(obj);
    }

    public struct MemoryInfo
    {
        public long ManagedMemoryMB;
        public int TrackedObjectsCount;
        public int CleanupActionsCount;
    }
}

/// <summary>
/// Disposable wrapper for Unity objects to ensure proper cleanup.
/// </summary>
public class DisposableWrapper<T> : IDisposable where T : UnityEngine.Object
{
    private T obj;
    private bool disposed = false;

    public DisposableWrapper(T obj)
    {
        this.obj = obj;
    }

    public T Object => disposed ? null : obj;

    public void Dispose()
    {
        if (!disposed)
        {
            ResourceManager.SafeDestroy(obj);
            obj = null;
            disposed = true;
        }
    }
}

/// <summary>
/// MonoBehaviour that automatically handles resource cleanup on application shutdown.
/// Add this to a GameObject in your first scene.
/// </summary>
public class ResourceManagerInitializer : MonoBehaviour
{
    [Header("Resource Management")]
    [SerializeField, Tooltip("Enable periodic memory cleanup")]
    private bool enablePeriodicCleanup = true;
    
    [SerializeField, Tooltip("Interval between cleanup checks in seconds")]
    private float cleanupInterval = 60f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        if (enablePeriodicCleanup)
        {
            InvokeRepeating(nameof(PeriodicCleanup), cleanupInterval, cleanupInterval);
        }
    }

    private void PeriodicCleanup()
    {
        ResourceManager.CheckTrackedObjects();
    }

    private void OnApplicationQuit()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Application quitting - executing cleanup actions");
        ResourceManager.ExecuteCleanupActions();
    }

    private void OnDestroy()
    {
        ResourceManager.ExecuteCleanupActions();
    }

#if UNITY_EDITOR
    [ContextMenu("Force Cleanup")]
    private void ForceCleanupFromMenu()
    {
        ResourceManager.ForceCleanup();
    }

    [ContextMenu("Log Memory Info")]
    private void LogMemoryInfo()
    {
        var memInfo = ResourceManager.GetMemoryInfo();
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"Memory Info - Managed: {memInfo.ManagedMemoryMB}MB, Tracked Objects: {memInfo.TrackedObjectsCount}, Cleanup Actions: {memInfo.CleanupActionsCount}");
    }
#endif
}