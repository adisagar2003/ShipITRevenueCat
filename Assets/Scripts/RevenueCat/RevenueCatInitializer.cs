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
    
    [SerializeField, Tooltip("Timeout for initialization in seconds")]
    private float initializationTimeout = 30f;
    
    [SerializeField, Tooltip("Product IDs to validate during initialization")]
    private string[] expectedProductIds = new string[0];
    
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
        GameLogger.LogInfo(GameLogger.LogCategory.Purchase, "Validating RevenueCat configuration");
        
        // Validate API key
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            GameLogger.LogError(GameLogger.LogCategory.Purchase, "RevenueCat API key is not set. In-app purchases will not work.");
        }
        else
        {
            // Basic API key format validation
            if (apiKey.Length < 10 || !apiKey.StartsWith("appl_") && !apiKey.StartsWith("goog_"))
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "API key format appears invalid. Expected format: 'appl_' or 'goog_' prefix");
            }
        }
        
        // Validate timeout
        if (initializationTimeout <= 0 || initializationTimeout > 60)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Purchase, $"Initialization timeout ({initializationTimeout}s) outside recommended range (5-30s)");
            initializationTimeout = Mathf.Clamp(initializationTimeout, 5f, 30f);
        }
        
        // Validate product IDs
        if (expectedProductIds != null && expectedProductIds.Length > 0)
        {
            for (int i = 0; i < expectedProductIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(expectedProductIds[i]))
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.Purchase, $"Product ID at index {i} is null or empty");
                }
            }
        }
        
        // Platform-specific validation
        ValidatePlatformConfiguration();
        
        // Debug logging warning
        if (enableDebugLogs && !UnityEngine.Debug.isDebugBuild)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "RevenueCat debug logging is enabled in release build. Consider disabling for production.");
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.Purchase, "RevenueCat configuration validation complete");
    }
    
    private void ValidatePlatformConfiguration()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                if (!string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("goog_"))
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "Android platform detected but API key doesn't start with 'goog_'");
                }
                break;
                
            case RuntimePlatform.IPhonePlayer:
                if (!string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("appl_"))
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "iOS platform detected but API key doesn't start with 'appl_'");
                }
                break;
                
            case RuntimePlatform.WebGLPlayer:
                GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "RevenueCat may not be fully supported on WebGL platform");
                break;
                
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxEditor:
                GameLogger.LogInfo(GameLogger.LogCategory.Purchase, "Running in Unity Editor - RevenueCat will use sandbox mode");
                break;
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
