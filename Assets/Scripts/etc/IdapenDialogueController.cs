using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class IdapenDialogueLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;

    public bool hasChoices;
    public IdapenChoiceOption[] choices;
}

[System.Serializable]
public class IdapenChoiceOption
{
    public string choiceText;
    public IdapenChoiceResponseLine[] responseLines = new IdapenChoiceResponseLine[1];
}

[System.Serializable]
public class IdapenChoiceResponseLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;
}

public class IdapenDialogueController : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [SerializeField] private GameObject dialogueCanvasRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image characterImage;
    [SerializeField] private GameObject alternateDialogueCanvasRoot;
    [SerializeField] private TMP_Text alternateNameText;
    [SerializeField] private TMP_Text alternateDialogueText;
    [SerializeField] private Image alternateCharacterImage;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource clickSfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 5f)] private float voiceVolume = 1f;

    [Header("Dialogue")]
    [SerializeField] private IdapenDialogueLine[] lines = new IdapenDialogueLine[1];
    [SerializeField] private string guardSpeakerName = "17구역 경비";
    [SerializeField] private string shakeSpeakerName = "???";
    [SerializeField] private string shakeDialogueText = "멈춰요!";
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string battleMissionId = BattleScoreStore.DefaultMissionId;
    [SerializeField] private string victory2ReturnSceneName = "MainScene";
    [SerializeField] private float victory2GuardFadeDuration = 0.85f;
    [SerializeField] private int victory2GuardDustCount = 24;
    [SerializeField] private int victory2Element3LineIndex = 2;
    [SerializeField] private float victory2Element3ShakeDuration = 0.32f;
    [SerializeField] private float victory2Element3ShakeDistance = 18f;
    [SerializeField] private Color victory2Element3TintColor = new Color(0.58f, 0.58f, 0.58f, 1f);

    [Header("Timing")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float lineEndDelay = 0.08f;
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeDistance = 18f;
    [SerializeField] private float shakeInterval = 0.035f;
    [SerializeField] private float battleSceneFadeDuration = 0.8f;

    [Header("Screen Shake")]
    [SerializeField] private Camera screenShakeCamera;

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private bool choiceSelected;
    private IdapenChoiceOption selectedChoice;
    private Coroutine typingRoutine;
    private RectTransform choicePanel;
    private RectTransform dialogueControlPanel;
    private TextMeshProUGUI autoButtonText;
    private TMP_Text currentNameText;
    private TMP_Text currentDialogueText;
    private Image currentCharacterImage;
    private GameObject currentDialogueCanvasRoot;
    private Coroutine shakeRoutine;
    private Coroutine victory2Element3Routine;
    private RectTransform[] activeShakeTargets;
    private Vector2[] activeShakeStartPositions;
    private Transform activeShakeCameraTransform;
    private Vector3 activeShakeCameraStartPosition;
    private bool autoModeEnabled;
    private bool currentLineHasVoice;
    private bool sceneTransitionStarted;
    private bool victory2GuardExitEffectPlayed;
    private bool victory2Element3EffectPlayed;
    private int lastClickSfxFrame = -1;
    private const float ChoiceButtonHeight = 58f;
    private const float ChoiceFontSize = 41f;
    private const float ChoicePanelWidth = 680f;

    private void Awake()
    {
        EnsureDialogueCanvases();
        EnsureChoiceSlots();
        SetActiveDialogueCanvas(false);
        EnsureDialogueControlButtons();
    }

    private void Start()
    {
        if (ShouldReturnToMainFromVictory2() && !IsIdapenMission())
        {
            SetActiveDialogueCanvas(false);
            return;
        }

        StartCoroutine(PlayDialogue());
    }

    private bool IsIdapenMission()
    {
        string missionId = PlayerPrefs.GetString("LastVictoryMission", BattleScoreStore.DefaultMissionId);
        return missionId == BattleScoreStore.DefaultMissionId;
    }

    private void OnDisable()
    {
        ResetActiveShake();
    }

    private void Update()
    {
        if (choicePanel != null && choicePanel.gameObject.activeInHierarchy)
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

private IEnumerator PlayDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            yield break;
        }

        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            IdapenDialogueLine line = lines[lineIndex];
            if (line == null)
            {
                continue;
            }

            ShowLine(line);
            typingRoutine = StartCoroutine(TypeLine(line.dialogue));
            yield return typingRoutine;
            typingRoutine = null;

            yield return PlayVictory2GuardExitEffectIfNeeded(line);

            bool showedChoices = ShouldShowChoices(line);
            if (showedChoices)
            {
                yield return ShowChoices(line);
                yield return PlayChoiceResponse(line, selectedChoice);

                if (IsLastDialogueLine(lineIndex))
                {
                    yield return FadeToBattleScene();
                    yield break;
                }
            }

            yield return new WaitForSecondsRealtime(lineEndDelay);
            if (!showedChoices)
            {
                advanceRequested = false;
                yield return WaitForAdvanceOrAuto();
                advanceRequested = false;
            }
        }
    }

    private void ShowLine(IdapenDialogueLine line)
    {
        SetActiveDialogueCanvas(ShouldUseGuardCanvas(line.speakerName));

        if (currentNameText != null)
        {
            currentNameText.text = line.speakerName;
        }

        if (currentCharacterImage != null)
        {
            currentCharacterImage.sprite = line.characterSprite;
            currentCharacterImage.enabled = line.characterSprite != null;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            voiceSource.volume = GameAudioSettings.GetVoiceSourceVolume(voiceVolume);
            currentLineHasVoice = line.voiceClip != null;
            if (line.voiceClip != null)
            {
                voiceSource.Play();
            }
        }
        else
        {
            currentLineHasVoice = false;
        }

        if (ShouldShakeForLine(line.speakerName, line.dialogue))
        {
            StartShake();
        }

        PlayVictory2Element3EffectIfNeeded();
    }

    private void ShowLine(IdapenChoiceResponseLine line, IdapenDialogueLine fallbackLine)
    {
        string speakerName = string.IsNullOrWhiteSpace(line.speakerName) ? fallbackLine.speakerName : line.speakerName;
        Sprite characterSprite = line.characterSprite != null ? line.characterSprite : fallbackLine.characterSprite;
        AudioClip voiceClip = line.voiceClip != null ? line.voiceClip : fallbackLine.voiceClip;

        SetActiveDialogueCanvas(ShouldUseGuardCanvas(speakerName));

        if (currentNameText != null)
        {
            currentNameText.text = speakerName;
        }

        if (currentCharacterImage != null)
        {
            currentCharacterImage.sprite = characterSprite;
            currentCharacterImage.enabled = characterSprite != null;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = voiceClip;
            voiceSource.volume = GameAudioSettings.GetVoiceSourceVolume(voiceVolume);
            currentLineHasVoice = voiceClip != null;
            if (voiceClip != null)
            {
                voiceSource.Play();
            }
        }
        else
        {
            currentLineHasVoice = false;
        }

        if (ShouldShakeForLine(speakerName, line.dialogue))
        {
            StartShake();
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTypingRequested = false;

        if (currentDialogueText != null)
        {
            currentDialogueText.text = "";
        }

        string text = line ?? "";
        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested)
            {
                break;
            }

            builder.Append(text[i]);
            if (currentDialogueText != null)
            {
                currentDialogueText.text = builder.ToString();
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        if (currentDialogueText != null)
        {
            currentDialogueText.text = text;
        }

        isTyping = false;
    }

    private void EnsureDialogueCanvases()
    {
        if (dialogueCanvasRoot == null && dialogueCanvasPrefab != null)
        {
            dialogueCanvasRoot = FindSceneObjectByName("DialogueCanvas");

            if (dialogueCanvasRoot == null)
            {
                dialogueCanvasRoot = Instantiate(dialogueCanvasPrefab);
                dialogueCanvasRoot.name = "DialogueCanvas";
            }
        }

        if (alternateDialogueCanvasRoot == null || alternateDialogueCanvasRoot == dialogueCanvasRoot)
        {
            GameObject existingAlternate = FindSceneObjectByName("DialogueCanvas (1)");
            if (existingAlternate == null)
            {
                existingAlternate = FindSceneObjectByName("DialogueCanvas 1");
            }

            if (existingAlternate != null)
            {
                alternateDialogueCanvasRoot = existingAlternate;
            }
            else if (dialogueCanvasPrefab != null)
            {
                alternateDialogueCanvasRoot = Instantiate(dialogueCanvasPrefab);
                alternateDialogueCanvasRoot.name = "DialogueCanvas (1)";
            }
        }

        PrepareDialogueCanvas(dialogueCanvasRoot, ref nameText, ref dialogueText, ref characterImage);
        PrepareDialogueCanvas(alternateDialogueCanvasRoot, ref alternateNameText, ref alternateDialogueText, ref alternateCharacterImage);
    }

    private void PrepareDialogueCanvas(GameObject canvasRoot, ref TMP_Text canvasNameText, ref TMP_Text canvasDialogueText, ref Image canvasCharacterImage)
    {
        if (canvasRoot == null)
        {
            return;
        }

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        RectTransform rectTransform = canvasRoot.GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
        {
            rectTransform.localScale = Vector3.one;
        }

        if (canvasRoot.transform.localScale == Vector3.zero)
        {
            canvasRoot.transform.localScale = Vector3.one;
        }

        if (canvasNameText == null || !canvasNameText.transform.IsChildOf(canvasRoot.transform))
        {
            canvasNameText = FindChildComponent<TMP_Text>(canvasRoot.transform, "NameText");
        }

        if (canvasDialogueText == null || !canvasDialogueText.transform.IsChildOf(canvasRoot.transform))
        {
            canvasDialogueText = FindChildComponent<TMP_Text>(canvasRoot.transform, "DialogueText");
        }

        if (canvasCharacterImage == null || !canvasCharacterImage.transform.IsChildOf(canvasRoot.transform))
        {
            canvasCharacterImage = FindChildComponent<Image>(canvasRoot.transform, "CharacterImage");
            if (canvasCharacterImage == null)
            {
                canvasCharacterImage = FindChildComponent<Image>(canvasRoot.transform, "CharacterImage2");
            }
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
#if UNITY_2023_1_OR_NEWER
        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
#endif
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null && objects[i].scene == gameObject.scene && objects[i].name == objectName)
            {
                return objects[i];
            }
        }

        return null;
    }

    private T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i].GetComponent<T>();
            }
        }

        return null;
    }

    private void SetActiveDialogueCanvas(bool useGuardCanvas)
    {
        if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(!useGuardCanvas);
        }

        if (alternateDialogueCanvasRoot != null)
        {
            alternateDialogueCanvasRoot.SetActive(useGuardCanvas);
        }

        currentDialogueCanvasRoot = useGuardCanvas ? alternateDialogueCanvasRoot : dialogueCanvasRoot;
        currentNameText = useGuardCanvas ? alternateNameText : nameText;
        currentDialogueText = useGuardCanvas ? alternateDialogueText : dialogueText;
        currentCharacterImage = useGuardCanvas ? alternateCharacterImage : characterImage;
        EnsureDialogueControlButtons();
    }

    private void EnsureDialogueControlButtons()
    {
        EnsureEventSystem();

        RectTransform dialoguePanel = currentDialogueText != null
            ? currentDialogueText.transform.parent as RectTransform
            : null;

        if (dialoguePanel == null)
        {
            return;
        }

        if (dialogueControlPanel != null)
        {
            if (dialogueControlPanel.parent != dialoguePanel)
            {
                dialogueControlPanel.SetParent(dialoguePanel, false);
            }

            PlaceDialogueControlPanel();
            dialogueControlPanel.SetAsLastSibling();
            return;
        }

        GameObject panelObject = new GameObject("DialogueControlButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        panelObject.transform.SetParent(dialoguePanel, false);

        dialogueControlPanel = panelObject.GetComponent<RectTransform>();
        PlaceDialogueControlPanel();

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

    private void PlaceDialogueControlPanel()
    {
        if (dialogueControlPanel == null)
        {
            return;
        }

        dialogueControlPanel.anchorMin = new Vector2(1f, 0f);
        dialogueControlPanel.anchorMax = new Vector2(1f, 0f);
        dialogueControlPanel.pivot = new Vector2(1f, 0f);
        dialogueControlPanel.anchoredPosition = new Vector2(-54f, 24f);
        dialogueControlPanel.sizeDelta = new Vector2(190f, 42f);
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
        button.onClick.AddListener(PlayClickSfx);
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
        labelText.color = GetControlTextColor(false);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;

        if (currentDialogueText != null && currentDialogueText.font != null)
        {
            labelText.font = currentDialogueText.font;
        }

        AddTextHover(buttonObject.GetComponent<EventTrigger>(), labelText);
        return labelText;
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
        if (sceneTransitionStarted)
        {
            return;
        }

        StartCoroutine(FadeToBattleScene());
    }

    private void ToggleAutoMode()
    {
        PlayClickSfx();
        autoModeEnabled = !autoModeEnabled;

        if (autoButtonText != null)
        {
            autoButtonText.color = GetControlTextColor(true);
        }
    }

    private IEnumerator WaitForAdvanceOrAuto()
    {
        while (!advanceRequested)
        {
            if (autoModeEnabled)
            {
                yield return WaitForVoiceToFinishOrAutoCancel();

                if (advanceRequested || autoModeEnabled)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator WaitForVoiceToFinishOrAutoCancel()
    {
        if (currentLineHasVoice)
        {
            yield return new WaitWhile(() =>
                autoModeEnabled
                && !advanceRequested
                && voiceSource != null
                && voiceSource.isPlaying);
        }
        else
        {
            float elapsed = 0f;
            while (autoModeEnabled && !advanceRequested && elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private bool ShouldUseGuardCanvas(string speakerName)
    {
        return NormalizeSpeakerName(speakerName) == NormalizeSpeakerName(guardSpeakerName);
    }

    private bool ShouldShakeForSpeaker(string speakerName)
    {
        return NormalizeSpeakerName(speakerName) == NormalizeSpeakerName(shakeSpeakerName);
    }

    private bool ShouldShakeForLine(string speakerName, string dialogue)
    {
        bool speakerMatches = ShouldShakeForSpeaker(speakerName);
        string normalizedShakeText = NormalizeSpeakerName(shakeDialogueText);
        bool dialogueMatches = !string.IsNullOrEmpty(normalizedShakeText)
            && NormalizeSpeakerName(dialogue).Contains(normalizedShakeText);

        return speakerMatches || dialogueMatches;
    }

    private string NormalizeSpeakerName(string speakerName)
    {
        return string.IsNullOrWhiteSpace(speakerName) ? "" : speakerName.Trim();
    }

    private bool ShouldShowChoices(IdapenDialogueLine line)
    {
        return line != null && line.hasChoices && GetChoiceCount(line) > 0;
    }

    private int GetChoiceCount(IdapenDialogueLine line)
    {
        if (line == null || line.choices == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < line.choices.Length; i++)
        {
            if (line.choices[i] != null && !string.IsNullOrWhiteSpace(line.choices[i].choiceText))
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerator ShowChoices(IdapenDialogueLine line)
    {
        EnsureChoicePanel();
        RefreshChoiceButtons(line);

        choiceSelected = false;
        selectedChoice = null;
        choicePanel.gameObject.SetActive(true);
        choicePanel.SetAsLastSibling();

        yield return new WaitUntil(() => choiceSelected);

        choicePanel.gameObject.SetActive(false);
    }

    private IEnumerator PlayChoiceResponse(IdapenDialogueLine sourceLine, IdapenChoiceOption choice)
    {
        if (choice == null || choice.responseLines == null)
        {
            yield break;
        }

        for (int i = 0; i < choice.responseLines.Length; i++)
        {
            IdapenChoiceResponseLine responseLine = choice.responseLines[i];
            if (responseLine == null || string.IsNullOrWhiteSpace(responseLine.dialogue))
            {
                continue;
            }

            ShowLine(responseLine, sourceLine);
            typingRoutine = StartCoroutine(TypeLine(responseLine.dialogue));
            yield return typingRoutine;
            typingRoutine = null;

            yield return new WaitForSecondsRealtime(lineEndDelay);
            advanceRequested = false;
            yield return WaitForAdvanceOrAuto();
            advanceRequested = false;
        }
    }

    private void EnsureChoicePanel()
    {
        RectTransform dialoguePanel = currentDialogueText != null
            ? currentDialogueText.transform.parent as RectTransform
            : null;

        if (choicePanel != null)
        {
            PlaceChoicePanel(dialoguePanel);
            return;
        }

        Transform parent = dialoguePanel != null
            ? dialoguePanel
            : currentDialogueCanvasRoot != null ? currentDialogueCanvasRoot.transform : transform;
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

        panelObject.SetActive(false);
    }

    private void PlaceChoicePanel(RectTransform dialoguePanel)
    {
        if (choicePanel == null)
        {
            return;
        }

        Transform parent = dialoguePanel != null
            ? dialoguePanel
            : currentDialogueCanvasRoot != null ? currentDialogueCanvasRoot.transform : transform;

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

    private void RefreshChoiceButtons(IdapenDialogueLine line)
    {
        EnsureChoicePanel();

        for (int i = choicePanel.childCount - 1; i >= 0; i--)
        {
            Destroy(choicePanel.GetChild(i).gameObject);
        }

        if (line == null || line.choices == null)
        {
            return;
        }

        for (int i = 0; i < line.choices.Length; i++)
        {
            IdapenChoiceOption choice = line.choices[i];
            if (choice == null || string.IsNullOrWhiteSpace(choice.choiceText))
            {
                continue;
            }

            CreateChoiceButton(choice);
        }
    }

    private void CreateChoiceButton(IdapenChoiceOption choice)
    {
        GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(EventTrigger));
        buttonObject.transform.SetParent(choicePanel, false);

        float buttonWidth = GetChoiceButtonWidth(choice.choiceText);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(buttonWidth, ChoiceButtonHeight);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.06f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateChoiceButtonColors();
        button.onClick.AddListener(() =>
        {
            selectedChoice = choice;
            choiceSelected = true;
        });

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = buttonWidth;
        layoutElement.minWidth = buttonWidth;
        layoutElement.preferredHeight = ChoiceButtonHeight;
        layoutElement.minHeight = ChoiceButtonHeight;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 4f);
        labelRect.offsetMax = new Vector2(-18f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = choice.choiceText;
        label.fontSize = ChoiceFontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        if (currentDialogueText != null && currentDialogueText.font != null)
        {
            label.font = currentDialogueText.font;
        }

        EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => label.color = new Color(0.62f, 0.62f, 0.62f, 1f));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => label.color = Color.white);
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

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private bool IsLastDialogueLine(int index)
    {
        return lines != null && index >= lines.Length - 1;
    }

    private IEnumerator FadeToBattleScene()
    {
        if (sceneTransitionStarted)
        {
            yield break;
        }

        sceneTransitionStarted = true;
        Image fadeImage = CreateFadeImage();
        yield return FadeImage(fadeImage, 0f, 1f, battleSceneFadeDuration);

        if (ShouldReturnToMainFromVictory2())
        {
            if (!string.IsNullOrWhiteSpace(victory2ReturnSceneName))
            {
                QueuePostVictoryDialogueIfNeeded();
                SceneManager.LoadScene(victory2ReturnSceneName);
            }
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(battleSceneName))
        {
            PlayerPrefs.SetString(BattleScoreStore.ActiveBattleMissionKey, ResolveBattleMissionId());
            PlayerPrefs.Save();
            SceneManager.LoadScene(battleSceneName);
        }
    }

    private string ResolveBattleMissionId()
    {
        if (!string.IsNullOrWhiteSpace(battleMissionId)
            && battleMissionId != BattleScoreStore.DefaultMissionId)
        {
            return battleMissionId;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName) && sceneName.ToLowerInvariant().Contains("karim"))
        {
            return BattleScoreStore.KarimHasanMissionId;
        }

        return BattleScoreStore.DefaultMissionId;
    }

    private Image CreateFadeImage()
    {
        return SceneFadeOverlay.CreateImage("BattleSceneFade");
    }

    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        if (image == null)
        {
            yield break;
        }

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

    private void EnsureChoiceSlots()
    {
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null || !lines[i].hasChoices)
            {
                continue;
            }

            if (lines[i].choices == null || lines[i].choices.Length == 0)
            {
                lines[i].choices = new IdapenChoiceOption[2];
            }

            for (int j = 0; j < lines[i].choices.Length; j++)
            {
                if (lines[i].choices[j] == null)
                {
                    lines[i].choices[j] = new IdapenChoiceOption();
                }

                if (lines[i].choices[j].responseLines == null || lines[i].choices[j].responseLines.Length == 0)
                {
                    lines[i].choices[j].responseLines = new IdapenChoiceResponseLine[1];
                }
            }
        }
    }

    private void StartShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            ResetActiveShake();
        }

        shakeRoutine = StartCoroutine(ShakeScreen());
    }

    private IEnumerator ShakeScreen()
    {
        RectTransform[] targets = GetScreenShakeTargets();
        Vector2[] startPositions = new Vector2[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            startPositions[i] = targets[i] != null ? targets[i].anchoredPosition : Vector2.zero;
        }

        float elapsed = 0f;
        int direction = 1;
        Camera shakeCamera = GetScreenShakeCamera();
        Transform cameraTransform = shakeCamera != null ? shakeCamera.transform : null;
        Vector3 cameraStartPosition = cameraTransform != null ? cameraTransform.localPosition : Vector3.zero;
        float cameraShakeDistance = GetCameraShakeDistance(shakeCamera);
        activeShakeTargets = targets;
        activeShakeStartPositions = startPositions;
        activeShakeCameraTransform = cameraTransform;
        activeShakeCameraStartPosition = cameraStartPosition;

        while (elapsed < shakeDuration)
        {
            float offset = direction * shakeDistance;
            float cameraOffset = direction * cameraShakeDistance;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].anchoredPosition = startPositions[i] + new Vector2(offset, 0f);
                }
            }

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = cameraStartPosition + new Vector3(cameraOffset, 0f, 0f);
            }

            direction *= -1;
            elapsed += shakeInterval;
            yield return new WaitForSecondsRealtime(shakeInterval);
        }

        ResetActiveShake();
        shakeRoutine = null;
    }

    private void ResetActiveShake()
    {
        if (activeShakeTargets != null && activeShakeStartPositions != null)
        {
            int count = Mathf.Min(activeShakeTargets.Length, activeShakeStartPositions.Length);
            for (int i = 0; i < count; i++)
            {
                if (activeShakeTargets[i] != null)
                {
                    activeShakeTargets[i].anchoredPosition = activeShakeStartPositions[i];
                }
            }
        }

        if (activeShakeCameraTransform != null)
        {
            activeShakeCameraTransform.localPosition = activeShakeCameraStartPosition;
        }

        activeShakeTargets = null;
        activeShakeStartPositions = null;
        activeShakeCameraTransform = null;
        activeShakeCameraStartPosition = Vector3.zero;
    }

    private Camera GetScreenShakeCamera()
    {
        if (screenShakeCamera != null)
        {
            return screenShakeCamera;
        }

        return Camera.main;
    }

    private float GetCameraShakeDistance(Camera shakeCamera)
    {
        if (shakeCamera == null)
        {
            return 0f;
        }

        if (shakeCamera.orthographic && Screen.height > 0)
        {
            return shakeDistance * (shakeCamera.orthographicSize * 2f / Screen.height);
        }

        return shakeDistance * 0.01f;
    }

    private RectTransform[] GetScreenShakeTargets()
    {
        System.Collections.Generic.List<RectTransform> targets = new System.Collections.Generic.List<RectTransform>();
        AddShakeTarget(targets, GameObject.Find("Canvas1"));
        AddShakeTarget(targets, dialogueCanvasRoot);
        AddShakeTarget(targets, alternateDialogueCanvasRoot);
        return targets.ToArray();
    }

    private void AddShakeTarget(System.Collections.Generic.List<RectTransform> targets, GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        Canvas canvas = targetObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            int addedCountBeforeCanvas = targets.Count;
            for (int i = 0; i < targetObject.transform.childCount; i++)
            {
                RectTransform childTransform = targetObject.transform.GetChild(i) as RectTransform;
                AddShakeRectTransform(targets, childTransform);
            }

            if (targets.Count > addedCountBeforeCanvas)
            {
                return;
            }
        }

        RectTransform rectTransform = targetObject.GetComponent<RectTransform>();
        AddShakeRectTransform(targets, rectTransform);
    }

    private void AddShakeRectTransform(System.Collections.Generic.List<RectTransform> targets, RectTransform rectTransform)
    {
        if (rectTransform != null && !targets.Contains(rectTransform))
        {
            targets.Add(rectTransform);
        }
    }


