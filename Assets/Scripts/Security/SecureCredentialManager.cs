using UnityEngine;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Secure credential management system that prevents hardcoded sensitive data.
/// Handles API keys, tokens, and other sensitive configuration securely.
/// Compatible with Unity 6 - uses standard .NET APIs that remain stable across Unity versions.
/// </summary>
public static class SecureCredentialManager
{
    private static readonly Dictionary<string, string> _credentials = new Dictionary<string, string>();
    private static readonly object _lockObject = new object();
    private static bool _isInitialized = false;

    // Environment variable names (safe to be public)
    public const string REVENUE_CAT_API_KEY_ANDROID = "REVENUECAT_API_KEY_ANDROID";
    public const string REVENUE_CAT_API_KEY_IOS = "REVENUECAT_API_KEY_IOS";
    public const string UNITY_SERVICES_PROJECT_ID = "UNITY_SERVICES_PROJECT_ID";
    
    /// <summary>
    /// Initialize the credential manager and load credentials from secure sources.
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        lock (_lockObject)
        {
            if (_isInitialized) return;

            try
            {
                LoadCredentialsFromEnvironment();
                LoadCredentialsFromPlayerPrefs();
                ValidateRequiredCredentials();
                
                _isInitialized = true;
                GameLogger.LogInfo(GameLogger.LogCategory.General, "SecureCredentialManager initialized successfully");
            }
            catch (Exception ex)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, $"Failed to initialize SecureCredentialManager: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get a credential value safely. Never logs the actual value.
    /// </summary>
    public static string GetCredential(string key)
    {
        if (!_isInitialized)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.General, "SecureCredentialManager not initialized, initializing now");
            Initialize();
        }

        lock (_lockObject)
        {
            if (_credentials.TryGetValue(key, out string value))
            {
                if (string.IsNullOrEmpty(value))
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.General, $"Credential '{key}' is empty");
                    return null;
                }
                
                GameLogger.LogDebug(GameLogger.LogCategory.General, $"Retrieved credential '{key}' (length: {value.Length})");
                return value;
            }
            
            GameLogger.LogWarning(GameLogger.LogCategory.General, $"Credential '{key}' not found");
            return null;
        }
    }

    /// <summary>
    /// Set a credential value (for runtime configuration).
    /// </summary>
    public static void SetCredential(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            GameLogger.LogError(GameLogger.LogCategory.General, "Cannot set credential with null or empty key");
            return;
        }

        lock (_lockObject)
        {
            _credentials[key] = value;
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Credential '{key}' updated (length: {value?.Length ?? 0})");
        }
    }

    /// <summary>
    /// Check if a credential exists and is not empty.
    /// </summary>
    public static bool HasCredential(string key)
    {
        if (!_isInitialized) Initialize();

        lock (_lockObject)
        {
            return _credentials.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value);
        }
    }

    /// <summary>
    /// Get the appropriate RevenueCat API key for the current platform.
    /// </summary>
    public static string GetRevenueCatApiKey()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetCredential(REVENUE_CAT_API_KEY_ANDROID);
#elif UNITY_IOS && !UNITY_EDITOR
        return GetCredential(REVENUE_CAT_API_KEY_IOS);
#else
        // In editor, try to get either key for testing
        string androidKey = GetCredential(REVENUE_CAT_API_KEY_ANDROID);
        if (!string.IsNullOrEmpty(androidKey)) return androidKey;
        
        string iosKey = GetCredential(REVENUE_CAT_API_KEY_IOS);
        if (!string.IsNullOrEmpty(iosKey)) return iosKey;
        
        GameLogger.LogWarning(GameLogger.LogCategory.Purchase, "No RevenueCat API key found for current platform");
        return null;
