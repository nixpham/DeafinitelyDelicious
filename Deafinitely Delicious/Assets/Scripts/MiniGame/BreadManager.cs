using UnityEngine;
using UnityEngine.SceneManagement; // For restarting the minigame

public class BreadManager : MonoBehaviour
{
    public SpriteRenderer breadRenderer; // The sprite renderer for the bread
    public Sprite[] breadSprites; // Array of bread images (uncut, 1 cut, 2 cuts, etc.)

    private int sliceIndex = 0;
    private int successfulSlices = 0;
    private int totalAttempts = 0;
    private int maxAttempts = 4; // Player gets 4 attempts

    public void UpdateBreadSprite(bool isSuccessful)
    {
        totalAttempts++;

        if (isSuccessful)
        {
            successfulSlices++;
            if (sliceIndex < breadSprites.Length - 1)
            {
                sliceIndex++; // Move to the next bread slice sprite
                breadRenderer.sprite = breadSprites[sliceIndex];
            }
        }

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (totalAttempts >= maxAttempts)
        {
            MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
            
            if (successfulSlices < 2)
            {
                Debug.Log("Not enough successful slices! Restarting...");
                RestartMinigame();
            }
            else
            {
                Debug.Log("Minigame completed successfully!");
                minigameManager.CloseSlicingMinigame();
            }
        }
    }


    private void RestartMinigame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the scene
    }
}
