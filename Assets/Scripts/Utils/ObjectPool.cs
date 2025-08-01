using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

/// <summary>
/// Generic object pool for Unity GameObjects to prevent memory leaks from frequent instantiation/destruction.
/// Thread-safe implementation with automatic cleanup and memory management.
/// </summary>
/// <typeparam name="T">The component type to pool</typeparam>
public class ObjectPool<T> : IDisposable where T : Component
{
    private readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();
    private readonly HashSet<T> _activeObjects = new HashSet<T>();
    private readonly object _lock = new object();
    
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly int _initialSize;
    private readonly int _maxSize;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onReturn;
    private readonly Action<T> _onDestroy;
    
    private bool _disposed = false;
    private int _totalCreated = 0;

    /// <summary>
    /// Create a new object pool.
    /// </summary>
    /// <param name="prefab">The prefab to instantiate</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
    /// <param name="parent">Parent transform for pooled objects</param>
    /// <param name="onGet">Called when object is retrieved from pool</param>
    /// <param name="onReturn">Called when object is returned to pool</param>
    /// <param name="onDestroy">Called when object is destroyed</param>
    public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null,
                     Action<T> onGet = null, Action<T> onReturn = null, Action<T> onDestroy = null)
    {
        _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        _initialSize = Mathf.Max(0, initialSize);
        _maxSize = Mathf.Max(0, maxSize);
        _parent = parent;
        _onGet = onGet;
        _onReturn = onReturn;
        _onDestroy = onDestroy;

        // Pre-populate the pool
        PrePopulate();
        
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"ObjectPool<{typeof(T).Name}> created with initial size {_initialSize}, max size {_maxSize}");
    }

    /// <summary>
    /// Get an object from the pool.
    /// </summary>
    public T Get()
    {
        if (_disposed)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, "Cannot get object from disposed pool");
            return null;
        }

        T obj = null;
        
        // Try to get from pool first
        if (_pool.TryDequeue(out obj) && obj != null)
        {
            // Reactivate the object
            obj.gameObject.SetActive(true);
        }
        else
        {
            // Create new if pool is empty
            obj = CreateNewObject();
        }

        if (obj != null)
        {
            lock (_lock)
            {
                _activeObjects.Add(obj);
            }
            
            // Call user-defined get callback
            try
            {
                _onGet?.Invoke(obj);
            }
            catch (Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, $"Error in onGet callback: {ex.Message}");
            }
            
            GameLogger.LogDebug(GameLogger.LogCategory.General, 
                $"ObjectPool<{typeof(T).Name}> Get: Active={_activeObjects.Count}, Pooled={_pool.Count}");
        }

        return obj;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    public void Return(T obj)
    {
        if (_disposed || obj == null)
        {
            return;
        }

        lock (_lock)
        {
            if (!_activeObjects.Remove(obj))
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, 
                    $"Attempted to return object that wasn't tracked by pool: {obj.name}");
                return;
            }
        }

        // Call user-defined return callback
        try
        {
            _onReturn?.Invoke(obj);
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, $"Error in onReturn callback: {ex.Message}");
        }

        // Reset object state
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_parent);

        // Return to pool if under max size limit
        if (_maxSize == 0 || _pool.Count < _maxSize)
        {
            _pool.Enqueue(obj);
            GameLogger.LogDebug(GameLogger.LogCategory.General, 
                $"ObjectPool<{typeof(T).Name}> Return: Active={_activeObjects.Count}, Pooled={_pool.Count}");
        }
        else
        {
            // Destroy if pool is at capacity
            DestroyObject(obj);
        }
    }

    /// <summary>
    /// Return all active objects to the pool.
    /// </summary>
    public void ReturnAll()
    {
        List<T> activeObjectsCopy;
        
        lock (_lock)
        {
            activeObjectsCopy = new List<T>(_activeObjects);
        }

        foreach (T obj in activeObjectsCopy)
        {
            if (obj != null)
            {
                Return(obj);
            }
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.General, 
            $"ObjectPool<{typeof(T).Name}> ReturnAll: Returned {activeObjectsCopy.Count} objects");
    }

    /// <summary>
    /// Clear the pool and destroy all objects.
    /// </summary>
    public void Clear()
    {
        // Return all active objects first
        ReturnAll();

        // Destroy all pooled objects
        while (_pool.TryDequeue(out T obj))
        {
            if (obj != null)
            {
                DestroyObject(obj);
            }
        }

        lock (_lock)
        {
            _activeObjects.Clear();
        }
        
        _totalCreated = 0;
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"ObjectPool<{typeof(T).Name}> cleared");
    }

    /// <summary>
    /// Get pool statistics.
    /// </summary>
    public PoolStats GetStats()
    {
        lock (_lock)
        {
            return new PoolStats
            {
                ActiveCount = _activeObjects.Count,
                PooledCount = _pool.Count,
                TotalCreated = _totalCreated,
                MaxSize = _maxSize
            };
        }
    }

    /// <summary>
    /// Pre-populate the pool with initial objects.
    /// </summary>
    private void PrePopulate()
    {
        for (int i = 0; i < _initialSize; i++)
        {
            T obj = CreateNewObject();
            if (obj != null)
            {
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
    }

    /// <summary>
    /// Create a new object instance.
    /// </summary>
    private T CreateNewObject()
    {
        try
        {
            GameObject go = UnityEngine.Object.Instantiate(_prefab.gameObject, _parent);
            T component = go.GetComponent<T>();
            
            if (component == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, 
                    $"Prefab does not have component of type {typeof(T).Name}");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            _totalCreated++;
            ResourceManager.TrackObject(go, $"PooledObject_{typeof(T).Name}_{_totalCreated}");
            
            return component;
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, 
                $"Failed to create pooled object: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Destroy an object and call cleanup callbacks.
    /// </summary>
    private void DestroyObject(T obj)
    {
        if (obj == null) return;

        try
        {
            _onDestroy?.Invoke(obj);
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, $"Error in onDestroy callback: {ex.Message}");
        }

        ResourceManager.SafeDestroy(obj.gameObject);
    }

    /// <summary>
    /// Dispose of the object pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        Clear();
        _disposed = true;
        
        GC.SuppressFinalize(this);
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"ObjectPool<{typeof(T).Name}> disposed");
    }

    ~ObjectPool()
    {
        if (!_disposed)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"ObjectPool<{typeof(T).Name}> was not properly disposed");
        }
    }
}

