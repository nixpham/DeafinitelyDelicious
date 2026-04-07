using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Engine;
using Common;
using Model;



public class FoodScript : MonoBehaviour
{
    public Food food;
    private Image image;

    //public GameObject signRecognizer;
    //public SimpleExecutionEngine engine;
    private bool init;
    private int frame = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
        image.sprite = food.sprite;


        


    }

    // Update is called once per frame
    // void Update()
    // {
    //     if (engine == null )
    //     {
    //         return;
    //         Debug.Log("Engine is missing");
    //     }
    //     if (signRecognizer == null)
    //     {
    //         return;
    //         Debug.Log("recognizer is missing");
    //     }
    //     if (!init)
    //     {
    //         engine.recognizer.AddCallback("print", OnSignRecognized);
    //         engine.recognizer.outputFilters.Clear();
    //         //engine.recognizer.outputFilters.Add(new FocusSublistFilter<string>(levelSign));
    //         engine.recognizer.outputFilters.Add(new Thresholder<string>(0.1f));
    //         init = true;
    //     }

    //     if (frame == 200)
    //     {
    //         frame = 0;
    //         engine.buffer.TriggerCallbacks();
    //     }
    //     else frame++;

    // }


    // public void activateSign()
    // {
    //     EnableCamera(true);
    // }
    // public void EnableCamera(bool enable)
    // {
    //     signRecognizer.SetActive(enable);
    // }

    // private void OnSignRecognized(string raw)
    // {
    //     string s = raw.ToLowerInvariant();
    //     Debug.Log("Recognized sign: " + s);

    //     if (s == food.sign)
    //     {
    //         Debug.Log("Correct Sign");
    //         return;
    //     }
    // }
}
