using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PostVictoryDialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;
}

[System.Serializable]
public class PostVictoryDialogueCanvas
{
    public string missionId;
    public string canvasName;
    public GameObject canvasRoot;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Image characterImage;
    public AudioSource voiceSource;
    public PostVictoryDialogueLine[] lines;
}

public class PostVictoryMainDialogueController : MonoBehaviour
{
    [Header("Dialogue Canvases")]
    [SerializeField] private PostVictoryDialogueCanvas[] dialogueCanvases;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.028f;
    [SerializeField] private float lineEndDelay = 0.08f;
    [SerializeField] private float fadeDuration = 0.16f;
    [SerializeField] private float rewardToastDuration = 1.65f;

    private PostVictoryDialogueCanvas activeCanvas;
    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private Coroutine typingRoutine;
    private Coroutine playRoutine;
    private bool destroyWhenFinished;
    private readonly List<GameObject> hiddenHouseMasterObjects = new List<GameObject>();
    private readonly List<bool> hiddenHouseMasterOriginalStates = new List<bool>();

    public static void PlayPendingIfAny(Transform sceneRoot)
    {
        string missionId = PlayerPrefs.GetString(BattleScoreStore.PendingPostVictoryDialogueKey, string.Empty);
        if (string.IsNullOrWhiteSpace(missionId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(BattleScoreStore.PendingPostVictoryDialogueKey);
        MarkDialogueAsShown(missionId);
        PlayerPrefs.Save();

        PostVictoryMainDialogueController controller = FindSceneController();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("PostVictoryDialogueController");
            if (sceneRoot != null)
            {
                controllerObject.transform.SetParent(sceneRoot, false);
            }

            controller = controllerObject.AddComponent<PostVictoryMainDialogueController>();
            controller.destroyWhenFinished = true;
        }

        controller.EnsureDialogueHierarchy();
        controller.PlayMission(missionId);
    }

    public void EnsureDialogueHierarchy()
    {
        EnsureDefaultCanvasEntries();

        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            PostVictoryDialogueCanvas entry = dialogueCanvases[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.canvasRoot == null)
            {
                Transform existing = transform.Find(entry.canvasName);
                entry.canvasRoot = existing != null ? existing.gameObject : CreateDialogueCanvas(entry.canvasName, entry);
            }

            PrepareDialogueCanvas(entry);
        }

        HideAllCanvases();
    }

