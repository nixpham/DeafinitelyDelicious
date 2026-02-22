using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class DialogueButtons : MonoBehaviour
{
    public NPC npcScript;
    public Button[] buttons;
    public TMP_Text[] buttonText;

    void Start()
    {
        HideButtons();
    }

    /// <summary>
    /// Show choices for a specific dialogue line index.
    /// NPC should call this using the line index that just finished typing.
    /// </summary>
    public bool SetTextButton(int lineIndex)
    {
        if (npcScript == null || npcScript.dialogueData == null)
        {
            Debug.LogWarning("[DialogueButtons] npcScript/dialogueData not assigned.");
            HideButtons();
            return false;
        }

        var choicesArr = npcScript.dialogueData.choices;

        Debug.Log($"[DialogueButtons] SetTextButton(lineIndex={lineIndex}) " +
                  $"choicesNull={(choicesArr == null)} " +
                  $"choicesLen={(choicesArr != null ? choicesArr.Length : -1)}");

        if (choicesArr == null ||
            lineIndex < 0 ||
            choicesArr.Length <= lineIndex ||
            choicesArr[lineIndex] == null ||
            choicesArr[lineIndex].options == null ||
            choicesArr[lineIndex].options.Length == 0)
        {
            HideButtons();
            return false;
        }

        DialogueOption[] options = choicesArr[lineIndex].options;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            if (i < options.Length)
            {
                buttons[i].gameObject.SetActive(true);

                if (buttonText != null && i < buttonText.Length && buttonText[i] != null)
                    buttonText[i].text = options[i].text;

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

    /// <summary>
    /// Backwards compatible overload (not recommended).
    /// Uses npcScript.dialogueIndex and can be wrong if NPC increments early.
    /// </summary>
    public bool SetTextButton()
    {
        return SetTextButton(npcScript != null ? npcScript.dialogueIndex : -1);
    }

    public bool AnyButtonActive()
    {
        foreach (var button in buttons)
        {
            if (button != null && button.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    void HideButtons()
    {
        foreach (var button in buttons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayDialogue(int nextIndex, int nextNextIndex)
    {
        if (npcScript == null)
            yield break;

        // Play the immediate branch response line
        yield return StartCoroutine(npcScript.PlayAtIndex(nextIndex));

        // Allow continuing (NPC will block advancement itself if it shows more choices)
        npcScript.runNextLine = true;

        // Wait for a REAL dialogue-box click, not any mouse click
        int startClicks = npcScript.DialogueBoxClickCount;
        yield return new WaitUntil(() => npcScript.DialogueBoxClickCount > startClicks);

        npcScript.ResumeAfterClick(nextNextIndex);
    }
}