private bool ShouldReturnToMainFromVictory2()
    {
        return SceneManager.GetActiveScene().name == "Victory2Scene";
    }

    private void QueuePostVictoryDialogueIfNeeded()
    {
        string missionId = PlayerPrefs.GetString("LastVictoryMission", BattleScoreStore.DefaultMissionId);

        if (missionId == BattleScoreStore.DefaultMissionId
            && PlayerPrefs.GetInt(BattleScoreStore.IdapenFirstVictoryJustWonKey, 0) == 1
            && PlayerPrefs.GetInt(BattleScoreStore.IdapenFirstVictoryDialogueShownKey, 0) == 0)
        {
            PlayerPrefs.SetString(BattleScoreStore.PendingPostVictoryDialogueKey, missionId);
        }
        else if (missionId == BattleScoreStore.KarimHasanMissionId
            && PlayerPrefs.GetInt(BattleScoreStore.KarimFirstVictoryJustWonKey, 0) == 1
            && PlayerPrefs.GetInt(BattleScoreStore.KarimFirstVictoryDialogueShownKey, 0) == 0)
        {
            PlayerPrefs.SetString(BattleScoreStore.PendingPostVictoryDialogueKey, missionId);
        }

        PlayerPrefs.DeleteKey(BattleScoreStore.IdapenFirstVictoryJustWonKey);
        PlayerPrefs.DeleteKey(BattleScoreStore.KarimFirstVictoryJustWonKey);
        PlayerPrefs.Save();
    }

    private void PlayClickSfx()
    {
        if (lastClickSfxFrame == Time.frameCount)
        {
            return;
        }

        lastClickSfxFrame = Time.frameCount;
        UIClickSoundPlayer.Play(gameObject, ref clickSfxSource, clickClip);
    }

    private void PlayVictory2Element3EffectIfNeeded()
    {
        if (!ShouldReturnToMainFromVictory2()
            || victory2Element3EffectPlayed
            || lineIndex != victory2Element3LineIndex
            || currentCharacterImage == null
            || !currentCharacterImage.enabled)
        {
            return;
        }

        victory2Element3EffectPlayed = true;
        if (victory2Element3Routine != null)
        {
            StopCoroutine(victory2Element3Routine);
        }

        victory2Element3Routine = StartCoroutine(Victory2Element3CharacterEffect(currentCharacterImage));
    }

    private IEnumerator Victory2Element3CharacterEffect(Image targetImage)
    {
        if (targetImage == null)
        {
            yield break;
        }

        RectTransform rectTransform = targetImage.rectTransform;
        Vector2 startPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        Color startColor = targetImage.color;
        float duration = Mathf.Max(0.05f, victory2Element3ShakeDuration);
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            float shake = Mathf.Sin(t * Mathf.PI * 10f) * victory2Element3ShakeDistance * (1f - t);
            float tintT = Mathf.Sin(t * Mathf.PI);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startPosition + new Vector2(shake, 0f);
            }

            targetImage.color = Color.Lerp(startColor, victory2Element3TintColor, tintT * 0.55f);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
        }

        targetImage.color = startColor;
        victory2Element3Routine = null;
    }

    private IEnumerator PlayVictory2GuardExitEffectIfNeeded(IdapenDialogueLine line)
    {
        if (!ShouldReturnToMainFromVictory2()
            || victory2GuardExitEffectPlayed
            || line == null
            || !ShouldUseGuardCanvas(line.speakerName)
            || currentCharacterImage == null
            || !currentCharacterImage.enabled)
        {
            yield break;
        }

        victory2GuardExitEffectPlayed = true;
        yield return FadeCharacterImageWithDust(currentCharacterImage);
    }

    private IEnumerator FadeCharacterImageWithDust(Image targetImage)
    {
        if (targetImage == null)
        {
            yield break;
        }

        RectTransform imageRect = targetImage.rectTransform;
        Transform dustParent = targetImage.transform.parent != null ? targetImage.transform.parent : targetImage.transform;
        int dustCount = Mathf.Max(0, victory2GuardDustCount);
        RectTransform[] dustRects = new RectTransform[dustCount];
        Image[] dustImages = new Image[dustCount];
        Vector2[] dustStarts = new Vector2[dustCount];
        Vector2[] dustEnds = new Vector2[dustCount];

        Vector2 rectSize = imageRect.rect.size;
        if (rectSize.x <= 1f || rectSize.y <= 1f)
        {
            rectSize = imageRect.sizeDelta;
        }

        for (int i = 0; i < dustCount; i++)
        {
            GameObject dust = new GameObject("GuardDust", typeof(RectTransform), typeof(Image));
            dust.transform.SetParent(dustParent, false);
            RectTransform dustRect = dust.GetComponent<RectTransform>();
            dustRect.anchorMin = imageRect.anchorMin;
            dustRect.anchorMax = imageRect.anchorMax;
            dustRect.pivot = imageRect.pivot;
            float size = Random.Range(5f, 13f);
            dustRect.sizeDelta = new Vector2(size, size);

            Vector2 start = imageRect.anchoredPosition + new Vector2(
                Random.Range(-rectSize.x * 0.24f, rectSize.x * 0.24f),
                Random.Range(rectSize.y * 0.02f, rectSize.y * 0.32f));
            Vector2 end = start + new Vector2(Random.Range(-90f, 90f), Random.Range(38f, 150f));
            dustRect.anchoredPosition = start;

            Image dustImage = dust.GetComponent<Image>();
            dustImage.color = new Color(0.74f, 0.70f, 0.66f, Random.Range(0.24f, 0.46f));
            dustImage.raycastTarget = false;

            dustRects[i] = dustRect;
            dustImages[i] = dustImage;
            dustStarts[i] = start;
            dustEnds[i] = end;
        }

        Color startColor = targetImage.color;
        float duration = Mathf.Max(0.05f, victory2GuardFadeDuration);
        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            targetImage.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, eased));

            for (int i = 0; i < dustCount; i++)
            {
                if (dustRects[i] == null || dustImages[i] == null)
                {
                    continue;
                }

                dustRects[i].anchoredPosition = Vector2.LerpUnclamped(dustStarts[i], dustEnds[i], eased);
                Color dustColor = dustImages[i].color;
                dustImages[i].color = new Color(dustColor.r, dustColor.g, dustColor.b, Mathf.Lerp(dustColor.a, 0f, t));
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        targetImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        targetImage.enabled = false;

        for (int i = 0; i < dustCount; i++)
        {
            if (dustRects[i] != null)
            {
                Destroy(dustRects[i].gameObject);
            }
        }
    }
}
