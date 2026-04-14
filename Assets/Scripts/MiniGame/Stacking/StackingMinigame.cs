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
        img.transform.SetAsLastSibling();
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
            return new Vector2(Mathf.Max(1, movingBreadRect.rect.width), Mathf.Max(1, movingBreadRect.rect.height));

        return new Vector2(220, 120);
    }

    private void Start()
    {
        cheeseHandStartAnchored = cheeseHandImage != null
            ? cheeseHandImage.rectTransform.anchoredPosition
            : Vector2.zero;

        ForceIdleState();
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (phase == Phase.Inactive || phase == Phase.Study || phase == Phase.Success)
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

        if (!breadPaused)
            SlideBread();
    }

    private void InitRecognizerIfNeeded()
    {
        if (recognizerInitialized || engine == null)
            return;

        engine.recognizer.AddCallback("print", OnSignRecognized);
        engine.recognizer.outputFilters.Clear();
        engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(recognizerSigns));
        engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
        recognizerInitialized = true;
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
        StopAllCoroutines();

        attempts = 0;
        successes = 0;
        breadPaused = false;
        phase = Phase.Study;

        ForceIdleState();
        OpenStudySession();
    }

    private void OpenStudySession()
    {
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
    }

    private void BeginGameplay()
    {
        attempts = 0;
        successes = 0;
        breadPaused = false;
        phase = Phase.Grating;

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
    }

    private void OnSignRecognized(string raw)
    {
        if (phase == Phase.Inactive || phase == Phase.Success)
            return;

        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
            return;

        if (phase == Phase.Study)
        {
            BeginGameplay();
            return;
        }

        string sign = raw.ToLowerInvariant();

        if (phase == Phase.Grating && sign == "dance")
        {
            StartCoroutine(HandleGrateAttemptFlow());
            return;
        }

        if ((phase == Phase.DropPrompt || phase == Phase.Dropping) && sign == "cut")
        {
            HandleDropAttempt();
        }
    }

    private IEnumerator HandleGrateAttemptFlow()
    {
        breadPaused = true;
        attempts = Mathf.Min(attempts + 1, 4);

        yield return StartCoroutine(PunchHandVertical(
            cheeseHandImage.rectTransform,
            cheeseHandStartAnchored,
            -40f,
            0.12f
        ));

        bool ok = IsAligned(movingBreadRect, graterHandRect, grateTolerance);

        if (ok)
        {
            successes = Mathf.Min(successes + 1, 2);
            UpdateCheesePile();
            yield return StartCoroutine(CheeseFallingFX());
        }
        else
        {
            yield return StartCoroutine(MissJitter(graterHandImage.rectTransform));
        }

        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(ok);

        if (successes >= 2)
        {
            EnterDropPhase();
        }
        else if (attempts >= 4)
        {
            RestartGameplayOnly();
        }
        else
        {
            uiManager?.UpdateSteps($"Sign 'Dance' when the bread is under the grater. ({successes}/2)");
            breadPaused = false;
        }
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
            topBreadImage.transform.SetAsLastSibling();
            Show(topBreadImage);
        }

        uiManager?.UpdateSteps("Sign 'Cut' to place the top slice.");
        breadPaused = false;
    }

    private void HandleDropAttempt()
    {
        phase = Phase.Dropping;

        bool ok = IsAligned(movingBreadRect, topBreadImage.rectTransform, dropTolerance);
        if (ok)
        {
            StartCoroutine(DropSuccessFlow());
        }
        else
        {
            RestartGameplayOnly();
        }
    }

    private IEnumerator DropSuccessFlow()
    {
        breadPaused = true;

        Vector3 startPos = topBreadImage.rectTransform.position;
        float targetY = movingBreadRect.position.y + stackYOffset;

        float t = 0f;
        while (t < topFallDuration)
        {
            t += Time.deltaTime;
            float u = t / topFallDuration;

            float followX = movingBreadRect.position.x;

            topBreadImage.rectTransform.position = new Vector3(
                Mathf.Lerp(startPos.x, followX, u),
                Mathf.Lerp(startPos.y, targetY, u),
                startPos.z
            );

            yield return null;
        }

        phase = Phase.Success;
        uiManager?.UpdateSteps("Success!");
        minigameManager?.ShowSuccessPopup("Success");
    }

    private void SlideBread()
    {
        if (movingBreadRect == null || leftBound == null || rightBound == null)
            return;

        float t = Mathf.PingPong(Time.time * slideSpeed, 1f);
        movingBreadRect.position = Vector3.Lerp(leftBound.position, rightBound.position, t);
    }

    private bool IsAligned(RectTransform a, RectTransform b, float tolerancePx)
    {
        if (a == null || b == null)
            return false;

        float dx = Mathf.Abs(a.position.x - b.position.x);
        return dx <= tolerancePx;
    }

    private void UpdateCheesePile()
    {
        if (cheesePileImage == null)
            return;

        if (cheesePileImage.transform.parent != movingBreadImage.transform)
            cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, false);

        cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
        ForceSize(cheesePileImage.rectTransform, BreadSize() * 0.6f);

        if (successes == 1)
            SetSprite(cheesePileImage, cheesePileSmallSprite);
        else if (successes >= 2)
            SetSprite(cheesePileImage, cheesePileMedSprite);

        Show(cheesePileImage);
    }

    private IEnumerator CheeseFallingFX()
    {
        if (cheeseFallingImage == null)
            yield break;

        ForceSize(cheeseFallingImage.rectTransform, new Vector2(64, 64));
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
        StopAllCoroutines();
        ForceIdleState();
        BeginGameplay();
    }
}