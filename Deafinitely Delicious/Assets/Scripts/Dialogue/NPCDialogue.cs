using UnityEngine;
[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;
    public string[] prologueLines;
    public string[] restaurantLines;
    public DialogueChoice[] choices;

    public float typingSpeed = 0.05f;
    public bool[] autoProgressLines;
    public float autoProgressLinesDelay = 1.5f;
}

[System.Serializable]
public class DialogueOption
{
    public string text;
    public int nextLineIndex;
}

[System.Serializable]
public class DialogueChoice
{
    public DialogueOption[] options;
}

