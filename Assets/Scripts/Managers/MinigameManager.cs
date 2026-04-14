using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameManager : MonoBehaviour
{
    [Serializable]
    public class MinigameEntry
    {
        public string minigameName;
        public GameObject panelRoot;
    }

    [Header("Camera / Recognizer")]
    [SerializeField] private GameObject signRecognizer;

    [Header("Minigame Panels")]
    [SerializeField] private MinigameEntry[] minigames;

    [Header("Shared Success Popup")]
    [SerializeField] private GameObject successPopupRoot;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button redoButton;

    [Header("Optional")]
    [SerializeField] private RecipeManager recipeManager;

    private GameObject activeMinigame;

    public bool IsMinigameOpen => activeMinigame != null;

    private void Awake()
    {
        HideSuccessPopup();

        // Keep recognizer/camera ON at all times
        if (signRecognizer != null)
        {
            signRecognizer.SetActive(true);
            Debug.Log("[MinigameManager] Sign recognizer forced ON in Awake.");
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextPressed);
        }

        if (redoButton != null)
        {
            redoButton.onClick.RemoveAllListeners();
            redoButton.onClick.AddListener(HandleRedoPressed);
        }
    }

    public void OpenMinigame(string minigameName)
    {
        Debug.Log("[MinigameManager] OpenMinigame called with: " + minigameName);

        GameObject minigamePanel = FindMinigamePanel(minigameName);
        if (minigamePanel == null)
        {
            Debug.LogError("[MinigameManager] Could not find minigame: " + minigameName);
            return;
        }

        activeMinigame = minigamePanel;
        activeMinigame.SetActive(true);

        HideSuccessPopup();

        activeMinigame.SendMessage("OnOpenedByManager", SendMessageOptions.DontRequireReceiver);
    }

    public void ShowSuccessPopup(string message = "Success")
    {
        if (successPopupRoot != null)
            successPopupRoot.SetActive(true);

        if (successText != null)
            successText.text = message;

        Debug.Log("[MinigameManager] Showing success popup.");
    }

    public void HideSuccessPopup()
    {
        if (successPopupRoot != null)
            successPopupRoot.SetActive(false);
    }

    public void CloseMinigame()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }

        HideSuccessPopup();

        Debug.Log("[MinigameManager] Minigame closed.");
    }

    public void RestartMinigame()
    {
        if (activeMinigame == null)
        {
            Debug.LogWarning("[MinigameManager] No active minigame to restart.");
            return;
        }

        HideSuccessPopup();
        activeMinigame.SendMessage("OnRedoPressed", SendMessageOptions.DontRequireReceiver);

        Debug.Log("[MinigameManager] Restart requested.");
    }

    private void HandleNextPressed()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SendMessage("OnNextPressed", SendMessageOptions.DontRequireReceiver);
        }

        if (recipeManager != null)
        {
            recipeManager.CompleteMinigame();
        }

        CloseMinigame();
    }

    private void HandleRedoPressed()
    {
        RestartMinigame();
    }

    private GameObject FindMinigamePanel(string minigameName)
    {
        if (minigames == null || minigames.Length == 0)
            return null;

        foreach (MinigameEntry entry in minigames)
        {
            if (entry == null)
                continue;

            if (string.Equals(entry.minigameName, minigameName, StringComparison.Ordinal))
                return entry.panelRoot;
        }

        return null;
    }
}