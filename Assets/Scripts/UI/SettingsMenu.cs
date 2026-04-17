using UnityEngine;
using UnityEngine.UI;
public class SettingsMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    [SerializeField] private GameObject settingsMenuPrefab;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private Button creditsButton;

    void Start()
    {
        ResolveReferences();

        if (settingsMenuPrefab != null)
        {
            settingsMenuPrefab.SetActive(false);
        }
        else
        {
            Debug.LogWarning("SettingsMenu could not find a SettingsPanel in this scene. The settings button will stay inactive until a panel is added.");
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
        }
    }

    public void ToggleVisibility()
    {
        if (gameIsPaused) Resume();
        else Pause();
    }

    private void Resume()
    {
        if (settingsMenuPrefab != null)
        {
            settingsMenuPrefab.SetActive(false);
        }

        Time.timeScale = 1f;
        gameIsPaused = false;
        Debug.Log("Resuming to title screen.");
    }

    private void Pause()
    {
        ResolveReferences();

        if (settingsMenuPrefab == null)
        {
            Debug.LogWarning("SettingsMenu pause requested, but no SettingsPanel exists in this scene.");
            return;
        }

        if (settingsMenuPrefab != null)
        {
            settingsMenuPrefab.SetActive(true);
        }

        Time.timeScale = 0f;
        gameIsPaused = true;
        Debug.Log("Showing settings menu.");
    }

    public void OnVolumeChanged(float val)
    {
        AudioManager.Instance.SetMusicVolume(val);
        Debug.Log($"Setting music volume to {val}");
    }

    public void OnSFXChanged(float val)
    {
        AudioManager.Instance.SetSfxVolume(val);
        Debug.Log($"Setting SFX volume to {val}");
    }

    public void LoadCredits()
    {
        Time.timeScale = 0f;
        Debug.Log("Loading the credits scene");
        //ScenesManager.Instance.LoadScene(ScenesManager.Scene.CreditsScreen);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        ScenesManager.Instance.LoadScene(ScenesManager.Scene.TitleScreen);
    }

    private void ResolveReferences()
    {
        if (settingsMenuPrefab == null)
        {
            settingsMenuPrefab = FindNamedGameObject("SettingsPanel");
        }

        if (volumeSlider == null)
        {
            volumeSlider = FindNamedSlider("VolumeSlider");
        }

        if (sfxSlider == null)
        {
            sfxSlider = FindNamedSlider("SFXVolumeSlider");
        }
    }

    private GameObject FindNamedGameObject(string objectName)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private Slider FindNamedSlider(string sliderName)
    {
        if (settingsMenuPrefab != null)
        {
            var nestedSliders = settingsMenuPrefab.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < nestedSliders.Length; i++)
            {
                if (nestedSliders[i].name == sliderName)
                {
                    return nestedSliders[i];
                }
            }
        }

        var allSliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allSliders.Length; i++)
        {
            if (allSliders[i].name == sliderName)
            {
                return allSliders[i];
            }
        }

        return null;
    }
}
