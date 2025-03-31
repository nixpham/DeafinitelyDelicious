using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text instructionText; // Ensure this is a public variable

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
}
