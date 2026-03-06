using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class StudySessionPopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject popupRoot;

    [Header("Header & Intro Text")]
    public TMP_Text titleText;
    public TMP_Text introText;
    public Button continueButton;

    [Header("Video Area")]
    public GameObject videoContainer;
    public VideoPlayer videoPlayer;
    public TMP_Text signNameText;

    [Header("Video Controls")]
    public Button replayButton;
    public Button nextButton;

    [Header("Sign Clips")]
    public List<VideoClip> signClips;

    private string[] _signNames;
    private int _currentIndex = 0;

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueIntro);

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplay);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
    }

    public void OpenSession(string[] signNames)
    {
        _signNames = signNames;
        _currentIndex = 0;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        ShowIntroState();
    }

    private void ShowIntroState()
    {
        if (titleText != null)
            titleText.text = "Study Session";

        if (introText != null)
        {
            introText.gameObject.SetActive(true);
            introText.text = "Before we start cooking, let's first learn the signs associated!";
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        if (videoContainer != null)
            videoContainer.SetActive(false);

        if (replayButton != null)
            replayButton.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
    }

    private void OnContinueIntro()
    {
        if (introText != null)
            introText.gameObject.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (videoContainer != null)
            videoContainer.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        PlayCurrentSign();
    }

    private void PlayCurrentSign()
    {
        if (videoPlayer == null)
        {
            Debug.LogWarning("StudySessionPopup: videoPlayer is NULL");
            return;
        }

        if (signClips == null || signClips.Count == 0)
        {
            Debug.LogWarning("StudySessionPopup: signClips is empty");
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= signClips.Count)
        {
            Debug.LogWarning("StudySessionPopup: _currentIndex out of range: " + _currentIndex);
            return;
        }

        Debug.Log("StudySessionPopup: Playing clip " + signClips[_currentIndex].name);

        videoPlayer.clip = signClips[_currentIndex];
        videoPlayer.Stop();
        videoPlayer.Play();

        if (signNameText != null && _signNames != null && _currentIndex < _signNames.Length)
            signNameText.text = "Sign: " + _signNames[_currentIndex];
    }

    private void OnReplay()
    {
        PlayCurrentSign();
    }

    private void OnNext()
    {
        _currentIndex++;

        if (_currentIndex < signClips.Count && _signNames != null && _currentIndex < _signNames.Length)
            PlayCurrentSign();
        else
            CloseSession();
    }

    private void CloseSession()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }
}