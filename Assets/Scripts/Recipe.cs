using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recipe
{
    public string recipeName;
    public GameObject completedFoodObject;   // Example: the Grilled Cheese final object
    public List<CookingStep> cookingSteps;
}

[System.Serializable]
public class CookingStep
{
    public string stepName;
    public string minigameName;

    // The exact kitchen objects that must be selected for this step to auto-start
    public List<GameObject> requiredSelections;
}