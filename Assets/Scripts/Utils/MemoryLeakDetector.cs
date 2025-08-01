using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

/// <summary>
/// Memory leak detector that monitors Unity objects and memory usage patterns.
/// Helps identify potential memory leaks and resource management issues.
/// </summary>
public class MemoryLeakDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField, Tooltip("Enable automatic leak detection")]
    private bool enableDetection = true;
    
    [SerializeField, Tooltip("Detection interval in seconds")]
    private float detectionInterval = 30f;
    
    [SerializeField, Tooltip("Minimum objects to consider as potential leak")]
    private int leakThreshold = 50;
    
    [SerializeField, Tooltip("Memory increase threshold in MB")]
    private float memoryThresholdMB = 50f;

    [Header("Debug Settings")]
    [SerializeField, Tooltip("Log detailed object counts")]
    private bool verboseLogging = false;
    
    [SerializeField, Tooltip("Force garbage collection during detection")]
    private bool forceGC = true;

    private Dictionary<System.Type, int> _lastObjectCounts = new Dictionary<System.Type, int>();
    private Dictionary<System.Type, List<int>> _objectCountHistory = new Dictionary<System.Type, List<int>>();
    private List<float> _memoryHistory = new List<float>();
    
    private long _initialMemory;
    private float _lastDetectionTime;
    private bool _isDetecting = false;

    public static MemoryLeakDetector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _initialMemory = GC.GetTotalMemory(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (enableDetection)
        {
            StartCoroutine(DetectionCoroutine());
            GameLogger.LogInfo(GameLogger.LogCategory.General, "MemoryLeakDetector started");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Manually trigger leak detection.
    /// </summary>
    public void TriggerDetection()
    {
        if (!_isDetecting)
        {
            StartCoroutine(DetectLeaks());
        }
    }

    /// <summary>
    /// Get current memory usage in MB.
    /// </summary>
    public float GetCurrentMemoryUsageMB()
    {
        return GC.GetTotalMemory(false) / (1024f * 1024f);
    }

    /// <summary>
    /// Get memory usage increase since start in MB.
    /// </summary>
    public float GetMemoryIncreaseMB()
    {
        return (GC.GetTotalMemory(false) - _initialMemory) / (1024f * 1024f);
    }

    /// <summary>
    /// Get a report of potential memory leaks.
    /// </summary>
    public MemoryLeakReport GenerateReport()
    {
        var report = new MemoryLeakReport
        {
            CurrentMemoryMB = GetCurrentMemoryUsageMB(),
            MemoryIncreaseMB = GetMemoryIncreaseMB(),
            PotentialLeaks = new List<LeakInfo>(),
            ObjectCounts = new Dictionary<System.Type, int>()
        };

        // Get current object counts
        var currentCounts = GetObjectCounts();
        report.ObjectCounts = currentCounts;

        // Analyze for potential leaks
        foreach (var kvp in currentCounts)
        {
            var type = kvp.Key;
            var currentCount = kvp.Value;

            // Check if this type has a history
            if (_objectCountHistory.TryGetValue(type, out List<int> history) && history.Count > 3)
            {
                // Check for consistent increase
                bool consistentIncrease = true;
                for (int i = 1; i < history.Count; i++)
                {
                    if (history[i] <= history[i - 1])
                    {
                        consistentIncrease = false;
                        break;
                    }
                }

                if (consistentIncrease && currentCount > leakThreshold)
                {
                    var leakInfo = new LeakInfo
                    {
                        ObjectType = type,
                        CurrentCount = currentCount,
                        CountHistory = new List<int>(history),
                        Severity = GetLeakSeverity(currentCount, history)
                    };
                    
                    report.PotentialLeaks.Add(leakInfo);
                }
            }
        }

        return report;
    }

    /// <summary>
    /// Detection coroutine that runs periodically.
    /// </summary>
    private IEnumerator DetectionCoroutine()
    {
        while (enableDetection)
        {
            yield return new WaitForSeconds(detectionInterval);
            
            if (!_isDetecting)
            {
                yield return StartCoroutine(DetectLeaks());
            }
        }
    }

    /// <summary>
    /// Perform leak detection analysis.
    /// </summary>
    private IEnumerator DetectLeaks()
    {
        _isDetecting = true;
        
        try
        {
            // Force garbage collection if enabled
            if (forceGC)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                yield return null; // Wait a frame after GC
            }

            // Get current memory usage
            float currentMemoryMB = GetCurrentMemoryUsageMB();
            _memoryHistory.Add(currentMemoryMB);
            
            // Keep only recent history
            if (_memoryHistory.Count > 20)
            {
                _memoryHistory.RemoveAt(0);
            }

            // Get object counts
            var currentCounts = GetObjectCounts();
            
            // Update object count history
            foreach (var kvp in currentCounts)
            {
                if (!_objectCountHistory.ContainsKey(kvp.Key))
                {
                    _objectCountHistory[kvp.Key] = new List<int>();
                }
                
                _objectCountHistory[kvp.Key].Add(kvp.Value);
                
                // Keep only recent history
                if (_objectCountHistory[kvp.Key].Count > 10)
                {
                    _objectCountHistory[kvp.Key].RemoveAt(0);
                }
            }

            // Analyze for potential issues
            AnalyzeMemoryTrends(currentMemoryMB);
            AnalyzeObjectCounts(currentCounts);
            
            _lastObjectCounts = currentCounts;
            _lastDetectionTime = Time.time;
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, $"Error during leak detection: {ex.Message}");
        }
        finally
        {
            _isDetecting = false;
        }
    }

    /// <summary>
    /// Get counts of all Unity objects by type.
    /// </summary>
    private Dictionary<System.Type, int> GetObjectCounts()
    {
        var counts = new Dictionary<System.Type, int>();
        
        // Get all Unity objects
        var allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
        
        foreach (var obj in allObjects)
        {
            if (obj == null) continue;
            
            var type = obj.GetType();
            
            // Skip editor-only objects
            if (type.Namespace != null && type.Namespace.Contains("UnityEditor"))
                continue;
                
            if (!counts.ContainsKey(type))
            {
                counts[type] = 0;
            }
            counts[type]++;
        }
        
        return counts;
    }

    /// <summary>
    /// Analyze memory usage trends.
    /// </summary>
    private void AnalyzeMemoryTrends(float currentMemoryMB)
    {
        if (_memoryHistory.Count < 3) return;

        // Check for consistent memory increase
        float memoryIncrease = GetMemoryIncreaseMB();
        
        if (memoryIncrease > memoryThresholdMB)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"Memory usage increased by {memoryIncrease:F1}MB since start. Current: {currentMemoryMB:F1}MB");
        }

        // Check for rapid memory growth
        if (_memoryHistory.Count >= 5)
        {
            var recentGrowth = _memoryHistory[_memoryHistory.Count - 1] - _memoryHistory[_memoryHistory.Count - 5];
            if (recentGrowth > 20f) // 20MB in recent samples
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, 
                    $"Rapid memory growth detected: {recentGrowth:F1}MB in recent samples");
            }
        }
    }

    /// <summary>
    /// Analyze object count changes.
    /// </summary>
    private void AnalyzeObjectCounts(Dictionary<System.Type, int> currentCounts)
    {
        foreach (var kvp in currentCounts)
        {
            var type = kvp.Key;
            var currentCount = kvp.Value;
            
            // Check for large increases
            if (_lastObjectCounts.TryGetValue(type, out int lastCount))
            {
                int increase = currentCount - lastCount;
                
                if (increase > 20 && currentCount > 50) // Significant increase
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.General, 
                        $"Large increase in {type.Name}: {lastCount} -> {currentCount} (+{increase})");
                }
            }
            
            // Check for consistently high counts
            if (currentCount > leakThreshold)
            {
                if (verboseLogging)
                {
                    GameLogger.LogInfo(GameLogger.LogCategory.General, 
                        $"High object count: {type.Name} = {currentCount}");
                }
            }
        }
    }

    /// <summary>
    /// Determine leak severity based on count and history.
    /// </summary>
    private LeakSeverity GetLeakSeverity(int currentCount, List<int> history)
    {
        if (currentCount > 500) return LeakSeverity.Critical;
        if (currentCount > 200) return LeakSeverity.High;
        if (currentCount > 100) return LeakSeverity.Medium;
        return LeakSeverity.Low;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Leak Report")]
    private void GenerateLeakReportInEditor()
    {
        var report = GenerateReport();
        GameLogger.LogInfo(GameLogger.LogCategory.General, "=== MEMORY LEAK REPORT ===");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Current Memory: {report.CurrentMemoryMB:F1}MB");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Memory Increase: {report.MemoryIncreaseMB:F1}MB");
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Potential Leaks: {report.PotentialLeaks.Count}");
        
        foreach (var leak in report.PotentialLeaks)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"[{leak.Severity}] {leak.ObjectType.Name}: {leak.CurrentCount} objects");
        }
    }

    [ContextMenu("Force Detection")]
    private void ForceDetectionInEditor()
    {
        TriggerDetection();
    }

    [ContextMenu("Clear History")]
    private void ClearHistoryInEditor()
    {
        _objectCountHistory.Clear();
        _memoryHistory.Clear();
        _lastObjectCounts.Clear();
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Memory leak detector history cleared");
    }
#endif
}

/// <summary>
/// Report containing memory leak analysis results.
/// </summary>
public class MemoryLeakReport
{
    public float CurrentMemoryMB;
    public float MemoryIncreaseMB;
    public List<LeakInfo> PotentialLeaks;
    public Dictionary<System.Type, int> ObjectCounts;
}

/// <summary>
/// Information about a potential memory leak.
/// </summary>
public class LeakInfo
{
    public System.Type ObjectType;
    public int CurrentCount;
    public List<int> CountHistory;
    public LeakSeverity Severity;
}

/// <summary>
/// Severity levels for memory leaks.
/// </summary>
public enum LeakSeverity
{
    Low,
    Medium,
    High,
    Critical
}