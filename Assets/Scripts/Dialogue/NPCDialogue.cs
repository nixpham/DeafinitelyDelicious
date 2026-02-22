using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    [Header("Scenes / Scripts")]
    public DialogueLine[] prologueLines;
    public DialogueLine[] restaurantLines;

    [Header("Speakers (define MC + other characters here)")]
    public SpeakerDefinition[] speakers;

    [Header("Choices (indexed by dialogue line index)")]
    public DialogueChoice[] choices;

    [Header("Typing")]
    public float typingSpeed = 0.05f;

    [Header("Optional Auto Progress (unused in this rewrite, kept for future)")]
    public bool[] autoProgressLines;
    public float autoProgressLinesDelay = 1.5f;
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
