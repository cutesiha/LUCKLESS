using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] [Range(0f, 2f)] private float voiceVolume = 1f;
    [SerializeField] private AudioSource titleMusicSource;
    [SerializeField] private AudioClip titleMusicClip;
    [SerializeField] private float titleMusicVolume = 1f;
    [SerializeField] private float titleMusicFadeInDuration = 1.5f;

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private Coroutine typingRoutine;
    private Coroutine titleMusicFadeRoutine;
    private Vector2 topEyeClosedPosition;
    private Vector2 bottomEyeClosedPosition;
    private PrologueDialogueLine[] currentLines;
    private bool titleTransitionPlayed;
    private RectTransform choicePanel;
    private PrologueChoiceOption selectedChoice;
    private bool choiceButtonsVisible;

    private void Awake()
    {
        EnsureDialogueCanvases();
        EnsureChoiceSlots();
        EnsureDialogueCanvas();
        PrepareDialogueCanvases();

        SetActiveDialogueCanvas(0);

        SetDialogueVisible(true);
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

        yield return PlayDialogueCanvas(0);
        yield return FadeGroup(dialogueGroup, 1f, 0f, fadeDuration, false);
        yield return new WaitForSecondsRealtime(titleAppearDelayAfterDialogue);
        yield return PlayTitleTransition();

        for (int i = 1; i < dialogueCanvases.Length; i++)
        {
            yield return PlayDialogueCanvas(i);
        }
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
            yield return new WaitUntil(() => advanceRequested);
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

        if (dialogueGroup != null)
        {
            SetGroup(dialogueGroup, visible ? 1f : 0f, visible);
        }
        else if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(visible);
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
            yield return new WaitUntil(() => advanceRequested);
            advanceRequested = false;
        }
    }

    private void EnsureChoicePanel(PrologueDialogueLine line)
    {
        if (choicePanel != null)
        {
            RefreshChoiceButtons(line);
            return;
        }

        RectTransform dialoguePanel = dialogueText != null
            ? dialogueText.transform.parent as RectTransform
            : null;
        Transform parent = dialogueCanvasRoot != null ? dialogueCanvasRoot.transform : null;

        if (parent == null && dialoguePanel != null)
        {
            parent = dialoguePanel.parent;
        }

        if (parent == null)
        {
            return;
        }

        GameObject panelObject = new GameObject("DialogueChoices", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        choicePanel = panelObject.GetComponent<RectTransform>();
        choicePanel.anchorMin = new Vector2(1f, 0f);
        choicePanel.anchorMax = new Vector2(1f, 0f);
        choicePanel.pivot = new Vector2(1f, 0f);
        choicePanel.anchoredPosition = GetChoicePanelPosition(dialoguePanel);
        choicePanel.sizeDelta = new Vector2(340f, 174f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RefreshChoiceButtons(line);
        choicePanel.gameObject.SetActive(false);
    }

    private Vector2 GetChoicePanelPosition(RectTransform dialoguePanel)
    {
        if (dialoguePanel == null)
        {
            return new Vector2(-64f, 260f);
        }

        return new Vector2(
            dialoguePanel.anchoredPosition.x + dialoguePanel.rect.xMax - 48f,
            dialoguePanel.anchoredPosition.y + dialoguePanel.rect.yMax + 18f);
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
        GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(choicePanel, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(340f, 50f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.06f, 0.88f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateChoiceButtonColors();
        button.onClick.AddListener(() => SelectChoice(option));

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 50f;
        layoutElement.minHeight = 50f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(18f, 4f);
        textTransform.offsetMax = new Vector2(-18f, -4f);

        TextMeshProUGUI labelText = textObject.GetComponent<TextMeshProUGUI>();
        labelText.text = option.choiceText;
        labelText.fontSize = 32f;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineRight;
        labelText.raycastTarget = false;

        if (dialogueText != null && dialogueText.font != null)
        {
            labelText.font = dialogueText.font;
        }
    }

    private ColorBlock CreateChoiceButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.05f, 0.05f, 0.06f, 0.88f);
        colors.highlightedColor = new Color(0.18f, 0.16f, 0.12f, 0.95f);
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

    private IEnumerator PlayTitleTransition()
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
        yield return FadeGroup(titleGroup, 0f, 1f, titleFadeDuration, true);
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
