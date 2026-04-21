using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class StudySessionPopup : MonoBehaviour
{
    [Serializable]
    public class SignEntry
    {
        public string signName;
        public VideoClip clip;
    }

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

    [Header("Available Signs")]
    public List<SignEntry> availableSigns = new();

    private readonly List<SignEntry> _currentSessionSigns = new();
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

    public void OpenSingleSign(string signName)
    {
        OpenSession(new string[] { signName });
    }

    public void OpenSession(string[] signNames)
    {
        _currentSessionSigns.Clear();
        _currentIndex = 0;

        if (signNames == null || signNames.Length == 0)
        {
            Debug.LogWarning("StudySessionPopup: No sign names were passed in.");
            return;
        }

        for (int i = 0; i < signNames.Length; i++)
        {
            SignEntry entry = FindSignEntry(signNames[i]);

            if (entry != null)
                _currentSessionSigns.Add(entry);
            else
                Debug.LogWarning("StudySessionPopup: No clip found for sign name: " + signNames[i]);
        }

        if (_currentSessionSigns.Count == 0)
        {
            Debug.LogWarning("StudySessionPopup: No valid signs found for this session.");
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);

        ShowIntroState();
    }

    private SignEntry FindSignEntry(string signName)
    {
        if (string.IsNullOrWhiteSpace(signName))
            return null;

        foreach (SignEntry entry in availableSigns)
        {
            if (entry == null)
                continue;

            if (string.Equals(entry.signName, signName, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
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

        if (_currentSessionSigns.Count == 0)
        {
            Debug.LogWarning("StudySessionPopup: current session signs list is empty");
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= _currentSessionSigns.Count)
        {
            Debug.LogWarning("StudySessionPopup: current index out of range: " + _currentIndex);
            return;
        }

        SignEntry currentEntry = _currentSessionSigns[_currentIndex];

        if (currentEntry.clip == null)
        {
            Debug.LogWarning("StudySessionPopup: clip is NULL for sign " + currentEntry.signName);
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = currentEntry.clip;
        videoPlayer.Play();

        if (signNameText != null)
            signNameText.text = currentEntry.signName;
    }

    private void OnReplay()
    {
        PlayCurrentSign();
    }

    private void OnNext()
    {
        _currentIndex++;

        if (_currentIndex < _currentSessionSigns.Count)
        {
            PlayCurrentSign();
        }
        else
        {
            CloseSession();
        }
    }

    private void CloseSession()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        _currentSessionSigns.Clear();
        _currentIndex = 0;

        if (popupRoot != null)
            popupRoot.SetActive(false);

        ShowIntroState();
    }
}