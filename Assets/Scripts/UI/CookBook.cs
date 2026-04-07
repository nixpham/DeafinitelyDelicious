using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookBook : MonoBehaviour
{
    public static bool CookBookOpen = false;

    public enum Tab { Dishes, Words, People }

    [Serializable]
    public class RecipeSection
    {
        public string recipeName;
        public Button openButton;
        public List<GameObject> pages = new();
    }

    [Header("Root")]
    [SerializeField] private GameObject cookBookRoot;

    [Header("Open/Close")]
    [SerializeField] private Button openCookbookButton;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button dishesTabButton;
    [SerializeField] private Button wordsTabButton;
    [SerializeField] private Button peopleTabButton;

    [Header("Hub Pages (one per tab)")]
    [SerializeField] private GameObject dishesHubPage;
    [SerializeField] private GameObject wordsHubPage;
    [SerializeField] private GameObject peopleHubPage;

    [Header("Nav Arrows (auto-hidden)")]
    [SerializeField] private Button leftArrowButton;   // back/prev
    [SerializeField] private Button rightArrowButton;  // next

    [Header("Recipes (expandable)")]
    [SerializeField] private List<RecipeSection> recipes = new();

    [Header("Help Popup (modal)")]
    [SerializeField] private Button helpButton;
    [SerializeField] private GameObject helpPopupPanel;
    [SerializeField] private Button helpBlockerButton; // fullscreen invisible button

    private Tab currentTab = Tab.Dishes;

    private List<GameObject> currentSectionPages = new(); // hub (1) or recipe (2-3)
    private bool inRecipeSection = false;
    private int pageIndex = 0;

    private void Awake()
    {
        if (cookBookRoot) cookBookRoot.SetActive(false);

        if (helpPopupPanel) helpPopupPanel.SetActive(false);
        if (helpBlockerButton) helpBlockerButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Open/close
        if (openCookbookButton) openCookbookButton.onClick.AddListener(OpenCookBook);
        if (closeButton) closeButton.onClick.AddListener(CloseCookBook);

        // Tabs reset to hub
        if (dishesTabButton) dishesTabButton.onClick.AddListener(() => SwitchTab(Tab.Dishes));
        if (wordsTabButton)  wordsTabButton.onClick.AddListener(() => SwitchTab(Tab.Words));
        if (peopleTabButton) peopleTabButton.onClick.AddListener(() => SwitchTab(Tab.People));

        // Nav
        if (leftArrowButton) leftArrowButton.onClick.AddListener(OnLeftArrow);
        if (rightArrowButton) rightArrowButton.onClick.AddListener(OnRightArrow);

        // Help (modal)
        if (helpButton) helpButton.onClick.AddListener(OpenHelp);
        if (helpBlockerButton) helpBlockerButton.onClick.AddListener(CloseHelp);

        // Hook recipe buttons
        foreach (var r in recipes)
        {
            if (r.openButton == null) continue;
            string nameCopy = r.recipeName;
            r.openButton.onClick.AddListener(() => OpenRecipe(nameCopy));
        }

        // Initialize to dishes hub (cookbook closed initially)
        SetToHub(Tab.Dishes);
        CloseHelp();
    }

    public void OpenCookBook()
    {
        if (!cookBookRoot) return;

        cookBookRoot.SetActive(true);
        CookBookOpen = true;

        SwitchTab(Tab.Dishes);
    }

    public void CloseCookBook()
    {
        if (!cookBookRoot) return;

        CloseHelp();
        cookBookRoot.SetActive(false);
        CookBookOpen = false;
    }

    // ---------- Tabs ----------
    private void SwitchTab(Tab tab)
    {
        if (IsHelpOpen()) return;

        currentTab = tab;
        SetToHub(tab);
    }

    private void SetToHub(Tab tab)
    {
        // Reset any recipe state
        inRecipeSection = false;
        pageIndex = 0;

        HideAllPages();

        GameObject hub = tab switch
        {
            Tab.Dishes => dishesHubPage,
            Tab.Words  => wordsHubPage,
            Tab.People => peopleHubPage,
            _ => dishesHubPage
        };

        currentSectionPages.Clear();
        if (hub != null)
        {
            hub.SetActive(true);
            currentSectionPages.Add(hub); // hub is a 1-page section
        }

        UpdateNav();
    }

    // ---------- Recipes ----------
    public void OpenRecipe(string recipeName)
    {
        if (IsHelpOpen()) return;
        if (currentTab != Tab.Dishes) return;

        var recipe = recipes.Find(r => r.recipeName == recipeName);
        if (recipe == null || recipe.pages == null || recipe.pages.Count == 0) return;

        HideAllPages();

        inRecipeSection = true;
        pageIndex = 0;

        currentSectionPages = recipe.pages;

        // Show recipe page 0
        if (currentSectionPages[0]) currentSectionPages[0].SetActive(true);

        UpdateNav();
    }

    // ---------- Navigation ----------
    private void OnLeftArrow()
    {
        if (IsHelpOpen()) return;

        if (inRecipeSection && pageIndex == 0)
        {
            SetToHub(Tab.Dishes);
            return;
        }

        if (pageIndex > 0)
            SetPage(pageIndex - 1);
    }

    private void OnRightArrow()
    {
        if (IsHelpOpen()) return;

        if (pageIndex < currentSectionPages.Count - 1)
            SetPage(pageIndex + 1);
    }

    private void SetPage(int newIndex)
    {
        newIndex = Mathf.Clamp(newIndex, 0, Mathf.Max(0, currentSectionPages.Count - 1));
        if (newIndex == pageIndex) return;

        if (currentSectionPages[pageIndex]) currentSectionPages[pageIndex].SetActive(false);
        pageIndex = newIndex;
        if (currentSectionPages[pageIndex]) currentSectionPages[pageIndex].SetActive(true);

        UpdateNav();
    }

    private void UpdateNav()
    {
        int count = currentSectionPages?.Count ?? 0;
        bool showArrows = count > 1;

        if (leftArrowButton)  leftArrowButton.gameObject.SetActive(showArrows);
        if (rightArrowButton) rightArrowButton.gameObject.SetActive(showArrows);

        if (!showArrows) return;

        // left enabled:
        // - recipe page 0: enabled (acts as "back to dishes")
        // - other pages: enabled if can go back
        bool leftEnabled = inRecipeSection ? true : pageIndex > 0;

        if (!inRecipeSection)
            leftEnabled = pageIndex > 0;

        bool rightEnabled = pageIndex < count - 1;

        leftArrowButton.interactable = leftEnabled;
        rightArrowButton.interactable = rightEnabled;
    }

    private void HideAllPages()
    {
        // hubs
        if (dishesHubPage) dishesHubPage.SetActive(false);
        if (wordsHubPage)  wordsHubPage.SetActive(false);
        if (peopleHubPage) peopleHubPage.SetActive(false);

        // recipe pages
        foreach (var r in recipes)
        {
            if (r.pages == null) continue;
            foreach (var p in r.pages)
                if (p) p.SetActive(false);
        }
    }

    // ---------- Help Popup (modal) ----------
    private void OpenHelp()
    {
        if (helpPopupPanel) helpPopupPanel.SetActive(true);
        if (helpBlockerButton) helpBlockerButton.gameObject.SetActive(true);
    }

    private void CloseHelp()
    {
        if (helpPopupPanel) helpPopupPanel.SetActive(false);
        if (helpBlockerButton) helpBlockerButton.gameObject.SetActive(false);
    }

    private bool IsHelpOpen()
    {
        return helpPopupPanel != null && helpPopupPanel.activeSelf;
    }
}