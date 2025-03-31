using UnityEngine;
using System.Collections.Generic;

public class MinigameManager : MonoBehaviour
{
    // Reference to each minigame panel
    public GameObject slicingMinigamePanel;
    public GameObject stackingMinigamePanel;
    public GameObject flippingMinigamePanel;

    private GameObject activeMinigame = null;

    // Method to open the minigame by name
    public void OpenMinigame(string minigameName)
    {
        // Deactivate any active minigame panel
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
        }

        // Activate the chosen minigame panel
        switch (minigameName)
        {
            case "SlicingMinigamePanel":
                slicingMinigamePanel.SetActive(true);
                activeMinigame = slicingMinigamePanel;
                break;

            case "StackingMinigamePanel":
                stackingMinigamePanel.SetActive(true);
                activeMinigame = stackingMinigamePanel;
                break;

            case "FlippingMinigamePanel":
                flippingMinigamePanel.SetActive(true);
                activeMinigame = flippingMinigamePanel;
                break;

            default:
                Debug.LogError("Minigame not found: " + minigameName);
                break;
        }
    }

    // Method to close the current minigame panel
    public void CloseMinigame()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }
    }
}
