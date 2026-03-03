using UnityEngine;
using UnityEngine.SceneManagement;

public class RestaurantFlowController : MonoBehaviour
{
    public NPC restaurantNPC;
    public NPC conversationNPC;

    public GameObject restaurantViewRoot;
    public GameObject conversationViewRoot;
    public GameObject momSprite;

    public GameObject doorHighlight;
    public GameObject momHighlight;

    public GameObject kitchenButton;
    public GameObject mapButton;

    public string mapSceneName = "MapScene";

    private bool momConvoFinished = false;
    private bool kitchenUnlocked = false;

    void Start()
    {
        ShowRestaurantView();

        if (doorHighlight != null) doorHighlight.SetActive(false);
        if (momHighlight != null) momHighlight.SetActive(false);
        if (momSprite != null) momSprite.SetActive(false);

        if (kitchenButton != null) kitchenButton.SetActive(false);
        if (mapButton != null) mapButton.SetActive(false);

        if (restaurantNPC != null)
            restaurantNPC.OnDialogueIndexChanged += HandleIntro1Index;

        if (conversationNPC != null)
            conversationNPC.OnSequenceEnded += HandleConvoEnded;

        if (restaurantNPC != null)
            restaurantNPC.PlaySequence(DialogueSequence.RestaurantIntro1, true, "RestaurantIntro1Played");
    }

    void OnDestroy()
    {
        if (restaurantNPC != null)
            restaurantNPC.OnDialogueIndexChanged -= HandleIntro1Index;

        if (conversationNPC != null)
            conversationNPC.OnSequenceEnded -= HandleConvoEnded;
    }

    private void HandleIntro1Index(int index)
    {
        if (index == 1)
        {
            if (doorHighlight != null) doorHighlight.SetActive(true);
            if (momHighlight != null) momHighlight.SetActive(false);

            if (!kitchenUnlocked && kitchenButton != null) kitchenButton.SetActive(false);
            if (!momConvoFinished && mapButton != null) mapButton.SetActive(false);

            restaurantNPC.SetExternalPause(true);
        }
    }

    public void OnDoorClicked()
    {
        if (momConvoFinished)
        {
            if (kitchenUnlocked && kitchenButton != null) kitchenButton.SetActive(true);
            if (mapButton != null) mapButton.SetActive(true);

            SceneManager.LoadScene(mapSceneName);
            return;
        }

        if (doorHighlight != null) doorHighlight.SetActive(false);

        if (momSprite != null) momSprite.SetActive(true);
        if (momHighlight != null) momHighlight.SetActive(true);

        if (restaurantNPC != null)
        {
            restaurantNPC.SetExternalPause(false);
            restaurantNPC.ResumeAfterClick(2);
        }
    }

    public void OnMomClicked()
    {
        if (momHighlight != null) momHighlight.SetActive(false);

        if (restaurantNPC != null)
            restaurantNPC.SetExternalPause(true);

        kitchenUnlocked = true;
        if (kitchenButton != null) kitchenButton.SetActive(true);

        Time.timeScale = 1f;
        ShowConversationView();

        if (conversationNPC != null)
        {
            if (conversationNPC.dialogueRoot != null)
                conversationNPC.dialogueRoot.SetActive(true);

            conversationNPC.PlaySequence(DialogueSequence.RestaurantMomConvo1, false, "");
        }
    }

    private void HandleConvoEnded(DialogueSequence seq)
    {
        if (seq != DialogueSequence.RestaurantMomConvo1) return;

        momConvoFinished = true;

        ShowRestaurantView();

        if (doorHighlight != null) doorHighlight.SetActive(true);
        if (mapButton != null) mapButton.SetActive(true);

        if (kitchenUnlocked && kitchenButton != null) kitchenButton.SetActive(true);

        if (restaurantNPC != null)
            restaurantNPC.SetExternalPause(true);
    }

    private void ShowRestaurantView()
    {
        if (restaurantViewRoot != null) restaurantViewRoot.SetActive(true);
        if (conversationViewRoot != null) conversationViewRoot.SetActive(false);

        if (kitchenUnlocked && kitchenButton != null) kitchenButton.SetActive(true);
    }

    private void ShowConversationView()
    {
        if (restaurantViewRoot != null) restaurantViewRoot.SetActive(false);
        if (conversationViewRoot != null) conversationViewRoot.SetActive(true);

        if (kitchenUnlocked && kitchenButton != null) kitchenButton.SetActive(true);
    }
}