using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private float flipHeight = 200f;
    [SerializeField] private float panFlipHeight = 40f;
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private float topBreadYOffset = 15f;

    [Header("Cooking Settings")]
    [SerializeField] private float cookRatePerSecond = 0.05f;
    [SerializeField] private float goodMin = 0.4f;
    [SerializeField] private float goodMax = 0.75f;
    [SerializeField] private float burnThreshold = 1.0f;

    [Header("Managers")]
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private StudySessionPopup studyPopup;

    [Header("Instructions")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;

    [Header("Recognizer")]
    public SimpleExecutionEngine engine;

    private bool recognizerInitialized;
    private bool actionLocked;
    private bool waitingForStudyToClose;
    private bool hasFlippedOnce;
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

    private readonly List<string> recognizerSigns = new() { "dance", "cut" };
    private readonly string[] studySigns = { "dance", "cut" };

    private void Awake()
    {
        if (engine == null && PersistentSignEngine.Instance != null)
            engine = PersistentSignEngine.Instance.Engine;

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

        if (minigameManager == null)
            minigameManager = FindObjectOfType<MinigameManager>();

        ForceIdleState();
    }

    private void Start()
    {
        if (engine == null)
            Debug.LogError("[Flip] Engine is NOT assigned in inspector.");
        else
            Debug.Log("[Flip] Engine FOUND: " + engine.name);

        Debug.Log("[Flip] Start ran. Phase = " + phase);
    }

    private void Update()
    {
        InitRecognizerIfNeeded();

        if (phase == Phase.Study && waitingForStudyToClose)
        {
            bool popupStillOpen = studyPopup != null
                && studyPopup.popupRoot != null
                && studyPopup.popupRoot.activeSelf;

            if (!popupStillOpen)
            {
                Debug.Log("[Flip] Study popup closed -> starting gameplay");
                waitingForStudyToClose = false;
                BeginGameplay();
                return;
            }
        }

        if (Input.anyKeyDown)
            Debug.Log("[Flip] A key was pressed. Current phase = " + phase);

        if (engine == null)
        {
            Debug.LogWarning("[Flip] Engine is NULL");
        }
        else
        {
            if (engine.recognizer == null)
                Debug.LogWarning("[Flip] Recognizer is NULL");

            if (engine.buffer == null)
                Debug.LogWarning("[Flip] Buffer is NULL");
        }

        if (phase != Phase.Inactive && phase != Phase.Success)
        {
            if (engine != null && engine.buffer != null)
            {
                if (frame >= 200)
                {
                    frame = 0;
                    Debug.Log("[Flip] Triggering callbacks | phase = " + phase);
                    engine.buffer.TriggerCallbacks();
                }
                else
                {
                    frame++;
                }
            }
        }

        if ((Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Alpha2))
            && phase == Phase.Cooking && !actionLocked)
        {
            Debug.Log("[Flip] HOTKEY: Simulate DANCE / Flip");
            StartCoroutine(FlipSandwichFX());
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha3))
            && phase == Phase.Cooking && !actionLocked)
        {
            Debug.Log("[Flip] HOTKEY: Simulate CUT");
            HandleCut();
        }

        if (phase != Phase.Cooking)
            return;

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

    private void SetInstruction(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
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
        Debug.Log("[Flip] Callback successfully registered to recognizer.");
    }

    public void OnOpenedByManager()
    {
        Debug.Log("[Flip] Minigame OPENED");
        FullReset();
    }

    public void OnRedoPressed()
    {
        Debug.Log("[Flip] Minigame REDO");
        FullReset();
    }

    public void OnNextPressed()
    {
        Debug.Log("[Flip] Minigame NEXT pressed");
    }

    private void FullReset()
    {
        StopAllCoroutines();

        frame = 0;
        actionLocked = false;
        waitingForStudyToClose = false;

        ForceIdleState();
        phase = Phase.Study;

        Debug.Log("[Flip] Reset -> Phase = STUDY");
        OpenStudySession();
    }

    private void OpenStudySession()
    {
        Debug.Log("[Flip] Opening study session");

        if (studyPopup == null)
        {
            Debug.LogWarning("[Flip] Study popup missing. Starting gameplay immediately.");
            BeginGameplay();
            return;
        }

        studyPopup.OpenSession(studySigns);
        waitingForStudyToClose = true;
        SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");
    }

    private void ForceIdleState()
    {
        bottomCook = 0f;
        topCook = 0f;
        sideDown = Side.Bottom;
        isCooking = false;
        actionLocked = false;
        hasFlippedOnce = false;
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
            sandwichGroup.localScale = Vector3.one;
        }

        if (topBreadImage != null)
        {
            topBreadImage.transform.SetSiblingIndex(1);
            RectTransform rt = topBreadImage.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, topBreadYOffset);
        }

        if (bottomBreadImage != null)
        {
            bottomBreadImage.transform.SetSiblingIndex(0);
            RectTransform rt = bottomBreadImage.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        }

        UpdateBreadSprites();

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

        Debug.Log("[Flip] Forced idle state.");
    }

    private void BeginGameplay()
    {
        StopAllCoroutines();
        Debug.Log("[Flip] BeginGameplay -> starting intro sequence");
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        phase = Phase.Intro;
        isCooking = false;
        actionLocked = true;

        Debug.Log("[Flip] Phase -> INTRO");
        SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");

        if (sandwichGroup != null)
            sandwichGroup.anchoredPosition = sandwichStartPos;

        yield return new WaitForSeconds(butterMeltDelay);

        if (butterImage != null && butterMeltedSprite != null)
        {
            butterImage.sprite = butterMeltedSprite;
            Debug.Log("[Flip] Butter melted");
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
        actionLocked = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(GameAudioPaths.FlippingCooking, 0.6f);

        Debug.Log("[Flip] Phase -> COOKING");
        SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");
    }

    private void OnSignRecognized(string raw)
    {
        Debug.Log("[Flip] Callback fired. Raw input = " + raw + " | phase = " + phase);

        if (phase == Phase.Success || phase == Phase.Inactive)
            return;

        if (string.IsNullOrEmpty(raw))
            return;

        if (phase == Phase.Study)
        {
            Debug.Log("[Flip] Ignored recognizer input during study.");
            return;
        }

        if (studyPopup != null && studyPopup.popupRoot != null && studyPopup.popupRoot.activeSelf)
        {
            Debug.Log("[Flip] Ignored (study popup open)");
            return;
        }

        if (phase != Phase.Cooking)
            return;

        string sign = raw.ToLowerInvariant();
        Debug.Log("[Flip] SIGN DETECTED: " + sign);

        if (sign == "dance" && !actionLocked)
        {
            StartCoroutine(FlipSandwichFX());
            return;
        }

        if (sign == "cut" && !actionLocked)
        {
            HandleCut();
        }
    }

    private IEnumerator FlipSandwichFX()
    {
        if (sandwichGroup == null)
            yield break;

        actionLocked = true;

        Vector2 startPos = sandwichGroup.anchoredPosition;
        Vector2 peakPos = startPos + new Vector2(0f, flipHeight);

        Vector2 panStart = panBasePos;
        Vector2 panPeak = panStart + new Vector2(0f, panFlipHeight);

        float halfDuration = Mathf.Max(0.01f, flipDuration * 0.5f);
        float t = 0f;
        bool previousCooking = isCooking;
        isCooking = false;

        Debug.Log("[Flip] Starting flip animation");

        if (panImage != null && panFlipSprite != null)
            panImage.sprite = panFlipSprite;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / halfDuration);

            sandwichGroup.anchoredPosition = Vector2.Lerp(startPos, peakPos, u);

            if (panRect != null)
                panRect.anchoredPosition = Vector2.Lerp(panStart, panPeak, u);

            sandwichGroup.localScale = Vector3.Lerp(
                Vector3.one,
                new Vector3(0.92f, 1.08f, 1f),
                u
            );

            yield return null;
        }

        Side cookedSide = sideDown;
        sideDown = sideDown == Side.Bottom ? Side.Top : Side.Bottom;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(GameAudioPaths.FlippingFlip, 0.85f);

        hasFlippedOnce = true;

        UpdateBreadSprites();
        SetVisibleTopSide(cookedSide);
        UpdateBreadSprites();

        float cookedSideValue = cookedSide == Side.Bottom ? bottomCook : topCook;

        Debug.Log("[Flip] Sandwich flipped. Visible top = " + cookedSide
            + " | revealed cook value = " + cookedSideValue.ToString("F2")
            + " | now cooking bottom = " + sideDown);

        if (cookedSideValue >= burnThreshold)
        {
            Debug.Log("[Flip] Newly revealed top side is burnt -> restarting");

            sandwichGroup.localScale = Vector3.one;
            sandwichGroup.anchoredPosition = peakPos;

            t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / halfDuration);

                sandwichGroup.anchoredPosition = Vector2.Lerp(peakPos, startPos, u);

                if (panRect != null)
                    panRect.anchoredPosition = Vector2.Lerp(panPeak, panStart, u);

                sandwichGroup.localScale = Vector3.Lerp(
                    new Vector3(0.92f, 1.08f, 1f),
                    Vector3.one,
                    u
                );

                yield return null;
            }

            sandwichGroup.anchoredPosition = startPos;
            sandwichGroup.localScale = Vector3.one;

            if (panRect != null)
                panRect.anchoredPosition = panBasePos;

            if (panImage != null && panIdleSprite != null)
                panImage.sprite = panIdleSprite;

            SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");
            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(false);

            yield return new WaitForSeconds(0.35f);
            RestartGameplayOnly();
            yield break;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / halfDuration);

            sandwichGroup.anchoredPosition = Vector2.Lerp(peakPos, startPos, u);

            if (panRect != null)
                panRect.anchoredPosition = Vector2.Lerp(panPeak, panStart, u);

            sandwichGroup.localScale = Vector3.Lerp(
                new Vector3(0.92f, 1.08f, 1f),
                Vector3.one,
                u
            );

            yield return null;
        }

        sandwichGroup.anchoredPosition = startPos;
        sandwichGroup.localScale = Vector3.one;

        if (panRect != null)
            panRect.anchoredPosition = panBasePos;

        if (panImage != null && panIdleSprite != null)
            panImage.sprite = panIdleSprite;

        isCooking = previousCooking;
        actionLocked = false;

        if (hasFlippedOnce)
            SetInstruction("Sign \"Cut\" once the grilled cheese is cooked to serve it!");
        else
            SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");

        UpdateCookVisuals();
    }

    private void HandleCut()
    {
        if (phase != Phase.Cooking)
            return;

        actionLocked = true;
        isCooking = false;

        bool bottomGood = IsGood(bottomCook);
        bool topGood = IsGood(topCook);
        bool bottomBurnt = bottomCook >= burnThreshold;
        bool topBurnt = topCook >= burnThreshold;

        bool anyBurnt = bottomBurnt || topBurnt;
        bool bothGood = bottomGood && topGood && !anyBurnt;

        UpdateBreadSprites();

        Debug.Log("[Flip] CUT pressed | bottomCook = " + bottomCook.ToString("F2")
            + " | topCook = " + topCook.ToString("F2")
            + " | bottomGood = " + bottomGood
            + " | topGood = " + topGood
            + " | anyBurnt = " + anyBurnt);

        if (bothGood)
        {

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(GameAudioPaths.UiGoodAction, 0.9f);

            phase = Phase.Success;

            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(true);

            Debug.Log("[Flip] SUCCESS STATE REACHED");
            SetInstruction("Success!");
            minigameManager?.ShowSuccessPopup("Success");
            actionLocked = false;
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(GameAudioPaths.UiWrongAction, 0.9f);

            if (attemptsUI != null)
                attemptsUI.RegisterAttempt(false);

            if (anyBurnt)
            {
                Debug.Log("[Flip] Failed: sandwich burnt");
                SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");
            }
            else
            {
                Debug.Log("[Flip] Failed: both sides were not ready");
                SetInstruction("Sign \"Dance\" to flip, be careful not to burn it!");
            }

            StartCoroutine(RestartAfterDelay(0.8f));
        }
    }

    private void SetVisibleTopSide(Side topSide)
    {
        if (topBreadImage == null || bottomBreadImage == null)
            return;

        RectTransform topRT = topBreadImage.rectTransform;
        RectTransform bottomRT = bottomBreadImage.rectTransform;

        if (topSide == Side.Top)
        {
            topBreadImage.transform.SetSiblingIndex(1);
            bottomBreadImage.transform.SetSiblingIndex(0);

            topRT.anchoredPosition = new Vector2(topRT.anchoredPosition.x, topBreadYOffset);
            bottomRT.anchoredPosition = new Vector2(bottomRT.anchoredPosition.x, 0f);
        }
        else
        {
            bottomBreadImage.transform.SetSiblingIndex(1);
            topBreadImage.transform.SetSiblingIndex(0);

            bottomRT.anchoredPosition = new Vector2(bottomRT.anchoredPosition.x, topBreadYOffset);
            topRT.anchoredPosition = new Vector2(topRT.anchoredPosition.x, 0f);
        }

        Debug.Log("[Flip] Visible top side set to: " + topSide);
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
            if (currentCook > goodMax)
                cookFillImage.color = RedColor();
            else if (currentCook >= goodMin)
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
        if (cookValue > goodMax && breadBurntSprite != null)
            return breadBurntSprite;

        if (cookValue >= goodMin && breadGoodSprite != null)
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
        Debug.Log("[Flip] RestartGameplayOnly called.");

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