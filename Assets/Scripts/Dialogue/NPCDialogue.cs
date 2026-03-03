using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    [Header("Scenes / Scripts")]
    public DialogueLine[] prologueLines;

    [Header("Restaurant Sequences")]
    public DialogueLine[] restaurantIntro1Lines;
    public DialogueLine[] restaurantMomConvo1Lines;
    public DialogueLine[] restaurantIntro2Lines;
    public DialogueLine[] restaurantIntro3Lines;

    [Header("Speakers (define MC + other characters here)")]
    public SpeakerDefinition[] speakers;

    [Header("Choices (PER SEQUENCE, indexed by line index within that sequence)")]
    public DialogueChoice[] prologueChoices;
    public DialogueChoice[] restaurantIntro1Choices;
    public DialogueChoice[] restaurantMomConvo1Choices;
    public DialogueChoice[] restaurantIntro2Choices;
    public DialogueChoice[] restaurantIntro3Choices;

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
    [Tooltip("Unique ID used by DialogueLine.speakerId")]
    public string id;

    public string displayName;
    public Sprite portrait;

    [Tooltip("MC should be Left. Other characters can default Left or Right.")]
    public SpeakerSide defaultSide = SpeakerSide.Left;
}

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Matches a SpeakerDefinition.id")]
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