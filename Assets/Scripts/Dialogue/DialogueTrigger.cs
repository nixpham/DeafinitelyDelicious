using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Conversation Runner")]
    public NPC conversationNPC;

    [Header("Sequence to play")]
    public DialogueSequence sequenceToPlay = DialogueSequence.RestaurantMomConvo1;

    [Header("Switch views")]
    public GameObject restaurantViewRoot;
    public GameObject conversationViewRoot;

    [Header("Optional: play once")]
    public bool playOnce = false;
    public string playOnceId = "";

    public void TriggerConversation()
    {
        if (restaurantViewRoot != null) restaurantViewRoot.SetActive(false);
        if (conversationViewRoot != null) conversationViewRoot.SetActive(true);

        if (conversationNPC != null)
            conversationNPC.PlaySequence(sequenceToPlay, playOnce, playOnceId);
    }
}