using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages lobby UI state and interactions.
/// Fixes the "Creating Lobby..." text persistence issue by properly managing UI state
/// across scene transitions and lobby workflow states.
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    #region UI References
    [Header("Lobby UI Components")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private GameObject creatingLobbyText;
    [SerializeField] private GameObject startingGameText;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Initialize UI to clean state
        ResetUIToLobbyState();
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "LobbyUIManager initialized");
    }

    private void OnEnable()
    {
        // Reset UI state whenever the component becomes active
        // This fixes the persistence issue when returning from race to lobby
        ResetUIToLobbyState();
    }
    #endregion

    #region Public UI Control Methods
    /// <summary>
    /// Resets all UI elements to default lobby state.
    /// Call this when returning to lobby or on scene transitions.
    /// </summary>
    public void ResetUIToLobbyState()
    {
        SetCreateLobbyButtonEnabled(true);
        SetCreatingLobbyTextVisible(false);
        SetStartingGameTextVisible(false);
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI reset to lobby state");
    }

    /// <summary>
    /// Shows the "Creating Lobby..." state - button disabled, text visible
    /// </summary>
    public void ShowCreatingLobbyState()
    {
        SetCreateLobbyButtonEnabled(false);
        SetCreatingLobbyTextVisible(true);
        SetStartingGameTextVisible(false);
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI set to creating lobby state");
    }

    /// <summary>
    /// Shows the lobby created state - button disabled, no loading text
    /// </summary>
    public void ShowLobbyCreatedState()
    {
        SetCreateLobbyButtonEnabled(false);
        SetCreatingLobbyTextVisible(false);
        SetStartingGameTextVisible(false);
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI set to lobby created state");
    }

    /// <summary>
    /// Shows the "Starting Game..." state
    /// </summary>
    public void ShowStartingGameState()
    {
        SetCreateLobbyButtonEnabled(false);
        SetCreatingLobbyTextVisible(false);
        SetStartingGameTextVisible(true);
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI set to starting game state");
    }

    /// <summary>
    /// Handles lobby creation failure - reset to usable state
    /// </summary>
    public void ShowLobbyCreationFailedState()
    {
        SetCreateLobbyButtonEnabled(true);
        SetCreatingLobbyTextVisible(false);
        SetStartingGameTextVisible(false);
        
        GameLogger.LogDebug(GameLogger.LogCategory.UI, "UI reset after lobby creation failure");
    }
    #endregion

    #region Private UI Helper Methods
    private void SetCreateLobbyButtonEnabled(bool enabled)
    {
        if (createLobbyButton != null)
        {
            createLobbyButton.interactable = enabled;
        }
        else if (enabled) // Only warn when trying to enable a missing button
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Create lobby button reference is null");
        }
    }

    private void SetCreatingLobbyTextVisible(bool visible)
    {
        if (creatingLobbyText != null)
        {
            // Try TextMeshPro first
            var tmpText = creatingLobbyText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = visible ? "Creating Lobby..." : "";
            }
            else
            {
                // Fallback to legacy Text component
                var legacyText = creatingLobbyText.GetComponent<UnityEngine.UI.Text>();
                if (legacyText != null)
                {
                    legacyText.text = visible ? "Creating Lobby..." : "";
                }
            }
        }
        else if (visible) // Only warn when trying to show missing text
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Creating lobby text reference is null");
        }
    }

    private void SetStartingGameTextVisible(bool visible)
    {
        if (startingGameText != null)
        {
            // Try TextMeshPro first
            var tmpText = startingGameText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = visible ? "Starting Game..." : "";
            }
            else
            {
                // Fallback to legacy Text component
                var legacyText = startingGameText.GetComponent<UnityEngine.UI.Text>();
                if (legacyText != null)
                {
                    legacyText.text = visible ? "Starting Game..." : "";
                }
            }
        }
        else if (visible) // Only warn when trying to show missing text
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Starting game text reference is null");
        }
    }
    #endregion

    #region Public Property Access
    public bool IsCreateLobbyButtonEnabled => createLobbyButton != null && createLobbyButton.interactable;
    public bool IsCreatingLobbyTextVisible => creatingLobbyText != null && !string.IsNullOrEmpty(GetTextContent(creatingLobbyText));
    public bool IsStartingGameTextVisible => startingGameText != null && !string.IsNullOrEmpty(GetTextContent(startingGameText));
    
    /// <summary>
    /// Helper method to get text content from either TextMeshPro or legacy Text components.
    /// </summary>
    private string GetTextContent(GameObject textObject)
    {
        if (textObject == null) return string.Empty;
        
        // Try TextMeshPro first
        var tmpText = textObject.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            return tmpText.text;
        }
        
        // Fallback to legacy Text component
        var legacyText = textObject.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            return legacyText.text;
        }
        
        return string.Empty;
    }
    #endregion

    #region Component References Setup
    /// <summary>
    /// Sets up UI component references. Called by LobbyManager during initialization.
    /// Handles null references gracefully and attempts automatic discovery if needed.
    /// </summary>
    public void SetupUIReferences(Button createButton, GameObject creatingText, GameObject startingText)
    {
        // Update references, keeping existing ones if new ones are null
        if (createButton != null)
            createLobbyButton = createButton;
        if (creatingText != null)
            creatingLobbyText = creatingText;
        if (startingText != null)
            startingGameText = startingText;
        
        // If any references are still null, attempt automatic discovery
        AttemptAutomaticUIDiscovery();
        
        // Immediately reset to clean state with new references
        ResetUIToLobbyState();
        
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "UI references configured");
    }
    
    /// <summary>
    /// Attempts to automatically discover UI elements if references are null.
    /// This provides a fallback when serialized references become stale.
    /// </summary>
    private void AttemptAutomaticUIDiscovery()
    {
        bool foundAny = false;
        
        // Try to find Create Lobby Button if null - look for "CreateLobby" GameObject
        if (createLobbyButton == null)
        {
            GameObject buttonObject = GameObject.Find("CreateLobby");
            if (buttonObject != null && buttonObject.TryGetComponent<Button>(out Button button))
            {
                createLobbyButton = button;
                foundAny = true;
                GameLogger.LogInfo(GameLogger.LogCategory.UI, "Auto-discovered createLobbyButton from 'CreateLobby' GameObject");
            }
        }
        
        // Try to find Creating Lobby Text if null - look for "CreatingLobbyText" GameObject
        if (creatingLobbyText == null)
        {
            GameObject textObject = GameObject.Find("CreatingLobbyText");
            if (textObject != null)
            {
                creatingLobbyText = textObject;
                foundAny = true;
                GameLogger.LogInfo(GameLogger.LogCategory.UI, "Auto-discovered creatingLobbyText from 'CreatingLobbyText' GameObject");
            }
        }
        
        // Try to find Starting Game Text if null - look for "StartingGameText" GameObject
        if (startingGameText == null)
        {
            GameObject textObject = GameObject.Find("StartingGameText");
            if (textObject != null)
            {
                startingGameText = textObject;
                foundAny = true;
                GameLogger.LogInfo(GameLogger.LogCategory.UI, "Auto-discovered startingGameText from 'StartingGameText' GameObject");
            }
        }
        
        if (!foundAny)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "No UI elements could be auto-discovered");
        }
    }
    
    
    /// <summary>
    /// Validates that all UI references are available and logs missing ones.
    /// </summary>
    public bool ValidateUIReferences()
    {
        bool allValid = true;
        
        if (createLobbyButton == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "createLobbyButton is null - buttons may not work");
            allValid = false;
        }
        
        if (creatingLobbyText == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "creatingLobbyText is null - creation status may not display");
            allValid = false;
        }
        
        if (startingGameText == null)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "startingGameText is null - game start status may not display");
            allValid = false;
        }
        
        if (allValid)
        {
            GameLogger.LogDebug(GameLogger.LogCategory.UI, "All UI references validated successfully");
        }
        
        return allValid;
    }
    #endregion
}