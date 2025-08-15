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
            creatingLobbyText.SetActive(visible);
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
            startingGameText.SetActive(visible);
        }
        else if (visible) // Only warn when trying to show missing text
        {
            GameLogger.LogWarning(GameLogger.LogCategory.UI, "Starting game text reference is null");
        }
    }
    #endregion

    #region Public Property Access
    public bool IsCreateLobbyButtonEnabled => createLobbyButton != null && createLobbyButton.interactable;
    public bool IsCreatingLobbyTextVisible => creatingLobbyText != null && creatingLobbyText.activeSelf;
    public bool IsStartingGameTextVisible => startingGameText != null && startingGameText.activeSelf;
    #endregion

    #region Component References Setup
    /// <summary>
    /// Sets up UI component references. Called by LobbyManager during initialization.
    /// </summary>
    public void SetupUIReferences(Button createButton, GameObject creatingText, GameObject startingText)
    {
        createLobbyButton = createButton;
        creatingLobbyText = creatingText;
        startingGameText = startingText;
        
        // Immediately reset to clean state with new references
        ResetUIToLobbyState();
        
        GameLogger.LogInfo(GameLogger.LogCategory.UI, "UI references configured");
    }
    #endregion
}