/// <summary>
/// Statistics for an object pool.
/// </summary>
public struct PoolStats
{
    public int ActiveCount;
    public int PooledCount;
    public int TotalCreated;
    public int MaxSize;

    public override string ToString()
    {
        return $"Active: {ActiveCount}, Pooled: {PooledCount}, Created: {TotalCreated}, Max: {MaxSize}";
    }
}

/// <summary>
/// Manager for multiple object pools with automatic cleanup.
/// </summary>
public static class ObjectPoolManager
{
    private static readonly Dictionary<string, IDisposable> _pools = new Dictionary<string, IDisposable>();
    private static readonly object _poolsLock = new object();

    /// <summary>
    /// Create or get an existing object pool.
    /// </summary>
    public static ObjectPool<T> GetPool<T>(string poolName, T prefab, int initialSize = 10, int maxSize = 100, 
                                          Transform parent = null, Action<T> onGet = null, Action<T> onReturn = null, 
                                          Action<T> onDestroy = null) where T : Component
    {
        lock (_poolsLock)
        {
            if (_pools.TryGetValue(poolName, out IDisposable existingPool))
            {
                return existingPool as ObjectPool<T>;
            }

            var newPool = new ObjectPool<T>(prefab, initialSize, maxSize, parent, onGet, onReturn, onDestroy);
            _pools[poolName] = newPool;
            
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Created new object pool: {poolName}");
            return newPool;
        }
    }

    /// <summary>
    /// Remove and dispose a pool.
    /// </summary>
    public static void DisposePool(string poolName)
    {
        lock (_poolsLock)
        {
            if (_pools.TryGetValue(poolName, out IDisposable pool))
            {
                pool.Dispose();
                _pools.Remove(poolName);
                GameLogger.LogInfo(GameLogger.LogCategory.General, $"Disposed object pool: {poolName}");
            }
        }
    }

    /// <summary>
    /// Dispose all pools.
    /// </summary>
    public static void DisposeAllPools()
    {
        lock (_poolsLock)
        {
            foreach (var pool in _pools.Values)
            {
                pool?.Dispose();
            }
            _pools.Clear();
            GameLogger.LogInfo(GameLogger.LogCategory.General, "Disposed all object pools");
        }
    }

    /// <summary>
    /// Get statistics for all pools.
    /// </summary>
    public static Dictionary<string, string> GetAllPoolStats()
    {
        var stats = new Dictionary<string, string>();
        
        lock (_poolsLock)
        {
            foreach (var kvp in _pools)
            {
                if (kvp.Value is ObjectPool<Component> pool)
                {
                    try
                    {
                        var poolStats = pool.GetStats();
                        stats[kvp.Key] = poolStats.ToString();
                    }
                    catch (Exception ex)
                    {
                        stats[kvp.Key] = $"Error: {ex.Message}";
                    }
                }
            }
        }
        
        return stats;
    }

    // Register cleanup with ResourceManager
    static ObjectPoolManager()
    {
        ResourceManager.RegisterCleanupAction(DisposeAllPools);
    }
}