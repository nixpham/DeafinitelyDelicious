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
    [SerializeField] private Image movingBreadImage;   // bottom bread (slides)
    [SerializeField] private Image cheesePileImage;    // child of bottom bread
    [SerializeField] private Image topBreadImage;      // shown for DROP phase

    [Header("Sprites")]
    [SerializeField] private Sprite breadSlideSprite;
    [SerializeField] private Sprite graterHandSprite;
    [SerializeField] private Sprite cheeseHandSprite;
    [SerializeField] private Sprite cheeseFallingSprite;
    [SerializeField] private Sprite cheesePileSmallSprite;
    [SerializeField] private Sprite cheesePileMedSprite;

    [Header("Layout / Movement")]
    [SerializeField] private RectTransform movingBreadRect;
    [SerializeField] private RectTransform graterHandRect; // X used for alignment
    [SerializeField] private RectTransform leftBound;
    [SerializeField] private RectTransform rightBound;
    [SerializeField] private float slideSpeed = 1.2f;

    [Header("Tolerances (px)")]
    [SerializeField] private float grateTolerance = 60f;   // “dance”
    [SerializeField] private float dropTolerance  = 110f;  // “cut”

    [Header("Drop Anim")]
    [SerializeField] private float topFallDuration = 0.25f;
    [SerializeField] private float stackYOffset    = 16f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    bool initRecognizer;
    int frame;
    int attempts;      // max 4
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
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   size.y);
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

        // baseline sprites (keep editor sizes for hands/background)
        SetSprite(graterHandImage, graterHandSprite);     Show(graterHandImage);
        SetSprite(cheeseHandImage, cheeseHandSprite);     Show(cheeseHandImage);
        cheeseHandStartAnchored = cheeseHandImage ? cheeseHandImage.rectTransform.anchoredPosition : Vector2.zero;

        SetSprite(cheeseFallingImage, cheeseFallingSprite); Hide(cheeseFallingImage);
        SetSprite(movingBreadImage, breadSlideSprite);      Show(movingBreadImage);

        // cheese pile follows bread; start hidden & centered
        if (cheesePileImage)
        {
            if (movingBreadImage && cheesePileImage.transform.parent != movingBreadImage.transform)
                cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, worldPositionStays: false);
            cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;
            Hide(cheesePileImage);
            cheesePileImage.sprite = null;
        }

        if (topBreadImage)
        {
            SetSprite(topBreadImage, breadSlideSprite);
            Hide(topBreadImage);
        }

        attempts = 0; successes = 0; breadPaused = false; phase = Phase.Grating;
        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
        Debug.Log("[Stacking] Ready. Waiting for 'dance'...");
    }

    void Update()
    {
        if (!initRecognizer)
        {
            if (engine == null) { Debug.LogError("[Stacking] Engine not assigned."); return; }
            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
            initRecognizer = true;
            Debug.Log("[Stacking] Recognizer initialized (dance, cut).");
        }

        if (frame == 200) { frame = 0; engine.buffer.TriggerCallbacks(); } else frame++;

        if (!breadPaused) SlideBread();
    }

    void OnSignRecognized(string raw)
    {
        Debug.Log("[Stacking] Recognized sign: " + raw);
        var s = raw.ToLowerInvariant();

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
        Debug.Log($"[Stacking] Grate attempt {nextAttempt}/4");
        attempts = nextAttempt;

        yield return StartCoroutine(PunchHandVertical(cheeseHandImage.rectTransform, cheeseHandStartAnchored, -40f, 0.12f));

        bool ok = IsAligned(movingBreadRect, graterHandRect, grateTolerance);
        if (ok)
        {
            successes = Mathf.Min(successes + 1, 2);
            Debug.Log($"[Stacking] SUCCESS grate ({successes}/2). Show cheese + fall FX.");
            UpdateCheesePile();
            yield return StartCoroutine(CheeseFallingFX());
        }
        else
        {
            Debug.Log("[Stacking] FAIL grate (bread not under grater).");
            yield return StartCoroutine(MissJitter(graterHandImage.rectTransform));
        }

        if (successes >= 2)
        {
            Debug.Log("[Stacking] 2 successes → Drop phase.");
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

        Hide(cheeseFallingImage);
        Hide(cheeseHandImage);
        Hide(graterHandImage);

        if (topBreadImage)
        {
            // Force size roughly equal to bottom bread
            ForceSize(topBreadImage.rectTransform, BreadSize());
            topBreadImage.transform.SetAsLastSibling();
            Show(topBreadImage);
            LogRect("TopBread(show)", topBreadImage.rectTransform);
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
            Debug.Log("[Stacking] DROP aligned → falling top slice.");
            StartCoroutine(DropSuccessFlow());
        }
        else
        {
            Debug.Log("[Stacking] DROP failed → restart.");
            RestartMinigame();
        }
    }

    IEnumerator DropSuccessFlow()
    {
        breadPaused = true;

        var startPos = topBreadImage.rectTransform.position;
        var targetY  = movingBreadRect.position.y + stackYOffset;

        float t = 0f;
        while (t < topFallDuration)
        {
            t += Time.deltaTime;
            float u = t / topFallDuration;
            var followX = movingBreadRect.position.x;
            var midPos = new Vector3(Mathf.Lerp(startPos.x, followX, u),
                                     Mathf.Lerp(startPos.y, targetY, u),
                                     startPos.z);
            topBreadImage.rectTransform.position = midPos;
            yield return null;
        }
        topBreadImage.rectTransform.position = new Vector3(movingBreadRect.position.x, targetY, startPos.z);

        uiManager?.UpdateSteps("Nice! Sandwich stacked.");
        Debug.Log("[Stacking] Success → closing panel.");
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
        if (!a || !b)
        {
            Debug.LogWarning("[Stacking] IsAligned missing rect(s).");
            return false;
        }
        float dx = Mathf.Abs(a.position.x - b.position.x);
        Debug.Log($"[Stacking] Align check dx={dx:F1} (tol={tolPx})");
        return dx <= tolPx;
    }

    void UpdateCheesePile()
    {
        if (!cheesePileImage) { Debug.LogWarning("[Stacking] No cheesePileImage assigned."); return; }

        // Make sure it’s childed, centered, sized, and on top of the bread
        if (movingBreadImage && cheesePileImage.transform.parent != movingBreadImage.transform)
            cheesePileImage.rectTransform.SetParent(movingBreadImage.rectTransform, worldPositionStays: false);
        cheesePileImage.rectTransform.anchoredPosition = Vector2.zero;

        // Force size to ~60% of bread size
        ForceSize(cheesePileImage.rectTransform, BreadSize() * 0.6f);

        if (successes == 1 && cheesePileSmallSprite)      SetSprite(cheesePileImage, cheesePileSmallSprite);
        else if (successes >= 2 && cheesePileMedSprite)   SetSprite(cheesePileImage, cheesePileMedSprite);

        Show(cheesePileImage);
        LogRect("CheesePile(show)", cheesePileImage.rectTransform);
    }

    IEnumerator CheeseFallingFX()
    {
        if (!cheeseFallingImage || !graterHandRect || !movingBreadRect) yield break;

        // Force a visible size and bring to top
        ForceSize(cheeseFallingImage.rectTransform, new Vector2(64, 64));
        cheeseFallingImage.transform.SetAsLastSibling();
        Show(cheeseFallingImage);

        var startPos = new Vector3(graterHandRect.position.x, graterHandRect.position.y - 10f, 0f);
        var endPos   = new Vector3(movingBreadRect.position.x, movingBreadRect.position.y + 5f, 0f);
        cheeseFallingImage.rectTransform.position = startPos;
        LogRect("CheeseFalling(start)", cheeseFallingImage.rectTransform);

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

        LogRect("CheeseFalling(end)", cheeseFallingImage.rectTransform);
        Hide(cheeseFallingImage);
    }

    IEnumerator PunchHandVertical(RectTransform rt, Vector2 baseAnchoredPos, float downOffset, float duration)
    {
        if (!rt) yield break;
        var start = baseAnchoredPos;
        var down  = start + new Vector2(0f, downOffset);

        float t = 0f;
        while (t < duration) { t += Time.deltaTime; rt.anchoredPosition = Vector2.Lerp(start, down, t / duration); yield return null; }
        t = 0f;
        while (t < duration) { t += Time.deltaTime; rt.anchoredPosition = Vector2.Lerp(down, start, t / duration); yield return null; }
        rt.anchoredPosition = start;
    }

    IEnumerator MissJitter(RectTransform rt)
    {
        if (!rt) yield break;
        var orig = rt.localScale;
        var up = orig * 1.05f;
        const float dur = 0.08f;

        float t = 0f;
        while (t < dur) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(orig, up, t / dur); yield return null; }
        t = 0f;
        while (t < dur) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(up, orig, t / dur); yield return null; }
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
        attempts = 0; successes = 0; breadPaused = false; phase = Phase.Grating;

        Show(graterHandImage); SetSprite(graterHandImage, graterHandSprite); graterHandImage.rectTransform.localScale = Vector3.one;
        Show(cheeseHandImage); SetSprite(cheeseHandImage, cheeseHandSprite); cheeseHandImage.rectTransform.anchoredPosition = cheeseHandStartAnchored;
        Hide(cheeseFallingImage);

        SetSprite(movingBreadImage, breadSlideSprite); Show(movingBreadImage);
        if (cheesePileImage) { Hide(cheesePileImage); cheesePileImage.sprite = null; cheesePileImage.rectTransform.anchoredPosition = Vector2.zero; }
        if (topBreadImage) { Hide(topBreadImage); SetSprite(topBreadImage, breadSlideSprite); }

        uiManager?.UpdateSteps("Sign 'Dance' when the bread is under the grater.");
        Debug.Log("[Stacking] Restarted. Waiting for 'dance'...");
    }
}
