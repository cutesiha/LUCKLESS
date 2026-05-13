using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PrologueDialogueLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;

    public bool hasChoices;
    public PrologueChoiceOption[] choices;
}

[System.Serializable]
public class PrologueDialogueCanvas
{
    public string canvasName;
    public CanvasGroup canvasGroup;
    public PrologueDialogueLine[] lines;
}

[System.Serializable]
public class PrologueChoiceOption
{
    public string choiceText;
    public PrologueChoiceResponseLine[] responseLines = new PrologueChoiceResponseLine[1];
}

[System.Serializable]
public class PrologueChoiceResponseLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;
}

public class PrologueSequenceController : MonoBehaviour
{
    [Header("Canvas Flow")]
    [HideInInspector] [SerializeField] private CanvasGroup canvas1Group;
    [HideInInspector] [SerializeField] private CanvasGroup canvas2Group;
    [SerializeField] private GameObject dialogueCanvasRoot;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private RectTransform topEyeCover;
    [SerializeField] private RectTransform bottomEyeCover;

    [Header("Dialogue UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image characterImage;

    [Header("Dialogue")]
    [SerializeField] private PrologueDialogueCanvas[] dialogueCanvases;
    [SerializeField] private int firstDialogueCanvasIndex = 1;
    [HideInInspector] [SerializeField] private bool dialogueCanvasesMigrated;
    [HideInInspector] public PrologueDialogueLine[] canvas1Lines;
    [HideInInspector] public PrologueDialogueLine[] canvas2Lines;

    [Header("Timing")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float lineEndDelay = 0.08f;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private float titleFadeDuration = 1f;
    [SerializeField] private float titleHoldSeconds = 2f;
    [SerializeField] private float titleAppearDelayAfterDialogue = 1.5f;
    [SerializeField] private float titleFadeAfterCanvas2Delay = 1f;
    [SerializeField] private float eyeOpenDuration = 1.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] [Range(0f, 5f)] private float voiceVolume = 1f;
    [SerializeField] private AudioSource titleMusicSource;
    [SerializeField] private AudioClip titleMusicClip;
    [SerializeField] private float titleMusicVolume = 1f;
    [SerializeField] private float titleMusicFadeInDuration = 1.5f;

    [Header("Handbook Reward")]
    [SerializeField] private int handbookRewardAfterCanvasNumber = 3;
    [SerializeField] private GameObject handbookPanelPrefab;
    [SerializeField] private string handbookRewardMessage = "\uD640\uB85C\uADF8\uB7A8 \uD578\uB4DC\uBD81\uC744 \uC5BB\uC5C8\uC2B5\uB2C8\uB2E4!";
    [SerializeField] private string handbookClickPrompt = "<- \uC778\uBCA4\uD1A0\uB9AC\uB97C \uD074\uB9AD\uD558\uC138\uC694";
    [SerializeField] private float handbookDimAlpha = 0.42f;
    [SerializeField] private float handbookRewardBlinkSeconds = 2.4f;
    [SerializeField] private float handbookRewardBlinkInterval = 0.22f;

    [Header("Control Buttons")]
    [SerializeField] private string mainSceneName = "MainScene";

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private bool autoModeEnabled;
    private Coroutine typingRoutine;
    private Coroutine titleMusicFadeRoutine;
    private Vector2 topEyeClosedPosition;
    private Vector2 bottomEyeClosedPosition;
    private PrologueDialogueLine[] currentLines;
    private bool titleTransitionPlayed;
    private RectTransform choicePanel;
    private PrologueChoiceOption selectedChoice;
    private bool choiceButtonsVisible;
    private bool handbookRewardPlayed;
    private RectTransform rewardOverlay;
    private Image rewardDimImage;
    private TextMeshProUGUI rewardMessageText;
    private CanvasGroup rewardMessageGroup;
    private TextMeshProUGUI handbookPromptText;
    private GameObject handbookPanelInstance;
    private RectTransform dialogueControlPanel;
    private TextMeshProUGUI autoButtonText;
    private bool mainSceneTransitionStarted;
    private const float ChoiceButtonHeight = 58f;
    private const float ChoiceFontSize = 41f;
    private const float ChoicePanelWidth = 680f;

    private void OnEnable()
    {
        InventoryPanelController.InventoryPanelClosed += OnInventoryPanelClosed;
    }

    private void OnDisable()
    {
        InventoryPanelController.InventoryPanelClosed -= OnInventoryPanelClosed;
    }

    private void Awake()
    {
        EnsureDialogueCanvases();
        EnsureChoiceSlots();
        EnsureDialogueCanvas();
        PrepareDialogueCanvases();
        EnsureDialogueControlButtons();

        bool startAtTitle = GetFirstDialogueCanvasIndex() > 0;

        SetActiveDialogueCanvas(startAtTitle ? -1 : GetFirstDialogueCanvasIndex());
        SetDialogueVisible(!startAtTitle);
        SetGroup(titleGroup, 0f, false);
        CacheEyeCoverPositions();
        SetEyeCoversVisible(false);

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(false);
            BringTitleToFront();
        }
    }

    private void OnValidate()
    {
        EnsureDialogueCanvases();
        EnsureChoiceSlots();
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        yield return PlayDialogueCanvasSequence();
    }

    private void Update()
    {
        if (choiceButtonsVisible)
        {
            return;
        }

        if (IsPointerOverDialogueControls())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                skipTypingRequested = true;
                return;
            }

            advanceRequested = true;
        }
    }

    private IEnumerator PlayDialogueCanvasSequence()
    {
        EnsureDialogueCanvases();

        if (dialogueCanvases == null || dialogueCanvases.Length == 0)
        {
            yield return PlayTitleTransition();
            yield break;
        }

        int firstCanvasIndex = GetFirstDialogueCanvasIndex();

        if (firstCanvasIndex == 0)
        {
            yield return PlayDialogueCanvas(0);
            yield return FadeGroup(dialogueGroup, 1f, 0f, fadeDuration, false);
            yield return new WaitForSecondsRealtime(titleAppearDelayAfterDialogue);
            yield return PlayTitleTransition();
            firstCanvasIndex = 1;
        }
        else
        {
            SetDialogueVisible(false);
            yield return PlayTitleTransition();
        }

        for (int i = firstCanvasIndex; i < dialogueCanvases.Length; i++)
        {
            yield return PlayDialogueCanvas(i);
        }
    }

    private int GetFirstDialogueCanvasIndex()
    {
        if (dialogueCanvases == null || dialogueCanvases.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(firstDialogueCanvasIndex, 0, dialogueCanvases.Length - 1);
    }

    private IEnumerator PlayDialogueCanvas(int canvasIndex)
    {
        if (dialogueCanvases == null || canvasIndex < 0 || canvasIndex >= dialogueCanvases.Length)
        {
            yield break;
        }

        PrologueDialogueCanvas dialogueCanvas = dialogueCanvases[canvasIndex];

        if (dialogueCanvas == null)
        {
            yield break;
        }

        PrepareDialogueCanvas(dialogueCanvas.canvasGroup);
        SetActiveDialogueCanvas(canvasIndex);
        SetDialogueVisible(true);
        yield return PlayDialogueLines(dialogueCanvas.lines, false);

        if (ShouldPlayHandbookReward(canvasIndex))
        {
            yield return FadeOutDialoguePanel();
            yield return PlayHandbookReward();
        }
    }

    private void SetActiveDialogueCanvas(int activeIndex)
    {
        if (dialogueCanvases == null)
        {
            return;
        }

        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            if (dialogueCanvases[i] == null)
            {
                continue;
            }

            bool isActive = i == activeIndex;
            SetGroup(dialogueCanvases[i].canvasGroup, isActive ? 1f : 0f, isActive);
        }
    }

    private IEnumerator PlayDialogueLines(PrologueDialogueLine[] lines, bool transitionWhenEmpty)
    {
        if (lines == null || lines.Length == 0)
        {
            if (transitionWhenEmpty)
            {
                yield break;
            }

            yield break;
        }

        currentLines = lines;

        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            ShowLine(lines[lineIndex]);
            typingRoutine = StartCoroutine(TypeLine(lines[lineIndex].dialogue));
            yield return typingRoutine;

            yield return new WaitForSecondsRealtime(lineEndDelay);

            if (ShouldShowChoices(lines[lineIndex]))
            {
                yield return ShowChoices(lines[lineIndex]);
                yield return PlayChoiceResponse(lines[lineIndex], selectedChoice);
                continue;
            }

            advanceRequested = false;
            yield return WaitForAdvanceOrAuto();
            advanceRequested = false;
        }

    }

    private void EnsureDialogueCanvas()
    {
        if (dialogueCanvasRoot == null && dialogueGroup != null)
        {
            dialogueCanvasRoot = dialogueGroup.gameObject;
        }

        if (dialogueCanvasRoot == null)
        {
            return;
        }

        dialogueCanvasRoot.SetActive(true);

        if (dialogueCanvasRoot.transform.localScale == Vector3.zero)
        {
            dialogueCanvasRoot.transform.localScale = Vector3.one;
        }

        Canvas dialogueCanvas = dialogueCanvasRoot.GetComponent<Canvas>();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            dialogueCanvas.worldCamera = Camera.main;
            dialogueCanvas.planeDistance = 1f;
            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 100;
        }

        if (dialogueGroup == null)
        {
            dialogueGroup = dialogueCanvasRoot.GetComponent<CanvasGroup>();

            if (dialogueGroup == null)
            {
                dialogueGroup = dialogueCanvasRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        EnsureDialogueCanvas();
        EnsureDialogueControlButtons();

        if (dialogueGroup != null)
        {
            SetGroup(dialogueGroup, visible ? 1f : 0f, visible);
        }
        else if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(visible);
        }
    }

    private void EnsureDialogueControlButtons()
    {
        EnsureEventSystem();

        if (dialogueControlPanel != null)
        {
            dialogueControlPanel.SetAsLastSibling();
            return;
        }

        RectTransform dialoguePanel = dialogueText != null
            ? dialogueText.transform.parent as RectTransform
            : null;

        if (dialoguePanel == null)
        {
            return;
        }

        GameObject panelObject = new GameObject("DialogueControlButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        panelObject.transform.SetParent(dialoguePanel, false);

        dialogueControlPanel = panelObject.GetComponent<RectTransform>();
        dialogueControlPanel.anchorMin = new Vector2(1f, 0f);
        dialogueControlPanel.anchorMax = new Vector2(1f, 0f);
        dialogueControlPanel.pivot = new Vector2(1f, 0f);
        dialogueControlPanel.anchoredPosition = new Vector2(-54f, 24f);
        dialogueControlPanel.sizeDelta = new Vector2(190f, 42f);

        HorizontalLayoutGroup layout = panelObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateControlButton("Skip", OnSkipClicked);
        autoButtonText = CreateControlButton("Auto", ToggleAutoMode);
        dialogueControlPanel.SetAsLastSibling();
    }

    private bool IsPointerOverDialogueControls()
    {
        if (dialogueControlPanel == null || !dialogueControlPanel.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = dialogueControlPanel.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(dialogueControlPanel, Input.mousePosition, eventCamera);
    }

    private TextMeshProUGUI CreateControlButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(EventTrigger));
        buttonObject.transform.SetParent(dialogueControlPanel, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.sizeDelta = new Vector2(92f, 44f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.35f, 0.35f, 0.35f, 0.28f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onClick);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 92f;
        layoutElement.preferredHeight = 44f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = Vector2.zero;
        textTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 34f;
        labelText.color = new Color(0.42f, 0.42f, 0.42f, 1f);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        if (dialogueText != null && dialogueText.font != null)
        {
            labelText.font = dialogueText.font;
        }

        AddTextHover(buttonObject.GetComponent<EventTrigger>(), labelText);
        return labelText;
    }

    private void AddTextHover(EventTrigger trigger, TextMeshProUGUI labelText)
    {
        trigger.triggers.Clear();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => labelText.color = Color.white);
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => labelText.color = GetControlTextColor(labelText == autoButtonText));
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private Color GetControlTextColor(bool isAutoButton)
    {
        if (isAutoButton && autoModeEnabled)
        {
            return new Color(0.62f, 0.62f, 0.62f, 1f);
        }

        return new Color(0.42f, 0.42f, 0.42f, 1f);
    }

    private void OnSkipClicked()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnInventoryPanelClosed()
    {
        if (!handbookRewardPlayed || mainSceneTransitionStarted)
        {
            return;
        }

        StartCoroutine(FadeToMainScene());
    }

    private void ToggleAutoMode()
    {
        autoModeEnabled = !autoModeEnabled;

        if (autoButtonText != null)
        {
            autoButtonText.color = GetControlTextColor(true);
        }
    }

    private void ShowLine(PrologueDialogueLine line)
    {
        ShowDialogue(line.speakerName, line.characterSprite, line.voiceClip);
    }

    private void ShowLine(PrologueChoiceResponseLine line, PrologueDialogueLine fallbackLine)
    {
        string speakerName = string.IsNullOrEmpty(line.speakerName)
            ? fallbackLine.speakerName
            : line.speakerName;
        Sprite characterSprite = line.characterSprite != null
            ? line.characterSprite
            : fallbackLine.characterSprite;
        AudioClip voiceClip = line.voiceClip;

        ShowDialogue(speakerName, characterSprite, voiceClip);
    }

    private void ShowDialogue(string speakerName, Sprite characterSprite, AudioClip voiceClip)
    {
        if (nameText != null)
        {
            nameText.text = speakerName;
        }

        if (characterImage != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.gameObject.SetActive(characterSprite != null);
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = voiceClip;
            voiceSource.volume = voiceVolume;

            if (voiceClip != null)
            {
                voiceSource.Play();
            }
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTypingRequested = false;
        dialogueText.text = "";

        if (line == null)
        {
            line = "";
        }

        foreach (char character in line)
        {
            if (skipTypingRequested)
            {
                break;
            }
            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        dialogueText.text = line;
        isTyping = false;
        skipTypingRequested = false;
    }

    private bool ShouldShowChoices(PrologueDialogueLine line)
    {
        return line != null
            && line.hasChoices
            && line.choices != null
            && GetChoiceCount(line) > 0;
    }

    private void EnsureChoiceSlots()
    {
        if (dialogueCanvases != null)
        {
            for (int i = 0; i < dialogueCanvases.Length; i++)
            {
                if (dialogueCanvases[i] != null)
                {
                    EnsureChoiceSlots(dialogueCanvases[i].lines);
                }
            }
        }

        EnsureChoiceSlots(canvas1Lines);
        EnsureChoiceSlots(canvas2Lines);
    }

    private void EnsureDialogueCanvases()
    {
        if (dialogueCanvasesMigrated)
        {
            EnsureDialogueCanvasEntries();
            return;
        }

        bool hasLegacyCanvas1 = canvas1Group != null || (canvas1Lines != null && canvas1Lines.Length > 0);
        bool hasLegacyCanvas2 = canvas2Group != null || (canvas2Lines != null && canvas2Lines.Length > 0);
        int legacyCount = (hasLegacyCanvas1 ? 1 : 0) + (hasLegacyCanvas2 ? 1 : 0);

        if (legacyCount == 0)
        {
            dialogueCanvases = new PrologueDialogueCanvas[1];
            dialogueCanvases[0] = new PrologueDialogueCanvas { canvasName = "Canvas 1" };
            dialogueCanvasesMigrated = true;
            return;
        }

        dialogueCanvases = new PrologueDialogueCanvas[legacyCount];
        int index = 0;

        if (hasLegacyCanvas1)
        {
            dialogueCanvases[index] = new PrologueDialogueCanvas
            {
                canvasName = "Canvas 1",
                canvasGroup = canvas1Group,
                lines = canvas1Lines
            };
            index++;
        }

        if (hasLegacyCanvas2)
        {
            dialogueCanvases[index] = new PrologueDialogueCanvas
            {
                canvasName = "Canvas 2",
                canvasGroup = canvas2Group,
                lines = canvas2Lines
            };
        }

        EnsureDialogueCanvasEntries();
        dialogueCanvasesMigrated = true;
    }

    private void EnsureDialogueCanvasEntries()
    {
        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            if (dialogueCanvases[i] == null)
            {
                dialogueCanvases[i] = new PrologueDialogueCanvas();
            }

            if (string.IsNullOrWhiteSpace(dialogueCanvases[i].canvasName))
            {
                dialogueCanvases[i].canvasName = "Canvas " + (i + 1);
            }
        }
    }

    private void EnsureChoiceSlots(PrologueDialogueLine[] lines)
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null)
            {
                continue;
            }

            if (lines[i].choices == null)
            {
                lines[i].choices = new PrologueChoiceOption[0];
            }

            for (int choiceIndex = 0; choiceIndex < lines[i].choices.Length; choiceIndex++)
            {
                if (lines[i].choices[choiceIndex] == null)
                {
                    lines[i].choices[choiceIndex] = new PrologueChoiceOption();
                }

                if (lines[i].choices[choiceIndex].responseLines == null)
                {
                    lines[i].choices[choiceIndex].responseLines = new PrologueChoiceResponseLine[1];
                }

                for (int responseIndex = 0; responseIndex < lines[i].choices[choiceIndex].responseLines.Length; responseIndex++)
                {
                    if (lines[i].choices[choiceIndex].responseLines[responseIndex] == null)
                    {
                        lines[i].choices[choiceIndex].responseLines[responseIndex] = new PrologueChoiceResponseLine();
                    }
                }
            }
        }
    }

    private IEnumerator ShowChoices(PrologueDialogueLine line)
    {
        EnsureChoicePanel(line);

        if (choicePanel == null)
        {
            yield break;
        }

        selectedChoice = null;
        choiceButtonsVisible = true;
        choicePanel.gameObject.SetActive(true);
        choicePanel.SetAsLastSibling();

        EnsureEventSystem();

        yield return new WaitUntil(() => selectedChoice != null);

        choiceButtonsVisible = false;
        choicePanel.gameObject.SetActive(false);
    }

    private void HideChoiceButtons()
    {
        choiceButtonsVisible = false;

        if (choicePanel != null)
        {
            choicePanel.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayChoiceResponse(PrologueDialogueLine sourceLine, PrologueChoiceOption choice)
    {
        if (choice == null || choice.responseLines == null || choice.responseLines.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < choice.responseLines.Length; i++)
        {
            PrologueChoiceResponseLine responseLine = choice.responseLines[i];

            if (responseLine == null || string.IsNullOrWhiteSpace(responseLine.dialogue))
            {
                continue;
            }

            ShowLine(responseLine, sourceLine);
            typingRoutine = StartCoroutine(TypeLine(responseLine.dialogue));
            yield return typingRoutine;

            yield return new WaitForSecondsRealtime(lineEndDelay);
            advanceRequested = false;
            yield return WaitForAdvanceOrAuto();
            advanceRequested = false;
        }
    }

    private IEnumerator WaitForAdvanceOrAuto()
    {
        if (autoModeEnabled)
        {
            yield return WaitForVoiceToFinish();
            yield break;
        }

        yield return new WaitUntil(() => advanceRequested);
    }

    private IEnumerator WaitForVoiceToFinish()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            yield return new WaitWhile(() => voiceSource != null && voiceSource.isPlaying);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    private void EnsureChoicePanel(PrologueDialogueLine line)
    {
        RectTransform dialoguePanel = dialogueText != null
            ? dialogueText.transform.parent as RectTransform
            : null;

        if (choicePanel != null)
        {
            PlaceChoicePanel(dialoguePanel);
            RefreshChoiceButtons(line);
            return;
        }

        Transform parent = dialoguePanel != null ? dialoguePanel : dialogueCanvasRoot != null ? dialogueCanvasRoot.transform : null;

        if (parent == null)
        {
            return;
        }

        GameObject panelObject = new GameObject("DialogueChoices", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        choicePanel = panelObject.GetComponent<RectTransform>();
        PlaceChoicePanel(dialoguePanel);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.LowerRight;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RefreshChoiceButtons(line);
        choicePanel.gameObject.SetActive(false);
    }

    private void PlaceChoicePanel(RectTransform dialoguePanel)
    {
        if (choicePanel == null)
        {
            return;
        }

        Transform parent = dialoguePanel != null ? dialoguePanel : dialogueCanvasRoot != null ? dialogueCanvasRoot.transform : null;

        if (parent != null && choicePanel.parent != parent)
        {
            choicePanel.SetParent(parent, false);
        }

        choicePanel.anchorMin = new Vector2(1f, 1f);
        choicePanel.anchorMax = new Vector2(1f, 1f);
        choicePanel.pivot = new Vector2(1f, 0f);
        choicePanel.anchoredPosition = new Vector2(-48f, 18f);
        choicePanel.sizeDelta = new Vector2(ChoicePanelWidth, 204f);
    }

    private void RefreshChoiceButtons(PrologueDialogueLine line)
    {
        if (choicePanel == null)
        {
            return;
        }

        for (int i = choicePanel.childCount - 1; i >= 0; i--)
        {
            Destroy(choicePanel.GetChild(i).gameObject);
        }

        for (int i = 0; i < line.choices.Length; i++)
        {
            PrologueChoiceOption option = line.choices[i];

            if (option == null || string.IsNullOrWhiteSpace(option.choiceText))
            {
                continue;
            }

            CreateChoiceButton(option);
        }
    }

    private int GetChoiceCount(PrologueDialogueLine line)
    {
        if (line == null || line.choices == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < line.choices.Length; i++)
        {
            PrologueChoiceOption option = line.choices[i];

            if (option != null && !string.IsNullOrWhiteSpace(option.choiceText))
            {
                count++;
            }
        }

        return count;
    }

    private void CreateChoiceButton(PrologueChoiceOption option)
    {
        GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(EventTrigger));
        buttonObject.transform.SetParent(choicePanel, false);

        float buttonWidth = GetChoiceButtonWidth(option.choiceText);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(buttonWidth, ChoiceButtonHeight);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.06f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateChoiceButtonColors();
        button.onClick.AddListener(() => SelectChoice(option));

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = buttonWidth;
        layoutElement.minWidth = buttonWidth;
        layoutElement.preferredHeight = ChoiceButtonHeight;
        layoutElement.minHeight = ChoiceButtonHeight;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(18f, 4f);
        textTransform.offsetMax = new Vector2(-18f, -4f);

        TextMeshProUGUI labelText = textObject.GetComponent<TextMeshProUGUI>();
        labelText.text = option.choiceText;
        labelText.fontSize = ChoiceFontSize;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineRight;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.raycastTarget = false;

        if (dialogueText != null && dialogueText.font != null)
        {
            labelText.font = dialogueText.font;
        }

        EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => labelText.color = new Color(0.62f, 0.62f, 0.62f, 1f));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => labelText.color = Color.white);
    }

    private float GetChoiceButtonWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 300f;
        }

        float width = 72f;

        foreach (char character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                width += 14f;
            }
            else if (character <= 0x007f)
            {
                width += 21f;
            }
            else
            {
                width += 38f;
            }
        }

        return Mathf.Clamp(width, 300f, ChoicePanelWidth);
    }

    private ColorBlock CreateChoiceButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.05f, 0.05f, 0.06f, 0.94f);
        colors.highlightedColor = new Color(0.18f, 0.16f, 0.12f, 1f);
        colors.pressedColor = new Color(0.28f, 0.23f, 0.15f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private void SelectChoice(PrologueChoiceOption option)
    {
        selectedChoice = option;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private bool ShouldPlayHandbookReward(int canvasIndex)
    {
        return !handbookRewardPlayed && canvasIndex == handbookRewardAfterCanvasNumber - 1;
    }

    private IEnumerator FadeOutDialoguePanel()
    {
        HideChoiceButtons();

        if (dialogueControlPanel != null)
        {
            dialogueControlPanel.gameObject.SetActive(false);
        }

        if (dialogueGroup != null)
        {
            float startAlpha = dialogueGroup.alpha;
            yield return FadeGroup(dialogueGroup, startAlpha, 0f, fadeDuration, false);
            yield break;
        }

        if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(false);
        }
    }

    private IEnumerator PlayHandbookReward()
    {
        handbookRewardPlayed = true;
        EnsureRewardOverlay();

        if (rewardOverlay == null)
        {
            yield break;
        }

        rewardOverlay.gameObject.SetActive(true);
        rewardOverlay.SetAsLastSibling();

        if (rewardDimImage != null)
        {
            rewardDimImage.color = Color.clear;
            yield return FadeImageAlpha(rewardDimImage, 0f, handbookDimAlpha, 0.45f);
        }

        if (rewardMessageText != null)
        {
            rewardMessageText.text = handbookRewardMessage;
        }

        if (rewardMessageGroup != null)
        {
            rewardMessageGroup.alpha = 1f;
            yield return BlinkRewardMessage();
            rewardMessageGroup.alpha = 0f;
        }

        ShowHandbookPanel();
        ShowHandbookPrompt();

        yield return null;
        yield return new WaitUntil(HasAnyClick);

        if (handbookPromptText != null)
        {
            handbookPromptText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeToMainScene()
    {
        mainSceneTransitionStarted = true;
        EnsureRewardOverlay();

        if (handbookPromptText != null)
        {
            handbookPromptText.gameObject.SetActive(false);
        }

        if (rewardOverlay != null)
        {
            rewardOverlay.gameObject.SetActive(true);
            rewardOverlay.SetAsLastSibling();
        }

        if (rewardDimImage != null)
        {
            rewardDimImage.raycastTarget = true;
            rewardDimImage.transform.SetAsLastSibling();
            float startAlpha = rewardDimImage.color.a;
            yield return FadeImageAlpha(rewardDimImage, startAlpha, 1f, 0.75f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.75f);
        }

        SceneManager.LoadScene(mainSceneName);
    }

    private void EnsureRewardOverlay()
    {
        if (rewardOverlay != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject(
            "HandbookRewardOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        rewardOverlay = overlayObject.GetComponent<RectTransform>();
        rewardOverlay.anchorMin = Vector2.zero;
        rewardOverlay.anchorMax = Vector2.one;
        rewardOverlay.offsetMin = Vector2.zero;
        rewardOverlay.offsetMax = Vector2.zero;

        Canvas overlayCanvas = overlayObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        overlayCanvas.worldCamera = Camera.main;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 200;

        CanvasScaler canvasScaler = overlayObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject dimObject = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimObject.transform.SetParent(rewardOverlay, false);
        RectTransform dimTransform = dimObject.GetComponent<RectTransform>();
        dimTransform.anchorMin = Vector2.zero;
        dimTransform.anchorMax = Vector2.one;
        dimTransform.offsetMin = Vector2.zero;
        dimTransform.offsetMax = Vector2.zero;
        rewardDimImage = dimObject.GetComponent<Image>();
        rewardDimImage.color = Color.clear;
        rewardDimImage.raycastTarget = false;

        GameObject messageObject = new GameObject("RewardMessage", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup), typeof(Outline));
        messageObject.transform.SetParent(rewardOverlay, false);
        RectTransform messageTransform = messageObject.GetComponent<RectTransform>();
        messageTransform.anchorMin = new Vector2(0.5f, 0.5f);
        messageTransform.anchorMax = new Vector2(0.5f, 0.5f);
        messageTransform.pivot = new Vector2(0.5f, 0.5f);
        messageTransform.anchoredPosition = Vector2.zero;
        messageTransform.sizeDelta = new Vector2(1500f, 180f);

        rewardMessageText = messageObject.GetComponent<TextMeshProUGUI>();
        rewardMessageText.alignment = TextAlignmentOptions.Center;
        rewardMessageText.color = Color.white;
        rewardMessageText.fontSize = 78f;
        rewardMessageText.enableWordWrapping = false;
        rewardMessageText.raycastTarget = false;

        if (dialogueText != null && dialogueText.font != null)
        {
            rewardMessageText.font = dialogueText.font;
        }

        Outline outline = messageObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, -4f);

        rewardMessageGroup = messageObject.GetComponent<CanvasGroup>();
        rewardMessageGroup.alpha = 0f;

        rewardOverlay.gameObject.SetActive(false);
    }

    private IEnumerator BlinkRewardMessage()
    {
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < handbookRewardBlinkSeconds)
        {
            rewardMessageGroup.alpha = visible ? 1f : 0.25f;
            visible = !visible;
            yield return new WaitForSecondsRealtime(handbookRewardBlinkInterval);
            elapsed += handbookRewardBlinkInterval;
        }
    }

    private void ShowHandbookPanel()
    {
        if (handbookPanelInstance != null)
        {
            handbookPanelInstance.SetActive(true);
            NormalizeHandbookPanelInstance(handbookPanelInstance);
            handbookPanelInstance.transform.SetAsLastSibling();
            return;
        }

        if (handbookPanelPrefab == null || rewardOverlay == null)
        {
            return;
        }

        handbookPanelInstance = Instantiate(handbookPanelPrefab, rewardOverlay);
        handbookPanelInstance.name = "HandbookPanel";
        handbookPanelInstance.SetActive(true);
        NormalizeHandbookPanelInstance(handbookPanelInstance);
        handbookPanelInstance.transform.SetAsLastSibling();
    }

    private void NormalizeHandbookPanelInstance(GameObject panelInstance)
    {
        RectTransform panelTransform = panelInstance.GetComponent<RectTransform>();

        if (panelTransform != null)
        {
            panelTransform.anchorMin = Vector2.zero;
            panelTransform.anchorMax = Vector2.one;
            panelTransform.offsetMin = Vector2.zero;
            panelTransform.offsetMax = Vector2.zero;
            panelTransform.localScale = Vector3.one;
        }

        Canvas panelCanvas = panelInstance.GetComponent<Canvas>();

        if (panelCanvas != null)
        {
            panelCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            panelCanvas.worldCamera = Camera.main;
            panelCanvas.overrideSorting = false;
        }
    }

    private void ShowHandbookPrompt()
    {
        if (rewardOverlay == null)
        {
            return;
        }

        if (handbookPromptText == null)
        {
            GameObject promptObject = new GameObject("HandbookClickPrompt", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Outline));
            promptObject.transform.SetParent(rewardOverlay, false);

            RectTransform promptTransform = promptObject.GetComponent<RectTransform>();
            promptTransform.anchorMin = new Vector2(0.5f, 0.5f);
            promptTransform.anchorMax = new Vector2(0.5f, 0.5f);
            promptTransform.pivot = new Vector2(0f, 0.5f);
            promptTransform.anchoredPosition = new Vector2(230f, 0f);
            promptTransform.sizeDelta = new Vector2(560f, 80f);

            handbookPromptText = promptObject.GetComponent<TextMeshProUGUI>();
            handbookPromptText.alignment = TextAlignmentOptions.MidlineLeft;
            handbookPromptText.color = Color.white;
            handbookPromptText.fontSize = 40f;
            handbookPromptText.enableWordWrapping = false;
            handbookPromptText.raycastTarget = false;

            if (dialogueText != null && dialogueText.font != null)
            {
                handbookPromptText.font = dialogueText.font;
            }

            Outline outline = promptObject.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        handbookPromptText.text = handbookClickPrompt;
        handbookPromptText.gameObject.SetActive(true);
        PositionHandbookPromptNearImage();
        handbookPromptText.transform.SetAsLastSibling();
    }

    private void PositionHandbookPromptNearImage()
    {
        if (handbookPanelInstance == null || handbookPromptText == null || rewardOverlay == null)
        {
            return;
        }

        Image targetImage = FindHandbookPromptTargetImage();
        RectTransform promptTransform = handbookPromptText.rectTransform;

        if (targetImage == null)
        {
            promptTransform.anchoredPosition = new Vector2(230f, 0f);
            return;
        }

        RectTransform imageTransform = targetImage.rectTransform;
        Vector3[] imageCorners = new Vector3[4];
        imageTransform.GetWorldCorners(imageCorners);

        Vector3 rightCenterWorld = (imageCorners[2] + imageCorners[3]) * 0.5f;
        Canvas overlayCanvas = rewardOverlay.GetComponentInParent<Canvas>();
        Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? overlayCanvas.worldCamera
            : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(overlayCamera, rightCenterWorld);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rewardOverlay, screenPoint, overlayCamera, out Vector2 localPoint))
        {
            promptTransform.anchoredPosition = localPoint + new Vector2(26f, 0f);
        }
    }

    private Image FindHandbookPromptTargetImage()
    {
        Transform zeroPortrait = handbookPanelInstance.transform.Find("ZeroPortrait");

        if (zeroPortrait != null && zeroPortrait.TryGetComponent(out Image zeroImage))
        {
            return zeroImage;
        }

        Image[] images = handbookPanelInstance.GetComponentsInChildren<Image>(true);
        Image largestImage = null;
        float largestArea = 0f;

        for (int i = 0; i < images.Length; i++)
        {
            RectTransform rectTransform = images[i].rectTransform;
            float area = rectTransform.rect.width * rectTransform.rect.height;

            if (area > largestArea)
            {
                largestArea = area;
                largestImage = images[i];
            }
        }

        return largestImage;
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            image.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private bool HasAnyClick()
    {
        return Input.GetMouseButtonDown(0) || Input.touchCount > 0;
    }

    private IEnumerator PlayTitleTransition(bool titleAlreadyVisible = false)
    {
        if (titleTransitionPlayed)
        {
            yield break;
        }

        titleTransitionPlayed = true;

        if (voiceSource != null)
        {
            voiceSource.Stop();
        }

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(true);
            BringTitleToFront();
        }

        StartTitleMusic();

        if (titleAlreadyVisible)
        {
            SetGroup(titleGroup, 1f, true);
        }
        else
        {
            yield return FadeGroup(titleGroup, 0f, 1f, titleFadeDuration, true);
        }

        yield return new WaitForSecondsRealtime(titleHoldSeconds);

        if (dialogueCanvases != null && dialogueCanvases.Length > 1)
        {
            PrepareDialogueCanvas(dialogueCanvases[1].canvasGroup);
            SetActiveDialogueCanvas(1);
        }

        yield return OpenEyeCovers();
        BringTitleToFront();
        yield return new WaitForSecondsRealtime(titleFadeAfterCanvas2Delay);
        SetGroup(titleGroup, 0f, false);

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(false);
        }

        SetDialogueVisible(true);
    }

    private void StartTitleMusic()
    {
        if (titleMusicSource == null)
        {
            return;
        }

        if (titleMusicClip != null)
        {
            titleMusicSource.clip = titleMusicClip;
        }

        if (titleMusicSource.clip == null)
        {
            return;
        }

        if (titleMusicFadeRoutine != null)
        {
            StopCoroutine(titleMusicFadeRoutine);
        }

        titleMusicSource.volume = 0f;

        if (!titleMusicSource.isPlaying)
        {
            titleMusicSource.Play();
        }

        titleMusicFadeRoutine = StartCoroutine(FadeAudio(titleMusicSource, titleMusicVolume, titleMusicFadeInDuration));
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float startVolume = source.volume;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        source.volume = targetVolume;
        titleMusicFadeRoutine = null;
    }

    private string GetCurrentLineText()
    {
        if (currentLines != null && lineIndex >= 0 && lineIndex < currentLines.Length)
        {
            return currentLines[lineIndex].dialogue;
        }

        return "";
    }

    private void PrepareDialogueCanvases()
    {
        if (dialogueCanvases == null)
        {
            return;
        }

        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            if (dialogueCanvases[i] != null)
            {
                PrepareDialogueCanvas(dialogueCanvases[i].canvasGroup);
            }
        }
    }

    private void PrepareDialogueCanvas(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return;
        }

        Transform canvasTransform = canvasGroup.transform;

        if (canvasTransform.localScale == Vector3.zero)
        {
            canvasTransform.localScale = Vector3.one;
        }

        Canvas canvas = canvasGroup.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5;
        }

        Transform background = canvasTransform.Find("HouseMasterBackground");

        if (background != null)
        {
            background.gameObject.SetActive(true);
            background.SetAsFirstSibling();
        }
    }

    private void CacheEyeCoverPositions()
    {
        if (topEyeCover != null)
        {
            topEyeClosedPosition = topEyeCover.anchoredPosition;
        }

        if (bottomEyeCover != null)
        {
            bottomEyeClosedPosition = bottomEyeCover.anchoredPosition;
        }
    }

    private void SetEyeCoversVisible(bool visible)
    {
        if (topEyeCover != null)
        {
            topEyeCover.gameObject.SetActive(visible);
            topEyeCover.anchoredPosition = topEyeClosedPosition;
        }

        if (bottomEyeCover != null)
        {
            bottomEyeCover.gameObject.SetActive(visible);
            bottomEyeCover.anchoredPosition = bottomEyeClosedPosition;
        }

        BringTitleToFront();
    }

    private void BringTitleToFront()
    {
        if (titleImage != null)
        {
            titleImage.SetAsLastSibling();
        }
    }

    private IEnumerator OpenEyeCovers()
    {
        if (topEyeCover == null || bottomEyeCover == null)
        {
            yield break;
        }

        SetEyeCoversVisible(true);

        float topDistance = topEyeCover.rect.height + 80f;
        float bottomDistance = bottomEyeCover.rect.height + 80f;
        Vector2 topOpenPosition = topEyeClosedPosition + Vector2.up * topDistance;
        Vector2 bottomOpenPosition = bottomEyeClosedPosition + Vector2.down * bottomDistance;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = eyeOpenDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / eyeOpenDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            topEyeCover.anchoredPosition = Vector2.Lerp(topEyeClosedPosition, topOpenPosition, eased);
            bottomEyeCover.anchoredPosition = Vector2.Lerp(bottomEyeClosedPosition, bottomOpenPosition, eased);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        SetEyeCoversVisible(false);
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration, bool activeAfter)
    {
        if (group == null)
        {
            yield break;
        }

        group.gameObject.SetActive(true);
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        group.alpha = to;
        SetGroup(group, to, activeAfter);
    }

    private void SetGroup(CanvasGroup group, float alpha, bool active)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;

        if (active && group.transform.localScale == Vector3.zero)
        {
            group.transform.localScale = Vector3.one;
        }

        group.gameObject.SetActive(active || alpha > 0f);
    }
}
