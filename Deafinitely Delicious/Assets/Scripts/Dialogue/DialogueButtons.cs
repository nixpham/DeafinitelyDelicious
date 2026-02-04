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
    //public bool resume = false;
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
                int nextNextIndex = options[i].nextNextLineIndex;
                buttons[i].onClick.AddListener(() =>
                {
                    HideButtons();
                    StartCoroutine(PlayDialogue(nextIndex, nextNextIndex));
  
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

    private IEnumerator PlayDialogue(int nextIndex, int nextNextIndex)
    {
        yield return StartCoroutine(npcScript.PlayAtIndex(nextIndex));
        npcScript.runNextLine = true;
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        npcScript.ResumeAfterClick(nextNextIndex);
    }
}

