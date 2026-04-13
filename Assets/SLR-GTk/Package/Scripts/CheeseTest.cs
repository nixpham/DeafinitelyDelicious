using System.Collections.Generic;
using UnityEngine;
using Engine;
using Common;

public class CheeseTest : MonoBehaviour
{
    [Header("References")]
    public SimpleExecutionEngine engine;

    private bool init = false;
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

        if (Input.GetKeyDown(KeyCode.Return))
        {
            engine.buffer.TriggerCallbacks();
            Debug.Log("Buffer triggered.");
        }
    }

    void HandleSign(string sign)
    {
        Debug.Log($"Sign recognized: '{sign}'");
    }
}