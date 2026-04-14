using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum DialogueSequence
{
    Prologue,
    RestaurantIntro1,
    RestaurantMomConvo1,
    RestaurantMomReminder1,
    GrandmasHouse1,
    GrandmasHouseGrandmaReminder,
    RestaurantIntro2,
    RestaurantMomReminder2,
    RestaurantGrandmaReminder2,
    KitchenTutorial,
    RestaurantIntroBefore3,
    RestaurantIntro3,

    RestaurantMomReminder3,
    RestaurantGrandmaReminder3,
    MomGrandmaGrilledCheeseDone,

    MapEndDemo
}

public class NPC : MonoBehaviour
{
    // =========================
    // GLOBAL DIALOGUE LOCK
    // =========================
    public static System.Action<bool> OnAnyDialogueActiveChanged;
    private static int activeDialogueCount = 0;
    public static bool AnyDialogueActive => activeDialogueCount > 0;

    private static void NotifyDialogueStarted()
    {
        activeDialogueCount++;
        if (activeDialogueCount == 1)
            OnAnyDialogueActiveChanged?.Invoke(true);
    }

    private static void NotifyDialogueEnded()
    {
        activeDialogueCount = Mathf.Max(0, activeDialogueCount - 1);
        if (activeDialogueCount == 0)
            OnAnyDialogueActiveChanged?.Invoke(false);
    }

    // =========================
    // EVENTS
    // =========================
    public System.Action<int> OnDialogueIndexChanged;
    public System.Action<DialogueSequence> OnSequenceEnded;

    [Header("Dialogue Data")]
    public NPCDialogue dialogueData;

    [Header("Sequence Selection")]
    public DialogueSequence sequenceToPlay = DialogueSequence.Prologue;

    [Header("Dialogue Root")]
    public GameObject dialogueRoot;
    public TMP_Text dialogueText;

    [Header("Name Frames")]
    public GameObject leftNameFrame;
    public TMP_Text leftNameText;
    public GameObject rightNameFrame;
    public TMP_Text rightNameText;

    [Header("Portrait Objects")]
    public GameObject momPortraitObj;
    public GameObject grannyPortraitObj;

    [Header("Portrait Images (for bump)")]
    public Image momPortrait;
    public Image grannyPortrait;
    public float bumpAmount = 20f;
    public float bumpSpeed = 12f;

    [Header("Auto Start / Play Once")]
    public bool autoStartInThisScene = true;
    public bool playOnce = false;
    public string playOnceId = "";

    [Header("Scene Flow (optional)")]
    [Tooltip("If true, NPC will auto-load restaurant after Prologue sequence ends. Recommended OFF if TutorialManager controls scenes.")]
    public bool autoLoadRestaurantAfterPrologue = false;
    public string restaurantSceneName = "RestaurantScene";

    [Header("Text Tokens")]
    public bool enableTextTokens = true;

    [SerializeField] private DialogueButtons dialogueButtons;

    // =========================
    // INTERNAL STATE
    // =========================
    public int dialogueIndex;
    public bool isTyping;
    public bool isDialogueActive;
    public bool runNextLine;

    private int dialogueBoxClickCount = 0;
    public int DialogueBoxClickCount => dialogueBoxClickCount;

    private DialogueLine[] currentDialogueLines;
    private DialogueChoice[] currentChoices;

    private readonly Dictionary<string, SpeakerDefinition> speakers = new();
    private readonly HashSet<string> introduced = new();

    private string currentFullText = "";
    private bool lastLineHadChoices = false;

    private RectTransform momRT;
    private RectTransform grannyRT;
    private Vector2 momBasePos;
    private Vector2 grannyBasePos;

    private bool externallyPaused = false;

    private bool hasQueuedNextIndex = false;
    private int queuedNextIndex = -1;

    private const string MOM_RESPONSE_KEY = "MOM_RESPONSE";

    public void QueueNextLineIndex(int index)
    {
        hasQueuedNextIndex = true;
        queuedNextIndex = index;
    }

    // =========================
    // UNITY
    // =========================
    private void Awake()
    {
        BuildSpeakerLookup();
        CachePortraitPositions();
    }

    private void Start()
    {
        if (dialogueData == null) return;
        if (!autoStartInThisScene) return;

        if (playOnce && !string.IsNullOrWhiteSpace(playOnceId))
        {
            if (PlayerPrefs.GetInt(playOnceId, 0) == 1)
                return;
        }

        string scene = SceneManager.GetActiveScene().name;

        bool shouldStart =
            (scene == "PrologueScene" && sequenceToPlay == DialogueSequence.Prologue) ||
            (scene == "RestaurantScene" && sequenceToPlay != DialogueSequence.Prologue);

        if (shouldStart)
            StartDialogue();
    }

