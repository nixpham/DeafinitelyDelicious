using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text instructionText; // Assign a UI Text element in Unity

    public void UpdateInstructions(string message)
    {
        instructionText.text = message;
    }
}
