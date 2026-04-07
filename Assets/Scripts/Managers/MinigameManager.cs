using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public GameObject signRecognizer;
    public GameObject slicingMinigamePanel;
    public GameObject stackingMinigamePanel;
    public GameObject flippingMinigamePanel;

    private GameObject activeMinigame = null;
    public RecipeManager recipeManager;

    public bool IsMinigameOpen => activeMinigame != null;

    public void OpenMinigame(string minigameName)
    {
        Debug.Log("OpenMinigame called with: " + minigameName);

        CloseCurrentOnly();

        switch (minigameName)
        {
            case "SlicingMinigamePanel":
                if (slicingMinigamePanel != null)
                {
                    slicingMinigamePanel.SetActive(true);
                    activeMinigame = slicingMinigamePanel;
                    Debug.Log("Slicing panel activeSelf: " + slicingMinigamePanel.activeSelf + ", activeInHierarchy: " + slicingMinigamePanel.activeInHierarchy);
                }
                else
                {
                    Debug.LogError("Slicing panel is NULL");
                }
                break;

            case "StackingMinigamePanel":
                if (stackingMinigamePanel != null)
                {
                    stackingMinigamePanel.SetActive(true);
                    activeMinigame = stackingMinigamePanel;
                    Debug.Log("Stacking panel activeSelf: " + stackingMinigamePanel.activeSelf + ", activeInHierarchy: " + stackingMinigamePanel.activeInHierarchy);
                }
                else
                {
                    Debug.LogError("Stacking panel is NULL");
                }
                break;

            case "FlippingMinigamePanel":
                if (flippingMinigamePanel != null)
                {
                    flippingMinigamePanel.SetActive(true);
                    activeMinigame = flippingMinigamePanel;
                    Debug.Log("Flipping panel activeSelf: " + flippingMinigamePanel.activeSelf + ", activeInHierarchy: " + flippingMinigamePanel.activeInHierarchy);
                }
                else
                {
                    Debug.LogError("Flipping panel is NULL");
                }
                break;

            default:
                Debug.LogError("Minigame not found: " + minigameName);
                return;
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

        if (recipeManager != null)
        {
            recipeManager.CompleteMinigame();
        }
    }

    public void RestartMinigame()
    {
        if (activeMinigame != null)
        {
            Debug.Log("Restarting minigame: " + activeMinigame.name);
            activeMinigame.SetActive(false);
            activeMinigame.SetActive(true);
        }
        else
        {
            Debug.LogError("No active minigame to restart.");
        }
    }

    private void CloseCurrentOnly()
    {
        if (activeMinigame != null)
        {
            activeMinigame.SetActive(false);
            activeMinigame = null;
        }

        EnableCamera(false);
    }

    public void EnableCamera(bool enable)
    {
        if (signRecognizer != null)
        {
            signRecognizer.SetActive(enable);
        }
    }
}