using UnityEngine;

public class CookingTool : MonoBehaviour
{
    public string toolName;
    public RecipeManager recipeManager;

    private void OnMouseDown()
    {
        recipeManager.TryStartMinigame(toolName);
    }
}
