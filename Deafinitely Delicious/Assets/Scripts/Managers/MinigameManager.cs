using UnityEngine;
using System.Collections.Generic;

public class MinigameManager : MonoBehaviour
{
    public GameObject signRecognizer;
    public GameObject slicingMinigamePanel;
    public GameObject stackingMinigamePanel;
    public GameObject flippingMinigamePanel;

    private GameObject activeMinigame = null;
    public RecipeManager recipeManager;

    public void OpenMinigame(string minigameName)
    {
        switch (minigameName)
        {
            case "SlicingMinigamePanel":
                slicingMinigamePanel.SetActive(true);
                activeMinigame = slicingMinigamePanel;
                Debug.Log("Opening slicing minigame panel");
                break;

            case "StackingMinigamePanel":
                stackingMinigamePanel.SetActive(true);
                activeMinigame = stackingMinigamePanel;
                Debug.Log("Opening stacking minigame panel");
                break;

            case "FlippingMinigamePanel":
                flippingMinigamePanel.SetActive(true);
                activeMinigame = flippingMinigamePanel;
                Debug.Log("Opening flipping minigame panel");
                break;

            default:
                Debug.LogError("Minigame not found: " + minigameName);
                break;
        }

        EnableCamera(true);
    }

    public void CloseMinigame()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }

        EnableCamera(false);

        // Notify RecipeManager that the minigame is completed
        if (recipeManager != null)
        {
            recipeManager.CompleteMinigame(); // Call this method to progress the recipe
        }
    }


    public void RestartMinigame()
    {
        if (activeMinigame != null)
        {
            Debug.Log("Restarting minigame: " + activeMinigame.name);
            activeMinigame.SetActive(false); // Close it first
            activeMinigame.SetActive(true);  // Reopen to reset
        }
        else
        {
            Debug.LogError("No active minigame to restart.");
        }
    }

    public void EnableCamera(bool enable)
    {
        signRecognizer.SetActive(enable);
    }
}
