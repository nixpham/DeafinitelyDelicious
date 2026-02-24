using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text instructionText;
    public TMP_Text stepsText;

    public void UpdateInstructions(string newText)
    {
        if (instructionText != null)
        {
            instructionText.text = newText;
        }
        else
        {
            Debug.LogError("Instruction Text is not assigned in UIManager!");
        }
    }

    public void UpdateSteps(string newText)
    {
        if (stepsText != null)
        {
            stepsText.text = newText;
        }
        else
        {
            Debug.LogError("Steps Text is not assigned in UIManager!");
        }
    }
}
