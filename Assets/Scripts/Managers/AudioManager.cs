using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "AudioManager.MusicVolume";
    private const string SfxVolumeKey = "AudioManager.SfxVolume";

    private static AudioManager instance;
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private bool firstSceneAudioHandled;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private float defaultMusicVolume = 0.8f;
    [SerializeField] private float defaultSfxVolume = 1f;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();
            }

            if (instance == null)
            {
                var audioManagerObject = new GameObject("AudioManager");
                instance = audioManagerObject.AddComponent<AudioManager>();
            }

            return instance;
        }
    }

    public float MusicVolume => musicSource != null ? musicSource.volume : defaultMusicVolume;
    public float SfxVolume => sfxSource != null ? sfxSource.volume : defaultSfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        LoadVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnValidate()
    {
        EnsureAudioSources();
        ApplyVolumes(MusicVolume, SfxVolume);
    }

    private void Start()
    {
        if (!firstSceneAudioHandled)
        {
            ApplySceneAudio(SceneManager.GetActiveScene().name);
            firstSceneAudioHandled = true;
        }
    }

    public void SetMusicVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);

        if (musicSource != null)
        {
            musicSource.volume = clamped;
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);

        if (sfxSource != null)
        {
            sfxSource.volume = clamped;
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
        PlayerPrefs.Save();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayMusic(string resourcePath, bool loop = true)
    {
        PlayMusic(LoadClip(resourcePath), loop);
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlaySfx(string resourcePath, float volumeScale = 1f)
    {
        PlaySfx(LoadClip(resourcePath), volumeScale);
    }

    public AudioClip LoadClip(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (clipCache.TryGetValue(resourcePath, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager could not find clip at Resources path '{resourcePath}'.");
            return null;
        }

        clipCache[resourcePath] = clip;
        return clip;
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            var sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != musicSource)
                {
                    sfxSource = sources[i];
                    break;
                }
            }
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureMusicSource();
        ConfigureSfxSource();
    }

    private void ConfigureMusicSource()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private void ConfigureSfxSource()
    {
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void LoadVolumes()
    {
        float savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        ApplyVolumes(savedMusicVolume, savedSfxVolume);

        if (Mathf.Approximately(savedMusicVolume, 0f) && Mathf.Approximately(savedSfxVolume, 0f))
        {
            Debug.LogWarning("AudioManager loaded with both music and SFX volumes at 0. Check the settings menu if audio seems silent.");
        }
    }

    private void ApplyVolumes(float musicVolume, float sfxVolume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(musicVolume);
        }

        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        firstSceneAudioHandled = true;
        ApplySceneAudio(scene.name);
    }

    private void ApplySceneAudio(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScreen":
                PlayMusic(GameAudioPaths.MusicGrandmasHouse);
                Debug.Log("AudioManager applied TitleScreen audio.");
                break;
            case "PrologueScene":
                PlayMusic(GameAudioPaths.MusicGrandmasHouse);
                PlaySfx(GameAudioPaths.DialogueDoorBell, 0.9f);
                Debug.Log("AudioManager applied PrologueScene audio.");
                break;
            case "RestaurantScene":
                PlayMusic(GameAudioPaths.MusicRestaurant);
                Debug.Log("AudioManager applied RestaurantScene audio.");
                break;
            case "MapScene":
            case "GroceryScene":
                PlayMusic(GameAudioPaths.MusicRestaurant);
                Debug.Log($"AudioManager applied travel audio for {sceneName}.");
                break;
            case "KitchenScene":
                PlayMusic(GameAudioPaths.MusicRestaurant);
                Debug.Log("AudioManager applied KitchenScene audio.");
                break;
            case "FridgeScene":
                PlayMusic(GameAudioPaths.MusicGrandmasHouse);
                PlaySfx(GameAudioPaths.KitchenFridge, 0.9f);
                Debug.Log("AudioManager applied FridgeScene audio.");
                break;
        }
    }
}
