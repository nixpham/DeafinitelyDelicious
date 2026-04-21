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
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        CloseAllSettings();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (AudioManager.Instance != null)
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            if (AudioManager.Instance != null)
                sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(val);
            Debug.Log("Music volume: " + val);
        }
    }

    public void OnSFXChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(val);
            Debug.Log("SFX volume: " + val);
        }
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        if (ScenesManager.Instance != null)
            ScenesManager.Instance.LoadScene(ScenesManager.Scene.TitleScreen);
    }
}