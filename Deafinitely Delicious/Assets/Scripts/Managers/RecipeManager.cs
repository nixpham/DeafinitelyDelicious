using UnityEngine;
using System.Collections.Generic;

public class RecipeManager : MonoBehaviour
{
    public List<Recipe> recipes;
    private Recipe currentRecipe;
    private int currentStepIndex = 0;
    private List<string> collectedIngredients = new List<string>();

    public MinigameManager minigameManager; // Reference to MinigameManager
    public UIManager uiManager; // Reference to update instructions

    public void SelectRecipe(string recipeName)
    {
        currentRecipe = recipes.Find(recipe => recipe.recipeName == recipeName);
        collectedIngredients.Clear();
        currentStepIndex = 0;
        uiManager.UpdateInstructions("Collect Ingredients: " + string.Join(", ", currentRecipe.requiredIngredients));
    }

    public void CollectIngredient(string ingredient)
    {
        if (currentRecipe != null && currentRecipe.requiredIngredients.Contains(ingredient))
        {
            collectedIngredients.Add(ingredient);
            uiManager.UpdateInstructions("Collected: " + string.Join(", ", collectedIngredients));

            // Check if all ingredients are collected
            if (collectedIngredients.Count == currentRecipe.requiredIngredients.Count)
            {
                uiManager.UpdateInstructions("Click the highlighted tool to start cooking.");
            }
        }
    }

    public void TryStartMinigame(string toolName)
    {
        if (currentRecipe != null && currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

            if (step.toolName == toolName && IngredientsAreCorrect())
            {
                minigameManager.OpenMinigame(step.minigameName);
            }
            else
            {
                uiManager.UpdateInstructions("Not the right tool or ingredients missing!");
            }
        }
    }

    public void CompleteMinigame()
    {
        currentStepIndex++;
        if (currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            uiManager.UpdateInstructions("Next, use the " + currentRecipe.cookingSteps[currentStepIndex].toolName);
        }
        else
        {
            uiManager.UpdateInstructions("Recipe complete!");
        }
    }

    private bool IngredientsAreCorrect()
    {
        return collectedIngredients.Count == currentRecipe.requiredIngredients.Count;
    }
}
