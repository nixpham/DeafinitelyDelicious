using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Recipe
{
    public string recipeName;
    public List<string> requiredIngredients;
    public List<CookingStep> cookingSteps;
}

[System.Serializable]
public class CookingStep
{
    public string toolName;
    public string minigameName;
    public List<GameObject> requiredObjects; // Objects that should be visible from the start
    public List<GameObject> requiredCountertopObjects; // Objects that must be placed on the counter
}
