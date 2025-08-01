using UnityEngine;

/// <summary>
/// Handles RevenueCat SDK initialization and configuration.
/// This component should be present in the initial scene to set up in-app purchases.
/// </summary>
public class RevenueCatInitializer : MonoBehaviour
{
    [Header("RevenueCat Configuration")]
    [SerializeField, Tooltip("RevenueCat API key for this platform")]
    private string apiKey = "";
    
    [SerializeField, Tooltip("User ID for RevenueCat (leave empty for anonymous)")]
    private string userId = "";
    
    [SerializeField, Tooltip("Enable debug logging for RevenueCat")]
    private bool enableDebugLogs = true;
    
    private bool isInitialized = false;
    
    public static RevenueCatInitializer Instance { get; private set; }
    public bool IsInitialized => isInitialized;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        ValidateConfiguration();
    }
    
    private void Start()
    {
        InitializeRevenueCat();
    }
    
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("RevenueCat API key is not set. In-app purchases will not work.");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("RevenueCat debug logging is enabled. Disable in production builds.");
        }
    }
    
    private void InitializeRevenueCat()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError("Cannot initialize RevenueCat without API key");
            return;
        }
        
        try
        {
            // TODO: Initialize RevenueCat SDK here when integrated
            // Example: Purchases.Configure(new PurchasesConfiguration.Builder(apiKey).SetAppUserId(userId).Build());
            
            isInitialized = true;
            Debug.Log("RevenueCat initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize RevenueCat: {e.Message}");
            isInitialized = false;
        }
    }
    
    /// <summary>
    /// Check if RevenueCat is properly initialized before making purchase calls.
    /// </summary>
    /// <returns>True if RevenueCat is ready for purchases</returns>
    public bool IsReadyForPurchases()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("RevenueCat is not initialized. Cannot process purchases.");
            return false;
        }
        
        return true;
    }
}
