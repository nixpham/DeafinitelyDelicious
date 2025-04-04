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
    public GameObject knife, cuttingBoard, bread, cheese, grater;

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
                    requiredObjects = new List<GameObject> { grater, cheese, bread }, 
                    requiredCountertopObjects = new List<GameObject> { cheese, bread }
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
            
            // Get the first step
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];
            string objectsNeeded = GetObjectNames(step.requiredCountertopObjects);
            uiManager.UpdateInstructions($"Place the {objectsNeeded} on the counter.");

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
        if (currentRecipe == null)
        {
            Debug.LogError("No recipe selected! Cannot start minigame.");
            return;
        }

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
        HideStepObjects(); // Hide objects from the previous step

        currentStepIndex++;
        placedObjects.Clear(); // Clear placed objects for the next step

        if (currentStepIndex < currentRecipe.cookingSteps.Count)
        {
            ShowStepObjects(); // Show next step's required objects

            // Prompt to place the countertop objects first
            CookingStep step = currentRecipe.cookingSteps[currentStepIndex];
            uiManager.UpdateInstructions($"Place the {GetObjectNames(step.requiredCountertopObjects)} on the counter.");
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
        Debug.Log("Attempting to hide step objects...");

        if (currentRecipe == null)
        {
            Debug.LogError("currentRecipe is NULL! Make sure a recipe is selected before calling HideStepObjects.");
            return;
        }

        if (currentStepIndex < 0 || currentStepIndex >= currentRecipe.cookingSteps.Count)
        {
            Debug.LogError($"Invalid step index! Step index {currentStepIndex} is out of range (max {currentRecipe.cookingSteps.Count - 1}).");
            return;
        }

        CookingStep step = currentRecipe.cookingSteps[currentStepIndex];

        if (step == null)
        {
            Debug.LogError("Step is null! Cannot hide objects.");
            return;
        }

        if (step.requiredCountertopObjects == null)
        {
            Debug.LogError("Step requiredCountertopObjects list is null!");
            return;
        }

        foreach (GameObject obj in step.requiredCountertopObjects)
        {
            if (obj == null)
            {
                Debug.LogError("One of the objects in requiredCountertopObjects is null!");
                continue;
            }

            Debug.Log($"Hiding {obj.name}");
            obj.SetActive(false);
        }

        Debug.Log("Successfully hid step objects.");
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

    private string GetObjectNames(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0) return "";

        List<string> names = new List<string>();
        foreach (var obj in objects)
        {
            names.Add(obj.name);
        }

        return string.Join(" and ", names);
    }

}