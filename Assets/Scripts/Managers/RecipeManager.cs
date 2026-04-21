using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RecipeManager : MonoBehaviour
{
    private const string SERVING_READY_KEY = "DEMO_GRILLED_CHEESE_READY_TO_SERVE";

    [Header("Managers")]
    public MinigameManager minigameManager;

    [Header("Kitchen Objects")]
    public GameObject knife;
    public GameObject cuttingBoard;
    public GameObject bread;
    public GameObject cheese;
    public GameObject butter;
    public GameObject grater;
    public GameObject pan;

    [Header("Completed Food")]
    public GameObject grilledCheeseObject;

    [Header("Scene Names")]
    public string restaurantSceneName = "RestaurantScene";

    private List<Recipe> recipes = new List<Recipe>();

    private Recipe currentRecipe;
    private int currentStepIndex = 0;
    private bool recipeLocked = false;

    private readonly HashSet<GameObject> selectedObjects = new HashSet<GameObject>();
    private readonly Dictionary<string, bool> completedRecipes = new Dictionary<string, bool>();

    private CookingStep CurrentStep
    {
        get
        {
            if (currentRecipe == null) return null;
            if (currentRecipe.cookingSteps == null) return null;
            if (currentStepIndex < 0 || currentStepIndex >= currentRecipe.cookingSteps.Count) return null;
            return currentRecipe.cookingSteps[currentStepIndex];
        }
    }

    private void Start()
    {
        InitializeRecipes();
        InitializeSceneState();
    }

    private void InitializeRecipes()
    {
        Recipe grilledCheese = new Recipe
        {
            recipeName = "Grilled Cheese",
            completedFoodObject = grilledCheeseObject,
            cookingSteps = new List<CookingStep>
            {
                new CookingStep
                {
                    stepName = "Slice Bread",
                    minigameName = "SlicingMinigamePanel",
                    requiredSelections = new List<GameObject> { bread, knife, cuttingBoard }
                },
                new CookingStep
                {
                    stepName = "Grate Cheese",
                    minigameName = "StackingMinigamePanel",
                    requiredSelections = new List<GameObject> { bread, cheese, grater }
                },
                new CookingStep
                {
                    stepName = "Cook Sandwich",
                    minigameName = "FlippingMinigamePanel",
                    requiredSelections = new List<GameObject> { bread, butter, pan }
                }
            }
        };

        recipes = new List<Recipe> { grilledCheese };

        foreach (Recipe recipe in recipes)
        {
            completedRecipes[recipe.recipeName] = false;

            if (recipe.completedFoodObject != null)
                recipe.completedFoodObject.SetActive(false);
        }
    }

    private void InitializeSceneState()
    {
        ClearAllSelections();
        currentRecipe = null;
        currentStepIndex = 0;
        recipeLocked = false;
        Debug.Log("RecipeManager initialized.");
    }

    public void HandleKitchenObjectClicked(KitchenSelectable selectable)
    {
        if (selectable == null || selectable.gameObject == null)
            return;

        GameObject clickedObject = selectable.gameObject;
        Debug.Log("Clicked: " + clickedObject.name);

        if (minigameManager != null && minigameManager.IsMinigameOpen)
        {
            Debug.Log("Ignored click because minigame is already open.");
            return;
        }

        Recipe foodRecipe = GetRecipeByCompletedFood(clickedObject);
        if (foodRecipe != null && completedRecipes.ContainsKey(foodRecipe.recipeName) && completedRecipes[foodRecipe.recipeName])
        {
            Debug.Log("Clicked completed food: " + clickedObject.name);
            OnCompletedFoodClicked(foodRecipe);
            return;
        }

        if (recipeLocked)
        {
            Debug.Log("Recipe is locked. Current step: " + CurrentStep.stepName);
            ToggleSelection(selectable);
            TryAutoStartLockedRecipeStep();
            return;
        }

        ToggleSelection(selectable);
        TryAutoDetectAndStartRecipe();
    }

    private void ToggleSelection(KitchenSelectable selectable)
    {
        if (selectable.IsSelected)
        {
            selectable.SetSelected(false);
            selectedObjects.Remove(selectable.gameObject);
            Debug.Log("Unselected: " + selectable.gameObject.name);
        }
        else
        {
            selectable.SetSelected(true);
            selectedObjects.Add(selectable.gameObject);
            Debug.Log("Selected: " + selectable.gameObject.name);
        }

        DebugSelectedObjects();
    }

    private void DebugSelectedObjects()
    {
        string msg = "Currently selected: ";
        foreach (GameObject obj in selectedObjects)
            msg += obj.name + " | ";
        Debug.Log(msg);
    }

    private void TryAutoDetectAndStartRecipe()
    {
        Debug.Log("Trying to detect recipe start...");

        foreach (Recipe recipe in recipes)
        {
            if (completedRecipes.ContainsKey(recipe.recipeName) && completedRecipes[recipe.recipeName])
                continue;

            if (recipe.cookingSteps == null || recipe.cookingSteps.Count == 0)
                continue;

            CookingStep firstStep = recipe.cookingSteps[0];
            Debug.Log("Checking recipe: " + recipe.recipeName + " | Need: " + GetObjectNames(firstStep.requiredSelections));

            if (SelectionMatchesExactly(firstStep.requiredSelections))
            {
                Debug.Log("MATCHED FIRST STEP for: " + recipe.recipeName);
                currentRecipe = recipe;
                currentStepIndex = 0;
                recipeLocked = true;

                minigameManager?.OpenMinigame(firstStep.minigameName);
                return;
            }
        }

        Debug.Log("No recipe start matched.");
    }

    private void TryAutoStartLockedRecipeStep()
    {
        if (currentRecipe == null || CurrentStep == null)
            return;

        Debug.Log("Trying locked step: " + CurrentStep.stepName + " | Need: " + GetObjectNames(CurrentStep.requiredSelections));

        if (SelectionMatchesExactly(CurrentStep.requiredSelections))
        {
            Debug.Log("MATCHED LOCKED STEP: " + CurrentStep.stepName);
            minigameManager?.OpenMinigame(CurrentStep.minigameName);
        }
        else
        {
            Debug.Log("Locked step not matched yet.");
        }
    }

    public void CompleteMinigame()
    {
        if (currentRecipe == null)
            return;

        Debug.Log("Completed minigame for step index: " + currentStepIndex);

        ClearAllSelections();
        currentStepIndex++;

        if (currentStepIndex >= currentRecipe.cookingSteps.Count)
        {
            MarkRecipeComplete(currentRecipe);
            return;
        }

        Debug.Log("Advanced to next step: " + CurrentStep.stepName);
    }

    private void MarkRecipeComplete(Recipe recipe)
    {
        completedRecipes[recipe.recipeName] = true;
        recipeLocked = false;
        currentRecipe = null;
        currentStepIndex = 0;

        if (recipe.completedFoodObject != null)
            recipe.completedFoodObject.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(GameAudioPaths.UiSparkle, 0.9f);

        Debug.Log("Recipe completed: " + recipe.recipeName);
    }

    private void OnCompletedFoodClicked(Recipe recipe)
    {
        completedRecipes[recipe.recipeName] = false;

        if (recipe.completedFoodObject != null)
            recipe.completedFoodObject.SetActive(false);

        ClearAllSelections();
        currentRecipe = null;
        currentStepIndex = 0;
        recipeLocked = false;

        PlayerPrefs.SetInt(SERVING_READY_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("Recipe reset and marked ready to serve: " + recipe.recipeName);
        SceneManager.LoadScene(restaurantSceneName);
    }

    private void ClearAllSelections()
    {
        selectedObjects.Clear();

        KitchenSelectable[] allSelectables = FindObjectsOfType<KitchenSelectable>(true);
        foreach (KitchenSelectable selectable in allSelectables)
        {
            if (selectable != null)
                selectable.ClearSelection();
        }
    }

    private bool SelectionMatchesExactly(List<GameObject> neededObjects)
    {
        if (neededObjects == null)
            return selectedObjects.Count == 0;

        if (selectedObjects.Count != neededObjects.Count)
            return false;

        HashSet<string> selectedNames = new HashSet<string>();
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
                selectedNames.Add(obj.name);
        }

        foreach (GameObject needed in neededObjects)
        {
            if (needed == null || !selectedNames.Contains(needed.name))
                return false;
        }

        return true;
    }

    private Recipe GetRecipeByCompletedFood(GameObject obj)
    {
        foreach (Recipe recipe in recipes)
        {
            if (recipe.completedFoodObject == obj)
                return recipe;
        }

        return null;
    }

    private string GetObjectNames(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0)
            return "";

        List<string> names = new List<string>();
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                names.Add(obj.name);
        }

        return string.Join(", ", names);
    }
}