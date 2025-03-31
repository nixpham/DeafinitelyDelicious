using UnityEngine;
using System.Collections.Generic;
using Common;
using Engine;
using System;

public class SlicingMinigame : MonoBehaviour
{
    public GameObject slicingPopup; // UI panel for minigame
    public KnifeRotation knife; // Reference to the knife
    public BreadManager breadManager; // Manages bread state
    public SimpleExecutionEngine engine; // ASL recognition engine

    private bool isMinigameActive = false;
    private bool init = false;
    private int frame = 0;
    private List<string> levelSigns = new List<string> { "knife", "cut" };

    void Start()
    {
        slicingPopup.SetActive(false); // Ensure minigame starts hidden
    }

    void Update()
    {
        if (!init)
        {
            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            init = true;
        }

        // Check for new sign every 120 frames
        if (frame == 120)
        {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }
        else frame++;
    }

    void OnSignRecognized(string sign)
    {
        Debug.Log("Got Sign: " + sign);

        KnifeRotation knife = FindObjectOfType<KnifeRotation>();
        MinigameManager minigameManager = FindObjectOfType<MinigameManager>();

        if (knife == null || minigameManager == null) return;

        if (sign == "knife")
        {
            knife.PickUpKnife();
            minigameManager.OpenSlicingMinigame();
        }
        else if (sign == "cut")
        {
            knife.AttemptSlice();
        }
    }


    public void PickUpKnife()
    {
        if (!isMinigameActive)
        {
            knife.PickUpKnife();
            OpenMinigame();
        }
    }

    public void AttemptSlice()
    {
        bool success = knife.AttemptSlice();
        if (success && breadManager != null)
        {
            breadManager.UpdateBreadState();
            CheckMinigameCompletion();
        }
    }

    public void OpenMinigame()
    {
        slicingPopup.SetActive(true);
        isMinigameActive = true;
    }

    public void CloseMinigame()
    {
        slicingPopup.SetActive(false);
        isMinigameActive = false;
    }

    private void CheckMinigameCompletion()
    {
        if (breadManager.IsMinigameComplete())
        {
            Debug.Log("Slicing minigame completed successfully!");
            CloseMinigame();
        }
    }
}
