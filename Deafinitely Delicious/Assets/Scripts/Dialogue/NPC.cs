using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour
{
    public System.Action<int> OnDialogueIndexChanged;
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    public int dialogueIndex;
    private bool isTyping, isDialogueActive;
    private bool runNextLine;
    public string[] currentDialogueLines;
    private bool activateBlock;


    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    void Update()
    {
        if (isDialogueActive && Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    public void Interact()
    {
        if (dialogueData == null)
        {
            return;
        }

        if (!isDialogueActive)
        {
            StartDialogue();
        }
        else
        {
            NextLine();
        }
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        runNextLine = true;
        dialogueIndex = 0;
        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcPortrait;
        if (SceneManager.GetActiveScene().name == "PrologueScene")
        {
            currentDialogueLines = dialogueData.prologueLines;
        }
        else if (SceneManager.GetActiveScene().name == "RestaurantScene")
        {
            currentDialogueLines = dialogueData.restaurantLines;
        }
        dialoguePanel.SetActive(true);

        StartCoroutine(TypeLine());
    }
    public void BlockUntilObjectClicked()
    {
        runNextLine = false;
        activateBlock = true;
    }
    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(currentDialogueLines[dialogueIndex]);
            isTyping = false;
            return;
        }
        else if (++dialogueIndex < currentDialogueLines.Length && runNextLine == true)
        {
            OnDialogueIndexChanged?.Invoke(dialogueIndex);
            StartCoroutine(TypeLine());
            Debug.Log("Running line " + dialogueIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");
        string line = currentDialogueLines[dialogueIndex];
        foreach (char letter in line)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
            Debug.Log("Typing line: " + currentDialogueLines[dialogueIndex]);

        }
        isTyping = false;
        DialogueButtons dialogueButtons = FindObjectOfType<DialogueButtons>();
        bool hasChoices = dialogueButtons != null && dialogueButtons.SetTextButton();
        if (dialogueButtons != null && dialogueButtons.AnyButtonActive())
        {
            runNextLine = false;
        }
        else
        {
            runNextLine = true;
        }
    }
    public void ResumeAfterClick(int nextIndex)
    {
        StopAllCoroutines();
        dialogueIndex = nextIndex;
        OnDialogueIndexChanged?.Invoke(dialogueIndex);
        Debug.Log("Dialogue Index: " + dialogueIndex + "Next Index: " + nextIndex);
        runNextLine = true;
        dialoguePanel.SetActive(true);
        StartCoroutine(TypeLine());
    }

    public void PlayAtIndex(int index)
    {
        dialogueIndex = index;
        StartCoroutine(TypeLine());
        Debug.Log("Played at index" + index);
    }
    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);
    }


    void Start()
    {
        if (dialogueData != null)
        {
            StartDialogue();
        }
    }
}