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

    private const string MOM_RESPONSE_KEY = "MOM_RESPONSE";
    private const int MOM_HOME = 0;
    private const int MOM_NOT_SAME = 1;

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
                int optionIndex = i;

                buttons[i].onClick.AddListener(() =>
                {
                    SaveSpecialChoiceIfNeeded(lineIndex, optionIndex);
                    HideButtons();
                    StartCoroutine(ChoiceFlow(nextIndex, nextNextIndex));
                });
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        return true;
    }

    IEnumerator ChoiceFlow(int nextIndex, int nextNextIndex)
    {
        HideButtons();

        yield return npcScript.StartCoroutine(npcScript.PlayAtIndex(nextIndex));

        npcScript.QueueNextLineIndex(nextNextIndex);
    }

    private void SaveSpecialChoiceIfNeeded(int lineIndex, int optionIndex)
    {
        if (npcScript == null) return;

        if (npcScript.sequenceToPlay == DialogueSequence.RestaurantMomConvo1)
        {
            if (optionIndex == 0)
            {
                PlayerPrefs.SetInt(MOM_RESPONSE_KEY, MOM_HOME);
                PlayerPrefs.Save();
            }
            else if (optionIndex == 1)
            {
                PlayerPrefs.SetInt(MOM_RESPONSE_KEY, MOM_NOT_SAME);
                PlayerPrefs.Save();
            }
        }
    }

    void HideButtons()
    {
        foreach (var b in buttons)
            if (b != null) b.gameObject.SetActive(false);
    }
}