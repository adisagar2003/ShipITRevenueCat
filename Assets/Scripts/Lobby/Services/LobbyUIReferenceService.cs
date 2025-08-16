using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Service responsible for managing UI reference discovery and polling for LobbyManager.
/// Handles the complex task of finding and maintaining UI element references across scene transitions
/// when LobbyManager persists as a DontDestroyOnLoad singleton.
/// 
/// This service solves the stale reference problem by:
/// - Continuously polling for null UI references
/// - Using multiple search strategies to find UI elements
/// - Automatically assigning button listeners when references are found
/// - Managing UI state and validation
/// </summary>
public class LobbyUIReferenceService : ThreadSafeSimpleSingleton<LobbyUIReferenceService>
{
    #region Events
    public event Action<Button> OnCreateButtonFound;
    public event Action<GameObject> OnCreatingTextFound;
    public event Action<GameObject> OnStartingTextFound;
    public event Action<Button> OnBackButtonFound;
    public event Action OnAllReferencesFound;
    #endregion

    #region UI Reference State
    public Button CreateLobbyButton { get; private set; }
    public GameObject CreatingLobbyText { get; private set; }
    public GameObject StartingGameText { get; private set; }
    public Button BackButton { get; private set; }
    
    public bool HasAllReferences => CreateLobbyButton != null && 
                                   CreatingLobbyText != null && 
                                   StartingGameText != null;
    #endregion

    #region Polling Configuration
    private MonoBehaviour coroutineHost;
    private Coroutine uiPollingCoroutine;
    private bool isPollingEnabled = true;
    private float pollingInterval = 1f;
    private float reducedPollingInterval = 5f;
    #endregion

