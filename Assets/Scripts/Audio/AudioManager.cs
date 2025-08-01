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
        PlaySFX(buttonClickSound);
        Debug.Log("Button click sound played!");
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
        }
        else
        {
            Debug.LogWarning($"Cannot play SFX: {(clip == null ? "AudioClip is null" : "SFX AudioSource is null")}");
        }
    }

    public void PlayArrowButtonClick()
    {
        PlaySFX(arrowClickSound);
        Debug.Log("Arrow click sound played!");
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
