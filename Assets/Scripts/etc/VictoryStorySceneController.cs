using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class VictoryStorySceneController : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string lastVictoryMissionKey = "LastVictoryMission";
    [SerializeField] private float fallbackStoryDuration = 2.6f;
    [SerializeField] private float fadeToMainDuration = 0.65f;
    [SerializeField] private Sprite idapenBackgroundSprite;
    [SerializeField] private Sprite idapenCharacterSprite;

    private const string Canvas1Name = "Canvas1_IdapenVictory";
    private const string Canvas2Name = "Canvas2_KarimHasanVictory";
    private const string Canvas3Name = "Canvas3_DoctorOlaVictory";

    private GameObject activeVictoryCanvas;
    private Coroutine returnRoutine;
    private bool returnStarted;

    private void Awake()
    {
        BuildScene();
    }

    private void OnEnable()
    {
        BuildScene();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            BeginReturnSequence();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!returnStarted
            && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            BeginFadeToMain();
        }
    }

    private void BuildScene()
    {
        HideLegacyWinCanvas();
        EnsureEventSystem();
        EnsureVictoryCanvases();
        activeVictoryCanvas = ShowCanvasForLastMission();
    }

    private void EnsureVictoryCanvases()
    {
        EnsureIdapenVictoryCanvas();
        CreateStoryCanvas(
            Canvas2Name,
            "\uCE74\uB9BC\uD558\uC0B0 \uC758\uB8B0 \uC2B9\uB9AC",
            "\uCE74\uB9BC\uD558\uC0B0\uC758 \uC758\uB8B0\uAC00 \uC885\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.\nCanvas 2\uB97C \uCD94\uAC00\uD558\uBA74 \uC774 \uC774\uB984\uC758 \uCE94\uBC84\uC2A4\uAC00 \uC7AC\uC0DD\uB418\uACE0, \uB05D\uB098\uC790\uB9C8\uC790 \uBA54\uC778\uC52C\uC73C\uB85C \uC774\uB3D9\uD569\uB2C8\uB2E4.");

        CreateStoryCanvas(
            Canvas3Name,
            "\uB2E5\uD130\uC62C\uB77C \uC758\uB8B0 \uC2B9\uB9AC",
            "\uB2E5\uD130\uC62C\uB77C \uC758\uB8B0\uAC00 \uC885\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
    }

    private void EnsureIdapenVictoryCanvas()
    {
        Transform existing = FindSceneTransform(Canvas1Name);
        if (existing != null && existing.Find("DialoguePanel") != null)
        {
            return;
        }

        if (existing != null)
        {
            DestroySceneObject(existing.gameObject);
        }

        GameObject canvasObject = CreateBaseCanvas(Canvas1Name);
        Canvas canvas = canvasObject.GetComponent<Canvas>();

        GameObject background = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = idapenBackgroundSprite;
        backgroundImage.color = Color.white;
        backgroundImage.raycastTarget = false;
        backgroundImage.preserveAspect = false;

        GameObject character = new GameObject("CharacterImage", typeof(RectTransform), typeof(Image));
        character.transform.SetParent(canvasObject.transform, false);
        RectTransform characterRect = character.GetComponent<RectTransform>();
        characterRect.anchorMin = new Vector2(0.5f, 0f);
        characterRect.anchorMax = new Vector2(0.5f, 0f);
        characterRect.pivot = new Vector2(0.5f, 0f);
        characterRect.anchoredPosition = new Vector2(260f, 120f);
        characterRect.sizeDelta = new Vector2(660f, 900f);
        Image characterImage = character.GetComponent<Image>();
        characterImage.sprite = idapenCharacterSprite;
        characterImage.color = Color.white;
        characterImage.raycastTarget = false;
        characterImage.preserveAspect = true;

        GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 34f);
        panelRect.sizeDelta = new Vector2(1540f, 250f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.85f);
        panelImage.raycastTarget = true;

        GameObject namePanel = new GameObject("NamePanel", typeof(RectTransform), typeof(Image));
        namePanel.transform.SetParent(panel.transform, false);
        RectTransform namePanelRect = namePanel.GetComponent<RectTransform>();
        namePanelRect.anchorMin = new Vector2(0f, 1f);
        namePanelRect.anchorMax = new Vector2(0f, 1f);
        namePanelRect.pivot = new Vector2(0f, 0.5f);
        namePanelRect.anchoredPosition = new Vector2(86f, -34f);
        namePanelRect.sizeDelta = new Vector2(360f, 76f);
        Image namePanelImage = namePanel.GetComponent<Image>();
        namePanelImage.color = new Color(1f, 1f, 1f, 0.85f);
        namePanelImage.raycastTarget = false;

        TextMeshProUGUI nameText = CreateAnchoredText("NameText", namePanel.transform, "\uC774\uB2E4 \uD39C", 38f, Vector2.zero, new Vector2(320f, 64f));
        nameText.color = Color.black;
        nameText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI dialogueText = CreateAnchoredText(
            "DialogueText",
            panel.transform,
            "\uC0DD\uAC01\uBCF4\uB2E4 \uB530\uB73B\uD55C \uACB0\uB9D0\uC774\uB124\uC694.\n\uD558\uC9C0\uB9CC \uC774 \uAD6C\uC5ED\uC5D0\uC11C \uB0A8\uC544\uC788\uB294 \uB9D0\uC740 \uC5B8\uC81C\uB098 \uB2E4\uC74C \uBE5A\uC758 \uC2DC\uC791\uC774\uAE30\uB3C4 \uD574\uC694.",
            36f,
            new Vector2(44f, -118f),
            new Vector2(1340f, 116f));
        dialogueText.color = Color.black;
        dialogueText.alignment = TextAlignmentOptions.MidlineLeft;

        CreateControlButton(panel.transform, "Skip", new Vector2(-150f, 28f));
        CreateControlButton(panel.transform, "Auto", new Vector2(-58f, 28f));

        canvasObject.SetActive(false);
        canvas.sortingOrder = 10;
    }

    private GameObject ShowCanvasForLastMission()
    {
        string missionId = PlayerPrefs.GetString(lastVictoryMissionKey, BattleScoreStore.DefaultMissionId);
        string activeCanvasName = missionId == BattleScoreStore.KarimHasanMissionId
            ? Canvas2Name
            : missionId == "Mission3"
                ? Canvas3Name
                : Canvas1Name;

        GameObject canvas1 = SetCanvasActive(Canvas1Name, activeCanvasName == Canvas1Name);
        GameObject canvas2 = SetCanvasActive(Canvas2Name, activeCanvasName == Canvas2Name);
        GameObject canvas3 = SetCanvasActive(Canvas3Name, activeCanvasName == Canvas3Name);

        if (activeCanvasName == Canvas2Name)
        {
            return canvas2;
        }

        if (activeCanvasName == Canvas3Name)
        {
            return canvas3;
        }

        return canvas1;
    }

    private GameObject SetCanvasActive(string canvasName, bool active)
    {
        Transform canvas = FindSceneTransform(canvasName);
        if (canvas == null)
        {
            return null;
        }

        canvas.gameObject.SetActive(active);
        return canvas.gameObject;
    }

    private void CreateStoryCanvas(string canvasName, string title, string body)
    {
        if (FindSceneTransform(canvasName) != null)
        {
            return;
        }

        GameObject canvasObject = CreateBaseCanvas(canvasName);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.02f, 0.02f, 0.025f, 1f);
        backgroundImage.raycastTarget = false;

        TextMeshProUGUI titleText = CreateText("Title", canvasObject.transform, title, 64f, new Vector2(0f, 170f), new Vector2(1500f, 110f));
        titleText.color = new Color(0.92f, 0.84f, 0.88f, 1f);

        TextMeshProUGUI bodyText = CreateText("Body", canvasObject.transform, body, 34f, new Vector2(0f, -30f), new Vector2(1500f, 300f));
        bodyText.color = new Color(0.78f, 0.75f, 0.72f, 1f);
        bodyText.lineSpacing = 18f;

        TextMeshProUGUI promptText = CreateText("Prompt", canvasObject.transform, "\uC790\uB3D9\uC73C\uB85C \uB3CC\uC544\uAC11\uB2C8\uB2E4", 24f, new Vector2(0f, -420f), new Vector2(700f, 60f));
        promptText.color = new Color(0.5f, 0.48f, 0.46f, 1f);
        promptText.characterSpacing = 6f;

        canvasObject.SetActive(false);
    }

    private void BeginReturnSequence()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
        }

        returnRoutine = StartCoroutine(ReturnSequenceRoutine());
    }

    private IEnumerator ReturnSequenceRoutine()
    {
        yield return WaitForActiveCanvasToFinish();
        BeginFadeToMain();
    }

    private IEnumerator WaitForActiveCanvasToFinish()
    {
        if (activeVictoryCanvas == null)
        {
            yield return new WaitForSecondsRealtime(fallbackStoryDuration);
            yield break;
        }

        Animator[] animators = activeVictoryCanvas.GetComponentsInChildren<Animator>(true);
        if (animators.Length <= 0)
        {
            yield return new WaitForSecondsRealtime(fallbackStoryDuration);
            yield break;
        }

        bool anyPlaying;
        do
        {
            anyPlaying = false;
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null || !animator.isActiveAndEnabled)
                {
                    continue;
                }

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (animator.IsInTransition(0) || state.normalizedTime < 1f)
                {
                    anyPlaying = true;
                    break;
                }
            }

            if (anyPlaying)
            {
                yield return null;
            }
        }
        while (anyPlaying);
    }

    private void BeginFadeToMain()
    {
        if (returnStarted)
        {
            return;
        }

        returnStarted = true;
        StartCoroutine(FadeToMainRoutine());
    }

    private IEnumerator FadeToMainRoutine()
    {
        Image fadeImage = SceneFadeOverlay.CreateImage("VictorySceneFade");
        yield return FadeImage(fadeImage, 0f, 1f, fadeToMainDuration);

        if (!string.IsNullOrWhiteSpace(mainSceneName))
        {
            string missionId = PlayerPrefs.GetString(lastVictoryMissionKey, BattleScoreStore.DefaultMissionId);
            if (missionId == BattleScoreStore.DefaultMissionId
                && PlayerPrefs.GetInt(BattleScoreStore.IdapenFirstVictoryJustWonKey, 0) == 1
                && PlayerPrefs.GetInt(BattleScoreStore.IdapenFirstVictoryDialogueShownKey, 0) == 0)
            {
                PlayerPrefs.SetString(BattleScoreStore.PendingPostVictoryDialogueKey, missionId);
                PlayerPrefs.SetInt(BattleScoreStore.IdapenFirstVictoryDialogueShownKey, 1);
            }
            else if (missionId == BattleScoreStore.KarimHasanMissionId
                && PlayerPrefs.GetInt(BattleScoreStore.KarimFirstVictoryJustWonKey, 0) == 1
                && PlayerPrefs.GetInt(BattleScoreStore.KarimFirstVictoryDialogueShownKey, 0) == 0)
            {
                PlayerPrefs.SetString(BattleScoreStore.PendingPostVictoryDialogueKey, missionId);
                PlayerPrefs.SetInt(BattleScoreStore.KarimFirstVictoryDialogueShownKey, 1);
            }

            PlayerPrefs.DeleteKey(BattleScoreStore.IdapenFirstVictoryJustWonKey);
            PlayerPrefs.DeleteKey(BattleScoreStore.KarimFirstVictoryJustWonKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene(mainSceneName);
        }
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

    private GameObject CreateBaseCanvas(string canvasName)
    {
        GameObject canvasObject = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject;
    }

    private void HideLegacyWinCanvas()
    {
        Transform legacyCanvas = FindSceneTransform("Canvas");
        if (legacyCanvas == null || legacyCanvas.Find("WinText") == null)
        {
            return;
        }

        legacyCanvas.gameObject.SetActive(false);
    }

    private Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName && candidate.gameObject.scene == gameObject.scene)
            {
                return candidate;
            }
        }

        return null;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        ApplyDefaultFont(label);
        return label;
    }

    private TextMeshProUGUI CreateAnchoredText(string objectName, Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
    {
        TextMeshProUGUI label = CreateText(objectName, parent, text, fontSize, position, size);
        RectTransform rectTransform = label.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        return label;
    }

    private void CreateControlButton(Transform parent, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(88f, 42f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.35f, 0.35f, 0.35f, 0.24f);
        image.raycastTarget = true;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 30f, Vector2.zero, rectTransform.sizeDelta);
        Stretch(text.rectTransform);
        text.color = new Color(0.26f, 0.26f, 0.26f, 1f);
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ApplyDefaultFont(TextMeshProUGUI label)
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }
    }

    private void DestroySceneObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
            return;
        }
#endif
        Destroy(target);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, gameObject.scene);
    }
}
