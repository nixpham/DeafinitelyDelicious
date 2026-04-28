using System;
using System.Collections;
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

    [Header("Sign Engine")]
    [SerializeField] private GameObject signEngineRoot;

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
    private Coroutine openRoutine;

    public bool IsMinigameOpen => activeMinigame != null;

    private void Awake()
    {
        HideSuccessPopup();

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

        // Do NOT turn engine off here.
        // Let the engine initialize like it does in the tutorial scene.
    }

    private IEnumerator Start()
    {
        // Engine ON so it can initialize
        SetEngineCanvasAlpha(0f);
        SetSignEngineActive(true);

        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.25f);

        // Turn engine OFF after init
        SetSignEngineActive(false);

        // Reset alpha back to visible for future minigames
        SetEngineCanvasAlpha(1f);
    }

    public void OpenMinigame(string minigameName)
    {
        Debug.Log("[MinigameManager] OpenMinigame called with: " + minigameName);

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenMinigameRoutine(minigameName));
    }

    private IEnumerator OpenMinigameRoutine(string minigameName)
    {
        SetSignEngineActive(true);

        // Small wait so engine is awake before panel sends OnOpenedByManager.
        yield return null;

        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }

        GameObject minigamePanel = FindMinigamePanel(minigameName);
        if (minigamePanel == null)
        {
            Debug.LogError("[MinigameManager] Could not find minigame: " + minigameName);
            SetSignEngineActive(false);
            openRoutine = null;
            yield break;
        }

        activeMinigame = minigamePanel;
        activeMinigame.SetActive(true);

        HideSuccessPopup();

        activeMinigame.SendMessage("OnOpenedByManager", SendMessageOptions.DontRequireReceiver);

        openRoutine = null;
    }

    public void CloseMinigame()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }

        HideSuccessPopup();
        SetSignEngineActive(false);

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
        SetSignEngineActive(true);

        activeMinigame.SendMessage("OnRedoPressed", SendMessageOptions.DontRequireReceiver);

        Debug.Log("[MinigameManager] Restart requested.");
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

    private void HandleNextPressed()
    {
        Debug.Log("[MinigameManager] NEXT button clicked");

        if (activeMinigame != null)
            activeMinigame.SendMessage("OnNextPressed", SendMessageOptions.DontRequireReceiver);

        if (recipeManager != null)
            recipeManager.CompleteMinigame();

        CloseMinigame();
    }

    private void HandleRedoPressed()
    {
        Debug.Log("[MinigameManager] REDO button clicked");
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

    private void SetSignEngineActive(bool active)
    {
        if (signEngineRoot != null)
            signEngineRoot.SetActive(active);
        else
            Debug.LogWarning("[MinigameManager] Sign Engine Root is not assigned.");

        if (!active)
            StopAllWebcams();
    }

    private void StopAllWebcams()
    {
        WebCamTexture[] cams = Resources.FindObjectsOfTypeAll<WebCamTexture>();

        foreach (WebCamTexture cam in cams)
        {
            if (cam != null && cam.isPlaying)
                cam.Stop();
        }
    }

    private void SetEngineCanvasAlpha(float alpha)
    {
        if (signEngineRoot == null)
            return;

        CanvasGroup canvasGroup = signEngineRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = signEngineRoot.AddComponent<CanvasGroup>();

        canvasGroup.alpha = alpha;
    }
}