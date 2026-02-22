using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour
{
    public System.Action<int> OnDialogueIndexChanged;

    [Header("Dialogue Data")]
    public NPCDialogue dialogueData;

    [Header("Dialogue Root")]
    public GameObject dialogueRoot;
    public TMP_Text dialogueText;

    [Header("Name Frames")]
    public GameObject leftNameFrame;
    public TMP_Text leftNameText;

    public GameObject rightNameFrame;
    public TMP_Text rightNameText;

    [Header("NPC Portraits")]
    public Image momPortrait;
    public GameObject momPortraitObj;

    public Image grannyPortrait;
    public GameObject grannyPortraitObj;

    [Header("Speaking Bump")]
    public float bumpAmount = 20f;
    public float bumpSpeed = 12f;

    // state
    public int dialogueIndex;
    public bool isTyping;
    public bool isDialogueActive;
    public bool runNextLine;

    private DialogueLine[] currentDialogueLines;
    private DialogueLine[] sourceLines;

    // speaker data
    private readonly Dictionary<string, SpeakerDefinition> speakers = new();
    private readonly HashSet<string> introduced = new();

    private const string MC = "mc";
    private const string MOM = "mom";
    private const string GRANNY = "granny";

    private string currentFullText = "";

    private RectTransform momRT;
    private RectTransform grannyRT;
    private Vector2 momBasePos;
    private Vector2 grannyBasePos;

    private int dialogueBoxClickCount = 0;
    public int DialogueBoxClickCount => dialogueBoxClickCount;

    void Awake()
    {
        BuildSpeakerLookup();
        CachePortraitPositions();
    }

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;

        if (dialogueData != null && (scene == "PrologueScene" || scene == "RestaurantScene"))
        {
            StartDialogue();
        }
    }

    void BuildSpeakerLookup()
    {
        speakers.Clear();
        if (dialogueData == null || dialogueData.speakers == null) return;

        foreach (var s in dialogueData.speakers)
        {
            if (!string.IsNullOrWhiteSpace(s.id))
                speakers[s.id] = s;
        }
    }

    void CachePortraitPositions()
    {
        if (momPortrait != null)
        {
            momRT = momPortrait.GetComponent<RectTransform>();
            momBasePos = momRT.anchoredPosition;
        }

        if (grannyPortrait != null)
        {
            grannyRT = grannyPortrait.GetComponent<RectTransform>();
            grannyBasePos = grannyRT.anchoredPosition;
        }
    }

    public void Interact()
    {
        if (dialogueData == null) return;

        if (!isDialogueActive)
            StartDialogue();
        else
            OnDialogueBoxClicked();
    }

    public void OnDialogueBoxClicked()
    {
        if (!isDialogueActive) return;

        dialogueBoxClickCount++;

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(currentFullText);
            isTyping = false;
            return;
        }

        if (!runNextLine) return;

        NextLine();
    }

    void StartDialogue()
    {
        BuildSpeakerLookup();
        CachePortraitPositions();

        isDialogueActive = true;
        runNextLine = true;
        dialogueIndex = 0;

        introduced.Clear();
        HideNPCPortraits();

        if (SceneManager.GetActiveScene().name == "PrologueScene")
            sourceLines = dialogueData.prologueLines;
        else if (SceneManager.GetActiveScene().name == "RestaurantScene")
            sourceLines = dialogueData.restaurantLines;
        else
            sourceLines = new DialogueLine[0];

        currentDialogueLines = new DialogueLine[sourceLines.Length];
        for (int i = 0; i < sourceLines.Length; i++)
        {
            currentDialogueLines[i] = new DialogueLine
            {
                speakerId = sourceLines[i].speakerId,
                text = sourceLines[i].text
            };
        }

        dialogueRoot.SetActive(true);
        StartCoroutine(TypeLine());
    }

    void HideNPCPortraits()
    {
        if (momPortraitObj != null) momPortraitObj.SetActive(false);
        if (grannyPortraitObj != null) grannyPortraitObj.SetActive(false);

        ResetPortraitPositions();
    }

    void ResetPortraitPositions()
    {
        if (momRT != null) momRT.anchoredPosition = momBasePos;
        if (grannyRT != null) grannyRT.anchoredPosition = grannyBasePos;
    }

    void IntroduceIfNeeded(string speakerId)
    {
        if (introduced.Contains(speakerId)) return;
        introduced.Add(speakerId);

        if (speakerId == MOM && momPortraitObj != null)
            momPortraitObj.SetActive(true);

        if (speakerId == GRANNY && grannyPortraitObj != null)
            grannyPortraitObj.SetActive(true);
    }

    void ApplyNameFrame(string speakerId)
    {
        string displayName = speakers.TryGetValue(speakerId, out var s)
            ? s.displayName
            : speakerId;

        bool left = speakerId == MC;

        leftNameFrame.SetActive(left);
        rightNameFrame.SetActive(!left);

        if (left)
            leftNameText.SetText(displayName);
        else
            rightNameText.SetText(displayName);
    }

    void ApplyPortraitBump(string speakerId)
    {
        ResetPortraitPositions();

        if (speakerId == MOM && momRT != null)
            StartCoroutine(BumpTo(momRT, momBasePos + Vector2.up * bumpAmount));

        if (speakerId == GRANNY && grannyRT != null)
            StartCoroutine(BumpTo(grannyRT, grannyBasePos + Vector2.up * bumpAmount));
    }

    IEnumerator BumpTo(RectTransform rt, Vector2 target)
    {
        while (Vector2.Distance(rt.anchoredPosition, target) > 0.5f)
        {
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, target, Time.deltaTime * bumpSpeed);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    void NextLine()
    {
        dialogueIndex++;

        if (dialogueIndex < currentDialogueLines.Length)
        {
            OnDialogueIndexChanged?.Invoke(dialogueIndex);
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        var line = currentDialogueLines[dialogueIndex];
        IntroduceIfNeeded(line.speakerId);
        ApplyNameFrame(line.speakerId);
        ApplyPortraitBump(line.speakerId);

        currentFullText = line.text;

        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        DialogueButtons buttons = FindObjectOfType<DialogueButtons>();
        bool hasChoices = dialogueButtons != null && dialogueButtons.SetTextButton(dialogueIndex);
        runNextLine = !(buttons != null && buttons.AnyButtonActive());
    }

    public IEnumerator PlayAtIndex(int index)
    {
        StopAllCoroutines();
        dialogueIndex = index;
        StartCoroutine(TypeLine());
        yield return null;
        runNextLine = false;
    }

    public void ResumeAfterClick(int nextIndex)
    {
        dialogueIndex = nextIndex;
        StartCoroutine(TypeLine());
    }

    void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        runNextLine = false;
        isTyping = false;

        dialogueText.SetText("");
        dialogueRoot.SetActive(false);

        if (SceneManager.GetActiveScene().name == "PrologueScene")
            SceneManager.LoadScene("RestaurantScene");
    }
}