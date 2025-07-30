using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to any button in any scene.
/// It will automatically find the AudioManager singleton and play button sounds.
/// No inspector assignments needed!
/// </summary>
public class UniversalButtonSound : MonoBehaviour
{
    [Header("Optional Custom Sound")]
    [Tooltip("Leave empty to use the default button sound from AudioManager")]
    public AudioClip customButtonSound;

    [Header("Auto-Setup")]
    [Tooltip("If true, automatically adds this sound to the button's OnClick event")]
    public bool autoSetup = true;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("UniversalButtonSound script must be attached to a GameObject with a Button component!");
            return;
        }

        if (autoSetup)
        {
            button.onClick.AddListener(PlayButtonSound);
            Debug.Log($"Button sound automatically added to {gameObject.name}");
        }
    }

    /// <summary>
    /// Call this method from button's OnClick event in the inspector
    /// OR it will be called automatically if autoSetup is true
    /// </summary>
    public void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
        {
            if (customButtonSound != null)
            {
                AudioManager.Instance.PlaySFX(customButtonSound);
            }
            else
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
        else
        {
            Debug.LogWarning("AudioManager not found! Make sure your AudioManager is in the scene and marked as DontDestroyOnLoad.");
        }
    }

    /// <summary>
    /// Play a specific sound effect through this button
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    public void PlayCustomSound(AudioClip clip)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonSound);
        }
    }

    public void PlaySoundByName(string soundName)
    {
        Debug.Log($"Playing sound: {soundName}");
        PlayButtonSound(); 
    }
}
