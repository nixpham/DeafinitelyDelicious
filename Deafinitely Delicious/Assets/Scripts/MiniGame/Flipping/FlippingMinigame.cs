using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using System; 
using Common;

public class FlippingMinigame : MonoBehaviour
{
    public GameObject pan;
    public Image breadImage;
    public Sprite[] breadSprites;
    public CookingIndicator cookingIndicator;

    private MinigameManager minigameManager;
    public UIManager uiManager;

    public SimpleExecutionEngine engine;
    private bool init;
    private int frame = 0;

    private List<string> levelSigns = new List<string>
    {
        "dance",
        "finish"
    };

    private float cookingTime = 0f;
    private int currentSide = 0;
    private CookingState[] sideStates = new CookingState[2];

    private float undercookedThreshold = 3f;
    private float perfectMin = 4f;
    private float perfectMax = 7f;
    private float burntThreshold = 9f;

    private bool isFlipped = false;

    private float lastSignTime = -999f;
    private float signCooldown = 2f;
    private string lastRecognizedSign = "";
    private int successfulFlips = 0;

    private enum CookingState
    {
        Raw,
        Undercooked,
        Perfect,
        Burnt
    }

    void Start()
    {
        minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not found in the scene.");
        }

        sideStates[0] = CookingState.Raw;
        sideStates[1] = CookingState.Raw;

        breadImage.sprite = breadSprites[0];
        uiManager.UpdateSteps("Sign 'dance' when the bread is perfectly golden!");
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

        cookingTime += Time.deltaTime;

        // Update the indicator with current cooking time
        if (cookingIndicator != null)
        {
            cookingIndicator.UpdateIndicator(cookingTime);
        }

        if (frame == 200)
        {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }
        else frame++;
    }

    private void OnSignRecognized(string sign)
    {
        // ⭐ CHECK COOLDOWN BEFORE PROCESSING
        float timeSinceLastSign = Time.time - lastSignTime;
        
        // Ignore if same sign recognized too quickly
        if (sign == lastRecognizedSign && timeSinceLastSign < signCooldown)
        {
            Debug.Log($"Sign '{sign}' ignored - cooldown active ({timeSinceLastSign:F1}s since last)");
            return;
        }

        Debug.Log($"Recognized sign: {sign} (time since last: {timeSinceLastSign:F1}s)");

        // Update tracking variables
        lastSignTime = Time.time;
        lastRecognizedSign = sign;

        // Process the sign
        if (sign.ToLower() == "dance")
        {
            FlipBread();
        }
        else if (sign.ToLower() == "finish")
        {
            CheckCompletion();
        }
    }

    private void FlipBread()
    {
        // Check the current cooking time at the moment they sign
        CookingState state = DetermineCookingState(cookingTime);
        sideStates[currentSide] = state;

        Debug.Log($"Side {currentSide + 1} flip attempt: {state} (Time: {cookingTime:F2}s)");

        if (state == CookingState.Burnt)
        {
            uiManager.UpdateSteps("Burnt! Try again.");
            RestartMinigame();
            return;
        }

        if (state == CookingState.Undercooked)
        {
            uiManager.UpdateSteps("Undercooked! Keep cooking this side.");
            // Don't restart - let them continue cooking
            return;
        }

        // Between perfect and burnt (the "danger zone")
        if (state == CookingState.Perfect)
        {
            successfulFlips++;  // ⭐ INCREMENT COUNTER
            
            Debug.Log($"✓ SUCCESSFUL FLIP! Total successful flips: {successfulFlips}/2");  // ⭐ ADD THIS
            
            isFlipped = true;
            currentSide = 1;
            cookingTime = 0f;

            UpdateBreadSprite();
            uiManager.UpdateSteps("Great flip! Now cook the other side. Sign 'Finish' when ready!");
        }
    }

    private CookingState DetermineCookingState(float time)
    {
        if (time < undercookedThreshold)
            return CookingState.Undercooked;
        else if (time >= perfectMin && time <= perfectMax)
            return CookingState.Perfect;
        else if (time > burntThreshold)
            return CookingState.Burnt;
        else
            return CookingState.Undercooked;
    }

    private void CheckCompletion()
    {
        if (sideStates[0] == CookingState.Perfect && sideStates[1] == CookingState.Perfect)
        {
            Debug.Log("Both sides perfect! Minigame complete!");
            EndMinigame();
        }
        else
        {
            string feedback = "Not done yet! ";
            if (sideStates[0] != CookingState.Perfect)
            {
                feedback += "Side 1 needs work. ";
            }
            if (sideStates[1] != CookingState.Perfect)
            {
                feedback += "Side 2 needs work.";
            }
            uiManager.UpdateSteps(feedback);
        }
    }

    private void UpdateBreadSprite()
    {
        if (currentSide == 1 && sideStates[0] == CookingState.Perfect)
        {
            breadImage.sprite = breadSprites[1];
        }
    }

    private void EndMinigame()
    {
        if (minigameManager != null)
        {
            minigameManager.CloseMinigame();
        }
    }
    
    private void RestartMinigame()
    {
        cookingTime = 0f;
        currentSide = 0;
        isFlipped = false;
        sideStates[0] = CookingState.Raw;
        sideStates[1] = CookingState.Raw;
        breadImage.sprite = breadSprites[0];

        lastSignTime = -999f;
        lastRecognizedSign = "";
        
        successfulFlips = 0;  // ⭐ RESET COUNTER

        // Reset the indicator
        if (cookingIndicator != null)
        {
            cookingIndicator.ResetIndicator();
        }

        uiManager.UpdateSteps("Sign 'dance' when the bread is perfectly golden!");
        Debug.Log("Minigame Restarted - Successful flips reset to 0");  // ⭐ ADD THIS
    }
}