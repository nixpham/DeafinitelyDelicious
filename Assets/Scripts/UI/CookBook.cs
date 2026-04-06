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
    [SerializeField] private Transform pagesRoot;

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
        ResolvePagesRoot();
        EnsureTabButtonsReceiveClicks();

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
        BringOverlayControlsToFront();
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

        HideAllPageObjects();

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
            ActivatePageChain(hub);
            currentSectionPages.Add(hub); // hub is a 1-page section
        }

        BringOverlayControlsToFront();
        UpdateNav();
    }

    // ---------- Recipes ----------
    public void OpenRecipe(string recipeName)
    {
        if (IsHelpOpen()) return;
        if (currentTab != Tab.Dishes) return;

        var recipe = recipes.Find(r => r.recipeName == recipeName);
        if (recipe == null || recipe.pages == null || recipe.pages.Count == 0) return;

        HideAllPageObjects();

        inRecipeSection = true;
        pageIndex = 0;

        currentSectionPages = recipe.pages;

        // Show recipe page 0
        if (currentSectionPages[0]) ActivatePageChain(currentSectionPages[0]);

        BringOverlayControlsToFront();
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

        pageIndex = newIndex;

        HideAllPageObjects();
        if (currentSectionPages[pageIndex]) ActivatePageChain(currentSectionPages[pageIndex]);

        BringOverlayControlsToFront();
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

    private void HideAllPageObjects()
    {
        HideAllPages();

        if (pagesRoot == null) return;

        for (int i = 0; i < pagesRoot.childCount; i++)
        {
            var child = pagesRoot.GetChild(i);
            if (IsPersistentPagesChild(child)) continue;
            child.gameObject.SetActive(false);
        }
    }

    private void ActivatePageChain(GameObject page)
    {
        if (page == null) return;

        if (pagesRoot == null)
        {
            page.SetActive(true);
            return;
        }

        var current = page.transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            if (current == pagesRoot) break;
            current = current.parent;
        }
    }

    private void ResolvePagesRoot()
    {
        if (pagesRoot != null) return;

        Transform sharedParent = null;
        var hubs = new[] { dishesHubPage, wordsHubPage, peopleHubPage };

        foreach (var hub in hubs)
        {
            if (hub == null) continue;

            if (sharedParent == null)
            {
                sharedParent = hub.transform.parent;
                continue;
            }

            if (hub.transform.parent != sharedParent)
                return;
        }

        pagesRoot = sharedParent;
    }

    private void EnsureTabButtonsReceiveClicks()
    {
        EnsureButtonGraphic(dishesTabButton);
        EnsureButtonGraphic(wordsTabButton);
        EnsureButtonGraphic(peopleTabButton);
    }

    private void EnsureButtonGraphic(Button button)
    {
        if (button == null) return;

        var graphic = button.GetComponent<Graphic>();
        if (graphic == null)
        {
            var image = button.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            graphic = image;
        }
        else
        {
            graphic.raycastTarget = true;
        }

        if (button.targetGraphic == null)
            button.targetGraphic = graphic;
    }

    private void BringOverlayControlsToFront()
    {
        Transform overlayRoot = cookBookRoot != null ? cookBookRoot.transform : null;

        BringRootToFront(closeButton != null ? closeButton.transform : null, overlayRoot);
        BringRootToFront(dishesTabButton != null ? dishesTabButton.transform : null, overlayRoot);
        BringRootToFront(helpBlockerButton != null ? helpBlockerButton.transform : null, overlayRoot);
        BringRootToFront(helpPopupPanel != null ? helpPopupPanel.transform : null, overlayRoot);

        BringRootToFront(leftArrowButton != null ? leftArrowButton.transform : null, pagesRoot);
        BringRootToFront(rightArrowButton != null ? rightArrowButton.transform : null, pagesRoot);
    }

    private void BringRootToFront(Transform target, Transform parentRoot)
    {
        Transform root = GetRootBelowParent(target, parentRoot);
        if (root != null)
            root.SetAsLastSibling();
    }

    private Transform GetRootBelowParent(Transform target, Transform parentRoot)
    {
        if (target == null) return null;
        if (parentRoot == null) return target;

        Transform root = target;
        while (root.parent != null && root.parent != parentRoot)
            root = root.parent;

        return root;
    }

    private bool IsPersistentPagesChild(Transform child)
    {
        if (child == null) return false;

        Transform leftRoot = GetRootBelowParent(leftArrowButton != null ? leftArrowButton.transform : null, pagesRoot);
        if (leftRoot == child) return true;

        Transform rightRoot = GetRootBelowParent(rightArrowButton != null ? rightArrowButton.transform : null, pagesRoot);
        return rightRoot == child;
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
