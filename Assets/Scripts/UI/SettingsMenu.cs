using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SettingsMenu : MonoBehaviour
{
    public static bool gameIsPaused = false;

    public GameObject settingsMenuPrefab;

    public AudioSource mainAudio;
    public Slider volumeSlider;

    public AudioSource sfxAudio;
    public Slider sfxSlider;
    public Button creditsButton;

    void Start()
    {
        settingsMenuPrefab.SetActive(false);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    public void ToggleVisibility()
    {
        if (gameIsPaused) Resume();
        else Pause();
    }

    private void Resume()
    {
        settingsMenuPrefab.SetActive(false);
        Time.timeScale = 1f;
        gameIsPaused = false;
        Debug.Log("Resuming to title screen.");
    }

    private void Pause()
    {
        settingsMenuPrefab.SetActive(true);
        Time.timeScale = 0f;
        gameIsPaused = true;
        Debug.Log("Showing settings menu.");
    }

    public void OnVolumeChanged(float val)
    {
        if (mainAudio != null)
        {
            mainAudio.volume = val;
            Debug.Log($"Setting volume to {val}");
        }
        else Debug.LogError("Assign a main audio to change its volume");
    }

    public void OnSFXChanged(float val)
    {
        if (sfxAudio != null)
        {
            sfxAudio.volume = val;
            Debug.Log($"Setting SFX volume to {val}");
        }
        else Debug.LogError("Assign a sfx audio to change its volume");
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
}
