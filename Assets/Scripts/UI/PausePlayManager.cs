using UnityEngine;

/// <summary>
/// Manages the pause/resume functionality for the game, controlling the visibility
/// and state of the pause menu UI.
/// </summary>
public class PausePlayManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    private bool isGamePaused = false;

    void Awake()
    {
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// Toggles the pause menu between visible and hidden states.
    /// </summary>
    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;
        pauseMenu.SetActive(isGamePaused);
    }

    /// <summary>
    /// Hides the pause panel and resumes the game.
    /// </summary>
    public void HidePausePanel()
    {
        isGamePaused = false;
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// Shows the pause panel and pauses the game.
    /// </summary>
    public void ShowPausePanel()
    {
        isGamePaused = true;
        pauseMenu.SetActive(true);
    }
}
