using UnityEngine;
using System;

/// <summary>
/// Thread-safe singleton base class for MonoBehaviour singletons.
/// Provides proper locking and initialization patterns to prevent race conditions.
/// </summary>
/// <typeparam name="T">The singleton type</typeparam>
public abstract class ThreadSafeSingleton<T> : MonoBehaviour where T : ThreadSafeSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    private static bool _isInitialized = false;

    /// <summary>
    /// Gets the singleton instance in a thread-safe manner.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, 
                    $"[Singleton] Instance of {typeof(T)} already destroyed on application quit. Won't create again.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindExistingInstance();
                    
                    if (_instance == null)
                    {
                        _instance = CreateNewInstance();
                    }
                    
                    if (_instance != null && !_isInitialized)
                    {
                        _instance.Initialize();
                        _isInitialized = true;
                    }
                }
                
                return _instance;
            }
        }
    }

    /// <summary>
    /// Check if the singleton instance exists without creating it.
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            lock (_lock)
            {
                return _instance != null && !_applicationIsQuitting;
            }
        }
    }

    /// <summary>
    /// Virtual method called during singleton initialization.
    /// Override in derived classes for custom initialization logic.
    /// </summary>
    protected virtual void Initialize()
    {
        GameLogger.LogDebug(GameLogger.LogCategory.General, $"{typeof(T).Name} singleton initialized");
    }

    /// <summary>
    /// Virtual method called when singleton is being destroyed.
    /// Override in derived classes for custom cleanup logic.
    /// </summary>
    protected virtual void OnSingletonDestroyed()
    {
        GameLogger.LogDebug(GameLogger.LogCategory.General, $"{typeof(T).Name} singleton destroyed");
    }

    protected virtual void Awake()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                
                if (!_isInitialized)
                {
                    Initialize();
                    _isInitialized = true;
                }
            }
            else if (_instance != this)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, 
                    $"[Singleton] Duplicate instance of {typeof(T)} found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        lock (_lock)
        {
            if (_instance == this)
            {
                OnSingletonDestroyed();
                _instance = null;
                _isInitialized = false;
            }
        }
    }

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    /// <summary>
    /// Find existing instance in the scene.
    /// </summary>
    private static T FindExistingInstance()
    {
        T[] instances = FindObjectsOfType<T>();
        
        if (instances.Length > 1)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"[Singleton] Multiple instances of {typeof(T)} found. This should not happen.");
            
            // Keep the first instance and destroy others
            for (int i = 1; i < instances.Length; i++)
            {
                Destroy(instances[i].gameObject);
            }
        }
        
        return instances.Length > 0 ? instances[0] : null;
    }

    /// <summary>
    /// Create a new singleton instance if none exists.
    /// This should only be called from the main thread.
    /// </summary>
    private static T CreateNewInstance()
    {
        if (!Application.isPlaying)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"[Singleton] Cannot create instance of {typeof(T)} outside of play mode.");
            return null;
        }

        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
        T instance = singletonObject.AddComponent<T>();
        
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"[Singleton] Created new instance of {typeof(T)}");
        
        return instance;
    }

    /// <summary>
    /// Force destroy the singleton instance (for testing purposes).
    /// </summary>
    public static void DestroyInstance()
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(_instance.gameObject);
                }
                
                _instance = null;
                _isInitialized = false;
            }
        }
    }
}

/// <summary>
/// Thread-safe singleton for NetworkBehaviour components.
/// Provides proper initialization for networked singletons.
/// </summary>
/// <typeparam name="T">The singleton type</typeparam>
public abstract class ThreadSafeNetworkSingleton<T> : Unity.Netcode.NetworkBehaviour where T : ThreadSafeNetworkSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    private static bool _isInitialized = false;

    /// <summary>
    /// Gets the singleton instance in a thread-safe manner.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, 
                    $"[NetworkSingleton] Instance of {typeof(T)} already destroyed on application quit.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindExistingInstance();
                    
                    if (_instance != null && !_isInitialized)
                    {
                        _instance.Initialize();
                        _isInitialized = true;
                    }
                }
                
                return _instance;
            }
        }
    }

    /// <summary>
    /// Check if the singleton instance exists without creating it.
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            lock (_lock)
            {
                return _instance != null && !_applicationIsQuitting;
            }
        }
    }

    /// <summary>
    /// Virtual method called during singleton initialization.
    /// </summary>
    protected virtual void Initialize()
    {
        GameLogger.LogDebug(GameLogger.LogCategory.Network, $"{typeof(T).Name} network singleton initialized");
    }

    /// <summary>
    /// Virtual method called when singleton is being destroyed.
    /// </summary>
    protected virtual void OnSingletonDestroyed()
    {
        GameLogger.LogDebug(GameLogger.LogCategory.Network, $"{typeof(T).Name} network singleton destroyed");
    }

    protected virtual void Awake()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = this as T;
                
                if (!_isInitialized)
                {
                    Initialize();
                    _isInitialized = true;
                }
            }
            else if (_instance != this)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Network, 
                    $"[NetworkSingleton] Duplicate instance of {typeof(T)} found. Destroying duplicate.");
                    
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    public override void OnDestroy()
    {
        lock (_lock)
        {
            if (_instance == this)
            {
                OnSingletonDestroyed();
                _instance = null;
                _isInitialized = false;
            }
        }
        
        base.OnDestroy();
    }

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    /// <summary>
    /// Find existing instance in the scene.
    /// </summary>
    private static T FindExistingInstance()
    {
        T[] instances = FindObjectsOfType<T>();
        
        if (instances.Length > 1)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Network, 
                $"[NetworkSingleton] Multiple instances of {typeof(T)} found. This should not happen.");
            
            // Keep the first instance and destroy others
            for (int i = 1; i < instances.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(instances[i].gameObject);
                }
            }
        }
        
        return instances.Length > 0 ? instances[0] : null;
    }

    /// <summary>
    /// Force destroy the singleton instance (for testing purposes).
    /// </summary>
    public static void DestroyInstance()
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(_instance.gameObject);
                }
                
                _instance = null;
                _isInitialized = false;
            }
        }
    }
}

/// <summary>
/// Simple thread-safe singleton for non-MonoBehaviour classes.
/// </summary>
/// <typeparam name="T">The singleton type</typeparam>
public abstract class ThreadSafeSimpleSingleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Check if the singleton instance exists.
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            lock (_lock)
            {
                return _instance != null;
            }
        }
    }

    /// <summary>
    /// Destroy the singleton instance.
    /// </summary>
    public static void DestroyInstance()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}