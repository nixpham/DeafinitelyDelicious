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
    GrandmaWaitCookbookForRemainingIngredients,
    GrandmaWaitRemainingSigns,
    GrandmaWaitBackButton,

    // Restaurant 2
    RestaurantIntro2,
    RestaurantFreeRoam
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Scene Names")]
    public string prologueSceneName = "PrologueScene";
    public string restaurantSceneName = "RestaurantScene";
    public string grandmaSceneName = "GrandmasHouseScene";
    public string kitchenSceneName = "KitchenScene";

    [Header("Saved Step Key")]
    public string tutorialStepKey = "DEMO_TUTORIAL_STEP";

    [Header("Grandma House Line Indices")]
    [Tooltip("After 'I have a gift for you' finishes, the popup should appear.")]
    public int grandmaGiftPopupShowAfterIndex = 5;

    [Tooltip("After 'It's all the knowledge I've gained...' finishes, the popup should disappear.")]
    public int grandmaGiftPopupHideAfterIndex = 7;

    [Tooltip("After 'Anything! I gave you my cookbook!' finishes, pause and wait for cookbook click.")]
    public int grandmaPauseForCookbookAfterIndex = 13;

    [Tooltip("Resume grandma dialogue at this index after first cookbook click.")]
    public int grandmaResumeAfterCookbookIndex = 14;

    [Tooltip("After 'Wow, I can't really understand any of this...' finishes, pause for grilled cheese click.")]
    public int grandmaPauseForGrilledCheeseAfterIndex = 14;

    [Tooltip("Resume grandma dialogue at this index after grilled cheese click.")]
    public int grandmaResumeAfterGrilledCheeseIndex = 15;

    [Tooltip("After 'In this game you will need to sign to progress in the game' finishes, wait for cookbook reopen.")]
    public int grandmaPauseForCookbookReopenAfterIndex = 18;

    [Tooltip("Resume grandma dialogue at this index after cookbook reopen.")]
    public int grandmaResumeAfterCookbookReopenIndex = 19;

    [Tooltip("After 'Click on bread' finishes, pause for bread button click.")]
    public int grandmaPauseForBreadClickAfterIndex = 20;

    [Tooltip("Resume grandma dialogue at this index after the study session closes.")]
    public int grandmaResumeAfterStudySessionIndex = 21;

    [Tooltip("After grandma says 'Let me know what you need', pause for Sign button.")]
    public int grandmaPauseForSignButtonAfterIndex = 22;

    [Tooltip("After grandma says 'Yes! Bread, here you go. What else?', wait for cookbook reopen for remaining ingredients.")]
    public int grandmaPauseForRemainingIngredientCookbookAfterIndex = 24;

    private TutorialSceneRefs refs;
    public TutorialStep Step { get; private set; } = TutorialStep.None;

    private bool breadSigned;
    private bool cheeseSigned;
    private bool butterSigned;
    private bool studySessionOpen;

    private const string MOM_RESPONSE_KEY = "MOM_RESPONSE";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
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

    private void ApplyStepForCurrentScene()
    {
        if (refs == null) return;

        ResetSceneUI();

        switch (Step)
        {
            case TutorialStep.Prologue:
                if (refs.sceneKind != TutorialSceneRefs.SceneKind.Prologue)
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
                if (refs.sceneKind != TutorialSceneRefs.SceneKind.Restaurant)
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
            case TutorialStep.GrandmaWaitCookbookForRemainingIngredients:
            case TutorialStep.GrandmaWaitRemainingSigns:
            case TutorialStep.GrandmaWaitBackButton:
                if (refs.sceneKind != TutorialSceneRefs.SceneKind.GrandmasHouse)
                {
                    SceneManager.LoadScene(grandmaSceneName);
                    return;
                }
                ApplyGrandmaStep();
                break;
        }
    }

    private void ApplyRestaurantStep()
    {
        ShowRestaurantView();

        if (refs.mapButton != null) refs.mapButton.interactable = false;
        if (refs.kitchenButton != null) refs.kitchenButton.interactable = false;

        switch (Step)
        {
            case TutorialStep.RestaurantIntro1:
                if (refs.momSprite != null) refs.momSprite.SetActive(false);
                if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(false);
                refs.restaurantNPC?.PlaySequence(DialogueSequence.RestaurantIntro1);
                break;

            case TutorialStep.RestaurantWaitDoorHighlight:
                SetActive(refs.doorHighlight, true);
                SetInteractable(refs.doorButton, true);

                if (refs.momSprite != null)
                {
                    refs.momSprite.SetActive(false);
                }

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

        if (refs.backButton != null) refs.backButton.interactable = false;

        switch (Step)
        {
            case TutorialStep.GrandmaIntro:
                if (refs.grandmaHouseSprite != null) refs.grandmaHouseSprite.SetActive(true);
                refs.conversationNPC?.PlaySequence(DialogueSequence.GrandmasHouse1);
                break;

            case TutorialStep.GrandmaWaitCookbookOpen:
                SetActive(refs.cookbookHighlight, true);
                if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(true);
                SetInteractable(refs.cookbookButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitGrilledCheeseClick:
                if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
                SetActive(refs.grilledCheeseHighlight, true);
                if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(true);
                SetInteractable(refs.grilledCheeseButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitCookbookReopen:
                SetActive(refs.cookbookHighlight, true);
                if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(true);
                SetInteractable(refs.cookbookButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitBreadClick:
                if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
                if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);
                SetActive(refs.breadHighlight, true);
                if (refs.breadButton != null) refs.breadButton.gameObject.SetActive(true);
                SetInteractable(refs.breadButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitStudyClose:
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitSignBreadButton:
                if (refs.signButton != null) refs.signButton.gameObject.SetActive(true);
                SetInteractable(refs.signButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitBreadSign:
                SetActive(refs.signEngineRoot, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitCookbookForRemainingIngredients:
                SetActive(refs.cookbookHighlight, true);
                if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(true);
                SetInteractable(refs.cookbookButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitRemainingSigns:
                if (refs.signButton != null) refs.signButton.gameObject.SetActive(true);
                SetInteractable(refs.signButton, true);
                refs.conversationNPC?.SetExternalPause(true);
                break;

            case TutorialStep.GrandmaWaitBackButton:
                SetInteractable(refs.backButton, true);
                break;
        }
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
        SetActive(refs.giftPopup, false);
        SetActive(refs.signEngineRoot, false);

        SetInteractable(refs.doorButton, false);
        SetInteractable(refs.momButton, false);
        SetInteractable(refs.restaurantGrandmaButton, false);
        SetInteractable(refs.backButton, false);
        SetInteractable(refs.cookbookButton, false);
        SetInteractable(refs.grilledCheeseButton, false);
        SetInteractable(refs.breadButton, false);
        SetInteractable(refs.signButton, false);
        SetInteractable(refs.kitchenButton, false);

        if (refs.cookbookButton != null) refs.cookbookButton.gameObject.SetActive(false);
        if (refs.grilledCheeseButton != null) refs.grilledCheeseButton.gameObject.SetActive(false);
        if (refs.breadButton != null) refs.breadButton.gameObject.SetActive(false);
        if (refs.signButton != null) refs.signButton.gameObject.SetActive(false);

        if (refs.momSprite != null) refs.momSprite.SetActive(false);
        if (refs.restaurantGrandmaSprite != null) refs.restaurantGrandmaSprite.SetActive(false);
        if (refs.grandmaHouseSprite != null) refs.grandmaHouseSprite.SetActive(false);

        if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(false);
        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(false);
        if (refs.studySessionRoot != null && !studySessionOpen) refs.studySessionRoot.SetActive(false);
    }

    private void ShowRestaurantViewIfRestaurantScene()
    {
        if (refs.sceneKind == TutorialSceneRefs.SceneKind.Restaurant)
        {
            ShowRestaurantView();
        }
        else if (refs.sceneKind == TutorialSceneRefs.SceneKind.GrandmasHouse)
        {
            ShowConversationView();
        }
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

    private void OnDoorPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.RestaurantWaitDoorHighlight)
        {
            SetActive(refs.doorHighlight, false);
            SetInteractable(refs.doorButton, false);

            if (refs.momSprite != null)
            {
                refs.momSprite.SetActive(true);
            }

            if (refs.momButton != null)
            {
                refs.momButton.interactable = false;
            }

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
            SetActive(refs.cookbookHighlight, false);

            if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);

            refs.conversationNPC?.SetExternalPause(false);
            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterCookbookIndex);

            Step = TutorialStep.GrandmaWaitGrilledCheeseClick;
            SaveStep();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitCookbookReopen)
        {
            SetActive(refs.cookbookHighlight, false);

            if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
            if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);

            refs.conversationNPC?.SetExternalPause(false);
            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterCookbookReopenIndex);

            Step = TutorialStep.GrandmaWaitBreadClick;
            SaveStep();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitCookbookForRemainingIngredients)
        {
            SetActive(refs.cookbookHighlight, false);

            if (refs.cookbookPanel != null) refs.cookbookPanel.SetActive(true);
            if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);

            Step = TutorialStep.GrandmaWaitRemainingSigns;
            SaveStep();
            ApplyStepForCurrentScene();
        }
    }

    private void OnGrilledCheesePressed()
    {
        if (refs == null || Step != TutorialStep.GrandmaWaitGrilledCheeseClick) return;

        SetActive(refs.grilledCheeseHighlight, false);

        if (refs.grilledCheesePage1 != null) refs.grilledCheesePage1.SetActive(true);

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterGrilledCheeseIndex);
    }

    private void OnBreadPressed()
    {
        if (refs == null || Step != TutorialStep.GrandmaWaitBreadClick) return;

        SetActive(refs.breadHighlight, false);

        if (refs.studySessionRoot != null)
        {
            refs.studySessionRoot.SetActive(true);
        }

        studySessionOpen = true;
        Step = TutorialStep.GrandmaWaitStudyClose;
        SaveStep();
        ApplyStepForCurrentScene();
    }

    private void OnSignPressed()
    {
        if (refs == null) return;

        if (Step == TutorialStep.GrandmaWaitSignBreadButton)
        {
            if (refs.signButton != null) refs.signButton.gameObject.SetActive(false);

            Step = TutorialStep.GrandmaWaitBreadSign;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitRemainingSigns)
        {
            if (refs.signButton != null) refs.signButton.gameObject.SetActive(false);

            SetActive(refs.signEngineRoot, true);
        }
    }

    private void OnBackPressed()
    {
        if (Step != TutorialStep.GrandmaWaitBackButton) return;

        Step = TutorialStep.RestaurantIntro2;
        SaveStep();
        SceneManager.LoadScene(restaurantSceneName);
    }

    private void OnKitchenPressed()
    {
        if (Step == TutorialStep.RestaurantFreeRoam)
        {
            SceneManager.LoadScene(kitchenSceneName);
        }
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

        if (Step == TutorialStep.GrandmaIntro && index == grandmaGiftPopupShowAfterIndex + 1)
        {
            SetActive(refs.giftPopup, true);
        }

        if ((Step == TutorialStep.GrandmaIntro || Step == TutorialStep.GrandmaWaitCookbookOpen) &&
            index == grandmaGiftPopupHideAfterIndex + 1)
        {
            SetActive(refs.giftPopup, false);
        }

        if (Step == TutorialStep.GrandmaIntro &&
            index == grandmaPauseForCookbookAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitCookbookOpen;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitGrilledCheeseClick &&
            index == grandmaPauseForGrilledCheeseAfterIndex + 1)
        {
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitGrilledCheeseClick)
        {
            return;
        }

        if (Step == TutorialStep.GrandmaIntro || Step == TutorialStep.GrandmaWaitCookbookOpen)
        {
            // no-op
        }

        if (Step != TutorialStep.GrandmaWaitCookbookOpen &&
            Step != TutorialStep.GrandmaWaitBreadClick &&
            Step != TutorialStep.GrandmaWaitStudyClose &&
            Step != TutorialStep.GrandmaWaitSignBreadButton &&
            Step != TutorialStep.GrandmaWaitBreadSign &&
            Step != TutorialStep.GrandmaWaitCookbookForRemainingIngredients &&
            Step != TutorialStep.GrandmaWaitRemainingSigns &&
            Step != TutorialStep.GrandmaWaitBackButton)
        {
            // continue using line-based pauses only while grandma sequence is active
        }

        if ((Step == TutorialStep.GrandmaIntro || Step == TutorialStep.GrandmaWaitGrilledCheeseClick) &&
            index == grandmaPauseForCookbookReopenAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitCookbookReopen;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (Step == TutorialStep.GrandmaWaitBreadClick &&
            index == grandmaPauseForBreadClickAfterIndex + 1)
        {
            ApplyStepForCurrentScene();
            return;
        }

        if ((Step == TutorialStep.GrandmaIntro ||
             Step == TutorialStep.GrandmaWaitCookbookReopen ||
             Step == TutorialStep.GrandmaWaitBreadClick) &&
            index == grandmaPauseForSignButtonAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitSignBreadButton;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if ((Step == TutorialStep.GrandmaWaitBreadSign ||
             Step == TutorialStep.GrandmaWaitSignBreadButton) &&
            index == grandmaPauseForRemainingIngredientCookbookAfterIndex + 1)
        {
            Step = TutorialStep.GrandmaWaitCookbookForRemainingIngredients;
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
            Step = TutorialStep.RestaurantFreeRoam;
            SaveStep();
            ApplyStepForCurrentScene();
            return;
        }

        if (sequence == DialogueSequence.RestaurantMomReminder2 ||
            sequence == DialogueSequence.RestaurantGrandmaReminder2)
        {
            ShowRestaurantView();
        }
    }

    // =========================
    // CALLED BY OTHER OBJECTS
    // =========================

    public void NotifyStudySessionClosed()
    {
        if (refs != null && refs.studySessionRoot != null)
        {
            refs.studySessionRoot.SetActive(false);
        }

        studySessionOpen = false;

        if (Step == TutorialStep.GrandmaWaitStudyClose)
        {
            refs.conversationNPC?.SetExternalPause(false);
            refs.conversationNPC?.ResumeAfterClick(grandmaResumeAfterStudySessionIndex);

            Step = TutorialStep.GrandmaWaitSignBreadButton;
            SaveStep();
        }
    }

    public void NotifyBreadSigned()
    {
        if (Step != TutorialStep.GrandmaWaitBreadSign) return;

        breadSigned = true;
        SetActive(refs.signEngineRoot, false);

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(23);
    }

    public void NotifyCheeseSigned()
    {
        cheeseSigned = true;
        CheckRemainingIngredientsFinished();
    }

    public void NotifyButterSigned()
    {
        butterSigned = true;
        CheckRemainingIngredientsFinished();
    }

    private void CheckRemainingIngredientsFinished()
    {
        if (Step != TutorialStep.GrandmaWaitRemainingSigns) return;
        if (!cheeseSigned || !butterSigned) return;

        SetActive(refs.signEngineRoot, false);

        refs.conversationNPC?.SetExternalPause(false);
        refs.conversationNPC?.ResumeAfterClick(25);
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
        if (Input.GetKeyDown(KeyCode.F10))
        {
            PlayerPrefs.DeleteKey(tutorialStepKey);
            PlayerPrefs.DeleteKey(MOM_RESPONSE_KEY);
            PlayerPrefs.Save();
            Debug.Log("Tutorial reset.");
        }
    }
#endif
}