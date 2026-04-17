using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class StackingMinigame : MonoBehaviour
{
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
    [SerializeField] private float grateTolerance = 60f;   // “dance”
    [SerializeField] private float dropTolerance = 110f;   // “cut”

    [Header("Drop Anim")]
    [SerializeField] private float topFallDuration = 0.25f;
    [SerializeField] private float stackYOffset = 16f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    bool initRecognizer;
    int frame;
    int attempts;      // max 4 (grating attempts)
    int successes;     // need 2
    bool breadPaused;
    Vector2 cheeseHandStartAnchored;

    enum Phase { Grating, DropPrompt, Dropping, Done }
    Phase phase = Phase.Grating;

    readonly List<string> levelSigns = new() { "dance", "cut" };

    // ---------- helpers ----------
    static void Show(Image img)
    {
        if (!img) return;
        var c = img.color; c.a = 1f; img.color = c;
        img.enabled = true;
        img.gameObject.SetActive(true);
        img.transform.SetAsLastSibling();
    }

    static void Hide(Image img)
    {
        if (!img) return;
        img.enabled = false;
    }

    static void SetSprite(Image img, Sprite s)
    {
        if (!img) return;
        img.sprite = s;
        var c = img.color; c.a = 1f; img.color = c;
    }

    static void ForceSize(RectTransform rt, Vector2 size)
    {
        if (!rt) return;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    void LogRect(string label, RectTransform rt)
    {
        if (!rt) return;
        var r = rt.rect;
        Debug.Log($"[Stacking] {label} pos={rt.position} size=({r.width:F1},{r.height:F1}) sibling={rt.GetSiblingIndex()}");
    }

    Vector2 BreadSize() =>
        movingBreadRect ? new Vector2(Mathf.Max(1, movingBreadRect.rect.width), Mathf.Max(1, movingBreadRect.rect.height))
                        : new Vector2(220, 120);

    void Start()
    {
        if (!graterHandImage || !cheeseHandImage || !movingBreadImage)
            Debug.LogWarning("[Stacking] Missing one or more required Image refs.");

        // Setup initial sprites
        SetSprite(graterHandImage, graterHandSprite); Show(graterHandImage);
        SetSprite(cheeseHandImage, cheeseHandSprite); Show(cheeseHandImage);
        cheeseHandStartAnchored = cheeseHandImage.rectTransform.anchoredPosition;

        SetSprite(cheeseFallingImage, cheeseFallingSprite); Hide(cheeseFallingImage);
        SetSprite(movingBreadImage, breadSlideSprite); Show(movingBreadImage);

        if (cheesePileImage)
        {
            if (movingBreadImage && cheesePileImage.transform.parent != movingBreadImage.transform)
                cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, false);
            cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
            Hide(cheesePileImage);
            cheesePileImage.sprite = null;
        }

        if (topBreadImage)
        {
            SetSprite(topBreadImage, breadSlideSprite);
            Hide(topBreadImage);
        }

        attempts = 0;
        successes = 0;
        breadPaused = false;
        phase = Phase.Grating;

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
        Debug.Log("[Stacking] Ready. Waiting for 'dance'...");

        // << NEW: Reset attempt circles
        if (attemptsUI != null) attemptsUI.ResetAttempts();
    }

    void Update()
    {
        if (!initRecognizer)
        {
            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
            initRecognizer = true;
            Debug.Log("[Stacking] Recognizer initialized.");
        }

        if (frame == 200) { frame = 0; engine.buffer.TriggerCallbacks(); }
        else frame++;

        if (!breadPaused) SlideBread();
    }

    void OnSignRecognized(string raw)
    {
        Debug.Log("[Stacking] Recognized sign: " + raw);
        string s = raw.ToLowerInvariant();

        if (phase == Phase.Grating && s == "dance")
        {
            StartCoroutine(HandleGrateAttemptFlow());
            return;
        }

        if ((phase == Phase.DropPrompt || phase == Phase.Dropping) && s == "cut")
        {
            HandleDropAttempt();
            return;
        }
    }

    IEnumerator HandleGrateAttemptFlow()
    {
        breadPaused = true;

        int nextAttempt = Mathf.Min(attempts + 1, 4);
        attempts = nextAttempt;

        Debug.Log($"[Stacking] Grate attempt {attempts}/4");

        yield return StartCoroutine(PunchHandVertical(
            cheeseHandImage.rectTransform,
            cheeseHandStartAnchored,
            -40f, 0.12f
        ));

        bool ok = IsAligned(movingBreadRect, graterHandRect, grateTolerance);
        if (ok)
        {
            successes = Mathf.Min(successes + 1, 2);
            AudioManager.Instance.PlaySfx(GameAudioPaths.StackingCheeseGrating, 0.85f);
            Debug.Log($"[Stacking] SUCCESS grate ({successes}/2).");
            UpdateCheesePile();
            yield return StartCoroutine(CheeseFallingFX());
        }
        else
        {
            AudioManager.Instance.PlaySfx(GameAudioPaths.UiWrongAction, 0.9f);
            Debug.Log("[Stacking] FAIL grate.");
            yield return StartCoroutine(MissJitter(graterHandImage.rectTransform));
        }

        // << NEW: Update attempt circle
        if (attemptsUI != null) attemptsUI.RegisterAttempt(ok);

        if (successes >= 2)
        {
            Debug.Log("[Stacking] Entering drop phase.");
            EnterDropPhase();
        }
        else if (attempts >= 4)
        {
            Debug.Log("[Stacking] Out of attempts → Restart.");
            RestartMinigame();
        }
        else
        {
            uiManager?.UpdateSteps($"Sign 'Dance' when the bread is under the grater. ({successes}/2)");
            breadPaused = false;
        }
    }

    void EnterDropPhase()
    {
        phase = Phase.DropPrompt;
        AudioManager.Instance.PlaySfx(GameAudioPaths.UiOkAction, 0.85f);

        Hide(cheeseFallingImage);
        Hide(cheeseHandImage);
        Hide(graterHandImage);

        if (topBreadImage)
        {
            ForceSize(topBreadImage.rectTransform, BreadSize());
            topBreadImage.transform.SetAsLastSibling();
            Show(topBreadImage);
        }

        uiManager?.UpdateSteps("Sign 'Cut' to place the top slice.");
        breadPaused = false;
    }

    void HandleDropAttempt()
    {
        phase = Phase.Dropping;
        Debug.Log("[Stacking] Drop attempt...");

        bool ok = IsAligned(movingBreadRect, topBreadImage.rectTransform, dropTolerance);
        if (ok)
        {
            Debug.Log("[Stacking] DROP aligned.");
            AudioManager.Instance.PlaySfx(GameAudioPaths.UiGoodAction, 0.9f);
            StartCoroutine(DropSuccessFlow());
        }
        else
        {
            Debug.Log("[Stacking] DROP FAILED → Restart.");
            AudioManager.Instance.PlaySfx(GameAudioPaths.UiWrongAction, 0.9f);
            RestartMinigame();
        }
    }

    IEnumerator DropSuccessFlow()
    {
        breadPaused = true;

        var startPos = topBreadImage.rectTransform.position;
        var targetY = movingBreadRect.position.y + stackYOffset;

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

        uiManager?.UpdateSteps("Nice! Sandwich stacked.");
        Debug.Log("[Stacking] Success → closing.");

        EndMinigame();
    }

    void SlideBread()
    {
        if (!movingBreadRect || !leftBound || !rightBound) return;

        float t = Mathf.PingPong(Time.time * slideSpeed, 1f);
        movingBreadRect.position = Vector3.Lerp(leftBound.position, rightBound.position, t);
    }

    bool IsAligned(RectTransform a, RectTransform b, float tolPx)
    {
        float dx = Mathf.Abs(a.position.x - b.position.x);
        Debug.Log($"[Stacking] dx={dx:F1} (tol={tolPx})");
        return dx <= tolPx;
    }

    void UpdateCheesePile()
    {
        if (!cheesePileImage) return;

        if (cheesePileImage.transform.parent != movingBreadImage.transform)
            cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, false);

        cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
        ForceSize(cheesePileImage.rectTransform, BreadSize() * 0.6f);

        if (successes == 1) SetSprite(cheesePileImage, cheesePileSmallSprite);
        else if (successes >= 2) SetSprite(cheesePileImage, cheesePileMedSprite);

        Show(cheesePileImage);
    }

    IEnumerator CheeseFallingFX()
    {
        if (!cheeseFallingImage) yield break;

        ForceSize(cheeseFallingImage.rectTransform, new Vector2(64, 64));
        Show(cheeseFallingImage);

        Vector3 startPos = new Vector3(graterHandRect.position.x, graterHandRect.position.y - 10f, 0);
        Vector3 endPos = new Vector3(movingBreadRect.position.x, movingBreadRect.position.y + 5f, 0);

        cheeseFallingImage.rectTransform.position = startPos;

        const float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = t / dur;

            endPos.x = movingBreadRect.position.x;
            cheeseFallingImage.rectTransform.position = Vector3.Lerp(startPos, endPos, u);

            yield return null;
        }

        Hide(cheeseFallingImage);
    }

    IEnumerator PunchHandVertical(RectTransform rt, Vector2 basePos, float downOffset, float duration)
    {
        if (!rt) yield break;

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

    IEnumerator MissJitter(RectTransform rt)
    {
        if (!rt) yield break;

        Vector3 orig = rt.localScale;
        Vector3 up = orig * 1.05f;

        const float dur = 0.08f;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(orig, up, t / dur);
            yield return null;
        }

        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(up, orig, t / dur);
            yield return null;
        }

        rt.localScale = orig;
    }

    void EndMinigame()
    {
        phase = Phase.Done;
        minigameManager?.CloseMinigame();
    }

    void RestartMinigame()
    {
        StopAllCoroutines();
        attempts = 0;
        successes = 0;
        breadPaused = false;
        phase = Phase.Grating;

        if (attemptsUI != null) attemptsUI.ResetAttempts();

        Show(graterHandImage); 
        SetSprite(graterHandImage, graterHandSprite);
        graterHandImage.rectTransform.localScale = Vector3.one;

        Show(cheeseHandImage);
        SetSprite(cheeseHandImage, cheeseHandSprite);
        cheeseHandImage.rectTransform.anchoredPosition = cheeseHandStartAnchored;

        Hide(cheeseFallingImage);

        SetSprite(movingBreadImage, breadSlideSprite); 
        Show(movingBreadImage);

        if (cheesePileImage)
        {
            Hide(cheesePileImage);
            cheesePileImage.sprite = null;
            cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (topBreadImage)
        {
            Hide(topBreadImage);
            SetSprite(topBreadImage, breadSlideSprite);
        }

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
        Debug.Log("[Stacking] Restarted.");
    }
}
