using UnityEngine;

public class HighlightObject : MonoBehaviour
{
    [Header("References")]
    public NPC npcScript;

    public GameObject doorHighlight;
    public GameObject momHighlight;

    [Header("Intro1 Indices (sequence-local)")]
    public int pauseAtDoorIndex = 1;
    public int resumeAfterDoorIndex = 2;

    public int pauseAtMomIndex = 3;
    public int resumeAfterMomIndex = 4;

    void Start()
    {
        if (npcScript != null)
            npcScript.OnDialogueIndexChanged += HandleDialogueIndexChanged;
    }

    void OnDestroy()
    {
        if (npcScript != null)
            npcScript.OnDialogueIndexChanged -= HandleDialogueIndexChanged;
    }

    private void HandleDialogueIndexChanged(int index)
    {
        if (npcScript == null) return;

        // Pause + highlight door
        if (index == pauseAtDoorIndex)
        {
            if (doorHighlight != null) doorHighlight.SetActive(true);
            if (momHighlight != null) momHighlight.SetActive(false);

            npcScript.SetExternalPause(true);
        }

        // Pause + highlight mom
        if (index == pauseAtMomIndex)
        {
            if (doorHighlight != null) doorHighlight.SetActive(false);
            if (momHighlight != null) momHighlight.SetActive(true);

            npcScript.SetExternalPause(true);
        }
    }

    public void OnDoorClicked()
    {
        if (doorHighlight != null) doorHighlight.SetActive(false);

        npcScript.SetExternalPause(false);
        npcScript.ResumeAfterClick(resumeAfterDoorIndex);
    }

    public void OnMomClicked()
    {
        if (momHighlight != null) momHighlight.SetActive(false);

        npcScript.SetExternalPause(false);
        npcScript.ResumeAfterClick(resumeAfterMomIndex);
    }
}