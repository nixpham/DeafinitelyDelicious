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

    [Serializable]
    public class IngredientStudyButton
    {
        public string signName;
        public Button ingredientButton;
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

    [Header("Nav Arrows")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Recipes")]
    [SerializeField] private List<RecipeSection> recipes = new();

    [Header("Help Popup (modal)")]
    [SerializeField] private Button helpButton;
    [SerializeField] private GameObject helpPopupPanel;
    [SerializeField] private Button helpBlockerButton;

    [Header("Study Session")]
    [SerializeField] private StudySessionPopup studySessionPopup;
    [SerializeField] private List<IngredientStudyButton> ingredientStudyButtons = new();

    private Tab currentTab = Tab.Dishes;

    private List<GameObject> currentSectionPages = new();
    private bool inRecipeSection = false;
    private int pageIndex = 0;

    private void Awake()
    {
        ResolvePagesRoot();
        EnsureTabButtonsReceiveClicks();

        if (cookBookRoot != null)
            cookBookRoot.SetActive(false);

        if (helpPopupPanel != null)
            helpPopupPanel.SetActive(false);

        if (helpBlockerButton != null)
            helpBlockerButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (openCookbookButton != null)
            openCookbookButton.onClick.AddListener(OpenCookBook);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCookBook);

        if (dishesTabButton != null)
            dishesTabButton.onClick.AddListener(() => SwitchTab(Tab.Dishes));

        if (wordsTabButton != null)
            wordsTabButton.onClick.AddListener(() => SwitchTab(Tab.Words));

        if (peopleTabButton != null)
            peopleTabButton.onClick.AddListener(() => SwitchTab(Tab.People));

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(OnLeftArrow);

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(OnRightArrow);

        if (helpButton != null)
            helpButton.onClick.AddListener(OpenHelp);

        if (helpBlockerButton != null)
            helpBlockerButton.onClick.AddListener(CloseHelp);

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.openButton == null)
                continue;

            string recipeNameCopy = recipe.recipeName;
            recipe.openButton.onClick.AddListener(() => OpenRecipe(recipeNameCopy));
        }

        foreach (var ingredient in ingredientStudyButtons)
        {
            if (ingredient == null || ingredient.ingredientButton == null)
                continue;

            string signNameCopy = ingredient.signName;
            ingredient.ingredientButton.onClick.AddListener(() => OpenStudySession(signNameCopy));
        }

        SetToHub(Tab.Dishes);
        CloseHelp();
    }

    public void OpenCookBook()
    {
        if (cookBookRoot == null)
            return;

        PlayPageFlip();

        cookBookRoot.SetActive(true);
        CookBookOpen = true;

        SwitchTab(Tab.Dishes);
        BringOverlayControlsToFront();
    }

    public void CloseCookBook()
    {
        if (cookBookRoot == null)
            return;

        PlayPageFlip();

        CloseHelp();
        cookBookRoot.SetActive(false);
        CookBookOpen = false;
    }

    private void SwitchTab(Tab tab)
    {
        if (IsHelpOpen())
            return;

        currentTab = tab;
        SetToHub(tab);
    }

    private void SetToHub(Tab tab)
    {
        inRecipeSection = false;
        pageIndex = 0;
        currentSectionPages.Clear();

        HideAllPageObjects();

        GameObject hub = tab switch
        {
            Tab.Dishes => dishesHubPage,
            Tab.Words => wordsHubPage,
            Tab.People => peopleHubPage,
            _ => dishesHubPage
        };

        if (hub != null)
        {
            ActivatePageChain(hub);
            currentSectionPages.Add(hub);
        }

        BringOverlayControlsToFront();
        UpdateNav();
    }

    public void OpenRecipe(string recipeName)
    {
        if (IsHelpOpen())
            return;

        if (currentTab != Tab.Dishes)
            return;

        RecipeSection recipe = recipes.Find(r => r.recipeName == recipeName);
        if (recipe == null || recipe.pages == null || recipe.pages.Count == 0)
            return;

        inRecipeSection = false;
        pageIndex = 0;
        currentSectionPages.Clear();

        HideAllPageObjects();

        inRecipeSection = true;
        currentSectionPages = new List<GameObject>(recipe.pages);
        pageIndex = 0;

        PlayPageFlip();

        if (currentSectionPages[0] != null)
            ActivatePageChain(currentSectionPages[0]);

        BringOverlayControlsToFront();
        UpdateNav();
    }

    private void OpenStudySession(string signName)
    {
        if (studySessionPopup == null)
            return;

        studySessionPopup.OpenSingleSign(signName);
    }

    private void OnLeftArrow()
    {
        if (IsHelpOpen())
            return;

        if (inRecipeSection && pageIndex == 0)
        {
            PlayPageFlip();
            SetToHub(Tab.Dishes);
            return;
        }

        if (pageIndex > 0)
            SetPage(pageIndex - 1);
    }

    private void OnRightArrow()
    {
        if (IsHelpOpen())
            return;

        if (pageIndex < currentSectionPages.Count - 1)
            SetPage(pageIndex + 1);
    }

    private void SetPage(int newIndex)
    {
        newIndex = Mathf.Clamp(newIndex, 0, Mathf.Max(0, currentSectionPages.Count - 1));

        if (newIndex == pageIndex)
            return;

        PlayPageFlip();

        pageIndex = newIndex;

        HideAllPageObjects();

        if (currentSectionPages[pageIndex] != null)
            ActivatePageChain(currentSectionPages[pageIndex]);

        BringOverlayControlsToFront();
        UpdateNav();
    }

    private void UpdateNav()
    {
        bool showLeft = false;
        bool showRight = false;

        if (inRecipeSection)
        {
            showLeft = true;
            showRight = pageIndex < currentSectionPages.Count - 1;
        }

        if (leftArrowButton != null)
            leftArrowButton.gameObject.SetActive(showLeft);

        if (rightArrowButton != null)
            rightArrowButton.gameObject.SetActive(showRight);
    }

    private void HideAllPages()
    {
        if (dishesHubPage != null)
            dishesHubPage.SetActive(false);

        if (wordsHubPage != null)
            wordsHubPage.SetActive(false);

        if (peopleHubPage != null)
            peopleHubPage.SetActive(false);

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.pages == null)
                continue;

            foreach (var page in recipe.pages)
            {
                if (page != null)
                    page.SetActive(false);
            }
        }
    }

    private void HideAllPageObjects()
    {
        HideAllPages();

        if (pagesRoot == null)
            return;

        for (int i = 0; i < pagesRoot.childCount; i++)
        {
            Transform child = pagesRoot.GetChild(i);

            if (IsPersistentPagesChild(child))
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private void ActivatePageChain(GameObject page)
    {
        if (page == null)
            return;

        if (pagesRoot == null)
        {
            page.SetActive(true);
            return;
        }

        Transform current = page.transform;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current == pagesRoot)
                break;

            current = current.parent;
        }
    }

    private void ResolvePagesRoot()
    {
        if (pagesRoot != null)
            return;

        Transform sharedParent = null;
        GameObject[] hubs = { dishesHubPage, wordsHubPage, peopleHubPage };

        foreach (GameObject hub in hubs)
        {
            if (hub == null)
                continue;

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
        if (button == null)
            return;

        Graphic graphic = button.GetComponent<Graphic>();

        if (graphic == null)
        {
            Image image = button.gameObject.AddComponent<Image>();
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
        BringRootToFront(wordsTabButton != null ? wordsTabButton.transform : null, overlayRoot);
        BringRootToFront(peopleTabButton != null ? peopleTabButton.transform : null, overlayRoot);
        BringRootToFront(helpButton != null ? helpButton.transform : null, overlayRoot);
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
        if (target == null)
            return null;

        if (parentRoot == null)
            return target;

        Transform root = target;

        while (root.parent != null && root.parent != parentRoot)
            root = root.parent;

        return root;
    }

    private bool IsPersistentPagesChild(Transform child)
    {
        if (child == null)
            return false;

        Transform leftRoot = GetRootBelowParent(leftArrowButton != null ? leftArrowButton.transform : null, pagesRoot);
        if (leftRoot == child)
            return true;

        Transform rightRoot = GetRootBelowParent(rightArrowButton != null ? rightArrowButton.transform : null, pagesRoot);
        if (rightRoot == child)
            return true;

        return false;
    }

    private void OpenHelp()
    {
        if (helpPopupPanel != null)
            helpPopupPanel.SetActive(true);

        if (helpBlockerButton != null)
            helpBlockerButton.gameObject.SetActive(true);

        BringOverlayControlsToFront();
    }

    private void CloseHelp()
    {
        if (helpPopupPanel != null)
            helpPopupPanel.SetActive(false);

        if (helpBlockerButton != null)
            helpBlockerButton.gameObject.SetActive(false);
    }

    private bool IsHelpOpen()
    {
        return helpPopupPanel != null && helpPopupPanel.activeSelf;
    }

    private void PlayPageFlip()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(GameAudioPaths.UiPageFlip, 0.75f);
    }
}