#endif
    }

    /// <summary>
    /// Validate that all required credentials are present.
    /// </summary>
    private static void ValidateRequiredCredentials()
    {
        var requiredCredentials = new List<string>();

        // Check platform-specific requirements
#if UNITY_ANDROID && !UNITY_EDITOR
        requiredCredentials.Add(REVENUE_CAT_API_KEY_ANDROID);
#elif UNITY_IOS && !UNITY_EDITOR
        requiredCredentials.Add(REVENUE_CAT_API_KEY_IOS);
#endif

        var missingCredentials = new List<string>();
        foreach (string credential in requiredCredentials)
        {
            if (!HasCredential(credential))
            {
                missingCredentials.Add(credential);
            }
        }

        if (missingCredentials.Count > 0)
        {
            string missing = string.Join(", ", missingCredentials);
            GameLogger.LogWarning(GameLogger.LogCategory.General, 
                $"Missing required credentials: {missing}. Features may not work properly.");
        }
        else if (requiredCredentials.Count > 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, "All required credentials are present");
        }
    }

    /// <summary>
    /// Load credentials from environment variables.
    /// </summary>
    private static void LoadCredentialsFromEnvironment()
    {
        var envCredentials = new[]
        {
            REVENUE_CAT_API_KEY_ANDROID,
            REVENUE_CAT_API_KEY_IOS,
            UNITY_SERVICES_PROJECT_ID
        };

        int loadedCount = 0;
        foreach (string credentialName in envCredentials)
        {
            string value = Environment.GetEnvironmentVariable(credentialName);
            if (!string.IsNullOrEmpty(value))
            {
                _credentials[credentialName] = value;
                loadedCount++;
                GameLogger.LogDebug(GameLogger.LogCategory.General, 
                    $"Loaded credential '{credentialName}' from environment");
            }
        }

        if (loadedCount > 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Loaded {loadedCount} credentials from environment variables");
        }
        else
        {
            GameLogger.LogDebug(GameLogger.LogCategory.General, "No credentials found in environment variables");
        }
    }

    /// <summary>
    /// Load credentials from encrypted PlayerPrefs (for development only).
    /// </summary>
    private static void LoadCredentialsFromPlayerPrefs()
    {
#if UNITY_EDITOR
        // Only load from PlayerPrefs in editor for development convenience
        var prefCredentials = new[]
        {
            REVENUE_CAT_API_KEY_ANDROID,
            REVENUE_CAT_API_KEY_IOS
        };

        int loadedCount = 0;
        foreach (string credentialName in prefCredentials)
        {
            string encryptedValue = PlayerPrefs.GetString($"Encrypted_{credentialName}", "");
            if (!string.IsNullOrEmpty(encryptedValue))
            {
                try
                {
                    string decryptedValue = SimpleDecrypt(encryptedValue);
                    if (!string.IsNullOrEmpty(decryptedValue))
                    {
                        _credentials[credentialName] = decryptedValue;
                        loadedCount++;
                        GameLogger.LogDebug(GameLogger.LogCategory.General, 
                            $"Loaded credential '{credentialName}' from PlayerPrefs");
                    }
                }
                catch (Exception ex)
                {
                    GameLogger.LogWarning(GameLogger.LogCategory.General, 
                        $"Failed to decrypt credential '{credentialName}': {ex.Message}");
                }
            }
        }

        if (loadedCount > 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Loaded {loadedCount} credentials from PlayerPrefs");
        }
#endif
    }

    /// <summary>
    /// Store an encrypted credential in PlayerPrefs (development only).
    /// </summary>
    public static void StoreCredentialInPlayerPrefs(string key, string value)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            GameLogger.LogError(GameLogger.LogCategory.General, "Cannot store empty credential");
            return;
        }

        try
        {
            string encryptedValue = SimpleEncrypt(value);
            PlayerPrefs.SetString($"Encrypted_{key}", encryptedValue);
            PlayerPrefs.Save();
            
            SetCredential(key, value);
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"Stored credential '{key}' in PlayerPrefs");
        }
        catch (Exception ex)
        {
            GameLogger.LogError(GameLogger.LogCategory.General, $"Failed to store credential '{key}': {ex.Message}");
        }
#else
        GameLogger.LogWarning(GameLogger.LogCategory.General, "Storing credentials in PlayerPrefs is only available in editor");
#endif
    }

    /// <summary>
    /// Simple encryption for PlayerPrefs storage (not cryptographically secure, just obfuscation).
    /// </summary>
    private static string SimpleEncrypt(string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] key = Encoding.UTF8.GetBytes("ShipITGameKey123"); // Simple key for obfuscation
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        
        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Simple decryption for PlayerPrefs storage.
    /// </summary>
    private static string SimpleDecrypt(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] key = Encoding.UTF8.GetBytes("ShipITGameKey123");
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key[i % key.Length]);
        }
        
        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Clear all stored credentials (for security purposes).
    /// </summary>
    public static void ClearAllCredentials()
    {
        lock (_lockObject)
        {
            _credentials.Clear();
            GameLogger.LogInfo(GameLogger.LogCategory.General, "All credentials cleared from memory");
        }

#if UNITY_EDITOR
        // Also clear from PlayerPrefs
        var prefCredentials = new[]
        {
            REVENUE_CAT_API_KEY_ANDROID,
            REVENUE_CAT_API_KEY_IOS
        };

        foreach (string credentialName in prefCredentials)
        {
            PlayerPrefs.DeleteKey($"Encrypted_{credentialName}");
        }
        PlayerPrefs.Save();
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Credentials cleared from PlayerPrefs");
#endif
    }

    /// <summary>
    /// Get debug info about loaded credentials (never shows actual values).
    /// </summary>
    public static string GetDebugInfo()
    {
        if (!_isInitialized) Initialize();

        lock (_lockObject)
        {
            var info = new StringBuilder();
            info.AppendLine("=== Credential Manager Status ===");
            info.AppendLine($"Initialized: {_isInitialized}");
            info.AppendLine($"Credentials loaded: {_credentials.Count}");
            
            foreach (var kvp in _credentials)
            {
                string status = string.IsNullOrEmpty(kvp.Value) ? "EMPTY" : $"SET (length: {kvp.Value.Length})";
                info.AppendLine($"  {kvp.Key}: {status}");
            }
            
            return info.ToString();
        }
    }
}

/// <summary>
/// Component to initialize the secure credential manager at game start.
/// </summary>
public class SecureCredentialManagerInitializer : MonoBehaviour
{
    [Header("Security Settings")]
    [SerializeField, Tooltip("Clear credentials from memory when application quits")]
    private bool clearOnQuit = true;

    private void Awake()
    {
        SecureCredentialManager.Initialize();
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        if (clearOnQuit)
        {
            SecureCredentialManager.ClearAllCredentials();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Show Credential Debug Info")]
    private void ShowCredentialDebugInfo()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, SecureCredentialManager.GetDebugInfo());
    }

    [ContextMenu("Clear All Credentials")]
    private void ClearAllCredentials()
    {
        SecureCredentialManager.ClearAllCredentials();
    }
#endif
}