    #region Service Lifecycle
    /// <summary>
    /// Initializes the UI reference service with a coroutine host.
    /// </summary>
    public void Initialize(MonoBehaviour host)
    {
        coroutineHost = host;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "LobbyUIReferenceService initialized");
    }

    /// <summary>
    /// Starts the UI polling system.
    /// </summary>
    public void StartPolling()
    {
        if (coroutineHost != null && uiPollingCoroutine == null && isPollingEnabled)
        {
            uiPollingCoroutine = coroutineHost.StartCoroutine(UIPollingCoroutine());
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI reference polling started");
        }
    }

    /// <summary>
    /// Stops the UI polling system.
    /// </summary>
    public void StopPolling()
    {
        if (uiPollingCoroutine != null && coroutineHost != null)
        {
            coroutineHost.StopCoroutine(uiPollingCoroutine);
            uiPollingCoroutine = null;
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI reference polling stopped");
        }
    }

    /// <summary>
    /// Cleanup method for service disposal.
    /// </summary>
    public void Cleanup()
    {
        StopPolling();
        ClearReferences();
        coroutineHost = null;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "LobbyUIReferenceService cleaned up");
    }
    #endregion

    #region UI Reference Management
    /// <summary>
    /// Manually refreshes UI references immediately.
    /// </summary>
    public void RefreshReferences()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "Manual UI reference refresh requested");
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Current scene: {sceneName}");
        
        bool foundAny = false;
        
        // Find UI elements
        if (CreateLobbyButton == null)
        {
            Button foundButton = FindButtonInScene("CreateLobby");
            if (foundButton != null)
            {
                SetCreateButtonReference(foundButton);
                foundAny = true;
            }
        }
        
        if (CreatingLobbyText == null)
        {
            GameObject foundText = FindGameObjectInScene("CreatingLobbyText");
            if (foundText != null)
            {
                SetCreatingTextReference(foundText);
                foundAny = true;
            }
        }
        
        if (StartingGameText == null)
        {
            GameObject foundText = FindGameObjectInScene("StartingGameText");
            if (foundText != null)
            {
                SetStartingTextReference(foundText);
                foundAny = true;
            }
        }
        
        if (BackButton == null)
        {
            Button foundButton = FindButtonInScene("BackButton");
            if (foundButton != null)
            {
                SetBackButtonReference(foundButton);
                foundAny = true;
            }
        }

        if (!foundAny)
        {
            LogAvailableGameObjects();
        }

        CheckAllReferencesFound();
    }

    /// <summary>
    /// Updates UI references from external sources.
    /// </summary>
    public void UpdateReferences(Button createButton, GameObject creatingText, GameObject startingText)
    {
        bool updated = false;
        
        if (createButton != null && CreateLobbyButton != createButton)
        {
            SetCreateButtonReference(createButton);
            updated = true;
        }
        
        if (creatingText != null && CreatingLobbyText != creatingText)
        {
            SetCreatingTextReference(creatingText);
            updated = true;
        }
        
        if (startingText != null && StartingGameText != startingText)
        {
            SetStartingTextReference(startingText);
            updated = true;
        }

        if (updated)
        {
            CheckAllReferencesFound();
        }
    }

    /// <summary>
    /// Clears all UI references.
    /// </summary>
    public void ClearReferences()
    {
        CreateLobbyButton = null;
        CreatingLobbyText = null;
        StartingGameText = null;
    }

    /// <summary>
    /// Validates current UI references and logs status.
    /// </summary>
    public bool ValidateReferences()
    {
        bool allValid = true;
        
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "=== UI Reference Validation ===");
        
        if (CreateLobbyButton == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "CreateLobbyButton is null");
            allValid = false;
        }
        else
        {
            GameLogger.LogInfo(GameLogger.LogCategory.UI, $"CreateLobbyButton: {CreateLobbyButton.gameObject.name}");
        }
        
        if (CreatingLobbyText == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "CreatingLobbyText is null");
            allValid = false;
        }
        else
        {
            GameLogger.LogInfo(GameLogger.LogCategory.UI, $"CreatingLobbyText: {CreatingLobbyText.name}");
        }
        
        if (StartingGameText == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "StartingGameText is null");
            allValid = false;
        }
        else
        {
            GameLogger.LogInfo(GameLogger.LogCategory.UI, $"StartingGameText: {StartingGameText.name}");
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"UI Polling: {(uiPollingCoroutine != null ? "Running" : "Stopped")}");
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"All References Valid: {allValid}");
        
        return allValid;
    }
    #endregion

    #region Private Reference Setters
    private void SetCreateButtonReference(Button button)
    {
        CreateLobbyButton = button;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"CreateLobbyButton reference set: {button.gameObject.name}");
        OnCreateButtonFound?.Invoke(button);
    }

    private void SetCreatingTextReference(GameObject textObject)
    {
        CreatingLobbyText = textObject;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"CreatingLobbyText reference set: {textObject.name}");
        OnCreatingTextFound?.Invoke(textObject);
    }

    private void SetStartingTextReference(GameObject textObject)
    {
        StartingGameText = textObject;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"StartingGameText reference set: {textObject.name}");
        OnStartingTextFound?.Invoke(textObject);
    }

    private void SetBackButtonReference(Button button)
    {
        BackButton = button;
        GameLogger.LogInfo(GameLogger.LogCategory.UI, $"BackButton reference set: {button.gameObject.name}");
        OnBackButtonFound?.Invoke(button);
    }

    private void CheckAllReferencesFound()
    {
        if (HasAllReferences)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "All UI references found!");
            OnAllReferencesFound?.Invoke();
        }
    }
    #endregion

    #region Polling Coroutine
    private IEnumerator UIPollingCoroutine()
    {
        while (isPollingEnabled)
        {
            yield return new WaitForSeconds(pollingInterval);
            
            // Only poll in lobby scenes
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (IsLobbyScene(currentScene))
            {
                bool foundAnyNullReferences = PollForMissingReferences();
                
                // Adjust polling frequency based on findings
                if (!foundAnyNullReferences && HasAllReferences)
                {
                    yield return new WaitForSeconds(reducedPollingInterval);
                }
            }
            else
            {
                yield return new WaitForSeconds(reducedPollingInterval);
            }
        }
    }

    private bool PollForMissingReferences()
    {
        bool foundAnyNullReferences = false;
        
        if (CreateLobbyButton == null)
        {
            foundAnyNullReferences = true;
            Button foundButton = FindButtonInScene("CreateLobby");
            if (foundButton != null)
            {
                SetCreateButtonReference(foundButton);
            }
        }
        
        if (CreatingLobbyText == null)
        {
            foundAnyNullReferences = true;
            GameObject foundText = FindGameObjectInScene("CreatingLobbyText");
            if (foundText != null)
            {
                SetCreatingTextReference(foundText);
            }
        }
        
        if (StartingGameText == null)
        {
            foundAnyNullReferences = true;
            GameObject foundText = FindGameObjectInScene("StartingGameText");
            if (foundText != null)
            {
                SetStartingTextReference(foundText);
            }
        }
        
        if (BackButton == null)
        {
            foundAnyNullReferences = true;
            Button foundButton = FindButtonInScene("BackButton");
            if (foundButton != null)
            {
                SetBackButtonReference(foundButton);
            }
        }

        if (!foundAnyNullReferences && HasAllReferences)
        {
            CheckAllReferencesFound();
        }
        
        return foundAnyNullReferences;
    }
    #endregion

    #region UI Discovery Methods
    private Button FindButtonInScene(string buttonName)
    {
        // Strategy 1: Direct GameObject.Find
        GameObject directFind = GameObject.Find(buttonName);
        if (directFind != null && directFind.TryGetComponent<Button>(out Button directButton))
        {
            GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found button '{buttonName}' by direct search");
            
            // Assign listener for CreateLobby button specifically
            if (buttonName.Equals("CreateLobby", StringComparison.OrdinalIgnoreCase))
            {
                LobbyManager.Instance.AssignCreateButtonListener();
            }
            // Assign listener for BackButton specifically
            else if (buttonName.Equals("BackButton", StringComparison.OrdinalIgnoreCase))
            {
                AssignBackButtonListener(directButton);
            }
            
            return directButton;
        }
        
        // Strategy 2: Find all buttons and search by name
        Button[] allButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (button.gameObject.name.Equals(buttonName, StringComparison.OrdinalIgnoreCase) ||
                button.gameObject.name.Contains(buttonName, StringComparison.OrdinalIgnoreCase))
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found button '{buttonName}' by component search");
                
                // Assign listener for CreateLobby button specifically
                if (buttonName.Equals("CreateLobby", StringComparison.OrdinalIgnoreCase))
                {
                    LobbyManager.Instance.AssignCreateButtonListener();
                }
                // Assign listener for BackButton specifically
                else if (buttonName.Equals("BackButton", StringComparison.OrdinalIgnoreCase))
                {
                    AssignBackButtonListener(button);
                }
                
                return button;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Assigns the BackToPreviousScene listener to the BackButton.
    /// </summary>
    private void AssignBackButtonListener(Button backButton)
    {
        if (backButton != null)
        {
            // Clear any existing listeners to avoid duplicates
            backButton.onClick.RemoveAllListeners();
            
            // Add listener to call BackToPreviousScene
            backButton.onClick.AddListener(() => LobbyManager.Instance.BackToPreviousScene());
            
            GameLogger.LogInfo(GameLogger.LogCategory.UI, "Assigned BackToPreviousScene listener to BackButton");
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Cannot assign back button listener - BackButton is null");
        }
    }

    private GameObject FindGameObjectInScene(string objectName)
    {
        // Strategy 1: Direct GameObject.Find
        GameObject directFind = GameObject.Find(objectName);
        if (directFind != null)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found GameObject '{objectName}' by direct search");
            return directFind;
        }
        
        // Strategy 2: Find all GameObjects and search by name
        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Equals(objectName, StringComparison.OrdinalIgnoreCase) ||
                obj.name.Contains(objectName, StringComparison.OrdinalIgnoreCase))
            {
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"Found GameObject '{objectName}' by component search");
                return obj;
            }
        }
        
        return null;
    }

    private void LogAvailableGameObjects()
    {
        GameLogger.LogWarning(GameLogger.LogCategory.UI, "UI elements not found. Listing available GameObjects for debugging:");
        
        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int loggedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Button", StringComparison.OrdinalIgnoreCase) ||
                obj.name.Contains("Text", StringComparison.OrdinalIgnoreCase) ||
                obj.name.Contains("Lobby", StringComparison.OrdinalIgnoreCase) ||
                obj.name.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
                obj.name.Contains("Start", StringComparison.OrdinalIgnoreCase))
            {
                string components = "";
                if (obj.GetComponent<Button>() != null) components += "[Button]";
                if (obj.GetComponent<UnityEngine.UI.Text>() != null) components += "[Text]";
                if (obj.GetComponent<TMPro.TextMeshProUGUI>() != null) components += "[TMPro]";
                
                GameLogger.LogDebug(GameLogger.LogCategory.UI, $"GameObject: '{obj.name}' {components}");
                loggedCount++;
                
                if (loggedCount > 20) break;
            }
        }
        
        if (loggedCount == 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "No UI-related GameObjects found in scene");
        }
    }

    private bool IsLobbyScene(string sceneName)
    {
        return sceneName.Contains("Lobby", StringComparison.OrdinalIgnoreCase) ||
               sceneName.Contains("Host", StringComparison.OrdinalIgnoreCase) ||
               sceneName == "LobbyandHost";
    }
    #endregion

    #region Public Control Methods
    public void SetPollingEnabled(bool enabled)
    {
        isPollingEnabled = enabled;
        if (enabled)
        {
            StartPolling();
        }
        else
        {
            StopPolling();
        }
    }

    public void SetPollingIntervals(float normal, float reduced)
    {
        pollingInterval = normal;
        reducedPollingInterval = reduced;
    }

    public bool IsPollingActive => uiPollingCoroutine != null;
    #endregion
}