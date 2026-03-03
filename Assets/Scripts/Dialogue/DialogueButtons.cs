using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueButtons : MonoBehaviour
{
    public NPC npcScript;
    public Button[] buttons;
    public TMP_Text[] buttonText;

    private DialogueChoice[] choicesSource;

    void Start() => HideButtons();

    public void SetChoicesSource(DialogueChoice[] source) => choicesSource = source;

    public bool SetTextButton(int lineIndex)
    {
        if (choicesSource == null ||
            lineIndex < 0 ||
            lineIndex >= choicesSource.Length ||
            choicesSource[lineIndex] == null ||
            choicesSource[lineIndex].options == null ||
            choicesSource[lineIndex].options.Length == 0)
        {
            HideButtons();
            return false;
        }

        var options = choicesSource[lineIndex].options;

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
                    StartCoroutine(ChoiceFlow(nextIndex, nextNextIndex));
                });
            }
            else buttons[i].gameObject.SetActive(false);
        }

        return true;
    }

    IEnumerator ChoiceFlow(int nextIndex, int nextNextIndex)
    {
        HideButtons();

        yield return npcScript.StartCoroutine(npcScript.PlayAtIndex(nextIndex));

        npcScript.QueueNextLineIndex(nextNextIndex);
    }

    void HideButtons()
    {
        foreach (var b in buttons)
            if (b != null) b.gameObject.SetActive(false);
    }
}