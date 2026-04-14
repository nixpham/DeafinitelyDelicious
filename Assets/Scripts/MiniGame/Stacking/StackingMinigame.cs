using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class StackingMinigame : MonoBehaviour
{
    private enum Phase
    {
        Inactive,
        Study,
        Grating,
        DropPrompt,
        Dropping,
        Success
    }

    [Header("UI Images")]
    [SerializeField] private Image graterHandImage;
    [SerializeField] private Image cheeseHandImage;
    [SerializeField] private Image cheeseFallingImage;
    [SerializeField] private Image movingBreadImage;
    [SerializeField] private Image cheesePileImage;
    [SerializeField] private Image topBreadImage;

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;

    [Header("Sprites")]
    [SerializeField] private Sprite breadSlideSprite;
    [SerializeField] private Sprite graterHandSprite;
    [SerializeField] private Sprite cheeseHandSprite;
    [SerializeField] private Sprite cheeseFallingSprite;
    [SerializeField] private Sprite cheesePileSmallSprite;
    [SerializeField] private Sprite cheesePileMedSprite;

    [Header("Layout / Movement")]
    [SerializeField] private RectTransform movingBreadRect;
    [SerializeField] private RectTransform graterHandRect;
    [SerializeField] private RectTransform leftBound;
    [SerializeField] private RectTransform rightBound;
    [SerializeField] private float slideSpeed = 1.2f;

    [Header("Tolerances (px)")]
    [SerializeField] private float grateTolerance = 60f;
    [SerializeField] private float dropTolerance = 110f;

    [Header("Drop Anim")]
    [SerializeField] private float topFallDuration = 0.25f;
    [SerializeField] private float stackYOffset = 16f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private StudySessionPopup studyPopup;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    private bool recognizerInitialized;
    private int frame;
    private int attempts;
    private int successes;
    private bool breadPaused;
    private bool actionLocked;
    private Vector2 cheeseHandStartAnchored;

    private Phase phase = Phase.Inactive;

    private readonly List<string> recognizerSigns = new() { "dance", "cut" };
    private readonly string[] studySigns = { "dance", "cut" };

    private static void Show(Image img)
    {
        if (!img) return;

        var c = img.color;
        c.a = 1f;
        img.color = c;
        img.enabled = true;
        img.gameObject.SetActive(true);
    }

    private static void Hide(Image img)
    {
        if (!img) return;

        img.enabled = false;
        img.gameObject.SetActive(false);
    }

    private static void SetSprite(Image img, Sprite s)
    {
        if (!img) return;

        img.sprite = s;
        var c = img.color;
        c.a = 1f;
        img.color = c;
    }

    private static void ForceSize(RectTransform rt, Vector2 size)
    {
        if (!rt) return;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    private Vector2 BreadSize()
    {
        if (movingBreadRect != null)
            return new Vector2(
                Mathf.Max(1, movingBreadRect.rect.width),
                Mathf.Max(1, movingBreadRect.rect.height)
            );

        return new Vector2(220, 120);
    }

    private void Awake()
    {
        if (minigameManager == null)
            minigameManager = FindObjectOfType<MinigameManager>();

        cheeseHandStartAnchored = cheeseHandImage != null
            ? cheeseHandImage.rectTransform.anchoredPosition
            : Vector2.zero;

        ForceIdleState();
    }

    private void Start()
    {
        if (engine == null)
            Debug.LogError("[Stacking] Engine is NOT assigned in the inspector.");
        else
            Debug.Log("[Stacking] Engine FOUND: " + engine.name);

        Debug.Log("[Stacking] Start ran. Phase remains = " + phase);
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (Input.anyKeyDown)
            Debug.Log("[Stacking] A key was pressed. Current phase = " + phase);

        if (engine == null)
        {
            Debug.LogWarning("[Stacking] Engine is NULL");
            return;
        }

        if (engine.recognizer == null)
        {
            Debug.LogWarning("[Stacking] Recognizer is NULL");
            return;
        }

        if (engine.buffer == null)
        {
            Debug.LogWarning("[Stacking] Buffer is NULL");
            return;
        }

        // Keep polling during Study / Grating / Drop like the old version.
        if (phase != Phase.Inactive && phase != Phase.Success)
        {
            if (frame >= 200)
            {
                frame = 0;
                Debug.Log("[Stacking] Triggering callbacks (engine running) | phase = " + phase);
                engine.buffer.TriggerCallbacks();
            }
            else
            {
                frame++;
            }
        }

        if (!breadPaused)
            SlideBread();

        // HOTKEY: Skip study -> gameplay
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Alpha1))
            && phase == Phase.Study)
        {
            Debug.Log("[Stacking] HOTKEY: Study -> Grating");

            if (studyPopup != null && studyPopup.popupRoot != null)
                studyPopup.popupRoot.SetActive(false);

            BeginGameplay();
        }

        // HOTKEY: Simulate dance
        if ((Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Alpha2))
            && (phase == Phase.Grating || phase == Phase.Study) && !actionLocked)
        {
            Debug.Log("[Stacking] HOTKEY: Simulate DANCE");

            if (phase == Phase.Study)
            {
                if (studyPopup != null && studyPopup.popupRoot != null)
                    studyPopup.popupRoot.SetActive(false);

                BeginGameplay();
            }

            if (phase == Phase.Grating)
                StartCoroutine(HandleGrateAttemptFlow());
        }

        // HOTKEY: Simulate cut
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha3))
            && (phase == Phase.DropPrompt || phase == Phase.Dropping) && !actionLocked)
        {
            Debug.Log("[Stacking] HOTKEY: Simulate CUT");
            HandleDropAttempt();
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
        Debug.Log("[Stacking] Callback successfully registered to recognizer.");
    }

    public void OnOpenedByManager()
    {
        Debug.Log("[Stacking] Minigame OPENED");
        FullReset();
    }

    public void OnRedoPressed()
    {
        Debug.Log("[Stacking] Minigame REDO");
        FullReset();
    }

    public void OnNextPressed()
    {
        Debug.Log("[Stacking] Minigame NEXT pressed");
    }

    private void FullReset()
    {
        StopAllCoroutines();

        frame = 0;
        attempts = 0;
        successes = 0;
        breadPaused = false;
        actionLocked = false;
        phase = Phase.Study;

        Debug.Log("[Stacking] Reset -> Phase = STUDY");

        ForceIdleState();
        OpenStudySession();
    }

    private void OpenStudySession()
    {
        Debug.Log("[Stacking] Opening study session");

        if (studyPopup == null)
        {
            Debug.LogWarning("[Stacking] Study popup missing. Starting gameplay immediately.");
            BeginGameplay();
            return;
        }

        studyPopup.OpenSession(studySigns);
        uiManager?.UpdateSteps("Before we start cooking, let's first learn the signs associated!");
    }

    private void ForceIdleState()
    {
        SetSprite(graterHandImage, graterHandSprite);
        Show(graterHandImage);

        SetSprite(cheeseHandImage, cheeseHandSprite);
        Show(cheeseHandImage);
        if (cheeseHandImage != null)
            cheeseHandImage.rectTransform.anchoredPosition = cheeseHandStartAnchored;

        SetSprite(cheeseFallingImage, cheeseFallingSprite);
        Hide(cheeseFallingImage);

        SetSprite(movingBreadImage, breadSlideSprite);
        Show(movingBreadImage);

        if (cheesePileImage != null)
        {
            if (movingBreadImage != null && cheesePileImage.transform.parent != movingBreadImage.transform)
                cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, false);

            cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
            cheesePileImage.sprite = null;
            Hide(cheesePileImage);
        }

        if (topBreadImage != null)
        {
            SetSprite(topBreadImage, breadSlideSprite);
            Hide(topBreadImage);
        }

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        Debug.Log("[Stacking] Forced idle state.");
    }

    private void BeginGameplay()
    {
        attempts = 0;
        successes = 0;
        breadPaused = false;
        actionLocked = false;
        phase = Phase.Grating;

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
        Debug.Log("[Stacking] Phase -> GRATING");
    }

    private void OnSignRecognized(string raw)
    {
        Debug.Log("[Stacking] Callback fired. Raw input = " + raw + " | phase = " + phase);

        if (phase == Phase.Inactive || phase == Phase.Success)
            return;

        if (string.IsNullOrEmpty(raw))
            return;

        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
        {
            Debug.Log("[Stacking] Ignored (study popup open)");
            return;
        }

        if (phase == Phase.Study)
        {
            Debug.Log("[Stacking] Study finished -> moving to GRATING");
            BeginGameplay();
            return;
        }

        string sign = raw.ToLowerInvariant();
        Debug.Log("[Stacking] SIGN DETECTED: " + sign);

        if (phase == Phase.Grating && sign == "dance" && !actionLocked)
        {
            StartCoroutine(HandleGrateAttemptFlow());
            return;
        }

        if ((phase == Phase.DropPrompt || phase == Phase.Dropping) && sign == "cut" && !actionLocked)
        {
            HandleDropAttempt();
        }
    }

    private IEnumerator HandleGrateAttemptFlow()
    {
        actionLocked = true;
        breadPaused = true;

        attempts = Mathf.Min(attempts + 1, 4);
        Debug.Log("[Stacking] Grate attempt " + attempts + "/4");

        if (cheeseHandImage != null)
        {
            yield return StartCoroutine(PunchHandVertical(
                cheeseHandImage.rectTransform,
                cheeseHandStartAnchored,
                -40f,
                0.12f
            ));
        }

        bool success = IsAligned(movingBreadRect, graterHandRect, grateTolerance);
        Debug.Log("[Stacking] Grate result = " + success);

        if (success)
        {
            successes = Mathf.Min(successes + 1, 2);
            Debug.Log("[Stacking] SUCCESS grate (" + successes + "/2)");

            UpdateCheesePile();
            yield return StartCoroutine(CheeseFallingFX());
        }
        else
        {
            Debug.Log("[Stacking] FAIL grate");

            if (graterHandImage != null)
                yield return StartCoroutine(MissJitter(graterHandImage.rectTransform));
        }

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        if (successes >= 2)
        {
            Debug.Log("[Stacking] Entering drop phase.");
            EnterDropPhase();
            actionLocked = false;
            yield break;
        }

        if (attempts >= 4)
        {
            Debug.Log("[Stacking] Out of grate attempts -> restart");
            RestartGameplayOnly();
            yield break;
        }

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater. (" + successes + "/2)");
        breadPaused = false;
        actionLocked = false;
    }

    private void EnterDropPhase()
    {
        phase = Phase.DropPrompt;

        Hide(cheeseFallingImage);
        Hide(cheeseHandImage);
        Hide(graterHandImage);

        if (topBreadImage != null)
        {
            ForceSize(topBreadImage.rectTransform, BreadSize());
            Show(topBreadImage);
        }

        uiManager?.UpdateSteps("Sign 'Cut' to place the top slice.");
        breadPaused = false;

        Debug.Log("[Stacking] Phase -> DROP PROMPT");
    }

    private void HandleDropAttempt()
    {
        actionLocked = true;
        phase = Phase.Dropping;

        Debug.Log("[Stacking] Drop attempt...");

        bool success = IsAligned(movingBreadRect, topBreadImage != null ? topBreadImage.rectTransform : null, dropTolerance);
        Debug.Log("[Stacking] Drop result = " + success);

        if (success)
        {
            StartCoroutine(DropSuccessFlow());
        }
        else
        {
            Debug.Log("[Stacking] DROP FAILED -> Restart");
            RestartGameplayOnly();
        }
    }

    private IEnumerator DropSuccessFlow()
    {
        breadPaused = true;

        if (topBreadImage == null || movingBreadRect == null)
        {
            Debug.LogWarning("[Stacking] Missing top bread or moving bread rect during drop success.");
            phase = Phase.Success;
            uiManager?.UpdateSteps("Success!");
            minigameManager?.ShowSuccessPopup("Success");
            actionLocked = false;
            yield break;
        }

        Vector3 startPos = topBreadImage.rectTransform.position;
        float targetY = movingBreadRect.position.y + stackYOffset;

        float t = 0f;
        while (t < topFallDuration)
        {
            t += Time.deltaTime;
            float u = topFallDuration <= 0f ? 1f : t / topFallDuration;

            float followX = movingBreadRect.position.x;

            topBreadImage.rectTransform.position = new Vector3(
                Mathf.Lerp(startPos.x, followX, u),
                Mathf.Lerp(startPos.y, targetY, u),
                startPos.z
            );

            yield return null;
        }

        phase = Phase.Success;
        Debug.Log("[Stacking] SUCCESS STATE REACHED");

        uiManager?.UpdateSteps("Success!");
        minigameManager?.ShowSuccessPopup("Success");
        actionLocked = false;
    }

    private void SlideBread()
    {
        if (movingBreadRect == null || leftBound == null || rightBound == null)
            return;

        float t = Mathf.PingPong(Time.time * slideSpeed, 1f);

        float x = Mathf.Lerp(leftBound.position.x, rightBound.position.x, t);
        Vector3 currentPos = movingBreadRect.position;

        movingBreadRect.position = new Vector3(x, currentPos.y, currentPos.z);
    }

    private bool IsAligned(RectTransform a, RectTransform b, float tolerancePx)
    {
        if (a == null || b == null)
            return false;

        float dx = Mathf.Abs(a.position.x - b.position.x);
        Debug.Log("[Stacking] dx = " + dx.ToString("F1") + " (tol = " + tolerancePx + ")");
        return dx <= tolerancePx;
    }

    private void UpdateCheesePile()
    {
        if (cheesePileImage == null)
            return;

        if (movingBreadImage != null && cheesePileImage.transform.parent != movingBreadImage.transform)
            cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, false);

        cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;

        if (successes == 1)
            SetSprite(cheesePileImage, cheesePileSmallSprite);
        else if (successes >= 2)
            SetSprite(cheesePileImage, cheesePileMedSprite);

        Show(cheesePileImage);
        Debug.Log("[Stacking] Cheese pile updated. successes = " + successes);
    }

    private IEnumerator CheeseFallingFX()
    {
        if (cheeseFallingImage == null || graterHandRect == null || movingBreadRect == null)
            yield break;

        ForceSize(cheeseFallingImage.rectTransform, new Vector2(64f, 64f));
        Show(cheeseFallingImage);

        Vector3 startPos = new Vector3(graterHandRect.position.x, graterHandRect.position.y - 10f, 0f);
        Vector3 endPos = new Vector3(movingBreadRect.position.x, movingBreadRect.position.y + 5f, 0f);

        cheeseFallingImage.rectTransform.position = startPos;

        const float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = t / duration;

            endPos.x = movingBreadRect.position.x;
            cheeseFallingImage.rectTransform.position = Vector3.Lerp(startPos, endPos, u);

            yield return null;
        }

        Hide(cheeseFallingImage);
    }

    private IEnumerator PunchHandVertical(RectTransform rt, Vector2 basePos, float downOffset, float duration)
    {
        if (rt == null)
            yield break;

        Vector2 start = basePos;
        Vector2 down = start + new Vector2(0f, downOffset);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(start, down, t / duration);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(down, start, t / duration);
            yield return null;
        }

        rt.anchoredPosition = start;
    }

    private IEnumerator MissJitter(RectTransform rt)
    {
        if (rt == null)
            yield break;

        Vector3 orig = rt.localScale;
        Vector3 up = orig * 1.05f;

        const float duration = 0.08f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(orig, up, t / duration);
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(up, orig, t / duration);
            yield return null;
        }

        rt.localScale = orig;
    }

    private void RestartGameplayOnly()
    {
        Debug.Log("[Stacking] RestartGameplayOnly called.");

        StopAllCoroutines();

        attempts = 0;
        successes = 0;
        breadPaused = false;
        actionLocked = false;
        phase = Phase.Grating;

        ForceIdleState();
        BeginGameplay();
    }
}