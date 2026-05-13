using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
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
    [SerializeField] [Range(0f, 5f)] private float voiceVolume = 1f;

    [Header("Dialogue")]
    [SerializeField] private IdapenDialogueLine[] lines = new IdapenDialogueLine[1];
    [SerializeField] private string guardSpeakerName = "17구역 경비";
    [SerializeField] private string shakeSpeakerName = "???";
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("Timing")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float lineEndDelay = 0.08f;
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeDistance = 18f;
    [SerializeField] private float shakeInterval = 0.035f;
    [SerializeField] private float battleSceneFadeDuration = 0.8f;

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private bool choiceSelected;
    private IdapenChoiceOption selectedChoice;
    private Coroutine typingRoutine;
    private RectTransform choicePanel;
    private TMP_Text currentNameText;
    private TMP_Text currentDialogueText;
    private Image currentCharacterImage;
    private GameObject currentDialogueCanvasRoot;
    private Coroutine shakeRoutine;
    private bool sceneTransitionStarted;

    private void Awake()
    {
        EnsureDialogueCanvases();
        EnsureChoiceSlots();
        SetActiveDialogueCanvas(false);
    }

    private void Start()
    {
        StartCoroutine(PlayDialogue());
    }

    private void Update()
    {
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

            bool showedChoices = ShouldShowChoices(line);
            if (showedChoices)
            {
                yield return ShowChoices(line);

                if (IsLastDialogueLine(lineIndex))
                {
                    yield return FadeToBattleScene();
                    yield break;
                }

                yield return PlayChoiceResponse(line, selectedChoice);
            }

            yield return new WaitForSecondsRealtime(lineEndDelay);
            if (!showedChoices)
            {
                advanceRequested = false;
                yield return new WaitUntil(() => advanceRequested);
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
            currentCharacterImage.preserveAspect = true;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            voiceSource.volume = GameAudioSettings.GetVoiceSourceVolume(voiceVolume);
            if (line.voiceClip != null)
            {
                voiceSource.Play();
            }
        }

        if (ShouldShakeForSpeaker(line.speakerName))
        {
            StartShake();
        }
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
            currentCharacterImage.preserveAspect = true;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = voiceClip;
            voiceSource.volume = GameAudioSettings.GetVoiceSourceVolume(voiceVolume);
            if (voiceClip != null)
            {
                voiceSource.Play();
            }
        }

        if (ShouldShakeForSpeaker(speakerName))
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
            dialogueCanvasRoot = Instantiate(dialogueCanvasPrefab);
            dialogueCanvasRoot.name = "DialogueCanvas";
        }

        if (alternateDialogueCanvasRoot == null)
        {
            GameObject existingAlternate = GameObject.Find("DialogueCanvas (1)");
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

        if (canvasNameText == null)
        {
            canvasNameText = FindChildComponent<TMP_Text>(canvasRoot.transform, "NameText");
        }

        if (canvasDialogueText == null)
        {
            canvasDialogueText = FindChildComponent<TMP_Text>(canvasRoot.transform, "DialogueText");
        }

        if (canvasCharacterImage == null)
        {
            canvasCharacterImage = FindChildComponent<Image>(canvasRoot.transform, "CharacterImage");
        }
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
            dialogueCanvasRoot.SetActive(useGuardCanvas);
        }

        if (alternateDialogueCanvasRoot != null)
        {
            alternateDialogueCanvasRoot.SetActive(!useGuardCanvas);
        }

        currentDialogueCanvasRoot = useGuardCanvas ? dialogueCanvasRoot : alternateDialogueCanvasRoot;
        currentNameText = useGuardCanvas ? nameText : alternateNameText;
        currentDialogueText = useGuardCanvas ? dialogueText : alternateDialogueText;
        currentCharacterImage = useGuardCanvas ? characterImage : alternateCharacterImage;
    }

    private bool ShouldUseGuardCanvas(string speakerName)
    {
        return NormalizeSpeakerName(speakerName) == NormalizeSpeakerName(guardSpeakerName);
    }

    private bool ShouldShakeForSpeaker(string speakerName)
    {
        return NormalizeSpeakerName(speakerName) == NormalizeSpeakerName(shakeSpeakerName);
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
            yield return new WaitUntil(() => advanceRequested);
        }
    }

    private void EnsureChoicePanel()
    {
        if (choicePanel != null)
        {
            Transform expectedParent = currentDialogueCanvasRoot != null ? currentDialogueCanvasRoot.transform : transform;
            if (choicePanel.parent != expectedParent)
            {
                choicePanel.SetParent(expectedParent, false);
                choicePanel.anchorMin = new Vector2(0.5f, 0.5f);
                choicePanel.anchorMax = new Vector2(0.5f, 0.5f);
                choicePanel.pivot = new Vector2(0.5f, 0.5f);
                choicePanel.anchoredPosition = new Vector2(0f, 80f);
                choicePanel.sizeDelta = new Vector2(680f, 210f);
            }
            return;
        }

        Transform parent = currentDialogueCanvasRoot != null ? currentDialogueCanvasRoot.transform : transform;
        GameObject panelObject = new GameObject("DialogueChoices", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        choicePanel = panelObject.GetComponent<RectTransform>();
        choicePanel.anchorMin = new Vector2(0.5f, 0.5f);
        choicePanel.anchorMax = new Vector2(0.5f, 0.5f);
        choicePanel.pivot = new Vector2(0.5f, 0.5f);
        choicePanel.anchoredPosition = new Vector2(0f, 80f);
        choicePanel.sizeDelta = new Vector2(680f, 210f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        panelObject.SetActive(false);
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
        GameObject buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(choicePanel, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 58f;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            selectedChoice = choice;
            choiceSelected = true;
        });

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 0f);
        labelRect.offsetMax = new Vector2(-24f, 0f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = choice.choiceText;
        label.fontSize = 36f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
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

        if (!string.IsNullOrWhiteSpace(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }

    private Image CreateFadeImage()
    {
        Transform parent = currentDialogueCanvasRoot != null ? currentDialogueCanvasRoot.transform : transform;
        GameObject fadeObject = new GameObject("BattleSceneFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObject.transform.SetParent(parent, false);
        fadeObject.transform.SetAsLastSibling();

        RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = true;
        return fadeImage;
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

        while (elapsed < shakeDuration)
        {
            float offset = direction * shakeDistance;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].anchoredPosition = startPositions[i] + new Vector2(offset, 0f);
                }
            }

            direction *= -1;
            elapsed += shakeInterval;
            yield return new WaitForSecondsRealtime(shakeInterval);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].anchoredPosition = startPositions[i];
            }
        }

        shakeRoutine = null;
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

        RectTransform rectTransform = targetObject.GetComponent<RectTransform>();
        if (rectTransform != null && !targets.Contains(rectTransform))
        {
            targets.Add(rectTransform);
        }
    }
}
