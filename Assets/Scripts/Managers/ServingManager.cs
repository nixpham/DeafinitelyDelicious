using UnityEngine;
using UnityEngine.SceneManagement;

public class ServingManager : MonoBehaviour
{
    private const string SERVING_READY_KEY = "DEMO_GRILLED_CHEESE_READY_TO_SERVE";
    private const string FIRST_SERVE_FINISHED_KEY = "DEMO_FIRST_GRILLED_CHEESE_SERVE_FINISHED";
    private const string TUTORIAL_COMPLETED_KEY = "DEMO_TUTORIAL_COMPLETED";
    private const string TUTORIAL_STEP_KEY = "DEMO_TUTORIAL_STEP";

    [Header("Scene Refs")]
    [SerializeField] private TutorialSceneRefs refs;

    [Header("Optional Direct Refs")]
    [SerializeField] private DraggableServeItem draggableServeItem;
    [SerializeField] private CanvasGroup draggableCanvasGroup;

    private bool waitingForIntroBefore3;
    private bool waitingForServeConversation;
    private bool waitingForMapEndDemo;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        HookNPCs();
    }

    private void Start()
    {
        RefreshRestaurantServeState();
    }

    private void OnDisable()
    {
        UnhookNPCs();
    }

    private void ResolveReferences()
    {
        if (refs == null)
            refs = FindObjectOfType<TutorialSceneRefs>(true);

        if (refs != null && refs.restaurantCompletedGrilledCheeseObject != null)
        {
            if (draggableServeItem == null)
                draggableServeItem = refs.restaurantCompletedGrilledCheeseObject.GetComponent<DraggableServeItem>();

            if (draggableCanvasGroup == null)
                draggableCanvasGroup = refs.restaurantCompletedGrilledCheeseObject.GetComponent<CanvasGroup>();
        }

        if (draggableServeItem != null)
            draggableServeItem.SetServingManager(this);
    }

    private void HookNPCs()
    {
        if (refs == null) return;

        if (refs.restaurantNPC != null)
        {
            refs.restaurantNPC.OnSequenceEnded -= OnRestaurantSequenceEnded;
            refs.restaurantNPC.OnSequenceEnded += OnRestaurantSequenceEnded;
        }

        if (refs.conversationNPC != null)
        {
            refs.conversationNPC.OnSequenceEnded -= OnConversationSequenceEnded;
            refs.conversationNPC.OnSequenceEnded += OnConversationSequenceEnded;
        }
    }

    private void UnhookNPCs()
    {
        if (refs == null) return;

        if (refs.restaurantNPC != null)
            refs.restaurantNPC.OnSequenceEnded -= OnRestaurantSequenceEnded;

        if (refs.conversationNPC != null)
            refs.conversationNPC.OnSequenceEnded -= OnConversationSequenceEnded;
    }

    private bool IsRestaurantScene()
    {
        return SceneManager.GetActiveScene().name == "RestaurantScene";
    }

    private bool IsServeReady()
    {
        return PlayerPrefs.GetInt(SERVING_READY_KEY, 0) == 1;
    }

    private bool IsTutorialCompleted()
    {
        return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
    }

    private bool IsFirstServeFinished()
    {
        return PlayerPrefs.GetInt(FIRST_SERVE_FINISHED_KEY, 0) == 1;
    }

    public void RefreshRestaurantServeState()
    {
        ResolveReferences();

        if (!IsRestaurantScene() || refs == null)
            return;

        waitingForIntroBefore3 = false;
        waitingForServeConversation = false;
        waitingForMapEndDemo = false;

        if (refs.restaurantCompletedGrilledCheeseObject == null)
            return;

        refs.restaurantCompletedGrilledCheeseObject.gameObject.SetActive(false);
        SetDishDragEnabled(false);

        if (!IsServeReady())
        {
            SetRestaurantButtonsInteractable(true);
            return;
        }

        if (refs.restaurantViewRoot != null)
            refs.restaurantViewRoot.SetActive(true);

        if (refs.conversationViewRoot != null)
            refs.conversationViewRoot.SetActive(false);

        refs.restaurantCompletedGrilledCheeseObject.gameObject.SetActive(true);

        if (draggableServeItem != null)
        {
            draggableServeItem.CacheStartPosition();
            draggableServeItem.ResetToStart();
        }

        if (!IsTutorialCompleted() && !IsFirstServeFinished())
        {
            waitingForIntroBefore3 = true;
            SetRestaurantButtonsInteractable(false);
            SetDishDragEnabled(false);

            if (refs.restaurantNPC != null)
            {
                Debug.Log("[ServingManager] Playing RestaurantIntroBefore3");
                refs.restaurantNPC.PlaySequence(DialogueSequence.RestaurantIntroBefore3);
            }
            else
            {
                Debug.LogWarning("[ServingManager] restaurantNPC is missing.");
            }
        }
        else
        {
            SetRestaurantButtonsInteractable(true);
            SetDishDragEnabled(true);
        }
    }

    private void SetDishDragEnabled(bool enabled)
    {
        if (draggableServeItem != null)
            draggableServeItem.SetDragEnabled(enabled);

        if (draggableCanvasGroup == null &&
            refs != null &&
            refs.restaurantCompletedGrilledCheeseObject != null)
        {
            draggableCanvasGroup = refs.restaurantCompletedGrilledCheeseObject.GetComponent<CanvasGroup>();
        }

        if (draggableCanvasGroup != null)
        {
            draggableCanvasGroup.interactable = enabled;
            draggableCanvasGroup.blocksRaycasts = enabled;
            draggableCanvasGroup.alpha = 1f;
        }
    }

    private void SetRestaurantButtonsInteractable(bool interactable)
    {
        if (refs == null) return;

        if (refs.momButton != null) refs.momButton.interactable = interactable;
        if (refs.restaurantGrandmaButton != null) refs.restaurantGrandmaButton.interactable = interactable;
        if (refs.mapButton != null) refs.mapButton.interactable = interactable;
        if (refs.kitchenButton != null) refs.kitchenButton.interactable = interactable;
        if (refs.doorButton != null) refs.doorButton.interactable = interactable;
    }

    public bool CanAcceptDrop(Vector2 screenPosition, UnityEngine.Camera eventCamera)
    {
        if (refs == null || refs.serveTableDropZone == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            refs.serveTableDropZone,
            screenPosition,
            eventCamera
        );
    }

    public void HandleSuccessfulServe(DraggableServeItem item)
    {
        if (refs == null || !IsRestaurantScene())
            return;

        Debug.Log("[ServingManager] Successful serve.");

        PlayerPrefs.SetInt(SERVING_READY_KEY, 0);
        PlayerPrefs.Save();

        SetDishDragEnabled(false);

        if (refs.restaurantCompletedGrilledCheeseObject != null)
            refs.restaurantCompletedGrilledCheeseObject.gameObject.SetActive(false);

        if (refs.restaurantViewRoot != null)
            refs.restaurantViewRoot.SetActive(false);

        if (refs.conversationViewRoot != null)
            refs.conversationViewRoot.SetActive(true);

        SetRestaurantButtonsInteractable(false);
        waitingForServeConversation = true;

        if (!IsTutorialCompleted() && !IsFirstServeFinished())
        {
            if (refs.conversationNPC != null)
            {
                Debug.Log("[ServingManager] Playing RestaurantIntro3");
                refs.conversationNPC.PlaySequence(DialogueSequence.RestaurantIntro3);
            }
            else
            {
                Debug.LogWarning("[ServingManager] conversationNPC is missing for RestaurantIntro3.");
            }
        }
        else
        {
            if (refs.conversationNPC != null)
            {
                Debug.Log("[ServingManager] Playing MomGrandmaGrilledCheeseDone");
                refs.conversationNPC.PlaySequence(DialogueSequence.MomGrandmaGrilledCheeseDone);
            }
            else
            {
                Debug.LogWarning("[ServingManager] conversationNPC is missing for MomGrandmaGrilledCheeseDone.");
            }
        }
    }

    private void OnRestaurantSequenceEnded(DialogueSequence sequence)
    {
        if (refs == null) return;

        if (sequence == DialogueSequence.RestaurantIntroBefore3 && waitingForIntroBefore3)
        {
            Debug.Log("[ServingManager] RestaurantIntroBefore3 ended. Drag now enabled.");
            waitingForIntroBefore3 = false;
            SetRestaurantButtonsInteractable(true);
            SetDishDragEnabled(true);
            return;
        }

        if (sequence == DialogueSequence.MapEndDemo && waitingForMapEndDemo)
        {
            Debug.Log("[ServingManager] MapEndDemo ended. Tutorial complete.");
            waitingForMapEndDemo = false;

            PlayerPrefs.SetInt(FIRST_SERVE_FINISHED_KEY, 1);
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            PlayerPrefs.SetInt(TUTORIAL_STEP_KEY, (int)TutorialStep.RestaurantFreeRoam);
            PlayerPrefs.Save();

            if (refs.restaurantViewRoot != null)
                refs.restaurantViewRoot.SetActive(true);

            if (refs.conversationViewRoot != null)
                refs.conversationViewRoot.SetActive(false);

            SetRestaurantButtonsInteractable(true);
        }
    }

    private void OnConversationSequenceEnded(DialogueSequence sequence)
    {
        if (!waitingForServeConversation || refs == null)
            return;

        if (sequence == DialogueSequence.RestaurantIntro3)
        {
            Debug.Log("[ServingManager] RestaurantIntro3 ended. Starting MapEndDemo.");
            waitingForServeConversation = false;
            waitingForMapEndDemo = true;

            if (refs.restaurantViewRoot != null)
                refs.restaurantViewRoot.SetActive(true);

            if (refs.conversationViewRoot != null)
                refs.conversationViewRoot.SetActive(false);

            if (refs.restaurantNPC != null)
            {
                refs.restaurantNPC.PlaySequence(DialogueSequence.MapEndDemo);
            }
            else
            {
                Debug.LogWarning("[ServingManager] restaurantNPC is missing for MapEndDemo.");
            }

            return;
        }

        if (sequence == DialogueSequence.MomGrandmaGrilledCheeseDone)
        {
            Debug.Log("[ServingManager] Repeat serve dialogue ended.");
            waitingForServeConversation = false;

            if (refs.restaurantViewRoot != null)
                refs.restaurantViewRoot.SetActive(true);

            if (refs.conversationViewRoot != null)
                refs.conversationViewRoot.SetActive(false);

            SetRestaurantButtonsInteractable(true);
        }
    }
}