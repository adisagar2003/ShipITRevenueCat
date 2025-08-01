using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip arrowClickSound;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Stop all audio sources to prevent memory leaks
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
        
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
        
        GameLogger.LogInfo(GameLogger.LogCategory.Audio, "AudioManager disposed");
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause audio when application is paused
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
                GameLogger.LogInfo(GameLogger.LogCategory.Audio, "Audio paused due to application pause");
            }
        }
        else
        {
            // Resume audio when application is unpaused
            if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
            {
                musicSource.UnPause();
                GameLogger.LogInfo(GameLogger.LogCategory.Audio, "Audio resumed after application unpause");
            }
        }
    }

    private void InitializeAudio()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume * masterVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume * masterVolume;
        }

        if (backgroundMusic != null && musicSource != null)
        {
            PlayBackgroundMusic();
        }
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Play the button click sound - call this from your button's OnClick event
    /// </summary>
    public void PlayButtonClick()
    {
        GameLogger.LogUserAction("ButtonClick", "Playing button click sound");
        PlaySFX(buttonClickSound);
    }

    /// <summary>
    /// Play any sound effect
    /// </summary>
    /// <param name="clip">The audio clip to play</param>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
            GameLogger.LogDebug(GameLogger.LogCategory.Audio, $"Playing SFX: {clip.name}");
        }
        else
        {
            string reason = clip == null ? "AudioClip is null" : "SFX AudioSource is null";
            GameLogger.LogWarning(GameLogger.LogCategory.Audio, $"Cannot play SFX: {reason}");
        }
    }

    public void PlayArrowButtonClick()
    {
        GameLogger.LogUserAction("ArrowButtonClick", "Playing arrow button click sound");
        PlaySFX(arrowClickSound);
    }

    /// <summary>
    /// Play background music
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
            GameLogger.LogInfo(GameLogger.LogCategory.Audio, $"Started background music: {backgroundMusic.name}");
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Audio, "Cannot play background music: missing clip or source");
        }
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            GameLogger.LogInfo(GameLogger.LogCategory.Audio, "Stopped background music");
        }
        else
        {
            GameLogger.LogWarning(GameLogger.LogCategory.Audio, "Cannot stop background music: musicSource is null");
        }
    }

    /// <summary>
    /// Set master volume (affects all audio)
    /// </summary>
    /// <param name="volume">Volume from 0 to 1</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    /// <summary>
    /// Set music volume
    /// </summary>
    /// <param name="volume">Volume from 0 to 1</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    /// <summary>
    /// Set sound effects volume
    /// </summary>
    /// <param name="volume">Volume from 0 to 1</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }

    #endregion

    private void UpdateVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }

    private void OnValidate()
    {
        UpdateVolumes();
    }
}
