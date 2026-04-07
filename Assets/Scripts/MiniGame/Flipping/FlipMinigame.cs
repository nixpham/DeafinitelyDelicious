using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class FlipMinigame : MonoBehaviour
{
    private enum Phase
    {
        Inactive,
        Study,
        Intro,
        Cooking,
        Success
    }

    private enum Side
    {
        Bottom,
        Top
    }

    [Header("Pan / Sandwich UI")]
    [SerializeField] private Image panImage;
    [SerializeField] private Image fireImage;
    [SerializeField] private RectTransform sandwichGroup;
    [SerializeField] private Image topBreadImage;
    [SerializeField] private Image bottomBreadImage;

    [Header("Cooking Slider")]
    [SerializeField] private Slider cookSlider;
    [SerializeField] private Image cookFillImage;

    [Header("Bread Sprites")]
    [SerializeField] private Sprite breadRawSprite;
    [SerializeField] private Sprite breadGoodSprite;
    [SerializeField] private Sprite breadBurntSprite;

    [Header("Pan Sprites")]
    [SerializeField] private Sprite panIdleSprite;
    [SerializeField] private Sprite panFlipSprite;

    [Header("Butter Setup")]
    [SerializeField] private Image butterImage;
    [SerializeField] private Sprite butterSolidSprite;
    [SerializeField] private Sprite butterMeltedSprite;
    [SerializeField] private float butterMeltDelay = 0.8f;
    [SerializeField] private float butterMeltDuration = 0.8f;
    [SerializeField] private float breadDropDelay = 0.4f;
    [SerializeField] private float breadDropDuration = 0.4f;

    [Header("Flip Animation")]
    [SerializeField] private float flipHeight = 150f;
    [SerializeField] private float panFlipHeight = 40f;
    [SerializeField] private float flipDuration = 0.35f;

    [Header("Cooking Settings")]
    [SerializeField] private float cookRatePerSecond = 0.05f;
    [SerializeField] private float goodMin = 0.4f;
    [SerializeField] private float goodMax = 0.75f;
    [SerializeField] private float burnThreshold = 1.0f;

    [Header("Managers")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private StudySessionPopup studyPopup;

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    private bool recognizerInitialized;
    private int frame;

    private Side sideDown = Side.Bottom;
    private float bottomCook;
    private float topCook;
    private bool isCooking;

    private RectTransform panRect;
    private Vector2 sandwichFinalPos;
    private Vector2 sandwichStartPos;
    private Vector2 panBasePos;

    private Phase phase = Phase.Inactive;

    // flip uses dance + cut too
    private readonly List<string> recognizerSigns = new() { "dance", "cut" };
    private readonly string[] studySigns = { "dance", "cut" };

    private void Start()
    {
        if (panImage != null)
            panRect = panImage.rectTransform;

        if (sandwichGroup != null)
        {
            sandwichFinalPos = sandwichGroup.anchoredPosition;
            sandwichStartPos = sandwichFinalPos + new Vector2(0f, 200f);
        }
        else
        {
            sandwichFinalPos = Vector2.zero;
            sandwichStartPos = Vector2.zero;
        }

        panBasePos = panRect != null ? panRect.anchoredPosition : Vector2.zero;

        ForceIdleState();
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (phase != Phase.Cooking)
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

        if (isCooking)
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
        ForceIdleState();
        phase = Phase.Study;
        OpenStudySession();
    }

    private void OpenStudySession()
    {
        if (studyPopup == null)
        {
            Debug.LogWarning("[Flip] Study popup missing. Starting gameplay immediately.");
            BeginGameplay();
            return;
        }

        studyPopup.OpenSession(studySigns);
        uiManager?.UpdateSteps("Before we start cooking, let's first learn the signs associated!");
    }

    private void ForceIdleState()
    {
        bottomCook = 0f;
        topCook = 0f;
        sideDown = Side.Bottom;
        isCooking = false;
        phase = Phase.Inactive;

        if (panImage != null && panIdleSprite != null)
            panImage.sprite = panIdleSprite;

        if (panRect != null)
            panRect.anchoredPosition = panBasePos;

        if (sandwichGroup != null)
        {
            sandwichGroup.gameObject.SetActive(false);
            sandwichGroup.anchoredPosition = sandwichFinalPos;
            sandwichGroup.localEulerAngles = Vector3.zero;
        }

        if (topBreadImage != null && breadRawSprite != null)
            topBreadImage.sprite = breadRawSprite;

        if (bottomBreadImage != null && breadRawSprite != null)
            bottomBreadImage.sprite = breadRawSprite;

        if (butterImage != null)
        {
            butterImage.gameObject.SetActive(true);
            if (butterSolidSprite != null)
                butterImage.sprite = butterSolidSprite;
        }

        if (cookSlider != null)
        {
            cookSlider.minValue = 0f;
            cookSlider.maxValue = 1f;
            cookSlider.value = 0f;
            cookSlider.interactable = false;
        }

        if (cookFillImage != null)
            cookFillImage.color = YellowColor();

        if (attemptsUI != null)
            attemptsUI.ResetAttempts();
    }

    private void BeginGameplay()
    {
        StopAllCoroutines();
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        phase = Phase.Intro;
        isCooking = false;

        uiManager?.UpdateSteps("Butter is melting...");

        if (sandwichGroup != null)
            sandwichGroup.anchoredPosition = sandwichStartPos;

        yield return new WaitForSeconds(butterMeltDelay);

        if (butterImage != null && butterMeltedSprite != null)
        {
            butterImage.sprite = butterMeltedSprite;
            yield return new WaitForSeconds(butterMeltDuration);
        }

        if (butterImage != null)
            butterImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(breadDropDelay);

        if (sandwichGroup != null)
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

        phase = Phase.Cooking;
        isCooking = true;
        uiManager?.UpdateSteps("Sandwich is cooking! Sign 'Dance' to flip. When both sides are green, sign 'Cut'.");
    }

    private void OnSignRecognized(string raw)
    {
        if (phase == Phase.Success || phase == Phase.Inactive)
            return;

        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
            return;

        if (phase == Phase.Study)
        {
            BeginGameplay();
            return;
        }

        if (phase != Phase.Cooking)
            return;

        string sign = raw.ToLowerInvariant();

        if (sign == "dance")
        {
            if (isCooking)
                StartCoroutine(FlipSandwichFX());
            return;
        }

        if (sign == "cut")
        {
            HandleCut();
        }
    }

    private IEnumerator FlipSandwichFX()
    {
        if (sandwichGroup == null)
            yield break;

        Vector2 startPos = sandwichGroup.anchoredPosition;
        Vector2 peakPos = startPos + new Vector2(0f, flipHeight);

        Vector2 panStart = panBasePos;
        Vector2 panPeak = panStart + new Vector2(0f, panFlipHeight);

        float t = 0f;
        bool previousCooking = isCooking;
        isCooking = false;

        if (panImage != null && panFlipSprite != null)
            panImage.sprite = panFlipSprite;

        while (t < flipDuration * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (flipDuration * 0.5f);

            sandwichGroup.anchoredPosition = Vector2.Lerp(startPos, peakPos, u);

            if (panRect != null)
                panRect.anchoredPosition = Vector2.Lerp(panStart, panPeak, u);

            yield return null;
        }

        sandwichGroup.localEulerAngles = new Vector3(0f, 0f, 180f);
        sideDown = sideDown == Side.Bottom ? Side.Top : Side.Bottom;
        UpdateBreadSprites();

        t = 0f;
        while (t < flipDuration * 0.5f)
        {
            t += Time.deltaTime;
            float u = t / (flipDuration * 0.5f);

            sandwichGroup.anchoredPosition = Vector2.Lerp(peakPos, startPos, u);

            if (panRect != null)
                panRect.anchoredPosition = Vector2.Lerp(panPeak, panStart, u);

            yield return null;
        }

        sandwichGroup.localEulerAngles = Vector3.zero;
        sandwichGroup.anchoredPosition = startPos;

        if (panRect != null)
            panRect.anchoredPosition = panBasePos;

        if (panImage != null && panIdleSprite != null)
            panImage.sprite = panIdleSprite;

        isCooking = previousCooking;

        string sideName = sideDown == Side.Bottom ? "bottom" : "top";
        uiManager?.UpdateSteps($"Now cooking the {sideName} side. Sign 'Dance' to flip. Sign 'Cut' when both sides are green.");
        UpdateCookVisuals();
    }

    private void HandleCut()
    {
        if (phase != Phase.Cooking)
            return;

        isCooking = false;

        bool bottomGood = IsGood(bottomCook);
        bool topGood = IsGood(topCook);
        bool bottomBurnt = bottomCook >= burnThreshold;
        bool topBurnt = topCook >= burnThreshold;

        bool anyBurnt = bottomBurnt || topBurnt;
        bool bothGood = bottomGood && topGood && !anyBurnt;

        UpdateBreadSprites();

        if (bothGood)
        {
            phase = Phase.Success;

            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(true);

            uiManager?.UpdateSteps("Success!");
            minigameManager?.ShowSuccessPopup("Success");
        }
        else
        {
            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(false);

            if (anyBurnt)
                uiManager?.UpdateSteps("The sandwich got burnt. Restarting from the butter.");
            else
                uiManager?.UpdateSteps("Both sides were not green yet. Restarting from the butter.");

            StartCoroutine(RestartAfterDelay(0.8f));
        }
    }

    private bool IsGood(float value)
    {
        return value >= goodMin && value <= goodMax;
    }

    private void ClampCookLevels()
    {
        bottomCook = Mathf.Max(0f, bottomCook);
        topCook = Mathf.Max(0f, topCook);
    }

    private void UpdateCookVisuals()
    {
        float currentCook = sideDown == Side.Bottom ? bottomCook : topCook;

        if (cookSlider != null)
        {
            float normalized = Mathf.InverseLerp(0f, burnThreshold, currentCook);
            cookSlider.value = Mathf.Clamp01(normalized);
        }

        if (cookFillImage != null)
        {
            if (currentCook >= burnThreshold)
                cookFillImage.color = RedColor();
            else if (currentCook >= goodMin && currentCook <= goodMax)
                cookFillImage.color = GreenColor();
            else
                cookFillImage.color = YellowColor();
        }

        UpdateBreadSprites();
    }

    private void UpdateBreadSprites()
    {
        if (bottomBreadImage != null)
            bottomBreadImage.sprite = SpriteForCook(bottomCook);

        if (topBreadImage != null)
            topBreadImage.sprite = SpriteForCook(topCook);
    }

    private Sprite SpriteForCook(float cookValue)
    {
        if (cookValue >= burnThreshold && breadBurntSprite != null)
            return breadBurntSprite;

        if (cookValue >= goodMin && cookValue <= goodMax && breadGoodSprite != null)
            return breadGoodSprite;

        return breadRawSprite;
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartGameplayOnly();
    }

    private void RestartGameplayOnly()
    {
        StopAllCoroutines();
        ForceIdleState();
        BeginGameplay();
    }

    private Color YellowColor()
    {
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
}