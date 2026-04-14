using System.Collections.Generic;
using UnityEngine;
using Engine;
using Common;
using TMPro;

public class CheeseTest : MonoBehaviour
{
    [Header("References")]
    public SimpleExecutionEngine engine;
    public TextMeshProUGUI feedbackText;

    [Header("Settings")]
    public float triggerInterval = 2f; // how often to check for a sign in seconds

    private bool init = false;
    private float timer = 0f;
    private List<string> testSigns = new List<string> { "cheese", "frog" };

    void Update()
    {
        if (!init && engine != null && engine.recognizer != null)
        {
            engine.recognizer.AddCallback("cheeseTest", HandleSign);
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(testSigns));
            init = true;
            Debug.Log("CheeseTest initialized.");
        }

        timer += Time.deltaTime;
        if (timer >= triggerInterval)
        {
            timer = 0f;
            if (engine != null && engine.buffer != null)
                engine.buffer.TriggerCallbacks();
        }
    }

    void HandleSign(string sign)
    {
        Debug.Log($"Sign recognized: '{sign}'");

        if (feedbackText == null) return;

        if (sign == "cheese" || sign == "frog")
            feedbackText.text = $"{sign} sign recognized!";
        else
            feedbackText.text = "Sign not recognized, try again!";
    }
}