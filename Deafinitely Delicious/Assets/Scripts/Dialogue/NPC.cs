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
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public Image portraitImage;
    public int dialogueIndex;
    public bool isTyping, isDialogueActive;
    public bool runNextLine;
    public DialogueLine[] currentDialogueLines;
    private bool activateBlock;
    private DialogueLine[] sourceLines;
    //public DialogueButtons dialogueButtons;


    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    void Update()
    {
        if (isDialogueActive && Input.GetMouseButtonDown(0) && runNextLine && !isTyping)
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
        //nameText.SetText(dialogueData.npcName);
        //portraitImage.sprite = dialogueData.npcPortrait;
        if (SceneManager.GetActiveScene().name == "PrologueScene")
        {
            sourceLines = dialogueData.prologueLines;
            
        }
        else if (SceneManager.GetActiveScene().name == "RestaurantScene")
        {
            sourceLines = dialogueData.restaurantLines;
        }
        else
        {
            sourceLines = new DialogueLine[0];
        }
        currentDialogueLines = new DialogueLine[sourceLines.Length];
        for (int i = 0; i < currentDialogueLines.Length; i++)
        {
            DialogueLine src = sourceLines[i];
            currentDialogueLines[i] = new DialogueLine
            {
                nameText = src.nameText,
                portraitImage = src.portraitImage,
                text = src.text
            };
        }

        dialoguePanel.SetActive(true);

        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(currentDialogueLines[dialogueIndex].text);
            isTyping = false;
            return;
        }
        if (!runNextLine)
        {
            return;
        }
        else if (++dialogueIndex < currentDialogueLines.Length)
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
        DialogueLine line = currentDialogueLines[dialogueIndex];
        nameText.SetText(line.nameText);
        portraitImage.sprite = line.portraitImage;
        foreach (char letter in line.text)
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
    }
    public void ResumeAfterClick(int nextIndex)
    {
        StopAllCoroutines();
        dialogueIndex = nextIndex;

        OnDialogueIndexChanged?.Invoke(dialogueIndex);
        Debug.Log("Dialogue Index: " + dialogueIndex);
        dialoguePanel.SetActive(true);
  
        StartCoroutine(TypeLine());
    }

    public IEnumerator PlayAtIndex(int index)
    {
        StopAllCoroutines();
        dialogueIndex = index;
        isTyping = true;
        dialogueText.SetText("");
        DialogueLine line = currentDialogueLines[index];
        nameText.SetText(line.nameText);
        portraitImage.sprite = line.portraitImage;
        foreach (char letter in line.text)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
            Debug.Log("Typing line: " + currentDialogueLines[dialogueIndex]);

        }
        isTyping = false;
        runNextLine = false;
        //dialogueButtons.resume = true;
        //Debug.Log("resume " + dialogueButtons.resume);
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