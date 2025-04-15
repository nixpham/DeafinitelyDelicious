using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using System; 
using Common;

public class SlicingMinigame : MonoBehaviour
{
    public GameObject tableKnife;  // The static knife on the table
    public GameObject rotatingKnife; // The knife that rotates
    public Image breadImage; // The bread image to update
    public Sprite[] breadSlices; // Array of sprites for different bread slice states

    private MinigameManager minigameManager;
    public UIManager uiManager;

    public SimpleExecutionEngine engine;
    private bool init;
    private int frame = 0;
    private int attempts = 0;
    private int successfulCuts = 0;

    private List<string> levelSigns = new List<string>
    {
        "dance",
        "cut"
    };

    void Start()
    {
        minigameManager = FindObjectOfType<MinigameManager>(); // Find MinigameManager in the scene
        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not found in the scene!");
        }

        rotatingKnife.SetActive(false); // Hide the rotating knife at start
        tableKnife.SetActive(true);  // Ensure the table knife is visible
        attempts = 0;
        successfulCuts = 0;
        breadImage.sprite = breadSlices[0]; // Set initial bread slice sprite
        uiManager.UpdateSteps("Sign 'Dance' to pick up the knife!");
    }

    void Update()
    {
        if (!init) {
            engine.recognizer.AddCallback("print", OnSignRecognized);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
            init = true;
        }

        if (Input.GetKeyDown(KeyCode.Space) && rotatingKnife.activeSelf) // Press Space to attempt a cut
        {
            CheckCut();
        }
        
        if (frame == 200) {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }

        else frame++;
    }

    private void OnSignRecognized(string sign)
    {
        Debug.Log("Recognized sign: " + sign);

        if (sign.ToLower() == "dance") // Check if the recognized sign is "knife"
        {
            PickUpKnife();
            uiManager.UpdateSteps("Sign 'Cut' to pick up the knife!");
        }

        if (sign.ToLower() == "cut" && rotatingKnife.activeSelf) // Check if sign is "cut" and knife has been picked up
        {
            CheckCut();
        }
    }

    public void PickUpKnife()
    {
        Debug.Log("Knife picked up!");
        tableKnife.SetActive(false); // Hide the table knife
        rotatingKnife.SetActive(true); // Show the rotating knife
    }

    private void CheckCut()
    {
        Debug.Log("Attempting a cut...");  // Add debug log to track the method being called
        attempts++;

        // Check the angle of the knife
        float angle = rotatingKnife.transform.eulerAngles.z;
        bool isVertical = Mathf.Abs(angle) < 10f || Mathf.Abs(angle - 360f) < 10f;

        if (isVertical)
        {
            successfulCuts++;
            Debug.Log("Successful Cut! (" + successfulCuts + " out of 2)");
        }
        else
        {
            Debug.Log("Failed Cut! Knife is too angled.");
        }

        // Update bread sprite based on attempts
        UpdateBreadSprite();

        // Check for success or failure
        if (successfulCuts >= 2)
        {
            Debug.Log("Minigame Success! Panel Closing...");
            EndMinigame();
        }
        else if (attempts >= 4)
        {
            Debug.Log("Minigame Failed! Restarting...");
            RestartMinigame();
        }
    }

    private void UpdateBreadSprite()
    {
        if (attempts < breadSlices.Length)
        {
            breadImage.sprite = breadSlices[attempts]; // Change bread sprite based on attempts
        }
        else
        {
            Debug.LogWarning("No more bread slices available!");
        }
    }

    private void EndMinigame()
    {
        Debug.Log("Ending the minigame...");  // Log to confirm the function is being called
        if (minigameManager != null)
        {
            minigameManager.CloseMinigame();
        }
        else
        {
            Debug.LogError("MinigameManager reference is missing! Cannot close minigame.");
        }
    }

    private void RestartMinigame()
    {
        // Reset variables
        attempts = 0;
        successfulCuts = 0;
        breadImage.sprite = breadSlices[0]; // Reset bread sprite to initial state

        // Ensure knife is hidden and table knife is visible again
        rotatingKnife.SetActive(false);
        tableKnife.SetActive(true);

        // Reset knife rotation and ready it for slicing again
        rotatingKnife.transform.rotation = Quaternion.identity; // Reset knife rotation to 0 degrees

        // Log to confirm that restart process is working
        Debug.Log("Minigame Restarted - Knife Reset");
    }
}