    public void PlayMission(string missionId)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureDialogueHierarchy();
        PostVictoryDialogueCanvas targetCanvas = FindCanvasEntry(missionId);
        if (targetCanvas == null || targetCanvas.lines == null || targetCanvas.lines.Length <= 0)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayDialogue(targetCanvas));
    }

    private static PostVictoryMainDialogueController FindSceneController()
    {
        PostVictoryMainDialogueController[] controllers = Resources.FindObjectsOfTypeAll<PostVictoryMainDialogueController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            PostVictoryMainDialogueController controller = controllers[i];
            if (controller == null || !controller.gameObject.scene.IsValid())
            {
                continue;
            }

            if (controller.gameObject.scene == SceneManager.GetActiveScene())
            {
                return controller;
            }
        }

        return null;
    }

    private void Awake()
    {
        EnsureDialogueHierarchy();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            PlayPendingIfAny(transform.root);
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureDialogueHierarchy();
        }
    }

    private static void MarkDialogueAsShown(string missionId)
    {
        if (missionId == BattleScoreStore.DefaultMissionId)
        {
            PlayerPrefs.SetInt(BattleScoreStore.IdapenFirstVictoryDialogueShownKey, 1);
        }
        else if (missionId == BattleScoreStore.KarimHasanMissionId)
        {
            PlayerPrefs.SetInt(BattleScoreStore.KarimFirstVictoryDialogueShownKey, 1);
        }
    }

    private void OnDisable()
    {
        RestoreMissionHouseMasterImages();
    }

    private void Update()
    {
        if (activeCanvas == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                FinishTypingLineImmediately();
                return;
            }

            advanceRequested = true;
        }
    }

    private void EnsureDefaultCanvasEntries()
    {
        if (dialogueCanvases == null || dialogueCanvases.Length < 2)
        {
            dialogueCanvases = new PostVictoryDialogueCanvas[2];
        }

        EnsureCanvasEntry(
            0,
            BattleScoreStore.DefaultMissionId,
            "Canvas1_IdapenPostVictory",
            "\uC774\uB2E4 \uD39C \uC758\uB8B0\uAC00 \uC885\uB8CC\uB418\uC5C8\uB2E4.",
            "\uC774\uACBC\uB2E4\uACE0 \uD574\uC11C \uBE5A\uC774 \uC0AC\uB77C\uC9C0\uB294 \uAC74 \uC544\uB2C8\uB2E4, ZERO.\n\uD558\uC9C0\uB9CC \uC624\uB298\uC758 \uC120\uD0DD\uC740 \uD655\uC2E4\uD788 \uAE30\uB85D\uD574\uB450\uC9C0.");

        EnsureCanvasEntry(
            1,
            BattleScoreStore.KarimHasanMissionId,
            "Canvas2_KarimHasanPostVictory",
            "\uCE74\uB9BC \uD558\uC0B0 \uC758\uB8B0\uAC00 \uC815\uC0C1\uC801\uC73C\uB85C \uC885\uB8CC\uB418\uC5C8\uB2E4.",
            "\uC88B\uC740 \uC77C\uC744 \uD588\uB2E4\uACE0 \uBBFF\uACE0 \uC2F6\uACA0\uC9C0, ZERO.\n\uADF8\uB7EC\uB098 \uC6B0\uB9AC\uC5D0\uAC8C \uC911\uC694\uD55C \uAC74 \uACB0\uACFC\uC640 \uAE30\uB85D\uC774\uB2E4.");
    }

    private void EnsureCanvasEntry(int index, string missionId, string canvasName, string firstLine, string secondLine)
    {
        if (dialogueCanvases[index] == null)
        {
            dialogueCanvases[index] = new PostVictoryDialogueCanvas();
        }

        PostVictoryDialogueCanvas entry = dialogueCanvases[index];
        if (string.IsNullOrWhiteSpace(entry.missionId))
        {
            entry.missionId = missionId;
        }

        if (string.IsNullOrWhiteSpace(entry.canvasName))
        {
            entry.canvasName = canvasName;
        }

        if (entry.lines == null || entry.lines.Length <= 0)
        {
            entry.lines = new[]
            {
                new PostVictoryDialogueLine
                {
                    speakerName = "\uD558\uC6B0\uC2A4\uB9C8\uC2A4\uD130",
                    dialogue = firstLine
                },
                new PostVictoryDialogueLine
                {
                    speakerName = "\uD558\uC6B0\uC2A4\uB9C8\uC2A4\uD130",
                    dialogue = secondLine
                }
            };
        }
    }

    private GameObject CreateDialogueCanvas(string canvasName, PostVictoryDialogueCanvas entry)
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 25000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject dimObject = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimObject.transform.SetParent(canvasObject.transform, false);
        Stretch(dimObject.GetComponent<RectTransform>());
        Image dim = dimObject.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.54f);
        dim.raycastTarget = true;

        GameObject panelObject = new GameObject("DialoguePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 44f);
        panelRect.sizeDelta = new Vector2(1540f, 250f);
        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(1f, 1f, 1f, 0.9f);
        panel.raycastTarget = true;

        GameObject namePanelObject = new GameObject("NamePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        namePanelObject.transform.SetParent(panelObject.transform, false);
        RectTransform namePanelRect = namePanelObject.GetComponent<RectTransform>();
        namePanelRect.anchorMin = new Vector2(0f, 1f);
        namePanelRect.anchorMax = new Vector2(0f, 1f);
        namePanelRect.pivot = new Vector2(0f, 0.5f);
        namePanelRect.anchoredPosition = new Vector2(86f, -34f);
        namePanelRect.sizeDelta = new Vector2(430f, 76f);
        Image namePanel = namePanelObject.GetComponent<Image>();
        namePanel.color = new Color(1f, 1f, 1f, 0.9f);
        namePanel.raycastTarget = false;

        entry.nameText = CreateText("NameText", namePanelObject.transform, 36f, TextAlignmentOptions.Center);
        Stretch(entry.nameText.rectTransform);
        entry.nameText.color = Color.black;

        entry.dialogueText = CreateText("DialogueText", panelObject.transform, 36f, TextAlignmentOptions.MidlineLeft);
        RectTransform dialogueRect = entry.dialogueText.rectTransform;
        dialogueRect.anchorMin = new Vector2(0f, 1f);
        dialogueRect.anchorMax = new Vector2(0f, 1f);
        dialogueRect.pivot = new Vector2(0f, 0.5f);
        dialogueRect.anchoredPosition = new Vector2(44f, -124f);
        dialogueRect.sizeDelta = new Vector2(1350f, 126f);
        entry.dialogueText.color = Color.black;

        GameObject characterObject = new GameObject("CharacterImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        characterObject.transform.SetParent(canvasObject.transform, false);
        entry.characterImage = characterObject.GetComponent<Image>();
        entry.characterImage.enabled = false;
        return canvasObject;
    }

    private void PrepareDialogueCanvas(PostVictoryDialogueCanvas entry)
    {
        if (entry.canvasRoot == null)
        {
            return;
        }

        if (entry.nameText == null)
        {
            Transform nameTransform = entry.canvasRoot.transform.Find("DialoguePanel/NamePanel/NameText");
            entry.nameText = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;
        }

        if (entry.dialogueText == null)
        {
            Transform dialogueTransform = entry.canvasRoot.transform.Find("DialoguePanel/DialogueText");
            entry.dialogueText = dialogueTransform != null ? dialogueTransform.GetComponent<TMP_Text>() : null;
        }

        if (entry.characterImage == null)
        {
            Transform characterTransform = entry.canvasRoot.transform.Find("CharacterImage");
            entry.characterImage = characterTransform != null ? characterTransform.GetComponent<Image>() : null;
        }

        CanvasGroup group = entry.canvasRoot.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = entry.canvasRoot.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private PostVictoryDialogueCanvas FindCanvasEntry(string missionId)
    {
        EnsureDefaultCanvasEntries();

        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            PostVictoryDialogueCanvas entry = dialogueCanvases[i];
            if (entry != null && entry.missionId == missionId)
            {
                return entry;
            }
        }

        return dialogueCanvases.Length > 0 ? dialogueCanvases[0] : null;
    }

    private IEnumerator PlayDialogue(PostVictoryDialogueCanvas targetCanvas)
    {
        HideAllCanvases();
        HideMissionHouseMasterImages();
        activeCanvas = targetCanvas;
        lineIndex = 0;

        CanvasGroup group = activeCanvas.canvasRoot.GetComponent<CanvasGroup>();
        activeCanvas.canvasRoot.SetActive(true);
        yield return FadeCanvas(group, 0f, 1f, fadeDuration);

        for (lineIndex = 0; lineIndex < activeCanvas.lines.Length; lineIndex++)
        {
            PostVictoryDialogueLine line = activeCanvas.lines[lineIndex];
            ShowLine(activeCanvas, line);
            typingRoutine = StartCoroutine(TypeLine(activeCanvas.dialogueText, line.dialogue));
            yield return typingRoutine;
            typingRoutine = null;

            advanceRequested = false;
            yield return new WaitForSecondsRealtime(lineEndDelay);
            while (!advanceRequested)
            {
                yield return null;
            }
        }

        yield return FadeCanvas(group, 1f, 0f, fadeDuration);
        activeCanvas.canvasRoot.SetActive(false);
        activeCanvas = null;
        playRoutine = null;
        RestoreMissionHouseMasterImages();

        if (targetCanvas.missionId == BattleScoreStore.DefaultMissionId)
        {
            yield return ShowRewardToast();
        }

        if (destroyWhenFinished)
        {
            Destroy(gameObject);
        }
    }

    private void HideMissionHouseMasterImages()
    {
        RestoreMissionHouseMasterImages();

        GameObject missionPanel = GameObject.Find("MissionPanel");
        if (missionPanel != null)
        {
            CollectHouseMasterObjects(missionPanel.transform);
        }

        if (hiddenHouseMasterObjects.Count <= 0)
        {
            GameObject direct = GameObject.Find("HouseMaster");
            if (direct != null)
            {
                AddHouseMasterObject(direct);
            }
        }

        for (int i = 0; i < hiddenHouseMasterObjects.Count; i++)
        {
            if (hiddenHouseMasterObjects[i] != null)
            {
                hiddenHouseMasterObjects[i].SetActive(false);
            }
        }
    }

    private void RestoreMissionHouseMasterImages()
    {
        for (int i = 0; i < hiddenHouseMasterObjects.Count; i++)
        {
            GameObject target = hiddenHouseMasterObjects[i];
            if (target != null)
            {
                target.SetActive(hiddenHouseMasterOriginalStates[i]);
            }
        }

        hiddenHouseMasterObjects.Clear();
        hiddenHouseMasterOriginalStates.Clear();
    }

    private void CollectHouseMasterObjects(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == root)
            {
                continue;
            }

            string objectName = child.gameObject.name;
            if (objectName == "HouseMaster" || objectName == "HouseMasterBackground")
            {
                AddHouseMasterObject(child.gameObject);
            }
        }
    }

    private void AddHouseMasterObject(GameObject target)
    {
        if (target == null || hiddenHouseMasterObjects.Contains(target))
        {
            return;
        }

        hiddenHouseMasterObjects.Add(target);
        hiddenHouseMasterOriginalStates.Add(target.activeSelf);
    }

    private void ShowLine(PostVictoryDialogueCanvas canvas, PostVictoryDialogueLine line)
    {
        if (canvas.nameText != null)
        {
            canvas.nameText.text = line.speakerName;
        }

        if (canvas.characterImage != null)
        {
            canvas.characterImage.sprite = line.characterSprite;
            canvas.characterImage.enabled = line.characterSprite != null;
        }

        if (canvas.voiceSource != null)
        {
            canvas.voiceSource.Stop();
            canvas.voiceSource.clip = line.voiceClip;
            if (line.voiceClip != null)
            {
                canvas.voiceSource.Play();
            }
        }
    }

    private void FinishTypingLineImmediately()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (activeCanvas != null && activeCanvas.dialogueText != null && activeCanvas.lines != null && lineIndex < activeCanvas.lines.Length)
        {
            activeCanvas.dialogueText.text = activeCanvas.lines[lineIndex].dialogue;
        }

        isTyping = false;
    }

    private IEnumerator TypeLine(TMP_Text targetText, string body)
    {
        isTyping = true;
        if (targetText != null)
        {
            targetText.text = string.Empty;
        }

        for (int i = 0; i < body.Length; i++)
        {
            if (targetText != null)
            {
                targetText.text += body[i];
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        group.blocksRaycasts = true;
        group.interactable = true;

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

        bool visible = to > 0.01f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    private IEnumerator ShowRewardToast()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddLux(50);
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;
        GameObject toastObject = new GameObject("IdapenVictoryRewardToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        toastObject.transform.SetParent(parent, false);
        toastObject.transform.SetAsLastSibling();

        RectTransform rectTransform = toastObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 210f);
        rectTransform.sizeDelta = new Vector2(760f, 96f);

        CanvasGroup group = toastObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        TextMeshProUGUI text = toastObject.GetComponent<TextMeshProUGUI>();
        text.text = "LUX 50\uC744 \uC5BB\uC5C8\uC2B5\uB2C8\uB2E4!";
        text.fontSize = 42f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = rewardToastDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / rewardToastDuration);
            float fadeIn = Mathf.Clamp01(t / 0.18f);
            float fadeOut = Mathf.Clamp01((1f - t) / 0.28f);
            float sparkle = 0.78f + Mathf.Abs(Mathf.Sin(elapsed * 18f)) * 0.22f;
            group.alpha = Mathf.Min(fadeIn, fadeOut) * sparkle;
            rectTransform.anchoredPosition = new Vector2(0f, 210f + Mathf.Sin(elapsed * 8f) * 6f);
            rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 12f) * 0.018f);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        Destroy(toastObject);
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private void HideAllCanvases()
    {
        if (dialogueCanvases == null)
        {
            return;
        }

        for (int i = 0; i < dialogueCanvases.Length; i++)
        {
            PostVictoryDialogueCanvas entry = dialogueCanvases[i];
            if (entry == null || entry.canvasRoot == null)
            {
                continue;
            }

            CanvasGroup group = entry.canvasRoot.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            entry.canvasRoot.SetActive(false);
        }
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
