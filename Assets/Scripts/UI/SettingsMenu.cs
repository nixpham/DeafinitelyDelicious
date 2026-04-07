using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    [Header("Whole Settings System")]
    [SerializeField] private GameObject settingsRoot;      // parent object that holds Image + Settings Pop Up + Credits Pop Up
    [SerializeField] private GameObject backgroundImage;   // the fullscreen image/button blocker

    [Header("Popups")]
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private GameObject creditsPopup;

    [Header("Audio")]
    [SerializeField] private AudioSource mainAudio;
    [SerializeField] private Slider volumeSlider;

    [SerializeField] private AudioSource sfxAudio;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        CloseAllSettings();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            if (mainAudio != null)
            {
                volumeSlider.value = mainAudio.volume;
            }
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);

            if (sfxAudio != null)
            {
                sfxSlider.value = sfxAudio.volume;
            }
        }
    }

    // SETTINGS BUTTON calls this
    public void ToggleSettings()
    {
        if (settingsRoot != null && settingsRoot.activeSelf)
        {
            CloseAllSettings();
        }
        else
        {
            OpenSettingsPopup();
        }
    }

    private void OpenSettingsPopup()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(true);

        if (backgroundImage != null)
            backgroundImage.SetActive(true);

        if (settingsPopup != null)
            settingsPopup.SetActive(true);

        if (creditsPopup != null)
            creditsPopup.SetActive(false);

        Debug.Log("Opened settings popup.");
    }

    public void OpenCreditsPopup()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(true);

        if (backgroundImage != null)
            backgroundImage.SetActive(true);

        if (settingsPopup != null)
            settingsPopup.SetActive(false);

        if (creditsPopup != null)
            creditsPopup.SetActive(true);

        Debug.Log("Opened credits popup.");
    }

    // IMAGE BUTTON calls this
    public void CloseAllSettings()
    {
        if (creditsPopup != null)
            creditsPopup.SetActive(false);

        if (settingsPopup != null)
            settingsPopup.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.SetActive(false);

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        Debug.Log("Closed all settings UI.");
    }

    public void OnVolumeChanged(float val)
    {
        if (mainAudio != null)
        {
            mainAudio.volume = val;
            Debug.Log("Music volume: " + val);
        }
        else
        {
            Debug.LogWarning("Main audio source is not assigned.");
        }
    }

    public void OnSFXChanged(float val)
    {
        if (sfxAudio != null)
        {
            sfxAudio.volume = val;
            Debug.Log("SFX volume: " + val);
        }
        else
        {
            Debug.LogWarning("SFX audio source is not assigned.");
        }
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}