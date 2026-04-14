using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private StudySessionPopup studyPopup;

    [Header("Instructions")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    [Header("Settings")]
    [SerializeField] private int maxPickupAttempts = 4;
    [SerializeField] private int maxSliceAttempts = 4;
    [SerializeField] private int requiredSuccessfulSlices = 2;
    [SerializeField] private float straightTolerance = 10f;

    [Header("Cut Animation")]
    [SerializeField] private float cutMoveDistance = 18f;
    [SerializeField] private float cutMoveDuration = 0.08f;
    [SerializeField] private int cutMotionRepeats = 2;

    private bool recognizerInitialized;
    private int frame;

    private int pickupAttempts;
    private int sliceAttempts;
    private int successfulCuts;

    private bool cutAnimationPlaying;

    private Phase phase = Phase.Inactive;

    private readonly List<string> recognizerSigns = new() { "dance", "cut" };
    private readonly string[] studySigns = { "dance", "cut" };

    private void Awake()
    {
        if (engine == null && PersistentSignEngine.Instance != null)
            engine = PersistentSignEngine.Instance.Engine;

        if (minigameManager == null)
            minigameManager = FindObjectOfType<MinigameManager>();

        ForceIdleVisualState();
    }

    private void Start()
    {
        if (engine == null)
            Debug.LogError("[Slicing] Engine is NOT assigned in the inspector.");
        else
            Debug.Log("[Slicing] Engine FOUND: " + engine.name);

        Debug.Log("[Slicing] Start ran. Phase remains = " + phase);
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (Input.anyKeyDown)
            Debug.Log("[Slicing] A key was pressed. Current phase = " + phase);

        if (engine == null)
        {
            Debug.LogWarning("[Slicing] Engine is NULL");
            return;
        }

        if (engine.recognizer == null)
        {
            Debug.LogWarning("[Slicing] Recognizer is NULL");
            return;
        }

        if (engine.buffer == null)
        {
            Debug.LogWarning("[Slicing] Buffer is NULL");
            return;
        }

        if (phase != Phase.Inactive && phase != Phase.Success)
        {
            if (frame >= 200)
            {
                frame = 0;
                Debug.Log("[Slicing] Triggering callbacks (engine running) | phase = " + phase);
                engine.buffer.TriggerCallbacks();
            }
            else
            {
                frame++;
            }
        }

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Alpha1))
            && phase == Phase.Study)
        {
            Debug.Log("[Slicing] HOTKEY: Study -> Pickup");

            if (studyPopup != null && studyPopup.popupRoot != null)
                studyPopup.popupRoot.SetActive(false);

            BeginPickupPhase();
        }

        if ((Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Alpha2))
            && (phase == Phase.Pickup || phase == Phase.Study))
        {
            Debug.Log("[Slicing] HOTKEY: Simulate DANCE");

            if (phase == Phase.Study)
            {
                if (studyPopup != null && studyPopup.popupRoot != null)
                    studyPopup.popupRoot.SetActive(false);

                BeginPickupPhase();
            }

            HandlePickupAttempt("dance");
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha3))
            && phase == Phase.Slice && !cutAnimationPlaying)
        {
            Debug.Log("[Slicing] HOTKEY: Simulate CUT");
            HandleSliceAttempt("cut");
        }
    }

    private void SetInstruction(string msg)
    {
        if (instructionText != null)
            instructionText.text = msg;
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
        Debug.Log("[Slicing] Callback successfully registered to recognizer.");
    }

    public void OnOpenedByManager()
    {
        Debug.Log("[Slicing] Minigame OPENED");
        FullReset();
    }

    public void OnRedoPressed()
    {
        Debug.Log("[Slicing] Minigame REDO");
        FullReset();
    }

    public void OnNextPressed()
    {
        Debug.Log("[Slicing] Minigame NEXT pressed");
    }

    private void FullReset()
    {
        StopAllCoroutines();
        cutAnimationPlaying = false;
        frame = 0;

        pickupAttempts = 0;
        sliceAttempts = 0;
        successfulCuts = 0;
        phase = Phase.Study;

        Debug.Log("[Slicing] Reset -> Phase = STUDY");

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        ForceIdleVisualState();
        ResetBreadVisual();
        OpenStudySession();
    }

    private void OpenStudySession()
    {
        Debug.Log("[Slicing] Opening study session");

        if (studyPopup == null)
        {
            Debug.LogWarning("[Slicing] Study popup missing.");
            BeginPickupPhase();
            return;
        }

        studyPopup.OpenSession(studySigns);
        SetInstruction("Sign \"Dance\" to pick up the knife!");
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

        Debug.Log("[Slicing] Forced idle visual state.");
    }

    private void BeginPickupPhase()
    {
        phase = Phase.Pickup;
        pickupAttempts = 0;
        cutAnimationPlaying = false;

        Debug.Log("[Slicing] Phase -> PICKUP");

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
        SetInstruction("Sign \"Dance\" to pick up the knife!");
    }

    private void BeginSlicePhase()
    {
        phase = Phase.Slice;
        sliceAttempts = 0;
        successfulCuts = 0;
        cutAnimationPlaying = false;
        frame = 0;

        Debug.Log("[Slicing] Phase -> SLICE");

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        if (tableKnife != null)
            tableKnife.SetActive(false);

        if (rotatingKnife != null)
            rotatingKnife.SetActive(true);

        if (knifeRotation != null)
        {
            knifeRotation.ResetRotation();
            knifeRotation.pauseRotation = false;
            knifeRotation.enabled = true;
        }

        ResetBreadVisual();
        SetInstruction("Now, sign \"Cut\" when the knife is vertical to make 2 straight slices!");
    }

    private void OnSignRecognized(string rawSign)
    {
        Debug.Log("[Slicing] Callback fired. Raw input = " + rawSign + " | phase = " + phase);

        if (phase == Phase.Inactive || phase == Phase.Success)
            return;

        if (string.IsNullOrEmpty(rawSign))
            return;

        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
        {
            Debug.Log("[Slicing] Ignored (study popup open)");
            return;
        }

        if (phase == Phase.Study)
        {
            Debug.Log("[Slicing] Study finished -> moving to PICKUP");
            BeginPickupPhase();
            return;
        }

        string sign = rawSign.ToLowerInvariant();
        Debug.Log("🔥 [Slicing] SIGN DETECTED: " + sign);

        if (phase == Phase.Pickup)
        {
            HandlePickupAttempt(sign);
        }
        else if (phase == Phase.Slice && !cutAnimationPlaying)
        {
            HandleSliceAttempt(sign);
        }
    }

    private void HandlePickupAttempt(string sign)
    {
        pickupAttempts++;
        bool success = sign == "dance";

        Debug.Log("[Slicing] Pickup attempt " + pickupAttempts + " | sign = " + sign + " | success = " + success);

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        if (success)
        {
            BeginSlicePhase();
            return;
        }

        if (pickupAttempts >= maxPickupAttempts)
        {
            Debug.Log("[Slicing] Pickup attempts exhausted -> restarting pickup");
            SetInstruction("Sign \"Dance\" to pick up the knife!");
            BeginPickupPhase();
        }
    }

    private void HandleSliceAttempt(string sign)
    {
        if (sign != "cut")
        {
            sliceAttempts++;

            Debug.Log("[Slicing] Slice attempt " + sliceAttempts + " | sign = " + sign + " | knifeStraight = false");
            Debug.Log("[Slicing] FAILED CUT");

            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(false);

            if (sliceAttempts >= maxSliceAttempts)
            {
                Debug.Log("[Slicing] Slice attempts exhausted -> back to pickup");
                SetInstruction("Sign \"Dance\" to pick up the knife!");
                BeginPickupPhase();
                return;
            }

            SetInstruction("Now, sign \"Cut\" when the knife is vertical to make 2 straight slices!");
            return;
        }

        bool knifeStraight = IsKnifeStraight();
        Debug.Log("[Slicing] CUT detected. knifeStraight at detection = " + knifeStraight);

        StartCoroutine(DoCutAttempt(knifeStraight));
    }

    private IEnumerator DoCutAttempt(bool success)
    {
        cutAnimationPlaying = true;
        sliceAttempts++;

        Vector3 savedLocalPosition = rotatingKnife != null ? rotatingKnife.transform.localPosition : Vector3.zero;

        Debug.Log("[Slicing] Saving knife state before cut: angle = "
            + (knifeRotation != null ? knifeRotation.GetCurrentAngle().ToString("F2") : "N/A")
            + " direction = "
            + (knifeRotation != null ? knifeRotation.GetDirection().ToString() : "N/A"));

        if (knifeRotation != null)
            knifeRotation.pauseRotation = true;

        Vector3 startPos = savedLocalPosition;
        Vector3 forwardPos = startPos + Vector3.down * cutMoveDistance;

        for (int i = 0; i < cutMotionRepeats; i++)
        {
            yield return MoveKnifeLocal(startPos, forwardPos, cutMoveDuration);
            yield return MoveKnifeLocal(forwardPos, startPos, cutMoveDuration);
        }

        if (rotatingKnife != null)
            rotatingKnife.transform.localPosition = savedLocalPosition;

        if (success)
        {
            successfulCuts++;
            UpdateBreadVisualForCuts();
            Debug.Log("[Slicing] SUCCESSFUL CUT (" + successfulCuts + "/" + requiredSuccessfulSlices + ")");
        }
        else
        {
            Debug.Log("[Slicing] FAILED CUT");
        }

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        if (successfulCuts >= requiredSuccessfulSlices)
        {
            if (knifeRotation != null)
                knifeRotation.pauseRotation = false;

            HandleSuccess();
            cutAnimationPlaying = false;
            yield break;
        }

        if (sliceAttempts >= maxSliceAttempts)
        {
            Debug.Log("[Slicing] Slice attempts exhausted -> back to pickup");

            if (knifeRotation != null)
                knifeRotation.pauseRotation = false;

            SetInstruction("Sign \"Dance\" to pick up the knife!");
            cutAnimationPlaying = false;
            BeginPickupPhase();
            yield break;
        }

        if (knifeRotation != null)
            knifeRotation.pauseRotation = false;

        SetInstruction("Now, sign \"Cut\" when the knife is vertical to make 2 straight slices!");
        cutAnimationPlaying = false;
    }

    private IEnumerator MoveKnifeLocal(Vector3 from, Vector3 to, float duration)
    {
        if (rotatingKnife == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            rotatingKnife.transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }

        rotatingKnife.transform.localPosition = to;
    }

    private bool IsKnifeStraight()
    {
        if (knifeRotation != null)
            return knifeRotation.IsStraight(straightTolerance);

        if (rotatingKnife == null)
            return false;

        float z = rotatingKnife.transform.eulerAngles.z;
        if (z > 180f)
            z -= 360f;

        return Mathf.Abs(z) <= straightTolerance;
    }

    private void HandleSuccess()
    {
        phase = Phase.Success;

        Debug.Log("[Slicing] SUCCESS STATE REACHED");

        if (knifeRotation != null)
            knifeRotation.enabled = false;

        SetInstruction("Success!");
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