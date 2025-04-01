using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Engine;
using System; 
using Common;

public class SlicingMinigame : MonoBehaviour
{
    public GameObject tableKnife;  // The static knife on the table
    public GameObject rotatingKnife; // The knife that rotates

    public SimpleExecutionEngine engine;
    private bool init;
    private int frame = 0;
    private List<string> levelSigns = new List<string>
    {
        "knife",
        "cut"
    };

    void Start()
    {
        rotatingKnife.SetActive(false); // Hide the rotating knife at start
        tableKnife.SetActive(true);  // Ensure the table knife is visible
    }

    void Update()
    {
        if (!init) {
            engine.recognizer.AddCallback("print", sign => Debug.Log("Got sign" + sign));
            engine.recognizer.outputFilters.Clear();
            engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSigns));
            init = true;
        }

        if (frame == 120) {
            frame = 0;
            engine.buffer.TriggerCallbacks();
        }

        else frame++;
    }

    public void PickUpKnife()
    {
        Debug.Log("Knife picked up!");
        tableKnife.SetActive(false); // Hide the table knife
        rotatingKnife.SetActive(true); // Show the rotating knife
    }
}
