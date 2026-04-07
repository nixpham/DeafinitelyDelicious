using UnityEngine;
using UnityEngine.UI;

public class TutorialSceneRefs : MonoBehaviour
{
    public enum SceneKind
    {
        Prologue,
        Restaurant,
        GrandmasHouse,
        Kitchen
    }

    [Header("Which scene is this?")]
    public SceneKind sceneKind;

    [Header("Dialogue Drivers")]
    public NPC restaurantNPC;
    public NPC conversationNPC;

    [Header("Scene Roots / Views")]
    public GameObject restaurantViewRoot;
    public GameObject conversationViewRoot;

    [Header("Restaurant UI")]
    public Button doorButton;
    public GameObject doorHighlight;

    public GameObject momSprite;
    public Button momButton;
    public GameObject momHighlight;

    public GameObject restaurantGrandmaSprite;
    public Button restaurantGrandmaButton;
    public GameObject restaurantGrandmaHighlight;

    public Button mapButton;
    public Button kitchenButton;

    [Header("Grandma House UI")]
    public GameObject grandmaHouseSprite;
    public Button backButton;

    [Header("Cookbook Flow")]
    public Button cookbookButton;
    public GameObject cookbookHighlight;
    public GameObject cookbookPanel;

    public Button grilledCheeseButton;
    public GameObject grilledCheeseHighlight;
    public GameObject grilledCheesePage1;

    public Button breadButton;
    public GameObject breadHighlight;

    public GameObject studySessionRoot;

    [Header("Signing Flow")]
    public Button signButton;
    public GameObject signEngineRoot;

    [Header("Optional Popup")]
    public GameObject giftPopup;

    private void Start()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RegisterScene(this);
        }
    }
}