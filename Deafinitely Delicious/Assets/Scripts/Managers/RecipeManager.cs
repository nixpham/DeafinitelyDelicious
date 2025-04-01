using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeManager : MonoBehaviour
{
    public List<Recipe> recipes;
    private Recipe currentRecipe;
    private int currentStepIndex = 0;
    private List<string> collectedIngredients = new List<string>();
    private HashSet<GameObject> placedObjects = new HashSet<GameObject>();

    public MinigameManager minigameManager; // Reference to MinigameManager
    public UIManager uiManager; // Reference to update instructions

    // Assign these in the Unity Inspector
    public GameObject knife, cuttingBoard, bread;

    void Start()
    {
        InitializeRecipes();
        InitializeStepObjects();
    }

    void InitializeRecipes()
    {
        Recipe grilledCheese = new Recipe
        {
            recipeName = "Grilled Cheese",
            requiredIngredients = new List<string> { "Bread", "Butter", "Cheese" },
            cookingSteps = new List<CookingStep>
            {
                new CookingStep 
                { 
                    toolName = "Knife", 
                    minigameName = "SlicingMinigamePanel", 
                    requiredObjects = new List<GameObject> { knife, bread, cuttingBoard }, 
                    requiredCountertopObjects = new List<GameObject> { bread, cuttingBoard } 
                },
                new CookingStep 
                { 
                    toolName = "CheeseGrater", 
                    minigameName = "StackingMinigamePanel", 
                    requiredObjects = new List<GameObject> { }, 
                    requiredCountertopObjects = new List<GameObject>() 
                },
                new CookingStep 
                { 
                    toolName = "Pan", 
                    minigameName = "FlippingMinigamePanel", 
                    requiredObjects = new List<GameObject> { }, 
                    requiredCountertopObjects = new List<GameObject>() 
                }
            }
        };

        recipes = new List<Recipe> { grilledCheese };
    }

    void InitializeStepObjects()
    {
        HashSet<GameObject> allRequiredObjects = new HashSet<GameObject>();

        foreach (Recipe recipe in recipes)
        {
            foreach (CookingStep step in recipe.cookingSteps)
            {
                allRequiredObjects.UnionWith(step.requiredObjects);
                allRequiredObjects.UnionWith(step.requiredCountertopObjects);
            }
        }

        // Hide only objects that aren't in any step
        foreach (GameObject obj in new GameObject[] { knife, bread, cuttingBoard })
        {
            if (!allRequiredObjects.Contains(obj))
            {
                obj.SetActive(false);
            }
        }
    }

    public void SelectRecipe(string recipeName)
    {
        currentRecipe = recipes.Find(recipe => recipe.recipeName == recipeName);
        collectedIngredients.Clear();
        placedObjects.Clear();
        currentStepIndex = 0;

        if (currentRecipe != null)
        {
            Debug.Log("Selected Recipe: " + currentRecipe.recipeName);
            uiManager.UpdateInstructions("Place the loaf of bread and the cutting board on the counter");

            ShowStepObjects(); // Show objects for the first step
        }
    }

    public void ObjectPlaced(GameObject obj)
    {
        if (currentRecipe == null) return;

        CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

        if (step.requiredCountertopObjects.Contains(obj))
        {
            placedObjects.Add(obj);
            obj.SetActive(true); // Ensure the object remains visible on the counter

            Debug.Log($"Placed {obj.name}, total placed objects: {placedObjects.Count}");

            // Check if all required counter objects are placed
            if (placedObjects.Count == step.requiredCountertopObjects.Count)
            {
                Debug.Log("All required objects placed, updating instructions.");
                uiManager.UpdateInstructions($"Now get the {step.toolName} to begin slicing!");
                HighlightTool(step.toolName);
            }
        }
    }

    public void TryStartMinigame(string toolName)
    {
        if (currentRecipe != null && currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

            if (step.toolName == toolName && placedObjects.Count == step.requiredCountertopObjects.Count)
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
        HideStepObjects(); // Hide previous step objects

        currentStepIndex++;
        placedObjects.Clear(); // Reset for next step

        if (currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            ShowStepObjects(); // Show next step objects
            uiManager.UpdateInstructions($"Next, use the {currentRecipe.cookingSteps[currentStepIndex].toolName}");
        }
        else
        {
            uiManager.UpdateInstructions("Recipe complete!");
        }
    }

    void ShowStepObjects()
    {
        if (currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

            // Ensure required objects are ALWAYS visible
            foreach (GameObject obj in step.requiredObjects)
            {
                if (obj != null && !obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    void HideStepObjects()
    {
        if (currentStepIndex >= 0 && currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

            foreach (GameObject obj in step.requiredCountertopObjects)
            {
                obj.SetActive(false);
            }
        }
    }

    void HighlightTool(string toolName)
    {
        GameObject tool = GameObject.Find(toolName);
        if (tool == null)
        {
            return;
        }

        Button button = tool.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        Outline outline = tool.GetComponent<Outline>();
        if (outline == null)
        {
            Debug.Log($"Adding Outline to {toolName}");
            outline = tool.AddComponent<Outline>();
        }
    }

}