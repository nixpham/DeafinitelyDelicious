using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeManager : MonoBehaviour
{
    public List<Recipe> recipes;

    public MinigameManager minigameManager;
    public UIManager uiManager;

    public RectTransform countertopArea;

    public GameObject knife, cuttingBoard, bread, cheese, grater, pan, plate;

    private Recipe currentRecipe;
    private int currentStepIndex = 0;
    private readonly HashSet<GameObject> placedObjects = new HashSet<GameObject>();

    private CookingStep CurrentStep =>
        currentRecipe != null &&
        currentRecipe.cookingSteps != null &&
        currentStepIndex >= 0 &&
        currentStepIndex < currentRecipe.cookingSteps.Count
            ? currentRecipe.cookingSteps[currentStepIndex]
            : null;

    void Start()
    {
        InitializeRecipes();
        InitializeStepObjects();
    }

    void InitializeRecipes()
    {
        var grilledCheese = new Recipe
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
                    toolName = "Grater",
                    minigameName = "StackingMinigamePanel",
                    requiredObjects = new List<GameObject> { grater, cheese, bread },
                    requiredCountertopObjects = new List<GameObject> { cheese, bread }
                },
                new CookingStep
                {
                    toolName = "Pan",
                    minigameName = "FlippingMinigamePanel",
                    requiredObjects = new List<GameObject> { pan },
                    requiredCountertopObjects = new List<GameObject>()
                }
            }
        };

        recipes = new List<Recipe> { grilledCheese };
    }

    void InitializeStepObjects()
    {
        var required = new HashSet<GameObject>();
        foreach (var r in recipes)
        foreach (var s in r.cookingSteps)
        {
            if (s.requiredObjects != null)            required.UnionWith(s.requiredObjects);
            if (s.requiredCountertopObjects != null)  required.UnionWith(s.requiredCountertopObjects);
        }

        var maybe = new[] { knife, cuttingBoard, bread, cheese, grater };
        foreach (var go in maybe)
            if (go && !required.Contains(go)) go.SetActive(false);

        if (plate) plate.SetActive(false);
    }

    public void SelectRecipe(string recipeName)
    {
        currentRecipe = recipes.Find(r => r.recipeName == recipeName);
        currentStepIndex = 0;
        placedObjects.Clear();

        if (plate) plate.SetActive(false);

        if (CurrentStep == null)
        {
            Debug.LogError($"Recipe '{recipeName}' not found or has no steps.");
            return;
        }

        ShowStepObjects();
        SyncPlacedObjectsWithCounterArea();
        UpdatePlacementInstruction();
        TryUnlockToolIfReady();
    }

    public void ObjectPlaced(GameObject obj)
    {
        var step = CurrentStep;
        if (step == null || obj == null) return;

        if (step.requiredCountertopObjects != null && step.requiredCountertopObjects.Contains(obj))
        {
            if (IsObjectOnCounterUI(obj))
            {
                placedObjects.Add(obj);
                obj.SetActive(true);

                if (!TryUnlockToolIfReady())
                    UpdatePlacementInstruction();
            }
            else
            {
                UpdatePlacementInstruction();
            }
        }
    }

    public void TryStartMinigame(string toolName)
    {
        var step = CurrentStep;
        if (step == null) return;

        SyncPlacedObjectsWithCounterArea();

        if (step.toolName == toolName && placedObjects.Count == (step.requiredCountertopObjects?.Count ?? 0))
        {
            minigameManager?.OpenMinigame(step.minigameName);
        }
        else
        {
            if (!TryUnlockToolIfReady())
                UpdatePlacementInstruction();
        }
    }

    public void CompleteMinigame()
    {
        HideObjectsNotNeededAnymore(currentStepIndex);

        currentStepIndex++;
        placedObjects.Clear();

        if (CurrentStep != null)
        {
            ShowStepObjects();
            SyncPlacedObjectsWithCounterArea();
            UpdatePlacementInstruction();
            TryUnlockToolIfReady();
        }
        else
        {
            if (plate) plate.SetActive(true);
            uiManager?.UpdateInstructions("Now grab the plate to serve it!");
        }
    }

    void ShowStepObjects()
    {
        var step = CurrentStep;
        if (step?.requiredObjects == null) return;

        foreach (var obj in step.requiredObjects)
            if (obj && !obj.activeSelf) obj.SetActive(true);
    }

    void HideObjectsNotNeededAnymore(int completedIndex)
    {
        if (currentRecipe == null ||
            completedIndex < 0 ||
            completedIndex >= currentRecipe.cookingSteps.Count) return;

        var justCompleted = currentRecipe.cookingSteps[completedIndex];

        var nextNeeds = new HashSet<GameObject>();
        var nextIndex = completedIndex + 1;
        if (nextIndex < currentRecipe.cookingSteps.Count)
        {
            var next = currentRecipe.cookingSteps[nextIndex];
            if (next.requiredCountertopObjects != null) nextNeeds.UnionWith(next.requiredCountertopObjects);
            if (next.requiredObjects != null)           nextNeeds.UnionWith(next.requiredObjects);
        }

        if (justCompleted.requiredCountertopObjects == null) return;

        foreach (var obj in justCompleted.requiredCountertopObjects)
            if (obj) obj.SetActive(nextNeeds.Contains(obj));
    }

    void SyncPlacedObjectsWithCounterArea()
    {
        var step = CurrentStep;
        if (step?.requiredCountertopObjects == null) return;

        foreach (var obj in step.requiredCountertopObjects)
            if (obj && IsObjectOnCounterUI(obj)) placedObjects.Add(obj);
    }

    bool IsObjectOnCounterUI(GameObject obj)
    {
        if (!countertopArea) return false;
        var rect = obj.GetComponent<RectTransform>();
        if (!rect) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(countertopArea, rect.position, null);
    }

    bool TryUnlockToolIfReady()
    {
        var step = CurrentStep;
        if (step == null) return false;

        int need = step.requiredCountertopObjects?.Count ?? 0;
        if (placedObjects.Count == need)
        {
            uiManager?.UpdateInstructions($"Now get the {step.toolName} to begin!");
            HighlightTool(step.toolName);
            return true;
        }
        return false;
    }

    void HighlightTool(string toolName)
    {
        var tool = GameObject.Find(toolName);
        if (!tool) return;

        var button = tool.GetComponent<Button>();
        if (!button) return;

        if (!tool.GetComponent<Outline>()) tool.AddComponent<Outline>();
    }

    void UpdatePlacementInstruction()
    {
        var step = CurrentStep;
        if (step == null) return;

        int need = step.requiredCountertopObjects?.Count ?? 0;
        int have = placedObjects.Count;
        uiManager?.UpdateInstructions($"Place the {GetObjectNames(step.requiredCountertopObjects)} on the counter. ({have}/{need})");
    }

    string GetObjectNames(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0) return "";
        var names = new List<string>();
        foreach (var o in objects) if (o) names.Add(o.name);
        return string.Join(" and ", names);
    }
}