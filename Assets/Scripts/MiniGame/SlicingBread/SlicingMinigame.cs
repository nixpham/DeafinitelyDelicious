using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class SlicingMinigame : MonoBehaviour
{
    private enum Phase
    {
        Inactive,
        Study,
        Pickup,
        Slice,
        Success
    }

    [Header("Objects")]
    [SerializeField] private GameObject tableKnife;
    [SerializeField] private GameObject rotatingKnife;
    [SerializeField] private KnifeRotation knifeRotation;

    [Header("Bread UI")]
    [SerializeField] private Image breadImage;
    [SerializeField] private Sprite[] breadSlices;

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;

    [Header("Managers")]
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private StudySessionPopup studyPopup;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    [Header("Settings")]
    [SerializeField] private int maxPickupAttempts = 4;
    [SerializeField] private int maxSliceAttempts = 4;
    [SerializeField] private int requiredSuccessfulSlices = 2;
    [SerializeField] private float straightTolerance = 10f;

    private bool recognizerInitialized;
    private int frame;

    private int pickupAttempts;
    private int sliceAttempts;
    private int successfulCuts;

    private Phase phase = Phase.Inactive;

    private readonly List<string> recognizerSigns = new() { "dance", "cut" };
    private readonly string[] studySigns = { "dance", "cut" };

    private void Start()
    {
        if (minigameManager == null)
            minigameManager = FindObjectOfType<MinigameManager>();

        if (engine == null)
            Debug.LogError("[Slicing] Engine is NOT assigned in the inspector.");

        ForceIdleVisualState();
        phase = Phase.Inactive;
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (phase == Phase.Inactive || phase == Phase.Study || phase == Phase.Success)
            return;

        // Do not use engine until all required internals exist
        if (engine == null || engine.recognizer == null || engine.buffer == null)
            return;

        if (frame >= 200)
        {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }
        else
        {
            frame++;
        }

        // Optional keyboard testing
        if (Input.GetKeyDown(KeyCode.Space) && phase == Phase.Slice)
        {
            HandleSliceAttempt("cut");
        }

        // Optional keyboard testing for pickup
        if (Input.GetKeyDown(KeyCode.P) && phase == Phase.Pickup)
        {
            HandlePickupAttempt("dance");
        }
    }

    private void InitRecognizerIfNeeded()
    {
        if (recognizerInitialized)
            return;

        if (engine == null)
            return;

        if (engine.recognizer == null || engine.buffer == null)
            return;

        engine.recognizer.AddCallback("print", OnSignRecognized);
        engine.recognizer.outputFilters.Clear();
        engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(recognizerSigns));
        engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));

        recognizerInitialized = true;
        Debug.Log("[Slicing] Recognizer initialized.");
    }

    public void OnOpenedByManager()
    {
        FullReset();
    }

    public void OnRedoPressed()
    {
        FullReset();
    }

    public void OnNextPressed()
    {
        // Manager handles closing.
    }

    private void FullReset()
    {
        frame = 0;

        pickupAttempts = 0;
        sliceAttempts = 0;
        successfulCuts = 0;
        phase = Phase.Study;

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        ForceIdleVisualState();
        ResetBreadVisual();

        OpenStudySession();
    }

    private void OpenStudySession()
    {
        if (studyPopup == null)
        {
            Debug.LogWarning("[Slicing] Study popup missing. Starting gameplay immediately.");
            BeginPickupPhase();
            return;
        }

        studyPopup.OpenSession(studySigns);
        uiManager?.UpdateSteps("Before we start cooking, let's first learn the signs associated!");
    }

    private void ForceIdleVisualState()
    {
        if (tableKnife != null)
            tableKnife.SetActive(true);

        if (rotatingKnife != null)
            rotatingKnife.SetActive(false);

        if (knifeRotation != null)
        {
            knifeRotation.enabled = false;
            knifeRotation.ResetRotation();
        }
    }

    private void BeginPickupPhase()
    {
        phase = Phase.Pickup;
        pickupAttempts = 0;

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        if (tableKnife != null)
            tableKnife.SetActive(true);

        if (rotatingKnife != null)
            rotatingKnife.SetActive(false);

        if (knifeRotation != null)
        {
            knifeRotation.enabled = false;
            knifeRotation.ResetRotation();
        }

        ResetBreadVisual();
        uiManager?.UpdateSteps("Sign 'Dance' to pick up the knife. You have 4 tries.");
    }

    private void BeginSlicePhase()
    {
        phase = Phase.Slice;
        sliceAttempts = 0;
        successfulCuts = 0;
        frame = 0;

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        if (tableKnife != null)
            tableKnife.SetActive(false);

        if (rotatingKnife != null)
            rotatingKnife.SetActive(true);

        if (knifeRotation != null)
        {
            knifeRotation.ResetRotation();
            knifeRotation.enabled = true;
        }

        ResetBreadVisual();
        uiManager?.UpdateSteps("Sign 'Cut' when the knife is straight. Need 2 successful slices in 4 tries.");
    }

    private void OnSignRecognized(string rawSign)
    {
        if (phase == Phase.Inactive || phase == Phase.Success)
            return;

        if (string.IsNullOrEmpty(rawSign))
            return;

        // While study popup is open, block gameplay
        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
            return;

        // If study just finished, move into pickup automatically
        if (phase == Phase.Study)
        {
            BeginPickupPhase();
            return;
        }

        string sign = rawSign.ToLowerInvariant();
        Debug.Log("[Slicing] Recognized sign: " + sign);

        if (phase == Phase.Pickup)
        {
            HandlePickupAttempt(sign);
        }
        else if (phase == Phase.Slice)
        {
            HandleSliceAttempt(sign);
        }
    }

    private void HandlePickupAttempt(string sign)
    {
        pickupAttempts++;
        bool success = sign == "dance";

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        if (success)
        {
            uiManager?.UpdateSteps("Nice! You picked up the knife.");
            BeginSlicePhase();
            return;
        }

        if (pickupAttempts >= maxPickupAttempts)
        {
            uiManager?.UpdateSteps("You ran out of pickup tries. Restarting pickup.");
            BeginPickupPhase();
        }
        else
        {
            int remaining = maxPickupAttempts - pickupAttempts;
            uiManager?.UpdateSteps($"That was not the right sign. Sign 'Dance' to pick up the knife. {remaining} tries left.");
        }
    }

    private void HandleSliceAttempt(string sign)
    {
        sliceAttempts++;

        bool success = false;

        if (sign == "cut" && IsKnifeStraight())
        {
            success = true;
            successfulCuts++;
            UpdateBreadVisualForCuts();
            Debug.Log($"[Slicing] Successful cut. ({successfulCuts}/{requiredSuccessfulSlices})");
        }
        else
        {
            Debug.Log("[Slicing] Failed cut.");
        }

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        if (successfulCuts >= requiredSuccessfulSlices)
        {
            HandleSuccess();
            return;
        }

        if (sliceAttempts >= maxSliceAttempts)
        {
            uiManager?.UpdateSteps("You did not get 2 successful slices in 4 tries. Going back to picking up the knife.");
            BeginPickupPhase();
            return;
        }

        int remaining = maxSliceAttempts - sliceAttempts;
        int needed = requiredSuccessfulSlices - successfulCuts;
        uiManager?.UpdateSteps($"Need {needed} more successful slice(s). Sign 'Cut' when the knife is straight. {remaining} tries left.");
    }

    private bool IsKnifeStraight()
    {
        if (knifeRotation != null)
            return knifeRotation.IsStraight(straightTolerance);

        if (rotatingKnife == null)
            return false;

        float z = rotatingKnife.transform.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return Mathf.Abs(z) <= straightTolerance;
    }

    private void HandleSuccess()
    {
        phase = Phase.Success;

        if (knifeRotation != null)
            knifeRotation.enabled = false;

        uiManager?.UpdateSteps("Success!");
        minigameManager?.ShowSuccessPopup("Success");
    }

    private void ResetBreadVisual()
    {
        if (breadImage != null && breadSlices != null && breadSlices.Length > 0)
            breadImage.sprite = breadSlices[0];
    }

    private void UpdateBreadVisualForCuts()
    {
        if (breadImage == null || breadSlices == null || breadSlices.Length == 0)
            return;

        int index = Mathf.Clamp(successfulCuts, 0, breadSlices.Length - 1);
        breadImage.sprite = breadSlices[index];
    }
}