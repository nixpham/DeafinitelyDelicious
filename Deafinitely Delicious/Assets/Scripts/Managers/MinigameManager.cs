using UnityEngine;
using System.Collections.Generic;

public class MinigameManager : MonoBehaviour
{
    public List<GameObject> minigamePanels; // List of all minigame panels
    public GameObject startButton; // The main start button
    public RecipeManager recipeManager; // Reference to track recipe progress

    private GameObject activeMinigame = null;

    public void OpenMinigame(string minigameName)
    {
        foreach (GameObject panel in minigamePanels)
        {
            if (panel.name == minigameName)
            {
                panel.SetActive(true);
                activeMinigame = panel;
                break;
            }
        }

        if (startButton != null)
            startButton.SetActive(false); // Hide the main start button
    }

    public void CloseMinigame()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;

            if (recipeManager != null)
                recipeManager.CompleteMinigame(); // Update recipe progress
            else
                Debug.LogWarning("RecipeManager is not assigned in MinigameManager.");
        }
    }
}
