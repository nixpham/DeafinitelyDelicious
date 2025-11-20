using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class FlippingMinigame : MonoBehaviour
{
    [Header("Pan / Sandwich UI")]
    [SerializeField] private Image panImage;
    [SerializeField] private Image fireImage;             // optional visual
    [SerializeField] private RectTransform sandwichGroup; // parent group that flips
    [SerializeField] private Image topBreadImage;         // top slice
    [SerializeField] private Image bottomBreadImage;      // bottom slice

    [Header("Cooking Slider")]
    [SerializeField] private Slider cookSlider;           // 0–1 slider
    [SerializeField] private Image cookFillImage;         // Fill child of the slider

    [Header("Bread Sprites")]
    [SerializeField] private Sprite breadRawSprite;       // Bread 1.png
    [SerializeField] private Sprite breadGoodSprite;      // Bread 2 (good).png
    [SerializeField] private Sprite breadBurntSprite;     // Bread 3.png

    [Header("Pan Sprites")]
    [SerializeField] private Sprite panIdleSprite;        // normal pan sprite
    [SerializeField] private Sprite panFlipSprite;        // “action” pan sprite

    [Header("Butter Setup")]
    [SerializeField] private Image butterImage;           // Butter image on pan
    [SerializeField] private Sprite butterSolidSprite;    // Butter 1.png
    [SerializeField] private Sprite butterMeltedSprite;   // Butter 2.png
    [SerializeField] private float butterMeltDelay = 0.8f;
    [SerializeField] private float butterMeltDuration = 0.8f;
    [SerializeField] private float breadDropDelay = 0.4f;
    [SerializeField] private float breadDropDuration = 0.4f;

    [Header("Flip Animation")]
    [SerializeField] private float flipHeight = 150f;     // how high the sandwich jumps
    [SerializeField] private float panFlipHeight = 40f;   // how much the pan nudges up
    [SerializeField] private float flipDuration = 0.35f;  // seconds per flip anim

    [Header("Cooking Settings")]
    [Tooltip("How fast the side on the pan cooks per second (0–1 scale).")]
    [SerializeField] private float cookRatePerSecond = 0.05f;

    [Tooltip("Minimum cooked value for a 'good' side (0–1).")]
    [SerializeField] private float goodMin = 0.4f;

    [Tooltip("Maximum cooked value for a 'good' side (0–1).")]
    [SerializeField] private float goodMax = 0.75f;

    [Tooltip("At or above this value, a side is considered burnt (0–1).")]
    [SerializeField] private float burnThreshold = 1.0f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    // --- internal state ---
    private bool initRecognizer;
    private int frame;

    private enum Side { Bottom, Top }
    private Side sideDown = Side.Bottom;

    private float bottomCook;    // 0–1
    private float topCook;       // 0–1

    private bool isCooking;      // timer active?
    private bool minigameOver;   // we already resolved success/fail
    private bool introDone;      // butter → melt → drop finished

    private RectTransform panRect;
    private Vector2 sandwichFinalPos;
    private Vector2 sandwichStartPos;
    private Vector2 panBasePos;

    private readonly List<string> levelSigns = new() { "dance", "cut" };

    void Start()
    {
        if (panImage) panRect = panImage.rectTransform;

        if (sandwichGroup)
        {
            sandwichFinalPos = sandwichGroup.anchoredPosition;
            sandwichStartPos = sandwichFinalPos + new Vector2(0f, 200f);
        }
        else
        {
            sandwichFinalPos = Vector2.zero;
            sandwichStartPos = Vector2.zero;
        }

        panBasePos = panRect ? panRect.anchoredPosition : Vector2.zero;

        // Set up slider defaults
        if (cookSlider != null)
        {
            cookSlider.minValue = 0f;
            cookSlider.maxValue = 1f;
            cookSlider.value = 0f;
            cookSlider.interactable = false;
        }

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        SetupCoreVisuals();
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // recognizer init
        if (!initRecognizer)
        {
            if (engine == null)
            {
                Debug.LogError("[Flipping] Engine not assigned.");
                return;
            }

            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
            initRecognizer = true;
            Debug.Log("[Flipping] Recognizer initialized (dance, cut).");
        }

        // recognizer tick
        if (frame == 200)
        {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }
        else frame++;

        // cooking
        if (isCooking && introDone && !minigameOver)
        {
            float delta = Time.deltaTime * cookRatePerSecond;

            if (sideDown == Side.Bottom)
                bottomCook += delta;
            else
                topCook += delta;

            ClampCookLevels();
            UpdateCookVisuals();
        }
    }

    // ---------- setup & intro ----------

    private void SetupCoreVisuals()
    {
        bottomCook = 0f;
        topCook = 0f;
        sideDown = Side.Bottom;
        isCooking = false;
        minigameOver = false;
        introDone = false;

        // pan
        if (panImage && panIdleSprite) panImage.sprite = panIdleSprite;
        if (panRect) panRect.anchoredPosition = panBasePos;

        // sandwich
        if (sandwichGroup)
        {
            sandwichGroup.gameObject.SetActive(false);
            sandwichGroup.anchoredPosition = sandwichFinalPos;
            sandwichGroup.localEulerAngles = Vector3.zero;
        }

        if (topBreadImage && breadRawSprite) topBreadImage.sprite = breadRawSprite;
        if (bottomBreadImage && breadRawSprite) bottomBreadImage.sprite = breadRawSprite;

        // butter
        if (butterImage)
        {
            butterImage.gameObject.SetActive(true);
            if (butterSolidSprite) butterImage.sprite = butterSolidSprite;
        }

        // slider
        if (cookSlider != null)
            cookSlider.value = 0f;
        if (cookFillImage != null)
            cookFillImage.color = YellowColor();
    }

    private IEnumerator IntroSequence()
    {
        SetupCoreVisuals();

        uiManager?.UpdateSteps("Butter is melting...");

        if (sandwichGroup) sandwichGroup.anchoredPosition = sandwichStartPos;

        // solid butter delay
        yield return new WaitForSeconds(butterMeltDelay);

        // melted butter
        if (butterImage && butterMeltedSprite)
        {
            butterImage.sprite = butterMeltedSprite;
            yield return new WaitForSeconds(butterMeltDuration);
        }

        // hide butter
        if (butterImage)
            butterImage.gameObject.SetActive(false);

        // pause then drop sandwich
        yield return new WaitForSeconds(breadDropDelay);

        if (sandwichGroup)
        {
            sandwichGroup.gameObject.SetActive(true);
            sandwichGroup.anchoredPosition = sandwichStartPos;

            float t = 0f;
            while (t < breadDropDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / breadDropDuration);
                sandwichGroup.anchoredPosition = Vector2.Lerp(sandwichStartPos, sandwichFinalPos, u);
                yield return null;
            }
            sandwichGroup.anchoredPosition = sandwichFinalPos;
        }

        // start cooking
        introDone = true;
        isCooking = true;
        uiManager?.UpdateSteps("Sandwich is cooking! Flip with 'DANCE'. When BOTH sides have been cooked while the slider is GREEN, sign 'CUT'.");
        Debug.Log("[Flipping] Intro finished. Cooking started on bottom side.");
    }

    // ---------- input / recognizer ----------

    private void OnSignRecognized(string raw)
    {
        if (!introDone || minigameOver) return;

        string s = raw.ToLowerInvariant();
        Debug.Log("[Flipping] Recognized sign: " + s);

        if (s == "dance")
        {
            if (isCooking)
                StartCoroutine(FlipSandwichFX());
            return;
        }

        if (s == "cut")
        {
            HandleCut();
            return;
        }
    }

    // ---------- flip animation ----------

    private IEnumerator FlipSandwichFX()
    {
        if (!sandwichGroup)
            yield break;

        Vector2 startPos = sandwichGroup.anchoredPosition;
        Vector2 peakPos = startPos + new Vector2(0f, flipHeight);

        Vector2 panStart = panBasePos;
        Vector2 panPeak = panStart + new Vector2(0f, panFlipHeight);

        float t = 0f;
        bool prevCooking = isCooking;
        isCooking = false;

        // --- UP PHASE ---
        while (t < flipDuration * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (flipDuration * 0.5f);

            // sandwich goes up
            sandwichGroup.anchoredPosition = Vector2.Lerp(startPos, peakPos, u);

            // pan bumps up
            if (panRect)
                panRect.anchoredPosition = Vector2.Lerp(panStart, panPeak, u);

            yield return null;
        }

        // ---- INSTANT SPIN AT PEAK ----
        // visually fast spin → 180° flip
        sandwichGroup.localEulerAngles = new Vector3(0f, 0f, 180f);

        // swap side logic
        sideDown = (sideDown == Side.Bottom) ? Side.Top : Side.Bottom;

        // === IMPORTANT ===
        // Update sprites NOW so when it lands, it shows the other cooked side
        UpdateBreadSprites();

        // --- DOWN PHASE ---
        t = 0f;
        while (t < flipDuration * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (flipDuration * 0.5f);

            sandwichGroup.anchoredPosition = Vector2.Lerp(peakPos, startPos, u);

            if (panRect)
                panRect.anchoredPosition = Vector2.Lerp(panPeak, panStart, u);

            yield return null;
        }

        // SNAP rotation back to clean state
        sandwichGroup.localEulerAngles = Vector3.zero;
        sandwichGroup.anchoredPosition = startPos;
        if (panRect) panRect.anchoredPosition = panBasePos;

        isCooking = prevCooking;

        string sideName = (sideDown == Side.Bottom) ? "bottom" : "top";
        uiManager?.UpdateSteps($"Now cooking the {sideName} side. Flip with 'DANCE' when the slider turns green.");
        UpdateCookVisuals();
    }

    // ---------- evaluation (CUT) ----------

    private void HandleCut()
    {
        if (!introDone || minigameOver) return;

        isCooking = false;
        minigameOver = true;

        bool bottomGood = IsGood(bottomCook);
        bool topGood = IsGood(topCook);

        bool bottomBurnt = bottomCook >= burnThreshold;
        bool topBurnt = topCook >= burnThreshold;

        bool anyBurnt = bottomBurnt || topBurnt;
        bool bothGood = bottomGood && topGood && !anyBurnt;

        UpdateBreadSprites(); // show final state

        if (bothGood)
        {
            uiManager?.UpdateSteps("Nice! Both sides were green and perfectly toasted.");
            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(true);

            Debug.Log("[Flipping] Success – both sides cooked.");
            minigameManager?.CloseMinigame();
        }
        else
        {
            string msg;
            if (anyBurnt)
                msg = "The slider went past green into red on at least one side. That's burnt! Try again from the butter.";
            else
                msg = "You signed before both sides were green. At least one side is undercooked. Try again from the butter.";

            uiManager?.UpdateSteps(msg);

            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(false);

            Debug.Log("[Flipping] Fail – restarting from butter.");
            StartCoroutine(RestartAfterDelay(0.8f));
        }
    }

    private bool IsGood(float cookValue)
    {
        return cookValue >= goodMin && cookValue <= goodMax;
    }

    private void ClampCookLevels()
    {
        bottomCook = Mathf.Max(0f, bottomCook);
        topCook = Mathf.Max(0f, topCook);
    }

    // ---------- visual updates ----------

    private void UpdateCookVisuals()
    {
        float currentCook = (sideDown == Side.Bottom) ? bottomCook : topCook;

        if (cookSlider != null)
        {
            float normalized = Mathf.InverseLerp(0f, burnThreshold, currentCook);
            cookSlider.value = Mathf.Clamp01(normalized);
        }

        // color logic: yellow → green → red
        if (cookFillImage != null)
        {
            Color c;
            if (currentCook >= burnThreshold)
                c = RedColor();
            else if (currentCook >= goodMin && currentCook <= goodMax)
                c = GreenColor();
            else
                c = YellowColor();

            cookFillImage.color = c;
        }

        UpdateBreadSprites();
    }

    private void UpdateBreadSprites()
    {
        if (!breadRawSprite || !breadGoodSprite || !breadBurntSprite)
            return;

        if (bottomBreadImage)
            bottomBreadImage.sprite = SpriteForCook(bottomCook);

        if (topBreadImage)
            topBreadImage.sprite = SpriteForCook(topCook);
    }

    private Sprite SpriteForCook(float cookValue)
    {
        if (cookValue >= burnThreshold && breadBurntSprite)
            return breadBurntSprite;
        if (cookValue >= goodMin && cookValue <= goodMax && breadGoodSprite)
            return breadGoodSprite;
        return breadRawSprite;
    }

    // ---------- colors ----------

    private Color YellowColor()
    {
        // warm yellow
        return new Color(1f, 0.92f, 0.25f, 1f);
    }

    private Color GreenColor()
    {
        return new Color(0.2f, 0.8f, 0.3f, 1f);
    }

    private Color RedColor()
    {
        return new Color(0.9f, 0.2f, 0.2f, 1f);
    }

    // ---------- restart ----------

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartMinigame();
    }

    private void RestartMinigame()
    {
        StopAllCoroutines();
        SetupCoreVisuals();
        StartCoroutine(IntroSequence());  // butter → melt → drop again
    }
}