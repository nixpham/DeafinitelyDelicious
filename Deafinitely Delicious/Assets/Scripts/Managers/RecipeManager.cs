using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    public List<Recipe> recipes;
    private Recipe currentRecipe;
    private int currentStepIndex = 0;
    private List<string> collectedIngredients = new List<string>();

    public MinigameManager minigameManager; // Reference to MinigameManager
    public UIManager uiManager; // Reference to update instructions

    void Start()
    {
        InitializeRecipes();
    }

    void InitializeRecipes()
    {
        Recipe grilledCheese = new Recipe
        {
            recipeName = "Grilled Cheese",
            requiredIngredients = new List<string> { "Bread", "Butter", "Cheese" },
            cookingSteps = new List<CookingStep>
            {
                new CookingStep { toolName = "Knife", minigameName = "SlicingMinigamePanel" },
                new CookingStep { toolName = "CheeseGrater", minigameName = "StackingMinigamePanel" },
                new CookingStep { toolName = "Pan", minigameName = "FlippingMinigamePanel" }
            }
        };

        recipes = new List<Recipe> { grilledCheese };
    }

    public void SelectRecipe(string recipeName)
    {
        currentRecipe = recipes.Find(recipe => recipe.recipeName == recipeName);
        collectedIngredients.Clear();
        currentStepIndex = 0;
        
        if (currentRecipe != null)
        {
            Debug.Log("Selected Recipe: " + currentRecipe.recipeName);
            uiManager.UpdateInstructions("Put the loaf of bread on the cutting board");
        }
    }

    public void CollectIngredient(string ingredient)
    {
        if (currentRecipe != null && currentRecipe.requiredIngredients.Contains(ingredient))
        {
            collectedIngredients.Add(ingredient);
            // Check if all ingredients are collected
            if (collectedIngredients.Count == currentRecipe.requiredIngredients.Count)
            {
                uiManager.UpdateInstructions("Select the knife to begin slicing the bread!");
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