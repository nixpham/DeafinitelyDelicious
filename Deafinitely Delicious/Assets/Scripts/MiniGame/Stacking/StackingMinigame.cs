using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;

public class StackingMinigame : MonoBehaviour
{
    [SerializeField] private Image graterImage;
    [SerializeField] private Image movingBreadImage;
    [SerializeField] private Image topBreadImage;

    [SerializeField] private Sprite[] graterStates;             // length 5 (full -> empty)
    [SerializeField] private Sprite[] bottomBreadStates;        // length 3 (0/1/2 cheese)
    [SerializeField] private Sprite topBreadSprite;
    [SerializeField] private Sprite completedSandwichSprite;

    [SerializeField] private RectTransform movingBreadRect;
    [SerializeField] private RectTransform graterRect;
    [SerializeField] private RectTransform leftBound;
    [SerializeField] private RectTransform rightBound;
    [SerializeField] private float slideSpeed = 1.2f;

    [SerializeField] private float grateTolerance = 60f;        // alignment window for "Cheese"
    [SerializeField] private float dropTolerance  = 110f;       // wider window for "Drop"

    [SerializeField] private UIManager uiManager;
    [SerializeField] private MinigameManager minigameManager;

    public SimpleExecutionEngine engine;

    private bool init;
    private int frame;
    private int attempts;               // max 4
    private int successes;              // need 2
    private enum Phase { Grating, DropPrompt, Dropping, Done }
    private Phase phase = Phase.Grating;

    private readonly List<string> levelSigns = new() { "cheese", "drop" };

    void Start()
    {
        attempts = 0;
        successes = 0;
        phase = Phase.Grating;

        topBreadImage.enabled   = false;
        topBreadImage.sprite    = topBreadSprite;

        if (graterStates is { Length: > 0 }) graterImage.sprite = graterStates[0];
        if (bottomBreadStates is { Length: > 0 }) movingBreadImage.sprite = bottomBreadStates[0];

        uiManager?.UpdateSteps("Sign 'Cheese' when the bread is under the grater.");
    }

    void Update()
    {
        if (!init)
        {
            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
            init = true;
        }

        if (frame == 200) { frame = 0; engine.buffer.TriggerCallbacks(); } else frame++;

        SlideBread();
    }

    void OnSignRecognized(string sign)
    {
        var s = sign.ToLowerInvariant();

        if (phase == Phase.Grating && s == "cheese")
        {
            HandleCheeseAttempt();
            return;
        }

        if ((phase == Phase.DropPrompt || phase == Phase.Dropping) && s == "drop")
        {
            HandleDropAttempt();
            return;
        }
    }

    void HandleCheeseAttempt()
    {
        attempts = Mathf.Min(attempts + 1, 4);
        UpdateGraterSprite();

        if (IsAligned(movingBreadRect, graterRect, grateTolerance))
        {
            successes = Mathf.Min(successes + 1, 2);
            UpdateBottomBreadSprite();
        }

        if (successes >= 2)
        {
            phase = Phase.DropPrompt;
            graterImage.enabled = false;

            topBreadImage.enabled = true;  // show top slice ready to drop
            uiManager?.UpdateSteps("Sign 'Drop' to place the top slice.");
        }
        else if (attempts >= 4)
        {
            RestartMinigame();
        }
        else
        {
            uiManager?.UpdateSteps($"Sign 'Cheese' when the bread is under the grater. ({successes}/2)");
        }
    }

    void HandleDropAttempt()
    {
        phase = Phase.Dropping;

        bool ok = IsAligned(movingBreadRect, topBreadImage.rectTransform, dropTolerance);
        if (ok)
        {
            if (completedSandwichSprite) movingBreadImage.sprite = completedSandwichSprite;
            topBreadImage.enabled = false;

            uiManager?.UpdateSteps("Nice! Sandwich stacked.");
            EndMinigame();
        }
        else
        {
            RestartMinigame();
        }
    }

    void SlideBread()
    {
        if (!movingBreadRect || !leftBound || !rightBound) return;

        float t = Mathf.PingPong(Time.time * slideSpeed, 1f);
        Vector3 left  = leftBound.position;
        Vector3 right = rightBound.position;

        movingBreadRect.position = Vector3.Lerp(left, right, t);
    }

    bool IsAligned(RectTransform a, RectTransform b, float tolerancePx)
    {
        if (!a || !b) return false;
        float dx = Mathf.Abs(a.position.x - b.position.x);
        return dx <= tolerancePx;
    }

    void UpdateGraterSprite()
    {
        if (graterStates == null || graterStates.Length == 0) return;
        int idx = Mathf.Clamp(attempts, 0, graterStates.Length - 1);
        graterImage.sprite = graterStates[idx];
    }

    void UpdateBottomBreadSprite()
    {
        if (bottomBreadStates == null || bottomBreadStates.Length == 0) return;
        int idx = Mathf.Clamp(successes, 0, bottomBreadStates.Length - 1);
        movingBreadImage.sprite = bottomBreadStates[idx];
    }

    void EndMinigame()
    {
        phase = Phase.Done;
        minigameManager?.CloseMinigame();
    }

    void RestartMinigame()
    {
        attempts = 0;
        successes = 0;
        phase = Phase.Grating;

        if (graterStates is { Length: > 0 }) graterImage.sprite = graterStates[0];
        if (bottomBreadStates is { Length: > 0 }) movingBreadImage.sprite = bottomBreadStates[0];

        graterImage.enabled   = true;
        topBreadImage.enabled = false;

        uiManager?.UpdateSteps("Sign 'Cheese' when the bread is under the grater.");
    }
}