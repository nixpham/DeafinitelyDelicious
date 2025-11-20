using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using System;
using Common;

public class SlicingMinigame : MonoBehaviour
{
    [Header("Objects")]
    public GameObject tableKnife;    // The static knife on the table
    public GameObject rotatingKnife; // The knife that rotates
    
    [Header("Bread UI")]
    public Image breadImage;         
    public Sprite[] breadSlices;     // Sprites for different bread slice states

    [Header("Attempts UI")]
    [SerializeField] private AttemptsUI attemptsUI;   // NEW — hook your AttemptsUI prefab here

    private MinigameManager minigameManager;
    public UIManager uiManager;

    public SimpleExecutionEngine engine;
    private bool init;
    private int frame = 0;

    private int attempts = 0;        // Total tries
    private int successfulCuts = 0;  // Successful cuts

    private List<string> levelSigns = new List<string>
    {
        "dance",
        "cut"
    };

    void Start()
    {
        minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not found!");
        }

        // Start in default state
        rotatingKnife.SetActive(false);
        tableKnife.SetActive(true);

        attempts = 0;
        successfulCuts = 0;

        breadImage.sprite = breadSlices[0];
        uiManager.UpdateSteps("Sign 'Dance' to pick up the knife!");

        // NEW: reset the circles at the start
        if (attemptsUI != null)
            attemptsUI.ResetAttempts();
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

        if (Input.GetKeyDown(KeyCode.Space) && rotatingKnife.activeSelf)
        {
            CheckCut();
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
        Debug.Log("Recognized sign: " + sign);

        if (sign.ToLower() == "dance")
        {
            PickUpKnife();
            uiManager.UpdateSteps("Sign 'Cut' to attempt a slice!");
        }

        if (sign.ToLower() == "cut" && rotatingKnife.activeSelf)
        {
            CheckCut();
        }
    }

    public void PickUpKnife()
    {
        Debug.Log("Knife picked up!");
        tableKnife.SetActive(false);
        rotatingKnife.SetActive(true);
    }

    private void CheckCut()
    {
        Debug.Log("Attempting a cut...");
        attempts++;

        float angle = rotatingKnife.transform.eulerAngles.z;
        bool success = Mathf.Abs(angle) < 10f || Mathf.Abs(angle - 360f) < 10f;

        if (success)
        {
            successfulCuts++;
            Debug.Log("Successful Cut! (" + successfulCuts + "/2)");
        }
        else
        {
            Debug.Log("Failed Cut! Knife is too angled.");
        }

        // NEW — update the circle UI
        if (attemptsUI != null)
            attemptsUI.RegisterAttempt(success);

        // Update bread sprite
        UpdateBreadSprite();

        if (successfulCuts >= 2)
        {
            Debug.Log("Minigame Success! Closing panel...");
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
            breadImage.sprite = breadSlices[attempts];
        }
        else
        {
            Debug.LogWarning("No more bread slice sprites!");
        }
    }

    private void EndMinigame()
    {
        Debug.Log("Ending the slicing minigame...");
        if (minigameManager != null)
            minigameManager.CloseMinigame();
        else
            Debug.LogError("MinigameManager missing!");
    }

    private void RestartMinigame()
    {
        attempts = 0;
        successfulCuts = 0;
        breadImage.sprite = breadSlices[0];

        // NEW: reset attempt circles
        if (attemptsUI != null)
            attemptsUI.ResetAttempts();

        rotatingKnife.SetActive(false);
        tableKnife.SetActive(true);
        rotatingKnife.transform.rotation = Quaternion.identity;

        Debug.Log("Minigame Restarted!");
        uiManager.UpdateSteps("Sign 'Dance' to pick up the knife!");
    }
}