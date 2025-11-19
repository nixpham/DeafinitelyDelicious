using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class DialogueButtons : MonoBehaviour
{
    public NPC npcScript;
    public Button[] buttons;
    public TMP_Text[] buttonText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HideButtons();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public bool SetTextButton()
    {
        int index = npcScript.dialogueIndex;
        if (npcScript.dialogueData.choices == null ||
        npcScript.dialogueData.choices.Length <= index ||
        npcScript.dialogueData.choices[index] == null ||
        npcScript.dialogueData.choices[index].options == null ||
        npcScript.dialogueData.choices[index].options.Length == 0)
        {
            HideButtons();
            return false;
        }
        DialogueOption[] options = npcScript.dialogueData.choices[index].options;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < options.Length)
            {
                buttons[i].gameObject.SetActive(true);
                buttonText[i].text = options[i].text;
                Debug.Log("Active buttons" + buttons[i]);
                Debug.Log("Button text" + buttonText[i].text);
                buttons[i].onClick.RemoveAllListeners();
                int nextIndex = options[i].nextLineIndex;
                buttons[i].onClick.AddListener(() =>
                {
                    npcScript.ResumeAfterClick(nextIndex);
                    Debug.Log("Next index " + nextIndex);
                    HideButtons();
                });
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
        return true;
    }
    public bool AnyButtonActive()
    {
        foreach (var button in buttons)
        {
            if (button.gameObject.activeSelf)
            {
                return true;
            }
        }
        return false;
    }
    void HideButtons()
    {
        foreach (var button in buttons)
        {
            button.gameObject.SetActive(false);
        }
    }
}

