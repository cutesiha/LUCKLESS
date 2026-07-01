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
    [SerializeField] private Sprite idapenBackgroundSprite;
    [SerializeField] private Sprite idapenCharacterSprite;

    private void Awake()
    {
        BuildScene();
    }

    private void OnEnable()
    {
        BuildScene();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }

    private void BuildScene()
    {
        HideLegacyWinCanvas();
        EnsureEventSystem();
        EnsureVictoryCanvases();
        ShowCanvasForLastMission();
    }

    private void EnsureVictoryCanvases()
    {
        EnsureIdapenVictoryCanvas();
        CreateStoryCanvas(
            "Canvas2_KarimHasanVictory",
            "카림하산 의뢰 승리",
            "카림하산의 통신이 끊겼다.\n거래소의 숫자는 흔들렸지만, 연결망은 아직 희미하게 남아 있다.\nZERO는 그 연결의 다음 흔적을 향해 움직인다.");

        CreateStoryCanvas(
            "Canvas3_DoctorOlaVictory",
            "닥터올라 의뢰 승리",
            "닥터올라의 시선이 꺼졌다.\n깨어난 기록과 사라진 기록 사이에서, LUX는 다시 조용히 흔들린다.\nZERO는 또 다른 선택지를 고른다.");
    }

    private void EnsureIdapenVictoryCanvas()
    {
        Transform existing = FindSceneTransform("Canvas1_IdapenVictory");
        if (existing != null && existing.Find("DialoguePanel") != null)
        {
            return;
        }

        if (existing != null)
        {
            DestroySceneObject(existing.gameObject);
        }

        GameObject canvasObject = CreateBaseCanvas("Canvas1_IdapenVictory");
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
        panelImage.color = new Color(1f, 1f, 1f, 0.8509804f);
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
        namePanelImage.color = new Color(1f, 1f, 1f, 0.8509804f);
        namePanelImage.raycastTarget = false;

        TextMeshProUGUI nameText = CreateAnchoredText("NameText", namePanel.transform, "이다 펜", 38f, Vector2.zero, new Vector2(320f, 64f));
        nameText.color = Color.black;
        nameText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI dialogueText = CreateAnchoredText(
            "DialogueText",
            panel.transform,
            "생각보다 끈질기군요.\n하지만 이 구역에서 살아남는 말은 언제나 다음 빛의 시작이기도 해요.",
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

    private void ShowCanvasForLastMission()
    {
        string missionId = PlayerPrefs.GetString(lastVictoryMissionKey, BattleScoreStore.DefaultMissionId);
        string activeCanvas = missionId == "Mission2"
            ? "Canvas2_KarimHasanVictory"
            : missionId == "Mission3"
                ? "Canvas3_DoctorOlaVictory"
                : "Canvas1_IdapenVictory";

        SetCanvasActive("Canvas1_IdapenVictory", activeCanvas == "Canvas1_IdapenVictory");
        SetCanvasActive("Canvas2_KarimHasanVictory", activeCanvas == "Canvas2_KarimHasanVictory");
        SetCanvasActive("Canvas3_DoctorOlaVictory", activeCanvas == "Canvas3_DoctorOlaVictory");
    }

    private void SetCanvasActive(string canvasName, bool active)
    {
        Transform canvas = FindSceneTransform(canvasName);
        if (canvas != null)
        {
            canvas.gameObject.SetActive(active);
        }
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

        TextMeshProUGUI promptText = CreateText("Prompt", canvasObject.transform, "클릭해서 돌아가기", 24f, new Vector2(0f, -420f), new Vector2(700f, 60f));
        promptText.color = new Color(0.5f, 0.48f, 0.46f, 1f);
        promptText.characterSpacing = 6f;

        canvasObject.SetActive(false);
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
