using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum TutorialStep
{
    None = 0,

    // Prologue
    Prologue,

    // Restaurant 1
    RestaurantIntro1,
    RestaurantWaitDoorHighlight,
    RestaurantWaitMomClick,
    RestaurantMomConvo1,
    RestaurantWaitDoorToGrandma,

    // Grandma house
    GrandmaIntro,
    GrandmaWaitCookbookOpen,
    GrandmaWaitGrilledCheeseClick,
    GrandmaWaitCookbookReopen,
    GrandmaWaitBreadClick,
    GrandmaWaitStudyClose,
    GrandmaWaitSignBreadButton,
    GrandmaWaitBreadSign,
    GrandmaWaitSignDoneGate,
    GrandmaWaitAfterSignDone,
    GrandmaWaitBackButton,

    // Restaurant 2 / Free roam
    RestaurantIntro2,
    RestaurantFreeRoam,

    // Kitchen
    KitchenTutorial
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Scene Names")]
    public string prologueSceneName = "PrologueScene";
    public string restaurantSceneName = "RestaurantScene";
    public string grandmaSceneName = "GrandmaHouse";
    public string kitchenSceneName = "KitchenScene";

    [Header("Saved Step Key")]
    public string tutorialStepKey = "DEMO_TUTORIAL_STEP";

    [Header("One-Time Sequence Keys")]
    public string restaurantIntro1SeenKey = "DEMO_RESTAURANT_INTRO1_SEEN";
    public string restaurantIntro2SeenKey = "DEMO_RESTAURANT_INTRO2_SEEN";
    public string kitchenTutorialSeenKey = "DEMO_KITCHEN_TUTORIAL_SEEN";

    [Header("Grandma Sprite Override")]
    [Tooltip("Assign ONLY the grandma character GameObject here if refs.grandmaHouseSprite is accidentally pointing to the background.")]
    public GameObject grandmaVisualOverride;

    [Header("Grandma House Line Indices")]
    [Tooltip("After 'Anything! I gave you my cookbook!' finishes, pause and wait for cookbook click.")]
    public int grandmaPauseForCookbookAfterIndex = 14;

    [Tooltip("Resume grandma dialogue at '*Wow, I can’t really understand any of this...*' after first cookbook click.")]
    public int grandmaResumeAfterCookbookIndex = 16;

    [Tooltip("After '*All that I could understand is the first page*' finishes, pause and wait for grilled cheese click.")]
    public int grandmaPauseForGrilledCheeseAfterIndex = 16;

    [Tooltip("Resume grandma dialogue at 'Do you want a grilled cheese sandwich?' after grilled cheese click.")]
    public int grandmaResumeAfterGrilledCheeseIndex = 18;

    [Tooltip("Close cookbook when Grandma says 'I would love that!'")]
    public int grandmaCloseCookbookOnLoveLineIndex = 19;

    [Tooltip("After 'You can learn to sign any necessary words in the cookbook' finishes, pause and wait for cookbook reopen.")]
    public int grandmaPauseForCookbookReopenAfterIndex = 22;

    [Tooltip("Resume grandma dialogue at 'Click on bread' after second cookbook click.")]
    public int grandmaResumeAfterCookbookReopenIndex = 24;

    [Tooltip("After 'Click on bread' finishes, pause and wait for bread click.")]
    public int grandmaPauseForBreadClickAfterIndex = 24;

    [Tooltip("Resume grandma dialogue at 'Let's try to sign bread to Grandma' after study session closes.")]
    public int grandmaResumeAfterStudySessionIndex = 25;

    [Tooltip("After Grandma says 'Let me know what you need', pause for Sign button.")]
    public int grandmaPauseForSignButtonAfterIndex = 25;

    [Tooltip("Resume grandma dialogue at 'Usually, you would sign to Grandma...' after sign button is pressed.")]
    public int grandmaResumeAfterSignButtonIndex = 27;

    [Header("NEW: Sign Done / Back Gating")]
    [Tooltip("About 3 lines after the sign section starts, pause and require the Done button before progressing.")]
    public int grandmaPauseForSignDoneAfterIndex = 28;

    [Tooltip("Resume dialogue at the line immediately after the Done-button gate.")]
    public int grandmaResumeAfterSignDoneIndex = 29;

    [Tooltip("After about 2 more lines, pause and require the Back button to finish Grandma's house and return to restaurant.")]
    public int grandmaPauseForBackButtonAfterIndex = 30;

    private TutorialSceneRefs refs;
    public TutorialStep Step { get; private set; } = TutorialStep.None;

    private const string MOM_RESPONSE_KEY = "MOM_RESPONSE";

    private bool hasClickedGrilledCheese = false;
    private bool canClickGrilledCheese = false;

    private bool wasStudySessionOpen = false;
    private bool hasHandledStudySessionClose = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadOrInitializeStep();
    }

    public void RegisterScene(TutorialSceneRefs sceneRefs)
    {
        refs = sceneRefs;

        HookNPCEvents();
        HookButtons();
        ApplyStepForCurrentScene();
    }

    private void HookNPCEvents()
    {
        if (refs == null) return;

        if (refs.restaurantNPC != null)
        {
            refs.restaurantNPC.OnDialogueIndexChanged -= OnRestaurantDialogueIndexChanged;
            refs.restaurantNPC.OnDialogueIndexChanged += OnRestaurantDialogueIndexChanged;

            refs.restaurantNPC.OnSequenceEnded -= OnSequenceEnded;
            refs.restaurantNPC.OnSequenceEnded += OnSequenceEnded;
        }

        if (refs.conversationNPC != null)
        {
            refs.conversationNPC.OnDialogueIndexChanged -= OnConversationDialogueIndexChanged;
            refs.conversationNPC.OnDialogueIndexChanged += OnConversationDialogueIndexChanged;

            refs.conversationNPC.OnSequenceEnded -= OnSequenceEnded;
            refs.conversationNPC.OnSequenceEnded += OnSequenceEnded;
        }
    }

    private void HookButtons()
    {
        if (refs == null) return;

        Rehook(refs.doorButton, OnDoorPressed);
        Rehook(refs.momButton, OnMomPressed);
        Rehook(refs.restaurantGrandmaButton, OnRestaurantGrandmaPressed);
        Rehook(refs.backButton, OnBackPressed);

        Rehook(refs.cookbookButton, OnCookbookPressed);
        Rehook(refs.grilledCheeseButton, OnGrilledCheesePressed);
        Rehook(refs.breadButton, OnBreadPressed);
        Rehook(refs.signButton, OnSignPressed);
        Rehook(refs.signDoneButton, OnSignDonePressed);
        Rehook(refs.kitchenButton, OnKitchenPressed);
    }

    private void Rehook(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void LoadOrInitializeStep()
    {
        if (PlayerPrefs.HasKey(tutorialStepKey))
        {
            Step = (TutorialStep)PlayerPrefs.GetInt(tutorialStepKey);
        }
        else
        {
            Step = TutorialStep.Prologue;
            SaveStep();
        }
    }

    private void SaveStep()
    {
        PlayerPrefs.SetInt(tutorialStepKey, (int)Step);
        PlayerPrefs.Save();
    }

    private bool HasSeenRestaurantIntro1() => PlayerPrefs.GetInt(restaurantIntro1SeenKey, 0) == 1;
    private bool HasSeenRestaurantIntro2() => PlayerPrefs.GetInt(restaurantIntro2SeenKey, 0) == 1;
    private bool HasSeenKitchenTutorial() => PlayerPrefs.GetInt(kitchenTutorialSeenKey, 0) == 1;

    private void MarkRestaurantIntro1Seen()
    {
        PlayerPrefs.SetInt(restaurantIntro1SeenKey, 1);
        PlayerPrefs.Save();
    }

    private void MarkRestaurantIntro2Seen()
    {
        PlayerPrefs.SetInt(restaurantIntro2SeenKey, 1);
        PlayerPrefs.Save();
    }

    private void MarkKitchenTutorialSeen()
    {
        PlayerPrefs.SetInt(kitchenTutorialSeenKey, 1);
        PlayerPrefs.Save();
    }

    private NPC GetActiveSceneNPC()
    {
        if (refs == null) return null;
        if (refs.conversationNPC != null) return refs.conversationNPC;
        if (refs.restaurantNPC != null) return refs.restaurantNPC;
        return null;
    }

    private void ApplyStepForCurrentScene()
    {
        if (refs == null) return;

        ResetSceneUI();

        string currentScene = SceneManager.GetActiveScene().name;

        switch (Step)
        {
            case TutorialStep.Prologue:
                if (currentScene != prologueSceneName)
                {
                    SceneManager.LoadScene(prologueSceneName);
                    return;
                }

                refs.restaurantNPC?.PlaySequence(DialogueSequence.Prologue);
                break;

            case TutorialStep.RestaurantIntro1:
            case TutorialStep.RestaurantWaitDoorHighlight:
            case TutorialStep.RestaurantWaitMomClick:
            case TutorialStep.RestaurantMomConvo1:
            case TutorialStep.RestaurantWaitDoorToGrandma:
            case TutorialStep.RestaurantIntro2:
            case TutorialStep.RestaurantFreeRoam:
                // Allow FreeRoam to exist in either Restaurant or Kitchen
                if (Step == TutorialStep.RestaurantFreeRoam && currentScene == kitchenSceneName)
                {
                    ApplyKitchenFreeRoamStep();
                    return;
                }

                if (currentScene != restaurantSceneName)
                {
                    SceneManager.LoadScene(restaurantSceneName);
                    return;
                }

                ApplyRestaurantStep();
                break;

            case TutorialStep.GrandmaIntro:
            case TutorialStep.GrandmaWaitCookbookOpen:
            case TutorialStep.GrandmaWaitGrilledCheeseClick:
            case TutorialStep.GrandmaWaitCookbookReopen:
            case TutorialStep.GrandmaWaitBreadClick:
            case TutorialStep.GrandmaWaitStudyClose:
            case TutorialStep.GrandmaWaitSignBreadButton:
            case TutorialStep.GrandmaWaitBreadSign:
            case TutorialStep.GrandmaWaitSignDoneGate:
            case TutorialStep.GrandmaWaitAfterSignDone:
            case TutorialStep.GrandmaWaitBackButton:
                if (currentScene != grandmaSceneName)
                {
                    SceneManager.LoadScene(grandmaSceneName);
                    return;
                }
                ApplyGrandmaStep();
                break;

            case TutorialStep.KitchenTutorial:
                if (currentScene != kitchenSceneName)
                {
                    SceneManager.LoadScene(kitchenSceneName);
                    return;
                }
                ApplyKitchenStep();
                break;
        }
    }

    private void ApplyRestaurantStep()
    {
        ShowRestaurantView();

        if (refs.mapButton != null) refs.mapButton.interactable = false;
        if (refs.kitchenButton != null) refs.kitchenButton.interactable = false;

        if (Step == TutorialStep.RestaurantIntro2 && HasSeenRestaurantIntro2())
        {
            Step = TutorialStep.RestaurantFreeRoam;
            SaveStep();
        }

        switch (Step)
        {
            case TutorialStep.RestaurantIntro1:
                if (HasSeenRestaurantIntro1())
                {
                    Step = TutorialStep.RestaurantWaitMomClick;
                    SaveStep();
                    ApplyRestaurantStep();
                    return;
                }

                if (refs.momSprite != null) refs.momSprite.SetActive(false);
                if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(false);
                refs.restaurantNPC?.PlaySequence(DialogueSequence.RestaurantIntro1);
                break;

            case TutorialStep.RestaurantWaitDoorHighlight:
                SetActive(refs.doorHighlight, true);
                SetInteractable(refs.doorButton, true);

                if (refs.momSprite != null)
                    refs.momSprite.SetActive(false);

                refs.restaurantNPC?.SetExternalPause(true);
                break;

            case TutorialStep.RestaurantWaitMomClick:
                if (refs.momSprite != null) refs.momSprite.SetActive(true);
                SetActive(refs.momHighlight, true);
                SetInteractable(refs.momButton, true);
                break;

            case TutorialStep.RestaurantMomConvo1:
                ShowConversationView();
                SetActive(refs.momHighlight, false);
                refs.conversationNPC?.PlaySequence(DialogueSequence.RestaurantMomConvo1);
                break;

            case TutorialStep.RestaurantWaitDoorToGrandma:
                ShowRestaurantView();

                if (refs.momSprite != null) refs.momSprite.SetActive(true);

                SetActive(refs.doorHighlight, true);
                SetInteractable(refs.doorButton, true);
                SetInteractable(refs.momButton, true);
                break;

            case TutorialStep.RestaurantIntro2:
                if (refs.momSprite != null) refs.momSprite.SetActive(true);
                if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(true);

                SetInteractable(refs.momButton, true);
                SetInteractable(refs.restaurantGrandmaButton, true);
                SetInteractable(refs.kitchenButton, true);

                refs.restaurantNPC?.PlaySequence(DialogueSequence.RestaurantIntro2);
                break;

            case TutorialStep.RestaurantFreeRoam:
                if (refs.momSprite != null) refs.momSprite.SetActive(true);
                if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(true);

                SetInteractable(refs.momButton, true);
                SetInteractable(refs.restaurantGrandmaButton, true);
                SetInteractable(refs.kitchenButton, true);
                break;
        }
    }

    private void ApplyGrandmaStep()
    {
        ShowConversationView();

        if (refs.backButton != null)
            refs.backButton.interactable = false;

        switch (Step)
        {
            case TutorialStep.GrandmaIntro:
                SetGrandmaVisible(true);
                refs.conversationNPC?.PlaySequence(DialogueSequence.GrandmasHouse1);
                break;

            case TutorialStep.GrandmaWaitCookbookOpen:
                SetGrandmaVisible(true);

                SetActive(refs.cookbookHighlight, true);
                if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(true);
                SetInteractable(refs.cookbookButton, true);

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitGrilledCheeseClick:
                SetGrandmaVisible(false);

                if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
                if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(true);

                SetActive(refs.cookbookHighlight, false);
                SetActive(refs.grilledCheeseHighlight, canClickGrilledCheese && !hasClickedGrilledCheese);

                SetInteractable(refs.cookbookButton, false);
                SetInteractable(refs.grilledCheeseButton, canClickGrilledCheese && !hasClickedGrilledCheese);
                break;

            case TutorialStep.GrandmaWaitCookbookReopen:
                SetGrandmaVisible(true);

                SetActive(refs.cookbookHighlight, true);
                if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(true);
                SetInteractable(refs.cookbookButton, true);

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitBreadClick:
                SetGrandmaVisible(false);

                if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
                if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);
                if (refs.breadButton != null) refs.breadButton.gameObject.SetActive(true);

                SetActive(refs.cookbookHighlight, false);
                SetInteractable(refs.cookbookButton, false);
                SetInteractable(refs.breadButton, true);

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitStudyClose:
                SetGrandmaVisible(false);

                if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
                if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitSignBreadButton:
                SetGrandmaVisible(true);

                if (refs.signButton != null) refs.signButton.gameObject.SetActive(true);
                SetInteractable(refs.signButton, true);

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitBreadSign:
                SetGrandmaVisible(true);

                SetActive(refs.signEngineRoot, true);

                if (refs.signDoneButton != null)
                {
                    refs.signDoneButton.gameObject.SetActive(false);
                    refs.signDoneButton.interactable = false;
                }

                refs.conversationNPC?.SetExternalPause(false);
                break;

            case TutorialStep.GrandmaWaitSignDoneGate:
                SetGrandmaVisible(true);

                SetActive(refs.signEngineRoot, true);

                if (refs.signDoneButton != null)
                {
                    refs.signDoneButton.gameObject.SetActive(true);
                    refs.signDoneButton.interactable = true;
                }

                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitAfterSignDone:
                SetGrandmaVisible(true);

                SetActive(refs.signEngineRoot, false);

                if (refs.signDoneButton != null)
                {
                    refs.signDoneButton.gameObject.SetActive(false);
                    refs.signDoneButton.interactable = false;
                }

                refs.conversationNPC?.SetExternalPause(false);
                break;

            case TutorialStep.GrandmaWaitBackButton:
                SetGrandmaVisible(true);

                SetActive(refs.signEngineRoot, false);

                if (refs.signDoneButton != null)
                {
                    refs.signDoneButton.gameObject.SetActive(false);
                    refs.signDoneButton.interactable = false;
                }

                if (refs.backButton != null)
                {
                    refs.backButton.gameObject.SetActive(true);
                    refs.backButton.interactable = true;
                }

                refs.conversationNPC?.SetExternalPause(true);
                break;
        }
    }

    private void ApplyKitchenStep()
    {
        NPC kitchenNPC = GetActiveSceneNPC();

        if (HasSeenKitchenTutorial())
        {
            Step = TutorialStep.RestaurantFreeRoam;
            SaveStep();
            return;
        }

        kitchenNPC?.PlaySequence(DialogueSequence.KitchenTutorial);
    }

    private GameObject GetGrandmaVisual()
    {
        if (grandmaVisualOverride != null) return grandmaVisualOverride;
        if (refs != null && refs.grandmaHouseSprite != null) return refs.grandmaHouseSprite;
        return null;
    }

    private void SetGrandmaVisible(bool visible)
    {
        GameObject grandmaVisual = GetGrandmaVisual();
        if (grandmaVisual != null)
            grandmaVisual.SetActive(visible);
    }

    private void OpenCookbookForFirstTime()
    {
        SetGrandmaVisible(false);

        if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
        if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(true);
        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(false);

        SetActive(refs.cookbookHighlight, false);
        SetActive(refs.grilledCheeseHighlight, false);

        SetInteractable(refs.cookbookButton, false);
        SetInteractable(refs.grilledCheeseButton, false);

        hasClickedGrilledCheese = false;
        canClickGrilledCheese = false;
    }

    private void OpenCookbookForBreadStep()
    {
        SetGrandmaVisible(false);

        if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);
        if (refs.breadButton != null) refs.breadButton.gameObject.SetActive(true);

        SetActive(refs.cookbookHighlight, false);
        SetActive(refs.grilledCheeseHighlight, false);
        SetActive(refs.breadHighlight, false);

        SetInteractable(refs.cookbookButton, false);
        SetInteractable(refs.grilledCheeseButton, false);
        SetInteractable(refs.breadButton, true);
    }

    private void CloseCookbookView()
    {
        if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(false);
        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(false);

        SetActive(refs.cookbookHighlight, false);
        SetActive(refs.grilledCheeseHighlight, false);
        SetActive(refs.breadHighlight, false);

        SetGrandmaVisible(true);
    }

    private void ResetSceneUI()
    {
        if (refs == null) return;

        ShowRestaurantViewIfRestaurantScene();

        SetActive(refs.doorHighlight, false);
        SetActive(refs.momHighlight, false);
        SetActive(refs.restaurantGrandmaHighlight, false);
        SetActive(refs.cookbookHighlight, false);
        SetActive(refs.grilledCheeseHighlight, false);
        SetActive(refs.breadHighlight, false);
        SetActive(refs.signEngineRoot, false);

        SetInteractable(refs.doorButton, false);
        SetInteractable(refs.momButton, false);
        SetInteractable(refs.restaurantGrandmaButton, false);
        SetInteractable(refs.backButton, false);
        SetInteractable(refs.cookbookButton, false);
        SetInteractable(refs.grilledCheeseButton, false);
        SetInteractable(refs.breadButton, false);
        SetInteractable(refs.signButton, false);
        SetInteractable(refs.signDoneButton, false);
        SetInteractable(refs.kitchenButton, false);

        if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(false);
        if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(false);
        if (refs.breadButton != null) refs.breadButton.gameObject.SetActive(false);
        if (refs.signButton != null) refs.signButton.gameObject.SetActive(false);
        if (refs.signDoneButton != null) refs.signDoneButton.gameObject.SetActive(false);

        if (refs.momSprite != null) refs.momSprite.SetActive(false);
        if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(false);

        GameObject grandmaVisual = GetGrandmaVisual();
        if (grandmaVisual != null) grandmaVisual.SetActive(false);

        if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(false);
        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(false);
        if (refs.studySessionRoot != null) refs.studySessionRoot.SetActive(false);
    }

    private void ShowRestaurantViewIfRestaurantScene()
    {
        if (refs == null) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == restaurantSceneName)
            ShowRestaurantView();
        else if (sceneName == grandmaSceneName || sceneName == kitchenSceneName)
            ShowConversationView();
    }

    private void ShowRestaurantView()
    {
        if (refs.restaurantViewRoot != null) refs.restaurantViewRoot.SetActive(true);
        if (refs.conversationViewRoot != null) refs.conversationViewRoot.SetActive(false);
    }

    private void ShowConversationView()
    {
        if (refs.restaurantViewRoot != null) refs.restaurantViewRoot.SetActive(false);
        if (refs.conversationViewRoot != null) refs.conversationViewRoot.SetActive(true);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void SetInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private void ApplyKitchenFreeRoamStep()
    {
        ShowConversationView();
    }

    private bool IsStudySessionCurrentlyOpen()
    {
        bool popupOpen = false;
        bool rootOpen = false;

        if (refs != null && refs.studySessionPopup != null)
            popupOpen = refs.studySessionPopup.gameObject.activeInHierarchy;

        if (refs != null && refs.studySessionRoot != null)
            rootOpen = refs.studySessionRoot.activeInHierarchy;

        return popupOpen || rootOpen;
    }

    private void CheckForStudySessionAutoClose()
    {
        if (refs == null) return;
        if (Step != TutorialStep.GrandmaWaitStudyClose) return;

        bool isOpen = IsStudySessionCurrentlyOpen();

        if (isOpen)
        {
            wasStudySessionOpen = true;
            return;
        }

        if (wasStudySessionOpen && !isOpen && !hasHandledStudySessionClose)
        {
            hasHandledStudySessionClose = true;
            NotifyStudySessionClosed();
        }
    }

    private void OnDoorPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.RestaurantWaitDoorHighlight)
        {
            SetActive(refs.doorHighlight, false);
            SetInteractable(refs.doorButton, false);

            if (refs.momSprite != null)
                refs.momSprite.SetActive(true);

            if (refs.momButton != null)
                refs.momButton.interactable = false;

            refs.restaurantNPC?.SetExternalPause(false);
            refs.restaurantNPC?.ResumeAfterClick(2);
            return;
        }

        if (Step == TutorialStep.RestaurantWaitDoorToGrandma)
        {
            SetActive(refs.doorHighlight, false);
            SetInteractable(refs.doorButton, false);

            Step = TutorialStep.GrandmaIntro;
            SaveStep();
            SceneManager.LoadScene(grandmaSceneName);
        }
    }

    private void OnMomPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.RestaurantWaitMomClick)
        {
            SetActive(refs.momHighlight, false);
            Step = TutorialStep.RestaurantMomConvo1;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.RestaurantWaitDoorToGrandma)
        {
            ShowConversationView();
            refs.conversationNPC?.PlaySequence(DialogueSequence.RestaurantMomReminder1);
            return;
        }

        if (Step == TutorialStep.RestaurantFreeRoam)
        {
            ShowConversationView();
            refs.conversationNPC?.PlaySequence(DialogueSequence.RestaurantMomReminder2);
        }
    }

    private void OnRestaurantGrandmaPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.RestaurantFreeRoam)
        {
            ShowConversationView();
            refs.conversationNPC?.PlaySequence(DialogueSequence.RestaurantGrandmaReminder2);
        }
    }

    private void OnCookbookPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.GrandmaWaitCookbookOpen)
        {
            OpenCookbookForFirstTime();

            refs.conversationNPC?.SetExternalPause(false);
            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterCookbookIndex);

            Step = TutorialStep.GrandmaWaitGrilledCheeseClick;
            SaveStep();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitCookbookReopen)
        {
            OpenCookbookForBreadStep();

            Step = TutorialStep.GrandmaWaitBreadClick;
            SaveStep();

            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterCookbookReopenIndex);
            refs.conversationNPC?.SetExternalPause(true);
            SetActive(refs.breadHighlight, true);
            SetInteractable(refs.breadButton, true);

            return;
        }
    }

    private void OnGrilledCheesePressed()
    {
        if (refs == null) return;
        if (Step != TutorialStep.GrandmaWaitGrilledCheeseClick) return;
        if (!canClickGrilledCheese || hasClickedGrilledCheese) return;

        hasClickedGrilledCheese = true;
        canClickGrilledCheese = false;

        SetActive(refs.grilledCheeseHighlight, false);
        SetInteractable(refs.grilledCheeseButton, false);

        if (refs.cookbookPanel != null)
            refs.cookbookPanel.SetActive(true);

        if (refs.grilledCheesePage1 != null)
            refs.grilledCheesePage1.SetActive(true);

        SetGrandmaVisible(false);

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterGrilledCheeseIndex);
    }

    private void OnBreadPressed()
    {
        if (refs == null || Step != TutorialStep.GrandmaWaitBreadClick) return;

        SetActive(refs.breadHighlight, false);
        SetInteractable(refs.breadButton, false);

        Step = TutorialStep.GrandmaWaitStudyClose;
        SaveStep();

        wasStudySessionOpen = false;
        hasHandledStudySessionClose = false;

        if (refs.cookbookPanel != null)
            refs.cookbookPanel.SetActive(true);

        if (refs.grilledCheesePage1 != null)
            refs.grilledCheesePage1.SetActive(true);

        SetGrandmaVisible(false);

        if (refs.studySessionPopup != null)
        {
            refs.studySessionPopup.OpenSession(new string[] { "Bread", "Butter", "Cheese" });
        }
        else if (refs.studySessionRoot != null)
        {
            refs.studySessionRoot.SetActive(true);
        }

        wasStudySessionOpen = IsStudySessionCurrentlyOpen();
    }

    private void OnSignPressed()
    {
        if (refs == null) return;
        if (Step != TutorialStep.GrandmaWaitSignBreadButton) return;

        if (refs.signButton != null)
            refs.signButton.gameObject.SetActive(false);

        Step = TutorialStep.GrandmaWaitBreadSign;
        SaveStep();
        ApplyStepForCurrentScene();

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterSignButtonIndex);
    }


    private void OnSignDonePressed()
    {
        if (refs == null) return;
        if (Step != TutorialStep.GrandmaWaitSignDoneGate) return;

        SetActive(refs.signEngineRoot, false);

        if (refs.signDoneButton != null)
        {
            refs.signDoneButton.gameObject.SetActive(false);
            refs.signDoneButton.interactable = false;
        }

        Step = TutorialStep.GrandmaWaitAfterSignDone;
        SaveStep();

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterSignDoneIndex);
    }

    private void OnBackPressed()
    {
        if (Step != TutorialStep.GrandmaWaitBackButton) return;

        Step = HasSeenRestaurantIntro2() ? TutorialStep.RestaurantFreeRoam : TutorialStep.RestaurantIntro2;
        SaveStep();
        SceneManager.LoadScene(restaurantSceneName);
    }

    private void OnKitchenPressed()
    {
        if (Step != TutorialStep.RestaurantFreeRoam) return;

        if (HasSeenKitchenTutorial())
        {
            SceneManager.LoadScene(kitchenSceneName);
            return;
        }

        Step = TutorialStep.KitchenTutorial;
        SaveStep();
        SceneManager.LoadScene(kitchenSceneName);
    }

    private void OnRestaurantDialogueIndexChanged(int index)
    {
        if (Step == TutorialStep.RestaurantIntro1 && index == 1)
        {
            Step = TutorialStep.RestaurantWaitDoorHighlight;
            SaveStep();
            ApplyStepForCurrentScene();
        }
    }

    private void OnConversationDialogueIndexChanged(int index)
    {
        if (refs == null) return;

        if (Step == TutorialStep.GrandmaIntro &&
            index == grandmaPauseForCookbookAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitCookbookOpen;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitGrilledCheeseClick &&
            !hasClickedGrilledCheese &&
            index == grandmaPauseForGrilledCheeseAfterIndex + 1)
        {
            refs.conversationNPC?.SetExternalPause(true);

            canClickGrilledCheese = true;
            SetGrandmaVisible(false);

            if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
            if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(true);
            if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(false);

            SetActive(refs.grilledCheeseHighlight, true);
            SetInteractable(refs.grilledCheeseButton, true);
            return;
        }

        if (Step == TutorialStep.GrandmaWaitGrilledCheeseClick &&
            hasClickedGrilledCheese &&
            index == grandmaResumeAfterGrilledCheeseIndex)
        {
            SetGrandmaVisible(false);

            if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
            if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);

            return;
        }

        if (Step == TutorialStep.GrandmaWaitGrilledCheeseClick &&
            hasClickedGrilledCheese &&
            index == grandmaCloseCookbookOnLoveLineIndex)
        {
            CloseCookbookView();
            return;
        }

        if ((Step == TutorialStep.GrandmaWaitGrilledCheeseClick || Step == TutorialStep.GrandmaIntro) &&
            index == grandmaPauseForCookbookReopenAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitCookbookReopen;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitBreadClick &&
            index == grandmaPauseForBreadClickAfterIndex)
        {
            SetActive(refs.breadHighlight, true);
            refs.conversationNPC?.SetExternalPause(true);
            return;
        }

        if (Step == TutorialStep.GrandmaWaitStudyClose &&
            index == grandmaResumeAfterStudySessionIndex)
        {
            CloseCookbookView();
            return;
        }

        if ((Step == TutorialStep.GrandmaIntro ||
            Step == TutorialStep.GrandmaWaitStudyClose ||
            Step == TutorialStep.GrandmaWaitGrilledCheeseClick ||
            Step == TutorialStep.GrandmaWaitBreadClick) &&
            index == grandmaPauseForSignButtonAfterIndex + 1)
        {
            CloseCookbookView();

            Step = TutorialStep.GrandmaWaitSignBreadButton;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitBreadSign &&
            index == grandmaPauseForSignDoneAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitSignDoneGate;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitAfterSignDone &&
            index == grandmaPauseForBackButtonAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitBackButton;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }
    }

    private void OnSequenceEnded(DialogueSequence sequence)
    {
        if (sequence == DialogueSequence.Prologue && Step == TutorialStep.Prologue)
        {
            Step = TutorialStep.RestaurantIntro1;
            SaveStep();
            SceneManager.LoadScene(restaurantSceneName);
            return;
        }

        if (sequence == DialogueSequence.RestaurantIntro1 &&
            Step == TutorialStep.RestaurantWaitDoorHighlight)
        {
            MarkRestaurantIntro1Seen();

            Step = TutorialStep.RestaurantWaitMomClick;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (sequence == DialogueSequence.RestaurantMomConvo1 &&
            Step == TutorialStep.RestaurantMomConvo1)
        {
            ShowRestaurantView();
            Step = TutorialStep.RestaurantWaitDoorToGrandma;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (sequence == DialogueSequence.RestaurantMomReminder1)
        {
            ShowRestaurantView();
            return;
        }

        if (sequence == DialogueSequence.GrandmasHouse1 &&
            Step == TutorialStep.GrandmaWaitBackButton)
        {
            ApplyStepForCurrentScene();
            return;
        }

        if (sequence == DialogueSequence.RestaurantIntro2 &&
            Step == TutorialStep.RestaurantIntro2)
        {
            MarkRestaurantIntro2Seen();

            Step = TutorialStep.RestaurantFreeRoam;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (sequence == DialogueSequence.RestaurantMomReminder2 ||
            sequence == DialogueSequence.RestaurantGrandmaReminder2)
        {
            ShowRestaurantView();
            return;
        }

        if (sequence == DialogueSequence.KitchenTutorial &&
            Step == TutorialStep.KitchenTutorial)
        {
            MarkKitchenTutorialSeen();
            Step = TutorialStep.RestaurantFreeRoam;
            SaveStep();
            return;
        }
    }

    public void NotifyStudySessionClosed()
    {
        if (refs != null && refs.studySessionRoot != null)
            refs.studySessionRoot.SetActive(false);

        if (refs != null && refs.studySessionPopup != null)
            refs.studySessionPopup.gameObject.SetActive(false);

        if (Step == TutorialStep.GrandmaWaitStudyClose)
        {
            refs.conversationNPC?.SetExternalPause(false);
            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterStudySessionIndex);
        }
    }

    public void NotifyGrandmaDialogueFinishedAndEnableBack()
    {
        Step = TutorialStep.GrandmaWaitBackButton;
        SaveStep();
        ApplyStepForCurrentScene();
    }

#if UNITY_EDITOR
    private void Update()
    {
        CheckForStudySessionAutoClose();

        if (Input.GetKeyDown(KeyCode.F10))
        {
            PlayerPrefs.DeleteKey(tutorialStepKey);
            PlayerPrefs.DeleteKey(MOM_RESPONSE_KEY);
            PlayerPrefs.DeleteKey(restaurantIntro1SeenKey);
            PlayerPrefs.DeleteKey(restaurantIntro2SeenKey);
            PlayerPrefs.DeleteKey(kitchenTutorialSeenKey);
            PlayerPrefs.Save();
            Debug.Log("Tutorial reset.");
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            Step = TutorialStep.GrandmaIntro;
            hasClickedGrilledCheese = false;
            canClickGrilledCheese = false;
            wasStudySessionOpen = false;
            hasHandledStudySessionClose = false;
            SaveStep();
            SceneManager.LoadScene(grandmaSceneName);
            Debug.Log("Jumped to Grandma intro.");
        }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (SceneManager.GetActiveScene().name == grandmaSceneName)
            {
                SetGrandmaVisible(true);
                ShowConversationView();
                refs.conversationNPC?.SetExternalPause(false);
                refs.conversationNPC?.PlaySequence(DialogueSequence.GrandmasHouse1);
                Debug.Log("Replaying Grandma dialogue.");
            }
        }
    }
#else
    private void Update()
    {
        CheckForStudySessionAutoClose();
    }
#endif
}