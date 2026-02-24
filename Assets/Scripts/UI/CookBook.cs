using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookBook : MonoBehaviour
{
    public static bool CookBookOpen = false;

    public GameObject cookBookUI; // The main cookbook UI
    public GameObject grilledCheesePage; // The recipe for a grilled cheese

    [SerializeField] Button _cookbook; // The button to open the cookbook
    [SerializeField] Button _xbutton; // The button to close the cookbook
    [SerializeField] Button _openGrilledCheese; // The button to open grilled cheese recipe
    [SerializeField] Button _cookbutton; // The button to cook a recipe

    public RecipeManager recipeManager; // Reference to RecipeManager
    public StudySessionPopup studySessionPopup;

    void Start()
    {
        // Add listeners for the buttons
        _cookbook.onClick.AddListener(OpenCookBook);
        _xbutton.onClick.AddListener(CloseCookBook);
        _openGrilledCheese.onClick.AddListener(OpenGrilledCheeseRecipe);
        _cookbutton.onClick.AddListener(CookRecipe);
    }

    private void OpenCookBook()
    {
        cookBookUI.SetActive(true); // Show the cookbook UI
        CookBookOpen = true;
    }

    private void CloseCookBook()
    {
        cookBookUI.SetActive(false); // Hide the cookbook UI
        CookBookOpen = false; 
    }

    // Open the grilled cheese recipe page
    private void OpenGrilledCheeseRecipe()
    {
        grilledCheesePage.SetActive(true); // Opens grilled cheese page
    }

    private void CookRecipe()
    {
        Debug.Log("CookRecipe called on " + gameObject.name);

        recipeManager.SelectRecipe("Grilled Cheese");
        CloseCookBook();

        if (studySessionPopup != null)
        {
            Debug.Log("Opening Study Session popup…");
            studySessionPopup.OpenSession(new [] { "Dance", "Cut" });
        }
        else
        {
            Debug.LogWarning("CookBook: studySessionPopup is not assigned in the Inspector.");
        }
    }
}
