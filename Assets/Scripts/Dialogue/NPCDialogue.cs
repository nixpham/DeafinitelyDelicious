using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public DialogueLine[] prologueLines;

    public DialogueLine[] restaurantIntro1Lines;
    public DialogueLine[] restaurantMomConvo1Lines;
    public DialogueLine[] restaurantMomReminder1Lines;

    public DialogueLine[] grandmasHouse1Lines;
    public DialogueLine[] grandmasHouseGrandmaReminderLines;

    public DialogueLine[] restaurantIntro2Lines;
    public DialogueLine[] restaurantMomReminder2Lines;
    public DialogueLine[] restaurantGrandmaReminder2Lines;

    public DialogueLine[] kitchenTutorialLines;
    public DialogueLine[] restaurantIntro3Lines;

    public DialogueLine[] restaurantIntroBefore3Lines;
    public DialogueLine[] restaurantMomReminder3Lines;
    public DialogueLine[] restaurantGrandmaReminder3Lines;
    public DialogueLine[] momGrandmaGrilledCheeseDoneLines;

    public DialogueLine[] mapEndDemoLines;

    [Header("Speakers (define MC + other characters here)")]
    public SpeakerDefinition[] speakers;

    [Header("Choices (PER SEQUENCE, indexed by line index within that sequence)")]
    public DialogueChoice[] restaurantMomConvo1Choices;

    [Header("Typing")]
    public float typingSpeed = 0.05f;
}

public enum SpeakerSide
{
    Left,
    Right
}

[System.Serializable]
public class SpeakerDefinition
{
    public string id;
    public string displayName;
    public Sprite portrait;
    public SpeakerSide defaultSide = SpeakerSide.Left;
}

[System.Serializable]
public class DialogueLine
{
    public string speakerId;
    [TextArea] public string text;
}

[System.Serializable]
public class DialogueOption
{
    public string text;
    public int nextLineIndex;
    public int nextNextLineIndex;
}

[System.Serializable]
public class DialogueChoice
{
    public DialogueOption[] options;
}