    // =========================
    // EXTERNAL CONTROL
    // =========================
    public void PlaySequence(DialogueSequence seq, bool usePlayOnce = false, string key = "")
    {
        sequenceToPlay = seq;
        playOnce = usePlayOnce;
        playOnceId = key;
        Debug.Log(gameObject.name + " is playing sequence: " + seq);
        StartDialogue();
    }

    public void SetExternalPause(bool paused)
    {
        externallyPaused = paused;
        runNextLine = (!externallyPaused && !lastLineHadChoices && !isTyping);
    }

    public IEnumerator PlayAtIndex(int index)
    {
        StopAllCoroutines();
        dialogueIndex = index;
        yield return StartCoroutine(TypeLine());
    }

    public void ResumeAfterClick(int nextIndex)
    {
        StopAllCoroutines();
        dialogueIndex = nextIndex;
        StartCoroutine(TypeLine());
    }

    public void OnDialogueBoxClicked()
    {
        if (!isDialogueActive) return;

        dialogueBoxClickCount++;

        if (isTyping)
        {
            StopAllCoroutines();

            if (dialogueText != null)
                dialogueText.SetText(currentFullText);

            isTyping = false;
            runNextLine = (!externallyPaused && !lastLineHadChoices);
            return;
        }

        if (!runNextLine) return;

        NextLine();
    }

    // =========================
    // CORE FLOW
    // =========================
    private void StartDialogue()
    {
        if (dialogueData == null) return;

        if (playOnce && !string.IsNullOrWhiteSpace(playOnceId))
        {
            if (PlayerPrefs.GetInt(playOnceId, 0) == 1)
                return;
        }

        currentDialogueLines = GetLinesForSequence(sequenceToPlay);
        currentChoices = GetChoicesForSequence(sequenceToPlay);

        if (currentDialogueLines == null || currentDialogueLines.Length == 0)
        {
            OnSequenceEnded?.Invoke(sequenceToPlay);
            return;
        }

        NotifyDialogueStarted();

        introduced.Clear();
        HideNPCPortraits();

        externallyPaused = false;
        isDialogueActive = true;
        runNextLine = true;
        dialogueIndex = 0;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        OnDialogueIndexChanged?.Invoke(dialogueIndex);

        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    private void NextLine()
    {
        if (hasQueuedNextIndex)
        {
            dialogueIndex = queuedNextIndex;
            hasQueuedNextIndex = false;
            queuedNextIndex = -1;
        }
        else
        {
            dialogueIndex++;
        }

        if (currentDialogueLines != null && dialogueIndex < currentDialogueLines.Length)
        {
            OnDialogueIndexChanged?.Invoke(dialogueIndex);
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.SetText("");

        var line = currentDialogueLines[dialogueIndex];

        IntroduceIfNeeded(line.speakerId);
        ApplyNameFrame(line.speakerId);
        ApplyPortraitBump(line.speakerId);

        currentFullText = ResolveDialogueTokens(line.text ?? "");

        if (dialogueButtons != null)
        {
            dialogueButtons.SetChoicesSource(currentChoices);
            lastLineHadChoices = dialogueButtons.SetTextButton(dialogueIndex);
        }
        else
        {
            lastLineHadChoices = false;
        }

        runNextLine = (!externallyPaused && !lastLineHadChoices);

        for (int i = 0; i < currentFullText.Length; i++)
        {
            if (dialogueText != null)
                dialogueText.text += currentFullText[i];

            yield return new WaitForSecondsRealtime(dialogueData.typingSpeed);
        }

        isTyping = false;
        runNextLine = (!externallyPaused && !lastLineHadChoices);
    }

    private void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        runNextLine = false;
        isTyping = false;
        externallyPaused = false;

        if (dialogueText != null)
            dialogueText.SetText("");

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (playOnce && !string.IsNullOrWhiteSpace(playOnceId))
        {
            PlayerPrefs.SetInt(playOnceId, 1);
            PlayerPrefs.Save();
        }

        NotifyDialogueEnded();

        OnSequenceEnded?.Invoke(sequenceToPlay);

        if (autoLoadRestaurantAfterPrologue &&
            SceneManager.GetActiveScene().name == "PrologueScene" &&
            sequenceToPlay == DialogueSequence.Prologue)
        {
            SceneManager.LoadScene(restaurantSceneName);
        }
    }

    // =========================
    // TOKEN RESOLUTION
    // =========================
    private string ResolveDialogueTokens(string input)
    {
        if (!enableTextTokens || string.IsNullOrEmpty(input))
            return input;

        string output = input;

        if (output.Contains("{MOM_RESPONSE_LINE}"))
        {
            int momResponse = PlayerPrefs.GetInt(MOM_RESPONSE_KEY, 0);

            string replacement =
                momResponse == 0
                    ? "Yes I did, it still felt like home"
                    : "Yeah, it doesn’t feel the same";

            output = output.Replace("{MOM_RESPONSE_LINE}", replacement);
        }

        return output;
    }

    // =========================
    // DATA GETTERS
    // =========================
    private DialogueLine[] GetLinesForSequence(DialogueSequence seq)
    {
        switch (seq)
        {
            case DialogueSequence.Prologue: return dialogueData.prologueLines;

            case DialogueSequence.RestaurantIntro1: return dialogueData.restaurantIntro1Lines;
            case DialogueSequence.RestaurantMomConvo1: return dialogueData.restaurantMomConvo1Lines;
            case DialogueSequence.RestaurantMomReminder1: return dialogueData.restaurantMomReminder1Lines;

            case DialogueSequence.GrandmasHouse1: return dialogueData.grandmasHouse1Lines;
            case DialogueSequence.GrandmasHouseGrandmaReminder: return dialogueData.grandmasHouseGrandmaReminderLines;

            case DialogueSequence.RestaurantIntro2: return dialogueData.restaurantIntro2Lines;
            case DialogueSequence.RestaurantMomReminder2: return dialogueData.restaurantMomReminder2Lines;
            case DialogueSequence.RestaurantGrandmaReminder2: return dialogueData.restaurantGrandmaReminder2Lines;

            case DialogueSequence.KitchenTutorial: return dialogueData.kitchenTutorialLines;

            case DialogueSequence.RestaurantIntro3: return dialogueData.restaurantIntro3Lines;

            case DialogueSequence.RestaurantIntroBefore3:
                return dialogueData.restaurantIntroBefore3Lines;

            case DialogueSequence.RestaurantMomReminder3:
                return dialogueData.restaurantMomReminder3Lines;

            case DialogueSequence.RestaurantGrandmaReminder3:
                return dialogueData.restaurantGrandmaReminder3Lines;

            case DialogueSequence.MomGrandmaGrilledCheeseDone:
                return dialogueData.momGrandmaGrilledCheeseDoneLines;

            case DialogueSequence.MapEndDemo: return dialogueData.mapEndDemoLines;

            default: return new DialogueLine[0];
        }
    }

    private DialogueChoice[] GetChoicesForSequence(DialogueSequence seq)
    {
        switch (seq)
        {
            case DialogueSequence.RestaurantMomConvo1:
                return dialogueData.restaurantMomConvo1Choices;
            default:
                return null;
        }
    }

    // =========================
    // UI HELPERS
    // =========================
    private void BuildSpeakerLookup()
    {
        speakers.Clear();
        if (dialogueData == null || dialogueData.speakers == null) return;

        foreach (var s in dialogueData.speakers)
        {
            if (!string.IsNullOrWhiteSpace(s.id))
                speakers[s.id] = s;
        }
    }

    private void CachePortraitPositions()
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

    private void HideNPCPortraits()
    {
        if (momPortraitObj != null) momPortraitObj.SetActive(false);
        if (grannyPortraitObj != null) grannyPortraitObj.SetActive(false);
    }

    private void IntroduceIfNeeded(string speakerId)
    {
        if (string.IsNullOrWhiteSpace(speakerId)) return;
        if (introduced.Contains(speakerId)) return;

        introduced.Add(speakerId);

        if (speakerId == "mom" && momPortraitObj != null)
            momPortraitObj.SetActive(true);

        if (speakerId == "granny" && grannyPortraitObj != null)
            grannyPortraitObj.SetActive(true);
    }

    private void ApplyNameFrame(string speakerId)
    {
        string displayName = speakers.TryGetValue(speakerId, out var s)
            ? s.displayName
            : speakerId;

        bool left = speakerId == "mc";

        if (leftNameFrame != null) leftNameFrame.SetActive(left);
        if (rightNameFrame != null) rightNameFrame.SetActive(!left);

        if (left && leftNameText != null)
            leftNameText.SetText(displayName);
        else if (!left && rightNameText != null)
            rightNameText.SetText(displayName);
    }

    private void ApplyPortraitBump(string speakerId)
    {
        if (speakerId == "mom" && momRT != null)
            StartCoroutine(BumpTo(momRT, momBasePos + Vector2.up * bumpAmount));

        if (speakerId == "granny" && grannyRT != null)
            StartCoroutine(BumpTo(grannyRT, grannyBasePos + Vector2.up * bumpAmount));
    }

    private IEnumerator BumpTo(RectTransform rt, Vector2 target)
    {
        while (Vector2.Distance(rt.anchoredPosition, target) > 0.5f)
        {
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, target, Time.deltaTime * bumpSpeed);
            yield return null;
        }
        rt.anchoredPosition = target;
    }
}