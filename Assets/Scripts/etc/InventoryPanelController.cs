using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Audio;

public enum WorldCodexBlockType
{
    Paragraph,
    Highlight,
    DataRow,
    Card,
    Organization,
    Quote
}

public enum WorldCodexAccent
{
    Default,
    Pink,
    PinkLight,
    Gold,
    TextMuted,
    TextDim
}

[Serializable]
public class WorldCodexBlock
{
    public WorldCodexBlockType type = WorldCodexBlockType.Paragraph;
    public WorldCodexAccent accent = WorldCodexAccent.Default;
    public string title;
    public string value;
    [TextArea(2, 8)] public string text;
}

[Serializable]
public class WorldCodexEntry
{
    public string category = "Category";
    public string navTitle = "Title";
    public string classified;
    public string tag = "Tag";
    public string title = "Title";
    public string subtitle = "Subtitle";
    public WorldCodexBlock[] blocks;
}

public class InventoryPanelController : MonoBehaviour
{
    private const string InventoryCloseButtonName = "InventoryCloseButton";
    private const string WorldInfoPanelName = "WorldInfoPanel";
    private const string WorldImageName = "World";
    private const string WordImageName = "Word";
    private const string GlossaryPanelName = "GlossaryPanel";
    private const string OptionImageName = "Option";
    private const string OptionPanelName = "OptionPanel";

    public static event Action InventoryPanelClosed;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource[] bgmAudioSources;
    [SerializeField] private AudioSource[] sfxAudioSources;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("World Info Panel")]
    [SerializeField] private TMP_FontAsset textFont;
    [SerializeField] private Vector2 worldInfoPanelSize = new Vector2(1440f, 830f);
    [Range(0.1f, 1f)]
    [SerializeField] private float worldInfoPanelAlpha = 0.94f;
    [SerializeField] private WorldCodexEntry[] codexEntries = CreateDefaultCodexEntries();

    private static readonly Color StatBadgeBg = Rgba(29, 158, 117, 0.15f);
    private static readonly Color StatBadgeText = Hex("#5dcaa5");
    private static readonly Color PersonBadgeBg = Rgba(127, 119, 221, 0.15f);
    private static readonly Color PersonBadgeText = Hex("#afa9ec");
    private static readonly Color PlaceBadgeBg = Rgba(216, 90, 48, 0.15f);
    private static readonly Color PlaceBadgeText = Hex("#f0997b");

    private static readonly Color Bg = Hex("#0a0a0c");
    private static readonly Color Bg2 = Hex("#111116");
    private static readonly Color Bg3 = Hex("#18181f");
    private static readonly Color Card = Hex("#13131a");
    private static readonly Color Border = Rgba(255, 255, 255, 0.07f);
    private static readonly Color BorderHover = Rgba(220, 80, 120, 0.4f);
    private static readonly Color Pink = Hex("#d4537e");
    private static readonly Color PinkLight = Hex("#f0799f");
    private static readonly Color PinkDim = Rgba(212, 83, 126, 0.15f);
    private static readonly Color PinkGlow = Rgba(212, 83, 126, 0.08f);
    private static readonly Color TextColor = Hex("#e8e4de");
    private static readonly Color TextMuted = Hex("#7a7672");
    private static readonly Color TextDim = Hex("#4a4845");
    private static readonly Color Gold = Hex("#c89a45");
    private static readonly Color GoldDim = Rgba(200, 154, 69, 0.12f);
    private static readonly Color BodyText = Hex("#c8c4be");
    private static readonly Color CardText = Hex("#9a9690");
    private static readonly Color OrgText = Hex("#8a8680");
    private static readonly Color QuoteText = Hex("#cac5bb");

    private readonly List<Coroutine> floatRoutines = new List<Coroutine>();
    private readonly List<Image> inventoryHoverImages = new List<Image>();
    private readonly List<Color> inventoryHoverOriginalColors = new List<Color>();
    private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>();
    private readonly List<Image> worldNavBackgrounds = new List<Image>();
    private readonly List<Image> worldNavDots = new List<Image>();
    private readonly List<Image> worldNavLeftBars = new List<Image>();
    private readonly List<TextMeshProUGUI> worldNavLabels = new List<TextMeshProUGUI>();

    private GameObject worldInfoPanel;
    private Image worldImage;
    private RectTransform worldContentRoot;
    private ScrollRect worldContentScroll;
    private int selectedWorldInfoIndex;
    private int hoveredWorldInfoIndex = -1;

    private Image wordImage;
    private GameObject glossaryPanel;
    private Image optionImage;
    private GameObject optionPanel;

    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider voiceSlider;
    private Slider sfxSlider;

    private TextMeshProUGUI masterValueText;
    private TextMeshProUGUI bgmValueText;
    private TextMeshProUGUI voiceValueText;
    private TextMeshProUGUI sfxValueText;

    private RectTransform glossaryTermList;
    private ScrollRect glossaryScroll;
    private TMP_InputField glossarySearchInput;
    private string glossaryCurrentCategory = "all";
    private readonly List<GlossaryItemUI> glossaryItemUIs = new List<GlossaryItemUI>();
    private readonly List<Image> glossaryNavBgs = new List<Image>();
    private readonly List<Image> glossaryNavBars = new List<Image>();
    private readonly List<Image> glossaryNavDots = new List<Image>();
    private readonly List<TextMeshProUGUI> glossaryNavTexts = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> glossaryNavCounts = new List<TextMeshProUGUI>();
    private readonly List<string> glossaryCatIds = new List<string>();
    private int hoveredGlossaryNavIndex = -1;
    private TextMeshProUGUI glossarySearchCursor;
    private bool glossarySearchFocused;
    private float glossaryCursorTimer;

    private sealed class GlossaryTermData
    {
        public readonly string cat, word, english, def, note;
        public GlossaryTermData(string cat, string word, string english, string def, string note = null)
        { this.cat = cat; this.word = word; this.english = english; this.def = def; this.note = note; }
    }

    private sealed class GlossaryItemUI
    {
        public string category;
        public string searchText;
        public GameObject root;
        public GameObject body;
        public Image headerImage;
        public TextMeshProUGUI wordText;
        public TextMeshProUGUI englishText;
        public TextMeshProUGUI arrow;
        public bool isOpen;
    }

    private void Reset()
    {
        EnsureDefaultCodexEntries();
    }

    private void OnValidate()
    {
        EnsureDefaultCodexEntries();
    }

    private void OnEnable()
    {
        SetupInventoryPanel();
    }

    private void OnDisable()
    {
        StopFloatRoutines();
    }

    public void SetupInventoryPanel()
    {
        transform.localScale = Vector3.one;
        EnsureDefaultCodexEntries();
        EnsureEventSystem();
        EnsureInventoryCloseButton();
        ApplyFontToExistingTexts();
        SetupInventoryImageEffects();
        SetupWorldImageInteraction();
        SetupWordImageInteraction();
        SetupOptionImageInteraction();
    }

    private void EnsureInventoryCloseButton()
    {
        RectTransform panelTransform = transform as RectTransform;

        if (panelTransform == null)
        {
            return;
        }

        Transform existingButton = transform.Find(InventoryCloseButtonName);
        TextMeshProUGUI closeText;
        Button closeButton;

        if (existingButton != null)
        {
            closeText = existingButton.GetComponent<TextMeshProUGUI>();
            closeButton = existingButton.GetComponent<Button>();
        }
        else
        {
            GameObject closeObject = new GameObject(InventoryCloseButtonName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Button));
            closeObject.transform.SetParent(panelTransform, false);

            RectTransform closeTransform = closeObject.GetComponent<RectTransform>();
            closeTransform.anchorMin = new Vector2(1f, 1f);
            closeTransform.anchorMax = new Vector2(1f, 1f);
            closeTransform.pivot = new Vector2(0.5f, 0.5f);
            closeTransform.anchoredPosition = new Vector2(-48f, -44f);
            closeTransform.sizeDelta = new Vector2(80f, 80f);

            closeText = closeObject.GetComponent<TextMeshProUGUI>();
            closeButton = closeObject.GetComponent<Button>();
        }

        if (closeText != null)
        {
            closeText.text = "X";
            closeText.fontSize = 50f;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.raycastTarget = true;
            ApplyTextFont(closeText);
            SetupTextHover(closeText, Color.white, new Color(0.62f, 0.62f, 0.62f, 1f));
        }

        if (closeButton != null)
        {
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeText;
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTopPanel);
        }

        Transform closeButtonTransform = transform.Find(InventoryCloseButtonName);

        if (closeButtonTransform != null)
        {
            closeButtonTransform.SetAsLastSibling();
        }
    }

    private void CloseTopPanel()
    {
        if (optionPanel != null && optionPanel.activeInHierarchy)
        {
            optionPanel.SetActive(false);
            return;
        }

        if (glossaryPanel != null && glossaryPanel.activeInHierarchy)
        {
            glossaryPanel.SetActive(false);
            return;
        }

        if (worldInfoPanel != null && worldInfoPanel.activeInHierarchy)
        {
            worldInfoPanel.SetActive(false);
            return;
        }

        CloseInventoryPanel();
    }

    private void CloseInventoryPanel()
    {
        StopFloatRoutines();
        gameObject.SetActive(false);
        InventoryPanelClosed?.Invoke();
    }

    private bool IsSubPanelOpen()
    {
        return (glossaryPanel != null && glossaryPanel.activeInHierarchy)
            || (worldInfoPanel != null && worldInfoPanel.activeInHierarchy)
            || (optionPanel != null && optionPanel.activeInHierarchy);
    }

    private void SetupInventoryImageEffects()
    {
        StopFloatRoutines();
        ResetInventoryImageHoverColors();
        inventoryHoverImages.Clear();
        inventoryHoverOriginalColors.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (!IsInventoryHoverImage(image))
            {
                continue;
            }

            image.raycastTarget = true;
            if (image.sprite != null && image.sprite.texture != null && image.sprite.texture.isReadable)
            {
                image.alphaHitTestMinimumThreshold = 0.1f;
            }
            inventoryHoverImages.Add(image);
            inventoryHoverOriginalColors.Add(image.color);

            if (image.gameObject.name == "Image")
            {
                continue;
            }

            RectTransform rectTransform = image.rectTransform;
            Vector2 basePosition = rectTransform.anchoredPosition;
            float amplitude = 8f + (i % 4) * 2.5f;
            float speed = 0.65f + (i % 5) * 0.18f;
            float phase = i * 0.71f;
            floatRoutines.Add(StartCoroutine(FloatInventoryImage(rectTransform, basePosition, amplitude, speed, phase)));
        }
    }

    private void SetupWorldImageInteraction()
    {
        worldImage = FindChildImageByName(transform, WorldImageName);

        if (worldImage == null)
        {
            return;
        }

        worldImage.raycastTarget = true;
        Button worldButton = worldImage.GetComponent<Button>();

        if (worldButton == null)
        {
            worldButton = worldImage.gameObject.AddComponent<Button>();
        }

        worldButton.transition = Selectable.Transition.None;
        worldButton.targetGraphic = worldImage;
        worldButton.onClick.RemoveAllListeners();
        worldButton.onClick.AddListener(OpenWorldInfoPanel);
        AddClickEvent(worldImage, OpenWorldInfoPanel);
    }

    private void SetupWordImageInteraction()
    {
        wordImage = FindChildImageByName(transform, WordImageName);
        if (wordImage == null) return;

        wordImage.raycastTarget = true;
        Button wordButton = wordImage.GetComponent<Button>();
        if (wordButton == null) wordButton = wordImage.gameObject.AddComponent<Button>();

        wordButton.transition = Selectable.Transition.None;
        wordButton.targetGraphic = wordImage;
        wordButton.onClick.RemoveAllListeners();
        wordButton.onClick.AddListener(OpenGlossaryPanel);
        AddClickEvent(wordImage, OpenGlossaryPanel);
    }

    private void SetupOptionImageInteraction()
    {
        optionImage = FindChildImageByName(transform, OptionImageName);
        if (optionImage == null) return;

        optionImage.raycastTarget = true;

        Button optionButton = optionImage.GetComponent<Button>();
        if (optionButton == null)
        {
            optionButton = optionImage.gameObject.AddComponent<Button>();
        }

        optionButton.transition = Selectable.Transition.None;
        optionButton.targetGraphic = optionImage;
        optionButton.onClick.RemoveAllListeners();
        optionButton.onClick.AddListener(OpenOptionPanel);

        AddClickEvent(optionImage, OpenOptionPanel);
    }

    private void OpenOptionPanel()
    {
        if (IsSubPanelOpen())
        {
            return;
        }

        EnsureOptionPanel();
        if (optionPanel == null) return;

        optionPanel.SetActive(true);
        optionPanel.transform.SetAsLastSibling();
        BringCloseButtonToFront();
        ResetInventoryImageHoverColors();
    }

    private void OpenGlossaryPanel()
    {
        if (IsSubPanelOpen())
        {
            return;
        }

        EnsureGlossaryPanel();
        if (glossaryPanel == null) return;

        glossaryPanel.SetActive(true);
        glossaryPanel.transform.SetAsLastSibling();
        BringCloseButtonToFront();
        ResetInventoryImageHoverColors();
    }

    private void EnsureGlossaryPanel()
    {
        if (glossaryPanel != null) return;

        RectTransform panelTransform = transform as RectTransform;
        if (panelTransform == null) return;

        Transform old = transform.Find(GlossaryPanelName);
        if (old != null) Destroy(old.gameObject);

        glossaryPanel = new GameObject(GlossaryPanelName, typeof(RectTransform), typeof(Image), typeof(Outline));
        glossaryPanel.transform.SetParent(panelTransform, false);

        RectTransform rt = glossaryPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1420f, 800f);

        Image panelImg = glossaryPanel.GetComponent<Image>();
        panelImg.color = WithAlpha(Bg, worldInfoPanelAlpha);
        panelImg.raycastTarget = true;

        Outline panelOutline = glossaryPanel.GetComponent<Outline>();
        panelOutline.effectColor = Border;
        panelOutline.effectDistance = new Vector2(2f, -2f);

        BuildGlossarySidebar(glossaryPanel.transform);
        BuildGlossaryContent(glossaryPanel.transform);
        BuildGlossaryTermItems();
        RefreshGlossaryNav();
        RefreshGlossaryTermVisibility();
    }

    private void EnsureOptionPanel()
    {
        if (optionPanel != null) return;

        RectTransform panelTransform = transform as RectTransform;
        if (panelTransform == null) return;

        Transform old = transform.Find(OptionPanelName);
        if (old != null) Destroy(old.gameObject);

        optionPanel = new GameObject(OptionPanelName, typeof(RectTransform), typeof(Image), typeof(Outline));
        optionPanel.transform.SetParent(panelTransform, false);

        RectTransform rt = optionPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(720f, 560f);

        Image bg = optionPanel.GetComponent<Image>();
        bg.color = Card;
        bg.raycastTarget = true;

        Outline outline = optionPanel.GetComponent<Outline>();
        outline.effectColor = Rgba(212, 83, 126, 0.25f);
        outline.effectDistance = new Vector2(2f, -2f);

        CreateOptionCorner(optionPanel.transform, "TL", new Vector2(0f, 1f), new Vector2(1f, 1f));
        CreateOptionCorner(optionPanel.transform, "TR", new Vector2(1f, 1f), new Vector2(0f, 1f));
        CreateOptionCorner(optionPanel.transform, "BL", new Vector2(0f, 0f), new Vector2(1f, 0f));
        CreateOptionCorner(optionPanel.transform, "BR", new Vector2(1f, 0f), new Vector2(0f, 0f));

        CreateOptionTitleBar(optionPanel.transform);
        CreateOptionBody(optionPanel.transform);

        LoadOptionValues();
    }

    private void CreateOptionTitleBar(Transform parent)
    {
        GameObject bar = CreateRectObject("TitleBar", parent, typeof(Image));
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 44f);

        Image img = bar.GetComponent<Image>();
        img.color = Bg2;
        img.raycastTarget = true;

        TextMeshProUGUI tag = CreateText("Tag", bar.transform, "LUCKLESS", 12f, Pink, TextAlignmentOptions.Left);
        RectTransform tagRT = tag.rectTransform;
        tagRT.anchorMin = new Vector2(0f, 0f);
        tagRT.anchorMax = new Vector2(0f, 1f);
        tagRT.pivot = new Vector2(0f, 0.5f);
        tagRT.anchoredPosition = new Vector2(28f, 0f);
        tagRT.sizeDelta = new Vector2(120f, 0f);
        tag.characterSpacing = 8f;

        TextMeshProUGUI title = CreateText("Title", bar.transform, "|  SETTINGS / 설정", 12f, TextMuted, TextAlignmentOptions.Left);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 0f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0f, 0.5f);
        titleRT.anchoredPosition = new Vector2(134f, 0f);
        titleRT.sizeDelta = new Vector2(-190f, 0f);
        title.characterSpacing = 4f;

        GameObject closeObj = CreateRectObject("OptionClose", bar.transform, typeof(Image), typeof(Button), typeof(Outline));
        RectTransform closeRT = closeObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 0.5f);
        closeRT.anchorMax = new Vector2(1f, 0.5f);
        closeRT.pivot = new Vector2(0.5f, 0.5f);
        closeRT.anchoredPosition = new Vector2(-28f, 0f);
        closeRT.sizeDelta = new Vector2(28f, 28f);

        Image closeImg = closeObj.GetComponent<Image>();
        closeImg.color = Color.clear;
        closeImg.raycastTarget = true;

        Outline closeOutline = closeObj.GetComponent<Outline>();
        closeOutline.effectColor = Border;
        closeOutline.effectDistance = new Vector2(1f, -1f);

        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() => optionPanel.SetActive(false));

        TextMeshProUGUI x = CreateText("X", closeObj.transform, "X", 15f, TextMuted, TextAlignmentOptions.Center);
        SetStretch(x.rectTransform, Vector2.zero, Vector2.zero);
    }

    private void CreateOptionBody(Transform parent)
    {
        CreateOptionSectionLabel(parent);

        CreateOptionSliderAbsolute(parent, "Master", "마스터 볼륨", "MASTER", 80, true, -130f, out masterSlider, out masterValueText);
        CreateOptionSliderAbsolute(parent, "BGM", "배경음악", "BGM", 70, false, -230f, out bgmSlider, out bgmValueText);
        CreateOptionSliderAbsolute(parent, "Voice", "캐릭터 음성", "VOICE", 100, false, -330f, out voiceSlider, out voiceValueText);
        CreateOptionSliderAbsolute(parent, "SFX", "효과음", "SFX", 75, false, -430f, out sfxSlider, out sfxValueText);

        CreateOptionFooter(parent);
    }

    private void CreateOptionSectionLabel(Transform parent)
    {
        TextMeshProUGUI section = CreateText("SectionLabel", parent, "오디오 설정", 15f, Pink, TextAlignmentOptions.Left);
        RectTransform rt = section.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -82f);
        rt.sizeDelta = new Vector2(-64f, 28f);
        rt.offsetMin = new Vector2(32f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-32f, rt.offsetMax.y);
        section.characterSpacing = 8f;
    }

    private void CreateOptionSliderAbsolute(
        Transform parent,
        string objectName,
        string koreanName,
        string englishName,
        int defaultValue,
        bool isMaster,
        float y,
        out Slider slider,
        out TextMeshProUGUI valueText)
    {
        GameObject row = CreateRectObject("OptionSlider_" + objectName, parent, typeof(RectTransform));
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, y);
        rowRT.sizeDelta = new Vector2(-96f, 76f);
        rowRT.offsetMin = new Vector2(48f, rowRT.offsetMin.y);
        rowRT.offsetMax = new Vector2(-48f, rowRT.offsetMax.y);

        TextMeshProUGUI label = CreateText("Label", row.transform,
            koreanName + "  <size=65%><color=#7A7672>" + englishName + "</color></size>",
            17f, TextColor, TextAlignmentOptions.Left);

        RectTransform labelRT = label.rectTransform;
        labelRT.anchorMin = new Vector2(0f, 1f);
        labelRT.anchorMax = new Vector2(0.8f, 1f);
        labelRT.pivot = new Vector2(0f, 1f);
        labelRT.anchoredPosition = Vector2.zero;
        labelRT.sizeDelta = new Vector2(0f, 28f);

        valueText = CreateText("Value", row.transform, defaultValue.ToString(), 17f, isMaster ? Gold : PinkLight, TextAlignmentOptions.Right);
        RectTransform valueRT = valueText.rectTransform;
        valueRT.anchorMin = new Vector2(1f, 1f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.pivot = new Vector2(1f, 1f);
        valueRT.anchoredPosition = Vector2.zero;
        valueRT.sizeDelta = new Vector2(80f, 28f);

        GameObject sliderObj = CreateRectObject("Slider", row.transform, typeof(RectTransform), typeof(Slider));
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 0f);
        sliderRT.anchorMax = new Vector2(1f, 0f);
        sliderRT.pivot = new Vector2(0.5f, 0f);
        sliderRT.anchoredPosition = new Vector2(0f, 8f);
        sliderRT.sizeDelta = new Vector2(0f, 28f);

        GameObject backgroundObj = CreateRectObject("Background", sliderObj.transform, typeof(Image));
        RectTransform bgRT = backgroundObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(0f, 2f);

        Image bgImg = backgroundObj.GetComponent<Image>();
        bgImg.color = Bg3;
        bgImg.raycastTarget = true;

        GameObject fillArea = CreateRectObject("Fill Area", sliderObj.transform, typeof(RectTransform));
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRT.anchoredPosition = Vector2.zero;
        fillAreaRT.sizeDelta = new Vector2(0f, 2f);

        GameObject fillObj = CreateRectObject("Fill", fillArea.transform, typeof(Image));
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = isMaster ? Gold : Pink;
        fillImg.raycastTarget = false;

        GameObject handleArea = CreateRectObject("Handle Slide Area", sliderObj.transform, typeof(RectTransform));
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = Vector2.zero;
        handleAreaRT.offsetMax = Vector2.zero;

        GameObject handleObj = CreateRectObject("Handle", handleArea.transform, typeof(Image), typeof(Outline));
        RectTransform handleRT = handleObj.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16f, 16f);

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = Bg2;
        handleImg.raycastTarget = true;

        Outline handleOutline = handleObj.GetComponent<Outline>();
        handleOutline.effectColor = isMaster ? Gold : Pink;
        handleOutline.effectDistance = new Vector2(2f, -2f);

        slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        TextMeshProUGUI capturedValue = valueText;
        slider.onValueChanged.AddListener(v =>
        {
            capturedValue.text = Mathf.RoundToInt(v).ToString();
        });
    }

    private void CreateOptionFooter(Transform parent)
    {
        GameObject line = CreateRectObject("FooterLine", parent, typeof(Image));
        RectTransform lineRT = line.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0f, 0f);
        lineRT.anchorMax = new Vector2(1f, 0f);
        lineRT.pivot = new Vector2(0.5f, 0f);
        lineRT.anchoredPosition = new Vector2(0f, 82f);
        lineRT.sizeDelta = new Vector2(-96f, 1f);
        line.GetComponent<Image>().color = Border;

        CreateOptionButtonAbsolute(parent, "초기화", new Vector2(-104f, 38f), ResetOptionValues, false);
        CreateOptionButtonAbsolute(parent, "적용", new Vector2(-20f, 38f), SaveOptionValues, true);
    }

    private void CreateOptionButtonAbsolute(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick, bool primary)
    {
        GameObject obj = CreateRectObject("Button_" + text, parent, typeof(Image), typeof(Button), typeof(Outline));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(76f, 32f);

        Image img = obj.GetComponent<Image>();
        img.color = primary ? PinkDim : Color.clear;
        img.raycastTarget = true;

        Outline outline = obj.GetComponent<Outline>();
        outline.effectColor = primary ? Pink : Border;
        outline.effectDistance = new Vector2(1f, -1f);

        Button btn = obj.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);

        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = obj.AddComponent<EventTrigger>();
        }

        Color normalColor = primary ? PinkDim : Color.clear;
        Color hoverColor = primary ? Rgba(212, 83, 126, 0.28f) : Rgba(255, 255, 255, 0.04f);
        Color normalOutline = primary ? Pink : Border;
        Color hoverOutline = primary ? PinkLight : Rgba(255, 255, 255, 0.18f);

        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            img.color = hoverColor;
            outline.effectColor = hoverOutline;
        });

        AddPointerEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            img.color = normalColor;
            outline.effectColor = normalOutline;
        });

        TextMeshProUGUI label = CreateText("Text", obj.transform, text, 14f, primary ? PinkLight : TextMuted, TextAlignmentOptions.Center);
        SetStretch(label.rectTransform, Vector2.zero, Vector2.zero);
    }

    private void CreateOptionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, bool primary)
    {
        GameObject obj = CreateRectObject(text, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.GetComponent<LayoutElement>().preferredWidth = 96f;

        Image img = obj.GetComponent<Image>();
        img.color = primary ? PinkDim : Color.clear;
        img.raycastTarget = true;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = primary ? Pink : Border;
        outline.effectDistance = new Vector2(1f, -1f);

        Button btn = obj.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateText("Text", obj.transform, text, 15f, primary ? PinkLight : TextMuted, TextAlignmentOptions.Center);
        SetStretch(label.rectTransform, Vector2.zero, Vector2.zero);
    }

    private void CreateOptionCorner(Transform parent, string name, Vector2 anchor, Vector2 pivot)
    {
        GameObject corner = CreateRectObject("Corner_" + name, parent, typeof(Image));
        RectTransform rt = corner.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(18f, 18f);

        Image img = corner.GetComponent<Image>();
        img.color = Pink;
        img.raycastTarget = false;
    }

    private void LoadOptionValues()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetInt("Option_Master", 80);
        if (bgmSlider != null) bgmSlider.value = PlayerPrefs.GetInt("Option_BGM", 70);
        if (voiceSlider != null) voiceSlider.value = PlayerPrefs.GetInt("Option_Voice", 100);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetInt("Option_SFX", 75);

        RefreshOptionValueTexts();
        ApplyOptionAudioValues();
    }

    private void SaveOptionValues()
    {
        int master = masterSlider != null ? Mathf.RoundToInt(masterSlider.value) : 80;
        int bgm = bgmSlider != null ? Mathf.RoundToInt(bgmSlider.value) : 70;
        int voice = voiceSlider != null ? Mathf.RoundToInt(voiceSlider.value) : 100;
        int sfx = sfxSlider != null ? Mathf.RoundToInt(sfxSlider.value) : 75;

        PlayerPrefs.SetInt("Option_Master", master);
        PlayerPrefs.SetInt("Option_BGM", bgm);
        PlayerPrefs.SetInt("Option_Voice", voice);
        PlayerPrefs.SetInt("Option_SFX", sfx);
        PlayerPrefs.Save();

        ApplyOptionAudioValues();
        RefreshOptionValueTexts();
    }

    private void ApplyOptionAudioValues()
    {
        float master = masterSlider != null ? masterSlider.value / 100f : 0.8f;
        float bgm = bgmSlider != null ? bgmSlider.value / 100f : 0.7f;
        float voice = voiceSlider != null ? voiceSlider.value / 100f : 1f;
        float sfx = sfxSlider != null ? sfxSlider.value / 100f : 0.75f;

        foreach (AudioSource source in bgmAudioSources)
        {
            if (source != null)
                source.volume = master * bgm;
        }

        foreach (AudioSource source in sfxAudioSources)
        {
            if (source != null)
                source.volume = master * sfx;
        }

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", LinearToDecibel(master));
            audioMixer.SetFloat("VoiceVolume", LinearToDecibel(master * voice));
            audioMixer.SetFloat("SFXVolume", LinearToDecibel(master * sfx));
            audioMixer.SetFloat("BGMVolume", LinearToDecibel(master * bgm));
        }
    }

    private float LinearToDecibel(float value)
    {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }
    private void ResetOptionValues()
    {
        if (masterSlider != null) masterSlider.value = 80;
        if (bgmSlider != null) bgmSlider.value = 70;
        if (voiceSlider != null) voiceSlider.value = 100;
        if (sfxSlider != null) sfxSlider.value = 75;

        RefreshOptionValueTexts();
    }

    private void RefreshOptionValueTexts()
    {
        if (masterSlider != null && masterValueText != null)
            masterValueText.text = Mathf.RoundToInt(masterSlider.value).ToString();

        if (bgmSlider != null && bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(bgmSlider.value).ToString();

        if (voiceSlider != null && voiceValueText != null)
            voiceValueText.text = Mathf.RoundToInt(voiceSlider.value).ToString();

        if (sfxSlider != null && sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value).ToString();
    }

    private void BuildGlossarySidebar(Transform parent)
    {
        GameObject sidebar = CreateRectObject("GlossarySidebar", parent, typeof(Image));
        RectTransform sidebarRT = sidebar.GetComponent<RectTransform>();
        sidebarRT.anchorMin = new Vector2(0f, 0f);
        sidebarRT.anchorMax = new Vector2(0f, 1f);
        sidebarRT.pivot = new Vector2(0f, 0.5f);
        sidebarRT.anchoredPosition = Vector2.zero;
        sidebarRT.sizeDelta = new Vector2(270f, 0f);
        sidebar.GetComponent<Image>().color = WithAlpha(Bg2, worldInfoPanelAlpha);

        // Header
        GameObject header = CreateRectObject("SidebarHeader", sidebar.transform, typeof(Image));
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 80f);
        header.GetComponent<Image>().color = WithAlpha(Bg2, worldInfoPanelAlpha);

        TextMeshProUGUI titleTxt = CreateText("Title", header.transform, "LUCKLESS", 17f, Pink, TextAlignmentOptions.Left);
        SetStretch(titleTxt.rectTransform, new Vector2(20f, 36f), new Vector2(-14f, -14f));
        titleTxt.characterSpacing = 18f;

        TextMeshProUGUI subTxt = CreateText("Sub", header.transform, "GLOSSARY v.2209", 16f, TextDim, TextAlignmentOptions.Left);
        SetStretch(subTxt.rectTransform, new Vector2(20f, 12f), new Vector2(-14f, -48f));

        GameObject hLine = CreateRectObject("Line", header.transform, typeof(Image));
        RectTransform hLineRT = hLine.GetComponent<RectTransform>();
        hLineRT.anchorMin = new Vector2(0f, 0f); hLineRT.anchorMax = new Vector2(1f, 0f);
        hLineRT.pivot = new Vector2(0.5f, 0.5f); hLineRT.anchoredPosition = Vector2.zero;
        hLineRT.sizeDelta = new Vector2(0f, 1f);
        hLine.GetComponent<Image>().color = Border;

        // Search box
        GameObject searchBox = CreateRectObject("SearchBox", sidebar.transform, typeof(Image));
        RectTransform searchBoxRT = searchBox.GetComponent<RectTransform>();
        searchBoxRT.anchorMin = new Vector2(0f, 1f); searchBoxRT.anchorMax = new Vector2(1f, 1f);
        searchBoxRT.pivot = new Vector2(0.5f, 1f);
        searchBoxRT.anchoredPosition = new Vector2(0f, -80f);
        searchBoxRT.sizeDelta = new Vector2(0f, 48f);
        searchBox.GetComponent<Image>().color = Border;
        searchBox.GetComponent<Image>().raycastTarget = false;

        glossarySearchInput = CreateGlossarySearchInput(searchBox.transform);

        // Category label
        TextMeshProUGUI catLbl = CreateText("CatLabel", sidebar.transform, "카테고리", 14f, TextDim, TextAlignmentOptions.Left);
        catLbl.rectTransform.anchorMin = new Vector2(0f, 1f);
        catLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        catLbl.rectTransform.pivot = new Vector2(0.5f, 1f);
        catLbl.rectTransform.anchoredPosition = new Vector2(0f, -128f);
        catLbl.rectTransform.sizeDelta = new Vector2(0f, 30f);
        catLbl.rectTransform.offsetMin = new Vector2(20f, catLbl.rectTransform.offsetMin.y);
        catLbl.characterSpacing = 12f;

        // Nav scroll
        GameObject navScroll = CreateRectObject("NavScroll", sidebar.transform, typeof(ScrollRect), typeof(RectMask2D));
        RectTransform navScrollRT = navScroll.GetComponent<RectTransform>();
        navScrollRT.anchorMin = Vector2.zero; navScrollRT.anchorMax = Vector2.one;
        navScrollRT.offsetMin = new Vector2(0f, 0f); navScrollRT.offsetMax = new Vector2(0f, -158f);

        GameObject navContent = CreateRectObject("NavContent", navScroll.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform navContentRT = navContent.GetComponent<RectTransform>();
        navContentRT.anchorMin = new Vector2(0f, 1f); navContentRT.anchorMax = new Vector2(1f, 1f);
        navContentRT.pivot = new Vector2(0.5f, 1f);
        navContentRT.anchoredPosition = Vector2.zero; navContentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup navVLG = navContent.GetComponent<VerticalLayoutGroup>();
        navVLG.spacing = 1f;
        navVLG.childControlWidth = true; navVLG.childControlHeight = true;
        navVLG.childForceExpandWidth = true; navVLG.childForceExpandHeight = false;

        navContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect navSR = navScroll.GetComponent<ScrollRect>();
        navSR.content = navContentRT; navSR.viewport = navScrollRT;
        navSR.horizontal = false; navSR.vertical = true;
        navSR.movementType = ScrollRect.MovementType.Clamped;
        navSR.scrollSensitivity = 24f;

        glossaryNavBgs.Clear(); glossaryNavBars.Clear(); glossaryNavDots.Clear();
        glossaryNavTexts.Clear(); glossaryNavCounts.Clear(); glossaryCatIds.Clear();

        string[] cats = { "all", "core", "org", "stat", "person", "place", "secret" };
        string[] labels = { "전체 용어", "핵심 개념", "조직·직위", "상태·수치", "등장인물", "장소·구역", "기밀 용어" };
        for (int i = 0; i < cats.Length; i++)
            CreateGlossaryNavItem(navContent.transform, cats[i], labels[i], i);
    }

    private TMP_InputField CreateGlossarySearchInput(Transform parent)
    {
        GameObject ifObj = CreateRectObject("InputField", parent, typeof(Image), typeof(TMP_InputField));
        RectTransform ifRT = ifObj.GetComponent<RectTransform>();
        ifRT.anchorMin = Vector2.zero; ifRT.anchorMax = Vector2.one;
        ifRT.offsetMin = new Vector2(2f, 2f); ifRT.offsetMax = new Vector2(-2f, -2f);
        ifObj.GetComponent<Image>().color = Bg3;
        ifObj.GetComponent<Image>().raycastTarget = true;

        TMP_InputField input = ifObj.GetComponent<TMP_InputField>();
        input.interactable = true;

        GameObject viewport = CreateRectObject("Viewport", ifObj.transform, typeof(RectMask2D));
        Image vpImage = viewport.GetComponent<Image>();
        if (vpImage != null)
        {
            vpImage.raycastTarget = false;
        }

        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(10f, 0f); vpRT.offsetMax = new Vector2(-10f, 0f);

        GameObject textObj = CreateRectObject("Text", viewport.transform, typeof(TextMeshProUGUI));
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.fontSize = 19f; textComp.color = TextColor;
        textComp.alignment = TextAlignmentOptions.MidlineLeft; textComp.raycastTarget = false;
        ApplyTextFont(textComp);

        GameObject phObj = CreateRectObject("Placeholder", viewport.transform, typeof(TextMeshProUGUI));
        RectTransform phRT = phObj.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phComp = phObj.GetComponent<TextMeshProUGUI>();
        phComp.text = "검색..."; phComp.fontSize = 19f; phComp.color = TextDim;
        phComp.alignment = TextAlignmentOptions.MidlineLeft; phComp.raycastTarget = false;
        ApplyTextFont(phComp);

        GameObject cursorObj = CreateRectObject("SearchCursor", viewport.transform, typeof(TextMeshProUGUI));
        RectTransform cursorRT = cursorObj.GetComponent<RectTransform>();
        cursorRT.anchorMin = Vector2.zero;
        cursorRT.anchorMax = Vector2.one;
        cursorRT.offsetMin = Vector2.zero;
        cursorRT.offsetMax = Vector2.zero;

        glossarySearchCursor = cursorObj.GetComponent<TextMeshProUGUI>();
        glossarySearchCursor.text = "|";
        glossarySearchCursor.fontSize = 23f;
        glossarySearchCursor.color = Color.white;
        glossarySearchCursor.alignment = TextAlignmentOptions.MidlineLeft;
        glossarySearchCursor.raycastTarget = false;
        glossarySearchCursor.gameObject.SetActive(false);
        ApplyTextFont(glossarySearchCursor);

        TMP_InputField field = ifObj.GetComponent<TMP_InputField>();
        field.textViewport = vpRT;
        field.textComponent = textComp;
        field.placeholder = phComp;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.onValueChanged.AddListener(OnGlossarySearchChanged);

        field.onSelect.AddListener(_ =>
        {
            glossarySearchFocused = true;
            UpdateGlossarySearchCursor();
        });

        field.onDeselect.AddListener(_ =>
        {
            glossarySearchFocused = false;
            UpdateGlossarySearchCursor();
        });

        UpdateGlossarySearchCursor();
        return field;
    }

    private void CreateGlossaryNavItem(Transform parent, string catId, string label, int index)
    {
        int capturedIdx = index;
        string capturedCat = catId;

        GameObject item = CreateRectObject("Nav_" + catId, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
        LayoutElement le = item.GetComponent<LayoutElement>();
        le.preferredHeight = 42f; le.minHeight = 42f;

        Image bg = item.GetComponent<Image>();
        bg.color = Color.clear; bg.raycastTarget = true;
        Button btn = item.GetComponent<Button>();
        btn.transition = Selectable.Transition.None; btn.targetGraphic = bg;
        btn.onClick.AddListener(() => SelectGlossaryCategory(capturedCat, capturedIdx));

        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = item.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            hoveredGlossaryNavIndex = capturedIdx;
            RefreshGlossaryNav();
        });

        AddPointerEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            if (hoveredGlossaryNavIndex == capturedIdx)
            {
                hoveredGlossaryNavIndex = -1;
            }

            RefreshGlossaryNav();
        });

        // Left bar
        GameObject barObj = CreateRectObject("Bar", item.transform, typeof(Image));
        RectTransform barRT = barObj.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 0f); barRT.anchorMax = new Vector2(0f, 1f);
        barRT.pivot = new Vector2(0f, 0.5f); barRT.anchoredPosition = Vector2.zero;
        barRT.sizeDelta = new Vector2(2f, 0f);
        Image bar = barObj.GetComponent<Image>(); bar.color = Color.clear; bar.raycastTarget = false;

        // Dot
        GameObject dotObj = CreateRectObject("Dot", item.transform, typeof(Image));
        RectTransform dotRT = dotObj.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0f, 0.5f); dotRT.anchorMax = new Vector2(0f, 0.5f);
        dotRT.pivot = new Vector2(0.5f, 0.5f); dotRT.anchoredPosition = new Vector2(18f, 0f);
        dotRT.sizeDelta = new Vector2(5f, 5f);
        Image dot = dotObj.GetComponent<Image>(); dot.color = TextDim; dot.raycastTarget = false;

        // Label
        TextMeshProUGUI labelTxt = CreateText("Label", item.transform, label, 19f, TextMuted, TextAlignmentOptions.Left);
        SetStretch(labelTxt.rectTransform, new Vector2(34f, 0f), new Vector2(-50f, 0f));
        labelTxt.textWrappingMode = TextWrappingModes.NoWrap;
        labelTxt.overflowMode = TextOverflowModes.Ellipsis;

        // Count
        GlossaryTermData[] terms = GetGlossaryTerms();
        int count = catId == "all" ? terms.Length : System.Array.FindAll(terms, t => t.cat == catId).Length;
        TextMeshProUGUI countTxt = CreateText("Count", item.transform, count.ToString(), 16f, TextDim, TextAlignmentOptions.Right);
        SetStretch(countTxt.rectTransform, new Vector2(0f, 0f), new Vector2(-12f, 0f));
        countTxt.textWrappingMode = TextWrappingModes.NoWrap;

        glossaryNavBgs.Add(bg);
        glossaryNavBars.Add(bar);
        glossaryNavDots.Add(dot);
        glossaryNavTexts.Add(labelTxt);
        glossaryNavCounts.Add(countTxt);
        glossaryCatIds.Add(catId);
    }

    private void BuildGlossaryContent(Transform parent)
    {
        GameObject contentObj = CreateRectObject("GlossaryContent", parent, typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero; contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(270f, 0f); contentRT.offsetMax = Vector2.zero;
        contentObj.GetComponent<Image>().color = WithAlpha(Bg, worldInfoPanelAlpha);

        GameObject listObj = CreateRectObject("TermList", contentObj.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        glossaryTermList = listObj.GetComponent<RectTransform>();
        glossaryTermList.anchorMin = new Vector2(0f, 1f); glossaryTermList.anchorMax = new Vector2(1f, 1f);
        glossaryTermList.pivot = new Vector2(0.5f, 1f);
        glossaryTermList.anchoredPosition = Vector2.zero; glossaryTermList.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = listObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 18, 22); vlg.spacing = 2f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = listObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        glossaryScroll = contentObj.GetComponent<ScrollRect>();
        glossaryScroll.viewport = contentRT; glossaryScroll.content = glossaryTermList;
        glossaryScroll.horizontal = false; glossaryScroll.vertical = true;
        glossaryScroll.movementType = ScrollRect.MovementType.Clamped;
        glossaryScroll.scrollSensitivity = 36f;
    }

    private void BuildGlossaryTermItems()
    {
        if (glossaryTermList == null) return;
        glossaryItemUIs.Clear();
        GlossaryTermData[] terms = GetGlossaryTerms();
        for (int i = 0; i < terms.Length; i++)
            CreateGlossaryTermItem(glossaryTermList, terms[i], i);
    }

    private void CreateGlossaryTermItem(RectTransform parent, GlossaryTermData term, int index)
    {
        int capturedIdx = index;

        // Root
        GameObject root = CreateRectObject("Term_" + index, parent, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Image rootImg = root.GetComponent<Image>(); rootImg.color = Card; rootImg.raycastTarget = false;
        VerticalLayoutGroup rootVLG = root.GetComponent<VerticalLayoutGroup>();
        rootVLG.spacing = 0f;
        rootVLG.childControlWidth = true; rootVLG.childControlHeight = true;
        rootVLG.childForceExpandWidth = true; rootVLG.childForceExpandHeight = false;
        root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        GameObject header = CreateRectObject("Header", root.transform, typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        header.GetComponent<LayoutElement>().preferredHeight = header.GetComponent<LayoutElement>().minHeight = 54f;
        Image headerImg = header.GetComponent<Image>(); headerImg.color = Color.clear; headerImg.raycastTarget = true;
        HorizontalLayoutGroup headerHLG = header.GetComponent<HorizontalLayoutGroup>();
        headerHLG.padding = new RectOffset(14, 14, 0, 0); headerHLG.spacing = 10f;
        headerHLG.childAlignment = TextAnchor.MiddleLeft;
        headerHLG.childControlWidth = false; headerHLG.childControlHeight = true;
        headerHLG.childForceExpandWidth = false; headerHLG.childForceExpandHeight = false;
        Button headerBtn = header.GetComponent<Button>();
        headerBtn.transition = Selectable.Transition.None; headerBtn.targetGraphic = headerImg;
        headerBtn.onClick.AddListener(() => ToggleGlossaryTerm(capturedIdx));

        // Badge
        (Color badgeBg, Color badgeTxt) = GetGlossaryBadgeColors(term.cat);
        GameObject badgeObj = CreateRectObject("Badge", header.transform, typeof(Image), typeof(LayoutElement));
        badgeObj.GetComponent<Image>().color = badgeBg;
        LayoutElement badgeLE = badgeObj.GetComponent<LayoutElement>();
        badgeLE.preferredWidth = 52f; badgeLE.preferredHeight = 28f; badgeLE.flexibleWidth = 0f;
        TextMeshProUGUI badgeLabel = CreateText("BText", badgeObj.transform, GetGlossaryBadgeLabel(term.cat), 15f, badgeTxt, TextAlignmentOptions.Center);
        SetStretch(badgeLabel.rectTransform, Vector2.zero, Vector2.zero);
        badgeLabel.characterSpacing = 2f;

        // Word
        TextMeshProUGUI wordTxt = CreateText("Word", header.transform, term.word, 20f, TextColor, TextAlignmentOptions.Left);
        LayoutElement wordLE = wordTxt.gameObject.AddComponent<LayoutElement>(); wordLE.flexibleWidth = 1f;
        wordTxt.textWrappingMode = TextWrappingModes.NoWrap; wordTxt.overflowMode = TextOverflowModes.Ellipsis;

        // English
        TextMeshProUGUI enTxt = CreateText("English", header.transform, term.english, 17f, TextDim, TextAlignmentOptions.Left);
        LayoutElement enLE = enTxt.gameObject.AddComponent<LayoutElement>(); enLE.preferredWidth = 190f; enLE.flexibleWidth = 0f;
        enTxt.textWrappingMode = TextWrappingModes.NoWrap; enTxt.overflowMode = TextOverflowModes.Ellipsis;

        // Arrow
        TextMeshProUGUI arrowTxt = CreateText("Arrow", header.transform, "▶", 18f, TextDim, TextAlignmentOptions.Right);
        arrowTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;

        EventTrigger trigger = header.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = header.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            headerImg.color = PinkGlow;
            wordTxt.color = PinkLight;
            enTxt.color = TextMuted;
        });

        AddPointerEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            headerImg.color = Color.clear;
            wordTxt.color = TextColor;
            enTxt.color = TextDim;
        });

        // Body
        GameObject body = CreateRectObject("Body", root.transform, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        body.SetActive(false);
        body.GetComponent<Image>().color = Bg3;
        VerticalLayoutGroup bodyVLG = body.GetComponent<VerticalLayoutGroup>();
        bodyVLG.padding = new RectOffset(14, 14, 0, 12); bodyVLG.spacing = 6f;
        bodyVLG.childControlWidth = true; bodyVLG.childControlHeight = true;
        bodyVLG.childForceExpandWidth = true; bodyVLG.childForceExpandHeight = false;
        body.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Divider inside body
        GameObject divObj = CreateRectObject("Divider", body.transform, typeof(Image), typeof(LayoutElement));
        divObj.GetComponent<Image>().color = Border;
        LayoutElement divLE = divObj.GetComponent<LayoutElement>(); divLE.preferredHeight = 1f; divLE.minHeight = 1f;

        // Definition
        TextMeshProUGUI defTxt = CreateText("Def", body.transform, term.def, 20f, BodyText, TextAlignmentOptions.Left);
        defTxt.lineSpacing = 14f; defTxt.textWrappingMode = TextWrappingModes.Normal;

        // Note
        if (!string.IsNullOrEmpty(term.note))
        {
            GameObject noteObj = CreateRectObject("Note", body.transform, typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            noteObj.GetComponent<Image>().color = Bg2;
            HorizontalLayoutGroup noteHLG = noteObj.GetComponent<HorizontalLayoutGroup>();
            noteHLG.padding = new RectOffset(10, 10, 8, 8); noteHLG.spacing = 8f;
            noteHLG.childControlWidth = true; noteHLG.childControlHeight = true;
            noteHLG.childForceExpandWidth = true; noteHLG.childForceExpandHeight = false;
            noteObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject noteBorder = CreateRectObject("Border", noteObj.transform, typeof(Image), typeof(LayoutElement));
            noteBorder.GetComponent<Image>().color = PinkDim;
            LayoutElement nbLE = noteBorder.GetComponent<LayoutElement>(); nbLE.preferredWidth = 2f; nbLE.flexibleWidth = 0f; nbLE.minHeight = 18f;

            TextMeshProUGUI noteTxt = CreateText("NoteText", noteObj.transform, term.note, 18f, TextMuted, TextAlignmentOptions.Left);
            noteTxt.lineSpacing = 12f; noteTxt.textWrappingMode = TextWrappingModes.Normal;
        }

        glossaryItemUIs.Add(new GlossaryItemUI
        {
            category = term.cat,
            searchText = (term.word + " " + term.english + " " + term.def + " " + (term.note ?? "")).ToLower(),
            root = root,
            body = body,
            headerImage = headerImg,
            wordText = wordTxt,
            englishText = enTxt,
            arrow = arrowTxt,
            isOpen = false
        });
    }

    private void ToggleGlossaryTerm(int index)
    {
        if (index < 0 || index >= glossaryItemUIs.Count) return;
        GlossaryItemUI item = glossaryItemUIs[index];
        item.isOpen = !item.isOpen;
        item.body.SetActive(item.isOpen);
        item.arrow.text = item.isOpen ? "▼" : "▶";
        item.arrow.color = item.isOpen ? Pink : TextDim;
        Canvas.ForceUpdateCanvases();
        if (glossaryTermList != null) LayoutRebuilder.ForceRebuildLayoutImmediate(glossaryTermList);
    }

    private void OnGlossarySearchChanged(string query)
    {
        UpdateGlossarySearchCursor();
        RefreshGlossaryTermVisibility();
    }

    private void UpdateGlossarySearchCursor()
    {
        if (glossarySearchCursor == null || glossarySearchInput == null)
        {
            return;
        }

        bool show = glossarySearchFocused && string.IsNullOrEmpty(glossarySearchInput.text);
        glossarySearchCursor.gameObject.SetActive(show);
    }

    private void BlinkGlossarySearchCursor()
    {
        if (glossarySearchCursor == null || !glossarySearchCursor.gameObject.activeSelf)
        {
            return;
        }

        glossaryCursorTimer += Time.deltaTime;
        float alpha = Mathf.PingPong(glossaryCursorTimer * 2.8f, 1f);
        glossarySearchCursor.color = new Color(1f, 1f, 1f, alpha);
    }

    private void SelectGlossaryCategory(string catId, int navIndex)
    {
        glossaryCurrentCategory = catId;
        RefreshGlossaryNav();
        RefreshGlossaryTermVisibility();
        if (glossaryScroll != null) { Canvas.ForceUpdateCanvases(); glossaryScroll.verticalNormalizedPosition = 1f; }
    }

    private void RefreshGlossaryNav()
    {
        for (int i = 0; i < glossaryCatIds.Count; i++)
        {
            bool sel = glossaryCatIds[i] == glossaryCurrentCategory;
            bool hover = i == hoveredGlossaryNavIndex;

            if (glossaryNavBgs[i] != null)
                glossaryNavBgs[i].color = sel ? PinkDim : hover ? PinkGlow : Color.clear;

            if (glossaryNavBars[i] != null)
                glossaryNavBars[i].color = sel ? Pink : hover ? Rgba(212, 83, 126, 0.3f) : Color.clear;

            if (glossaryNavDots[i] != null)
                glossaryNavDots[i].color = sel ? Pink : hover ? PinkLight : TextDim;

            if (glossaryNavTexts[i] != null)
                glossaryNavTexts[i].color = sel ? PinkLight : hover ? TextColor : TextMuted;

            if (glossaryNavCounts[i] != null)
                glossaryNavCounts[i].color = sel ? Pink : hover ? TextMuted : TextDim;
        }
    }

    private void RefreshGlossaryTermVisibility()
    {
        string q = glossarySearchInput != null ? glossarySearchInput.text.Trim().ToLower() : "";
        bool hasSearch = !string.IsNullOrEmpty(q);
        for (int i = 0; i < glossaryItemUIs.Count; i++)
        {
            GlossaryItemUI item = glossaryItemUIs[i];
            bool catOk = glossaryCurrentCategory == "all" || item.category == glossaryCurrentCategory;
            bool srchOk = !hasSearch || item.searchText.Contains(q);
            item.root.SetActive(catOk && srchOk);
        }
        Canvas.ForceUpdateCanvases();
        if (glossaryTermList != null) LayoutRebuilder.ForceRebuildLayoutImmediate(glossaryTermList);
    }

    private (Color bg, Color txt) GetGlossaryBadgeColors(string cat)
    {
        switch (cat)
        {
            case "core": return (PinkDim, PinkLight);
            case "org": return (GoldDim, Gold);
            case "stat": return (StatBadgeBg, StatBadgeText);
            case "person": return (PersonBadgeBg, PersonBadgeText);
            case "place": return (PlaceBadgeBg, PlaceBadgeText);
            case "secret": return (Rgba(255, 255, 255, 0.06f), TextMuted);
            default: return (PinkDim, PinkLight);
        }
    }

    private string GetGlossaryBadgeLabel(string cat)
    {
        switch (cat)
        {
            case "core": return "핵심";
            case "org": return "조직";
            case "stat": return "상태";
            case "person": return "인물";
            case "place": return "장소";
            case "secret": return "기밀";
            default: return cat;
        }
    }

    private GlossaryTermData[] GetGlossaryTerms()
    {
        return new[]
        {
            new GlossaryTermData("core","LUX","Luck Exchange Unit",
                "포르투나 코어에서 추출한 확률 왜곡 에너지의 단위. 행운을 수치화한 형태. 1 LUX는 일상적인 행운 하나에 해당하는 확률 편차 에너지다. 이식, 추출, 매매가 가능하다.",
                "시장 가격: 1 LUX ≈ 빈민가 3일치 식비 (2209년 기준)"),
            new GlossaryTermData("core","포르투나 코어","Fortuna Core",
                "인간 뇌의 특정 신경망 클러스터. 미세한 양자 신호를 방출해 국소 확률장을 비틀어 행운을 만들어낸다. 2187년 아델라 포르투나 박사가 발견했다.",
                "코어는 자아 인식 신경망과 연동되어 있다. 코어가 약해질수록 의지도 함께 흐려진다."),
            new GlossaryTermData("core","확률 왜곡","Probability Distortion",
                "포르투나 코어가 방출하는 신호로 발생하는 물리 현상. 총알이 빗나가고, 동전이 앞면으로 떨어지고, 계약이 유리하게 해석된다. 작은 차이가 수십 년에 걸쳐 누적되면 행운아와 불운아의 격차가 된다."),
            new GlossaryTermData("core","LUX 추출","LUX Extraction",
                "포르투나 코어에서 LUX를 인위적으로 꺼내는 작업. 자발적 추출과 강제 추출 두 가지가 있다. 강제 추출 시 마취를 하면 코어가 손상된다는 이유로 무마취로 진행한다."),
            new GlossaryTermData("core","LUX 이식","LUX Implantation",
                "외부에서 정제된 LUX를 타인의 포르투나 코어 인근에 주입하는 작업. 효과는 이식량에 비례한다. THE HOUSE의 핵심 수익 상품이며 \"웰니스 서비스\"라는 이름으로 합법 판매된다."),
            new GlossaryTermData("core","THE HOUSE","THE HOUSE",
                "전 세계 LUX 공급량의 78%를 통제하는 독점 카르텔. 표면은 127개국의 카지노·리조트·확률 보험 복합기업이다. 법도, 정부도, 군대도 이들 앞에서는 손님이다.",
                "공식 슬로건: \"조금 더 운이 좋은 삶을 위하여\""),
            new GlossaryTermData("core","블랙 포커","Black Poker",
                "카림 하산이 운영하는 독립 미디어 채널. THE HOUSE의 LUX 수거 시스템을 고발하는 다큐멘터리를 제작한다. THE HOUSE에 의해 강제 종료된다."),
            new GlossaryTermData("org","하우스마스터","Housemaster",
                "THE HOUSE의 최상위 의사결정자. 딜러와 직접 소통하는 유일한 상위 존재. 정체는 기밀. 공개 석상에 절대 나타나지 않는다. 스스로를 1세대 안드로이드라고 말한 바 있다.",
                "나이 때문이다. 숨이 없다. 이상하지만 이상하지 않을 것 같은 느낌."),
            new GlossaryTermData("org","딜러","Dealer",
                "THE HOUSE의 현장 집행관. 전 세계 6명뿐. 표적 제거, 강제 수거, 경쟁 조직 와해가 주임무다. 하우스마스터의 직접 지시를 받으며 사망 또는 배신 시 즉시 교체된다."),
            new GlossaryTermData("org","하우스키퍼","Housekeeper",
                "THE HOUSE의 행정·정보 분석 담당. LUX 거래 기록, 표적 데이터, 채무 현황 등 조직의 모든 내부 정보를 관리하는 두뇌 역할이다."),
            new GlossaryTermData("org","카드 카운터","Card Counter",
                "LUX 시장 모니터링 및 가격 조작 전문가. LUX 시세를 인위적으로 낮추거나 높여서 빈민층의 판매를 유도하거나, 부유층의 구매 욕구를 자극한다."),
            new GlossaryTermData("org","칩 러너","Chip Runner",
                "LUX 이반 및 세탁 담당 하위 조직. 조직도 최하위지만 인원이 가장 많다. 대부분은 자신이 무엇을 이반하는지 모른다."),
            new GlossaryTermData("org","LUX 클리닉","LUX Clinic",
                "THE HOUSE가 주요 도시마다 운영하는 합법적 시설. 표면은 웰니스·건강 관리 서비스를 제공하지만 실제로는 LUX 이식 및 자발적 추출의 공식 창구다."),
            new GlossaryTermData("stat","확률 부전증","Probability Dysfunction",
                "LUX가 극단적으로 제거되거나 코어가 손상된 상태에서 나타나는 증상. 일상적 불운이 반복되어 삶 전체에 영향을 미친다. 고계단에서 넘어지고, 비가 올 때 나가고, 차가 방향을 바꾼다. 증명할 수 없어 더 위험하다.",
                "공식 기록에서는 \"개인적 불운\" 또는 \"통상적 사례\"로 분류된다."),
            new GlossaryTermData("stat","LUX 수치","LUX Level",
                "개인이 보유한 LUX 총량. LUX-HIGH(200↑), LUX-NEUTRAL(10~200), LUX-ZERO(10↓)로 계층이 나뉜다. THE HOUSE는 이 수치로 표적의 우선순위를 결정한다."),
            new GlossaryTermData("stat","코어 차단 상태","Core Blocked State",
                "포르투나 코어가 외부에 의해 강제로 비활성화된 상태. 파괴와는 다르다. 차단된 경우 해제 코드로 복구가 이론적으로 가능하다.",
                "실험체 23번의 코어 상태. 파괴가 아닌 차단."),
            new GlossaryTermData("stat","행운 빈곤","Luck Poverty",
                "LUX 판매로 인해 점점 불운해지고, 불운해질수록 더 팔게 되는 악순환 상태. 이 사이클에 한 번 진입하면 빠져나오기 거의 불가능하다. THE HOUSE의 핵심 수익 구조이기도 하다."),
            new GlossaryTermData("person","제로","Zero",
                "THE HOUSE의 딜러. LUX 수치 0. 포르투나 코어가 차단되어 행운이 없다. THE HOUSE 초기 실험 프로그램의 산물이며 여섯 살 이전 코어 차단 수술을 받았다. 20년간의 훈련을 거쳐 현재의 딜러가 된다.",
                "코드명 제로의 의미: LUX 수치 영(零). 그리고 — 돌아갈 곳이 없는 존재."),
            new GlossaryTermData("person","이다 펜","Ida Pen",
                "17구역 주민. 71세. 손녀의 심장 수술 비를 마련하기 위해 LUX를 반복 판매한다. 첫 번째 의뢰인의 할머니. 강제 수거 이후 계단 낙상으로 사망한다.",
                "THE HOUSE 공식 기록: \"통상적 사례\""),
            new GlossaryTermData("person","카림 하산","Karim Hasan",
                "34세. 독립 저널리스트. 미디어 채널 블랙 포커 운영. THE HOUSE의 LUX 수거 시스템을 고발하는 다큐멘터리를 제작한다. 피해자 인터뷰 200건과 코어 강제 추출 사망 11건을 보유한다."),
            new GlossaryTermData("person","유이 소라","Yui Sora",
                "28세. 사이보그 카메라맨. 카림 하산의 파트너. 야간 촬영 능력 있음. 카림과 함께 THE HOUSE 고발 작업을 진행한다."),
            new GlossaryTermData("person","핀 아르코","Finn Arco",
                "38세. THE HOUSE 데이터 서버 유지 직원. 사촌인가 싶은 분 라일의 의료비를 위해 THE HOUSE에 입사한다. 분의 죽음이 THE HOUSE의 강제 수거 때문임을 알게 된 후 5년간 내부 데이터를 수집한다."),
            new GlossaryTermData("person","닥터 올라 베이크","Dr. Ola Baike",
                "62세. 전 THE HOUSE 수석 신경공학자. 실험체 23번(제로)의 코어 차단 수술을 집도했다. 17년 전 퇴직 후 빈민가에서 무료 진료소를 운영 중이다.",
                "코어 차단 해제 코드를 보유하고 있다."),
            new GlossaryTermData("place","17구역","District 17",
                "도시의 LUX-ZERO 계층 밀집 빈민가. 도시 배수로 아래에 위치한다. 확률 부전증 환자가 밀집해 있으며 비가 더 자주 내리는 것 같다고들 한다."),
            new GlossaryTermData("place","21구역","District 21",
                "카림 하산의 지하 작업실이 위치한 구역. 포스터와 낙서가 가득한 거리들. 카림은 이곳에서 THE HOUSE 피해자들의 사진과 기록으로 벽 전체를 채웠다."),
            new GlossaryTermData("place","성 루시아 요양원","St. Lucia Sanatorium",
                "17구역 인근 공공 요양원. 유리창이 깨져있고 복도 한쪽 형광등이 반쯤 꺼져있다. 핀 아르코의 분 라일이 식물인간 상태로 요양 중인 곳이다."),
            new GlossaryTermData("secret","실험체 23번","Subject 23",
                "THE HOUSE 초기 실험 프로그램의 첫 번째 대상. 여섯 살 이전 포르투나 코어 강제 차단 수술을 받았다. 이름은 삭제되고 번호만 남았다. 현재 THE HOUSE 딜러 제로의 과거 정체로 추정된다.",
                "THE HOUSE 기록: \"최초 실험 대상. 강제 LUX 제거 성공. 포르투나 코어 완전 차단 확인.\""),
            new GlossaryTermData("secret","코어 차단 해제 코드","Core Release Code",
                "강제 차단된 포르투나 코어를 복구할 수 있는 코드가 담긴 장치. 닥터 올라 베이크가 보유하고 있다. THE HOUSE는 이 코드의 존재를 공식적으로 부인한다.",
                "사용 시 효과 불명. 20년간 차단된 코어가 한꺼번에 열릴 경우 예측 불가능한 반응이 일어날 수 있다."),
            new GlossaryTermData("secret","포르투나-자아 연결","Fortuna-Ego Link",
                "포르투나 코어가 자아 인식 신경망과 연동되어 있다는 신경공학 연구 결과. THE HOUSE가 독점하고 있는 기밀 데이터. LUX를 파는 것은 단순히 불운해지는 것이 아니라 자기 자신을 잃는 것을 의미한다.",
                "이 데이터를 외부에 유출한 닥터 올라 베이크는 처리 대상이 된다."),
        };
    }

    private void Update()
    {
        BlinkGlossarySearchCursor();

        if (IsSubPanelOpen())
        {
            ResetInventoryImageHoverColors();
            return;
        }

        RefreshInventoryImageHoverColors();

        if (worldImage == null || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Canvas canvas = worldImage.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(worldImage.rectTransform, Input.mousePosition, eventCamera))
        {
            OpenWorldInfoPanel();
        }
    }

    private Image FindChildImageByName(Transform root, string objectName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name == objectName)
            {
                return images[i];
            }
        }

        return null;
    }

    private void OpenWorldInfoPanel()
    {
        if (IsSubPanelOpen())
        {
            return;
        }

        EnsureWorldInfoPanel();

        if (worldInfoPanel == null)
        {
            return;
        }

        worldInfoPanel.SetActive(true);
        worldInfoPanel.transform.SetAsLastSibling();
        BringCloseButtonToFront();
        ResetInventoryImageHoverColors();
        SelectWorldInfo(Mathf.Clamp(selectedWorldInfoIndex, 0, GetCodexEntryCount() - 1));
    }

    private void BringCloseButtonToFront()
    {
        Transform closeButtonTransform = transform.Find(InventoryCloseButtonName);

        if (closeButtonTransform != null)
        {
            closeButtonTransform.SetAsLastSibling();
        }
    }

    private void EnsureWorldInfoPanel()
    {
        if (worldInfoPanel != null)
        {
            return;
        }

        RectTransform panelTransform = transform as RectTransform;

        if (panelTransform == null)
        {
            return;
        }

        EnsureDefaultCodexEntries();

        Transform oldPanel = transform.Find(WorldInfoPanelName);
        if (oldPanel != null)
        {
            Destroy(oldPanel.gameObject);
        }

        worldInfoPanel = new GameObject(WorldInfoPanelName, typeof(RectTransform), typeof(Image), typeof(Outline));
        worldInfoPanel.transform.SetParent(panelTransform, false);

        RectTransform infoTransform = worldInfoPanel.GetComponent<RectTransform>();
        infoTransform.anchorMin = new Vector2(0.5f, 0.5f);
        infoTransform.anchorMax = new Vector2(0.5f, 0.5f);
        infoTransform.pivot = new Vector2(0.5f, 0.5f);
        infoTransform.anchoredPosition = Vector2.zero;
        infoTransform.sizeDelta = worldInfoPanelSize;

        Image panelImage = worldInfoPanel.GetComponent<Image>();
        panelImage.color = WithAlpha(Bg, worldInfoPanelAlpha);
        panelImage.raycastTarget = true;

        Outline panelOutline = worldInfoPanel.GetComponent<Outline>();
        panelOutline.effectColor = Border;
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CreateCodexSidebar(worldInfoPanel.transform);
        CreateCodexContentArea(worldInfoPanel.transform);
        BuildWorldNavigation();
    }

    private void CreateCodexSidebar(Transform parent)
    {
        GameObject sidebarObject = CreateRectObject("Sidebar", parent, typeof(Image));
        RectTransform sidebarTransform = sidebarObject.GetComponent<RectTransform>();
        sidebarTransform.anchorMin = new Vector2(0f, 0f);
        sidebarTransform.anchorMax = new Vector2(0f, 1f);
        sidebarTransform.pivot = new Vector2(0f, 0.5f);
        sidebarTransform.anchoredPosition = Vector2.zero;
        sidebarTransform.sizeDelta = new Vector2(330f, 0f);

        Image sidebarImage = sidebarObject.GetComponent<Image>();
        sidebarImage.color = WithAlpha(Bg2, worldInfoPanelAlpha);
        sidebarImage.raycastTarget = true;

        GameObject headerObject = CreateRectObject("SidebarHeader", sidebarObject.transform, typeof(Image));
        RectTransform headerTransform = headerObject.GetComponent<RectTransform>();
        headerTransform.anchorMin = new Vector2(0f, 1f);
        headerTransform.anchorMax = new Vector2(1f, 1f);
        headerTransform.pivot = new Vector2(0.5f, 1f);
        headerTransform.anchoredPosition = Vector2.zero;
        headerTransform.sizeDelta = new Vector2(0f, 92f);

        Image headerImage = headerObject.GetComponent<Image>();
        headerImage.color = WithAlpha(Bg2, worldInfoPanelAlpha);
        headerImage.raycastTarget = false;

        TextMeshProUGUI titleText = CreateText("SidebarTitle", headerObject.transform, "LUCKLESS", 19f, Pink, TextAlignmentOptions.Left);
        SetStretch(titleText.rectTransform, new Vector2(24f, 42f), new Vector2(-18f, -18f));
        titleText.characterSpacing = 18f;

        TextMeshProUGUI subText = CreateText("SidebarSub", headerObject.transform, "WORLD CODEX v.2209", 18f, TextDim, TextAlignmentOptions.Left);
        SetStretch(subText.rectTransform, new Vector2(24f, 16f), new Vector2(-18f, -50f));

        GameObject headerLine = CreateRectObject("HeaderLine", headerObject.transform, typeof(Image));
        RectTransform lineTransform = headerLine.GetComponent<RectTransform>();
        lineTransform.anchorMin = new Vector2(0f, 0f);
        lineTransform.anchorMax = new Vector2(1f, 0f);
        lineTransform.pivot = new Vector2(0.5f, 0.5f);
        lineTransform.anchoredPosition = Vector2.zero;
        lineTransform.sizeDelta = new Vector2(0f, 1f);
        headerLine.GetComponent<Image>().color = Border;
    }

    private void CreateCodexContentArea(Transform parent)
    {
        GameObject contentAreaObject = CreateRectObject("ContentArea", parent, typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform contentAreaTransform = contentAreaObject.GetComponent<RectTransform>();
        contentAreaTransform.anchorMin = new Vector2(0f, 0f);
        contentAreaTransform.anchorMax = new Vector2(1f, 1f);
        contentAreaTransform.offsetMin = new Vector2(330f, 0f);
        contentAreaTransform.offsetMax = Vector2.zero;

        Image contentAreaImage = contentAreaObject.GetComponent<Image>();
        contentAreaImage.color = WithAlpha(Bg, worldInfoPanelAlpha);
        contentAreaImage.raycastTarget = true;

        GameObject contentObject = CreateRectObject("EntryContent", contentAreaObject.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        worldContentRoot = contentObject.GetComponent<RectTransform>();
        worldContentRoot.anchorMin = new Vector2(0f, 1f);
        worldContentRoot.anchorMax = new Vector2(1f, 1f);
        worldContentRoot.pivot = new Vector2(0.5f, 1f);
        worldContentRoot.anchoredPosition = Vector2.zero;
        worldContentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(54, 54, 48, 52);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        worldContentScroll = contentAreaObject.GetComponent<ScrollRect>();
        worldContentScroll.viewport = contentAreaTransform;
        worldContentScroll.content = worldContentRoot;
        worldContentScroll.horizontal = false;
        worldContentScroll.vertical = true;
        worldContentScroll.movementType = ScrollRect.MovementType.Clamped;
        worldContentScroll.scrollSensitivity = 36f;
    }

    private void BuildWorldNavigation()
    {
        worldNavBackgrounds.Clear();
        worldNavDots.Clear();
        worldNavLeftBars.Clear();
        worldNavLabels.Clear();

        Transform sidebar = worldInfoPanel.transform.Find("Sidebar");
        if (sidebar == null)
        {
            return;
        }

        float currentY = -104f;
        string currentCategory = null;

        for (int i = 0; i < GetCodexEntryCount(); i++)
        {
            WorldCodexEntry entry = GetCodexEntry(i);

            if (currentCategory != entry.category)
            {
                currentCategory = entry.category;
                CreateCategoryLabel(sidebar, currentCategory, currentY);
                currentY -= 35f;
            }

            CreateNavItem(sidebar, i, currentY);
            currentY -= 47f;
        }

        RefreshWorldNavigation();
    }

    private void CreateCategoryLabel(Transform parent, string label, float y)
    {
        TextMeshProUGUI labelText = CreateText("Category_" + label, parent, label, 17f, TextDim, TextAlignmentOptions.Left);
        RectTransform labelTransform = labelText.rectTransform;
        labelTransform.anchorMin = new Vector2(0f, 1f);
        labelTransform.anchorMax = new Vector2(1f, 1f);
        labelTransform.pivot = new Vector2(0.5f, 1f);
        labelTransform.anchoredPosition = new Vector2(0f, y);
        labelTransform.sizeDelta = new Vector2(0f, 28f);
        labelTransform.offsetMin = new Vector2(24f, labelTransform.offsetMin.y);
        labelTransform.offsetMax = new Vector2(-18f, labelTransform.offsetMax.y);
        labelText.characterSpacing = 10f;
    }

    private void CreateNavItem(Transform parent, int index, float y)
    {
        int capturedIndex = index;
        GameObject itemObject = CreateRectObject("NavItem_" + index, parent, typeof(Image), typeof(Button), typeof(EventTrigger));
        RectTransform itemTransform = itemObject.GetComponent<RectTransform>();
        itemTransform.anchorMin = new Vector2(0f, 1f);
        itemTransform.anchorMax = new Vector2(1f, 1f);
        itemTransform.pivot = new Vector2(0.5f, 1f);
        itemTransform.anchoredPosition = new Vector2(0f, y);
        itemTransform.sizeDelta = new Vector2(0f, 42f);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.color = Color.clear;
        itemImage.raycastTarget = true;

        Button button = itemObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = itemImage;
        button.onClick.AddListener(() => SelectWorldInfo(capturedIndex));

        EventTrigger trigger = itemObject.GetComponent<EventTrigger>();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            hoveredWorldInfoIndex = capturedIndex;
            RefreshWorldNavigation();
        });
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () =>
        {
            if (hoveredWorldInfoIndex == capturedIndex)
            {
                hoveredWorldInfoIndex = -1;
            }

            RefreshWorldNavigation();
        });

        GameObject leftBarObject = CreateRectObject("LeftBar", itemObject.transform, typeof(Image));
        RectTransform leftBarTransform = leftBarObject.GetComponent<RectTransform>();
        leftBarTransform.anchorMin = new Vector2(0f, 0f);
        leftBarTransform.anchorMax = new Vector2(0f, 1f);
        leftBarTransform.pivot = new Vector2(0f, 0.5f);
        leftBarTransform.anchoredPosition = Vector2.zero;
        leftBarTransform.sizeDelta = new Vector2(3f, 0f);
        Image leftBar = leftBarObject.GetComponent<Image>();
        leftBar.color = Color.clear;
        leftBar.raycastTarget = false;

        GameObject dotObject = CreateRectObject("NavDot", itemObject.transform, typeof(Image));
        RectTransform dotTransform = dotObject.GetComponent<RectTransform>();
        dotTransform.anchorMin = new Vector2(0f, 0.5f);
        dotTransform.anchorMax = new Vector2(0f, 0.5f);
        dotTransform.pivot = new Vector2(0.5f, 0.5f);
        dotTransform.anchoredPosition = new Vector2(28f, 0f);
        dotTransform.sizeDelta = new Vector2(8f, 8f);
        Image dot = dotObject.GetComponent<Image>();
        dot.color = TextDim;
        dot.raycastTarget = false;

        TextMeshProUGUI itemText = CreateText("Label", itemObject.transform, GetCodexEntry(index).navTitle, 20f, TextMuted, TextAlignmentOptions.Left);
        SetStretch(itemText.rectTransform, new Vector2(48f, 0f), new Vector2(-14f, 0f));
        itemText.textWrappingMode = TextWrappingModes.NoWrap;
        itemText.overflowMode = TextOverflowModes.Ellipsis;

        worldNavBackgrounds.Add(itemImage);
        worldNavDots.Add(dot);
        worldNavLeftBars.Add(leftBar);
        worldNavLabels.Add(itemText);
    }

    private void SelectWorldInfo(int index)
    {
        if (GetCodexEntryCount() <= 0)
        {
            return;
        }

        selectedWorldInfoIndex = Mathf.Clamp(index, 0, GetCodexEntryCount() - 1);
        BuildWorldContent(GetCodexEntry(selectedWorldInfoIndex));
        RefreshWorldNavigation();

        if (worldContentScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            worldContentScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void RefreshWorldNavigation()
    {
        for (int i = 0; i < worldNavBackgrounds.Count; i++)
        {
            bool selected = i == selectedWorldInfoIndex;
            bool hovered = i == hoveredWorldInfoIndex;

            if (worldNavBackgrounds[i] != null)
            {
                worldNavBackgrounds[i].color = selected ? PinkDim : hovered ? PinkGlow : Color.clear;
            }

            if (worldNavDots[i] != null)
            {
                worldNavDots[i].color = selected ? Pink : hovered ? PinkLight : TextDim;
            }

            if (worldNavLeftBars[i] != null)
            {
                worldNavLeftBars[i].color = selected ? Pink : hovered ? Rgba(212, 83, 126, 0.3f) : Color.clear;
            }

            if (worldNavLabels[i] != null)
            {
                worldNavLabels[i].color = selected ? PinkLight : hovered ? TextColor : TextMuted;
            }
        }
    }

    private void BuildWorldContent(WorldCodexEntry entry)
    {
        if (worldContentRoot == null)
        {
            return;
        }

        ClearChildren(worldContentRoot);

        if (!string.IsNullOrWhiteSpace(entry.classified))
        {
            CreateClassified(entry.classified);
        }

        CreateEntryTag(entry.tag);
        CreateLayoutText("EntryTitle", entry.title, 38f, TextColor, TextAlignmentOptions.Left, FontStyles.Normal);
        CreateLayoutText("EntrySubtitle", entry.subtitle, 22f, TextDim, TextAlignmentOptions.Left, FontStyles.Normal);
        CreateDivider("SubtitleDivider", Border);

        if (entry.blocks == null)
        {
            return;
        }

        for (int i = 0; i < entry.blocks.Length; i++)
        {
            WorldCodexBlock block = entry.blocks[i];

            switch (block.type)
            {
                case WorldCodexBlockType.Highlight:
                    CreateHighlight(block.text);
                    break;
                case WorldCodexBlockType.DataRow:
                    CreateDataRow(block.title, block.value);
                    break;
                case WorldCodexBlockType.Card:
                    CreateTierCard(block.title, block.value, block.text, GetAccentColor(block.accent));
                    break;
                case WorldCodexBlockType.Organization:
                    CreateOrganizationItem(block.title, block.value, block.text, GetAccentColor(block.accent));
                    break;
                case WorldCodexBlockType.Quote:
                    CreateQuote(block.text, block.value);
                    break;
                default:
                    CreateParagraph(block.text);
                    break;
            }
        }
    }

    private void CreateClassified(string text)
    {
        GameObject classifiedObject = CreateRectObject("Classified", worldContentRoot, typeof(Image), typeof(Outline), typeof(LayoutElement));
        Image image = classifiedObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        Outline outline = classifiedObject.GetComponent<Outline>();
        outline.effectColor = Rgba(212, 83, 126, 0.3f);
        outline.effectDistance = new Vector2(1f, -1f);

        LayoutElement layoutElement = classifiedObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 240f;
        layoutElement.preferredHeight = 28f;
        layoutElement.flexibleWidth = 0f;

        TextMeshProUGUI classifiedText = CreateText("Text", classifiedObject.transform, text, 16f, Pink, TextAlignmentOptions.Center);
        SetStretch(classifiedText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));
        classifiedText.characterSpacing = 6f;
    }

    private void CreateEntryTag(string tag)
    {
        GameObject tagObject = CreateRectObject("EntryTag", worldContentRoot, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = tagObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement rootLayout = tagObject.GetComponent<LayoutElement>();
        rootLayout.preferredHeight = 26f;

        TextMeshProUGUI tagText = CreateText("Text", tagObject.transform, tag, 17f, Pink, TextAlignmentOptions.Left);
        tagText.characterSpacing = 10f;
        LayoutElement textLayout = tagText.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 260f;
        textLayout.preferredHeight = 26f;

        GameObject lineObject = CreateRectObject("Line", tagObject.transform, typeof(Image), typeof(LayoutElement));
        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.color = PinkDim;
        lineImage.raycastTarget = false;
        LayoutElement lineLayout = lineObject.GetComponent<LayoutElement>();
        lineLayout.preferredHeight = 1f;
        lineLayout.flexibleWidth = 1f;
    }

    private void CreateParagraph(string text)
    {
        CreateLayoutText("Paragraph", text, 23f, BodyText, TextAlignmentOptions.Left, FontStyles.Normal);
    }

    private void CreateHighlight(string text)
    {
        GameObject highlightObject = CreateRectObject("Highlight", worldContentRoot, typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        Image image = highlightObject.GetComponent<Image>();
        image.color = PinkDim;
        image.raycastTarget = false;

        HorizontalLayoutGroup layout = highlightObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 18, 16, 16);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = highlightObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject barObject = CreateRectObject("LeftBar", highlightObject.transform, typeof(Image), typeof(LayoutElement));
        Image barImage = barObject.GetComponent<Image>();
        barImage.color = Pink;
        barImage.raycastTarget = false;
        LayoutElement barLayout = barObject.GetComponent<LayoutElement>();
        barLayout.preferredWidth = 4f;
        barLayout.minHeight = 64f;

        TextMeshProUGUI textObject = CreateText("Text", highlightObject.transform, text, 22f, PinkLight, TextAlignmentOptions.Left);
        textObject.lineSpacing = 12f;
    }

    private void CreateDataRow(string key, string value)
    {
        GameObject rowObject = CreateRectObject("DataRow", worldContentRoot, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 6, 6);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 42f;

        TextMeshProUGUI keyText = CreateText("Key", rowObject.transform, key, 21f, TextDim, TextAlignmentOptions.Left);
        LayoutElement keyLayout = keyText.gameObject.AddComponent<LayoutElement>();
        keyLayout.flexibleWidth = 1f;

        TextMeshProUGUI valueText = CreateText("Value", rowObject.transform, value, 20f, Gold, TextAlignmentOptions.Right);
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 520f;
        valueLayout.flexibleWidth = 0f;

        CreateDivider("DataDivider", Border);
    }

    private void CreateTierCard(string title, string lux, string description, Color titleColor)
    {
        GameObject cardObject = CreateRectObject("TierCard", worldContentRoot, typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Image image = cardObject.GetComponent<Image>();
        image.color = WithAlpha(Card, worldInfoPanelAlpha);
        image.raycastTarget = false;

        Outline outline = cardObject.GetComponent<Outline>();
        outline.effectColor = titleColor == Gold ? Rgba(200, 154, 69, 0.25f) : Border;
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = cardObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 16, 16);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = cardObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI titleText = CreateText("Name", cardObject.transform, title, 20f, titleColor, TextAlignmentOptions.Left);
        titleText.characterSpacing = 5f;

        if (!string.IsNullOrWhiteSpace(lux))
        {
            CreateText("Lux", cardObject.transform, lux, 18f, Gold, TextAlignmentOptions.Left);
        }

        TextMeshProUGUI descText = CreateText("Description", cardObject.transform, description, 21f, CardText, TextAlignmentOptions.Left);
        descText.lineSpacing = 12f;
    }

    private void CreateOrganizationItem(string rank, string name, string description, Color accent)
    {
        GameObject itemObject = CreateRectObject("OrganizationItem", worldContentRoot, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        HorizontalLayoutGroup layout = itemObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 10, 10);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = itemObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject rankObject = CreateRectObject("Rank", itemObject.transform, typeof(Image), typeof(Outline), typeof(LayoutElement));
        Image rankImage = rankObject.GetComponent<Image>();
        rankImage.color = PinkDim;
        rankImage.raycastTarget = false;
        Outline rankOutline = rankObject.GetComponent<Outline>();
        rankOutline.effectColor = Rgba(212, 83, 126, 0.2f);
        rankOutline.effectDistance = new Vector2(1f, -1f);
        LayoutElement rankLayout = rankObject.GetComponent<LayoutElement>();
        rankLayout.preferredWidth = 48f;
        rankLayout.preferredHeight = 48f;
        rankLayout.flexibleWidth = 0f;

        TextMeshProUGUI rankText = CreateText("Text", rankObject.transform, rank, 18f, accent, TextAlignmentOptions.Center);
        SetStretch(rankText.rectTransform, Vector2.zero, Vector2.zero);

        GameObject detailObject = CreateRectObject("Detail", itemObject.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        VerticalLayoutGroup detailLayout = detailObject.GetComponent<VerticalLayoutGroup>();
        detailLayout.spacing = 5f;
        detailLayout.childAlignment = TextAnchor.UpperLeft;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;
        detailObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText("Name", detailObject.transform, name, 21f, TextColor, TextAlignmentOptions.Left);
        TextMeshProUGUI descText = CreateText("Description", detailObject.transform, description, 21f, OrgText, TextAlignmentOptions.Left);
        descText.lineSpacing = 10f;

        CreateDivider("OrgDivider", Border);
    }

    private void CreateQuote(string quote, string source)
    {
        GameObject quoteObject = CreateRectObject("QuoteBlock", worldContentRoot, typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Image image = quoteObject.GetComponent<Image>();
        image.color = GoldDim;
        image.raycastTarget = false;

        Outline outline = quoteObject.GetComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2f, -1f);

        VerticalLayoutGroup layout = quoteObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 24, 18, 18);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        quoteObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI quoteText = CreateText("Quote", quote, quoteObject.transform, 23f, QuoteText, TextAlignmentOptions.Left, FontStyles.Italic);
        quoteText.lineSpacing = 12f;
        TextMeshProUGUI sourceText = CreateText("Source", quoteObject.transform, source, 17f, TextDim, TextAlignmentOptions.Left);
        sourceText.characterSpacing = 4f;
    }

    private TextMeshProUGUI CreateLayoutText(string name, string text, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles fontStyle)
    {
        return CreateText(name, text, worldContentRoot, fontSize, color, alignment, fontStyle);
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        return CreateText(name, text, parent, fontSize, color, alignment, FontStyles.Normal);
    }

    private TextMeshProUGUI CreateText(string name, string text, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles fontStyle)
    {
        GameObject textObject = CreateRectObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text ?? string.Empty;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.fontStyle = fontStyle;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.raycastTarget = false;
        textComponent.lineSpacing = 8f;
        ApplyTextFont(textComponent);
        return textComponent;
    }

    private void CreateDivider(string name, Color color)
    {
        GameObject dividerObject = CreateRectObject(name, worldContentRoot, typeof(Image), typeof(LayoutElement));
        Image dividerImage = dividerObject.GetComponent<Image>();
        dividerImage.color = color;
        dividerImage.raycastTarget = false;
        LayoutElement layoutElement = dividerObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 1f;
        layoutElement.minHeight = 1f;
    }

    private GameObject CreateRectObject(string name, Transform parent, params Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private void SetStretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private int GetCodexEntryCount()
    {
        return codexEntries == null || codexEntries.Length == 0 ? 0 : codexEntries.Length;
    }

    private WorldCodexEntry GetCodexEntry(int index)
    {
        EnsureDefaultCodexEntries();
        return codexEntries[Mathf.Clamp(index, 0, codexEntries.Length - 1)];
    }

    private void EnsureDefaultCodexEntries()
    {
        if (codexEntries == null || codexEntries.Length == 0)
        {
            codexEntries = CreateDefaultCodexEntries();
        }
    }

    private Color GetAccentColor(WorldCodexAccent accent)
    {
        switch (accent)
        {
            case WorldCodexAccent.Pink:
                return Pink;
            case WorldCodexAccent.PinkLight:
                return PinkLight;
            case WorldCodexAccent.Gold:
                return Gold;
            case WorldCodexAccent.TextMuted:
                return TextMuted;
            case WorldCodexAccent.TextDim:
                return TextDim;
            default:
                return TextColor;
        }
    }

    private void StopFloatRoutines()
    {
        for (int i = 0; i < floatRoutines.Count; i++)
        {
            if (floatRoutines[i] != null)
            {
                StopCoroutine(floatRoutines[i]);
            }
        }

        floatRoutines.Clear();
    }

    private IEnumerator FloatInventoryImage(RectTransform rectTransform, Vector2 basePosition, float amplitude, float speed, float phase)
    {
        while (rectTransform != null && rectTransform.gameObject.activeInHierarchy)
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude;
            rectTransform.anchoredPosition = basePosition + Vector2.up * offsetY;
            yield return null;
        }
    }

    private void RefreshInventoryImageHoverColors()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Image hoveredImage = GetHoveredInventoryImage(eventCamera);

        for (int i = 0; i < inventoryHoverImages.Count; i++)
        {
            Image image = inventoryHoverImages[i];

            if (image == null)
            {
                continue;
            }

            image.color = image == hoveredImage
                ? GetLightPinkHoverColor(inventoryHoverOriginalColors[i])
                : inventoryHoverOriginalColors[i];
        }
    }

    private Image GetHoveredInventoryImage(Camera eventCamera)
    {
        GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();

        if (raycaster != null && EventSystem.current != null)
        {
            pointerRaycastResults.Clear();

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            raycaster.Raycast(pointerData, pointerRaycastResults);

            for (int i = 0; i < pointerRaycastResults.Count; i++)
            {
                Image image = pointerRaycastResults[i].gameObject.GetComponent<Image>();

                if (IsTrackedInventoryHoverImage(image))
                {
                    return image;
                }
            }
        }

        for (int i = inventoryHoverImages.Count - 1; i >= 0; i--)
        {
            Image image = inventoryHoverImages[i];

            if (IsPointerInsideImage(image, eventCamera))
            {
                return image;
            }
        }

        return null;
    }

    private bool IsPointerInsideImage(Image image, Camera eventCamera)
    {
        if (image == null || !image.isActiveAndEnabled)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, Input.mousePosition, eventCamera))
        {
            return false;
        }

        return image.IsRaycastLocationValid(Input.mousePosition, eventCamera);
    }

    private bool IsInventoryHoverImage(Image image)
    {
        if (image == null || image.transform == transform)
        {
            return false;
        }

        if (worldInfoPanel != null && image.transform.IsChildOf(worldInfoPanel.transform))
        {
            return false;
        }

        string objectName = image.gameObject.name;

        if (objectName == "Panel" || objectName == "BlackBackground" || objectName == InventoryCloseButtonName)
        {
            return false;
        }

        return image.sprite != null;
    }

    private bool IsTrackedInventoryHoverImage(Image image)
    {
        return image != null && inventoryHoverImages.Contains(image);
    }

    private void ResetInventoryImageHoverColors()
    {
        for (int i = 0; i < inventoryHoverImages.Count; i++)
        {
            if (inventoryHoverImages[i] != null)
            {
                inventoryHoverImages[i].color = inventoryHoverOriginalColors[i];
            }
        }
    }

    private void SetupTextHover(TextMeshProUGUI text, Color normalColor, Color hoverColor)
    {
        EventTrigger trigger = text.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = text.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerEnter || entry.eventID == EventTriggerType.PointerExit);
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => text.color = hoverColor);
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => text.color = normalColor);
    }

    private void ApplyFontToExistingTexts()
    {
        if (textFont == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            ApplyTextFont(texts[i]);
        }
    }

    private void ApplyTextFont(TextMeshProUGUI text)
    {
        if (textFont == null || text == null)
        {
            return;
        }

        text.font = textFont;
    }

    private void AddClickEvent(Image image, UnityEngine.Events.UnityAction callback)
    {
        image.raycastTarget = true;
        EventTrigger trigger = image.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerClick);
        AddPointerEvent(trigger, EventTriggerType.PointerClick, callback);
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private Color GetLightPinkHoverColor(Color originalColor)
    {
        return Color.Lerp(originalColor, new Color(PinkLight.r, PinkLight.g, PinkLight.b, originalColor.a), 0.35f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(string html)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(html, out color) ? color : Color.white;
    }

    private static Color Rgba(byte r, byte g, byte b, float a)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a);
    }

    private static WorldCodexBlock Paragraph(string text)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.Paragraph, text = text };
    }

    private static WorldCodexBlock Highlight(string text)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.Highlight, text = text, accent = WorldCodexAccent.Pink };
    }

    private static WorldCodexBlock Data(string key, string value)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.DataRow, title = key, value = value, accent = WorldCodexAccent.Gold };
    }

    private static WorldCodexBlock CardBlock(string title, string lux, string description, WorldCodexAccent accent)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.Card, title = title, value = lux, text = description, accent = accent };
    }

    private static WorldCodexBlock OrgBlock(string rank, string name, string description, WorldCodexAccent accent)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.Organization, title = rank, value = name, text = description, accent = accent };
    }

    private static WorldCodexBlock Quote(string text, string source)
    {
        return new WorldCodexBlock { type = WorldCodexBlockType.Quote, text = text, value = source, accent = WorldCodexAccent.Gold };
    }

    private static WorldCodexEntry Entry(string category, string navTitle, string classified, string tag, string title, string subtitle, params WorldCodexBlock[] blocks)
    {
        return new WorldCodexEntry
        {
            category = category,
            navTitle = navTitle,
            classified = classified,
            tag = tag,
            title = title,
            subtitle = subtitle,
            blocks = blocks
        };
    }

    private static WorldCodexEntry[] CreateDefaultCodexEntries()
    {
        return new[]
        {
            Entry(
                "기초 개념",
                "LUX란 무엇인가",
                "",
                "기초 개념 — 001",
                "LUX란 무엇인가",
                "행운을 단위로 쪼갠 화폐. 이 세계에서 가장 비싼 것.",
                Paragraph("LUX는 인간 뇌의 포르투나 코어에서 방출되는 양자 확률 신호를 추출·정제한 단위다. 쉽게 말해, 누군가의 행운을 물질화해서 거래 가능한 형태로 만든 것이다."),
                Highlight("1 LUX ≈ 일상적 행운 하나. 전철을 딱 맞게 탄다. 상사가 오늘따라 없다. 복권이 한 칸 더 긁힌다."),
                Paragraph("소량의 LUX 이식은 삶의 소소한 운을 높인다. 그러나 이것이 축적될수록 효과는 기하급수적으로 강해진다."),
                Data("100 LUX 이상", "전장 생존율, 수술 성공률 이상 상승"),
                Data("1,000 LUX 이상", "연속 투자 성공, 치명적 사고 회피"),
                Data("10,000 LUX 이상", "거의 모든 죽음의 위기를 \"우연히\" 넘김"),
                Data("10,000 LUX 초과", "데이터 없음 — THE HOUSE 독점 보유"),
                Paragraph("2209년 기준, LUX는 전 세계에서 가장 활발하게 거래되는 상품 중 하나다. 그러나 공식 화폐로 인정받지 않는다. THE HOUSE가 그것을 원하지 않기 때문이다.")
            ),
            Entry(
                "기초 개념",
                "포르투나 코어",
                "",
                "기초 개념 — 002",
                "포르투나 코어",
                "2187년 발견. 행운의 물리적 실체.",
                Paragraph("2187년, 신경공학자 아델라 포르투나 박사는 인간 뇌의 특정 신경망 클러스터에서 미세한 양자 신호가 방출된다는 사실을 발견했다. 그녀는 이 클러스터를 자신의 이름을 따 '포르투나 코어'라 명명했다."),
                Quote("\"처음에 학계는 비웃었다. '행운을 뇌에서 찾겠다는 거냐'고. 그러나 실험 결과 앞에서 냉소는 공포로 바뀌었다.\"", "— 포르투나 코어 발견 1주년 기고문, 2188"),
                Paragraph("포르투나 코어가 방출하는 신호는 국소 확률장을 미세하게 비틀었다. 총알이 1.3도 빗나가게. 동전이 앞면으로 떨어지게. 계약서 오탈자가 자신에게 유리하게 해석되게."),
                Paragraph("그 \"아주 조금\"이 수십 년, 수백 번 누적되면 — 그게 우리가 행운아라 부르던 사람들의 정체였다."),
                Highlight("코어는 생애 전반에 걸쳐 서서히 소모된다. 그 소모 속도를 인위적으로 높이면 — 추출이 가능하다.")
            ),
            Entry(
                "기초 개념",
                "LUX 거래 구조",
                "",
                "기초 개념 — 003",
                "LUX 거래 구조",
                "자발적 매각과 강제 수거. 두 가지 방법, 하나의 결과.",
                Paragraph("LUX가 거래되는 방식은 크게 두 가지다. 그러나 어느 쪽이든 판매자에게 돌아오는 것은 같다 — 불운."),
                CardBlock("① 자발적 판매", "", "빈민층이 생계를 위해 포르투나 코어에서 LUX를 추출해 판다. 1 LUX = 사흘치 식비(2209년 17구역 기준). 팔수록 더 불운해지고, 불운해질수록 더 팔게 된다. 스스로 선택했기 때문에 강제가 아니라는 것이 THE HOUSE의 공식 입장이다.", WorldCodexAccent.Gold),
                CardBlock("② 강제 수거", "", "채무 불이행 고객에게 포르투나 코어를 마취 없이 강제 추출한다. 마취를 하면 코어가 손상된다는 이유다. 당사자는 즉시 확률 부전증 상태에 빠진다. 계단에서 넘어진다. 비가 올 때 나간다. 아무도 이것이 LUX 때문이라고 증명하지 못한다.", WorldCodexAccent.Pink),
                Paragraph("강제 수거 이후 사망자 기록은 THE HOUSE 데이터베이스에 \"통상적 사례\"로 분류된다. 아무도 이의를 제기하지 않는다. 아무도 그 기록을 열람할 수 없기 때문이다.")
            ),
            Entry(
                "사회 구조",
                "도시 계층 시스템",
                "",
                "사회 구조 — 001",
                "도시 계층 시스템",
                "빈부 격차가 아니다. 행운 격차다.",
                Paragraph("2209년의 도시는 수직으로 나뉜다. 건물 높이가 아니라 LUX 보유량으로."),
                CardBlock("LUX-HIGH / 상층부", "LUX 200 이상 보유", "사고율 35% 낮음. 사업 성공률 62% 높음. 이들은 자신이 노력으로 성공했다고 믿는다. 실제로는 행운을 산 것이지만, 아무도 그 돈이 어디서 왔는지는 묻지 않는다.", WorldCodexAccent.Gold),
                CardBlock("LUX-NEUTRAL / 중층부", "LUX 10~200 보유", "평범한 삶. 가끔 LUX를 팔아 급전을 마련하지만 악순환에는 빠지지 않은 상태. THE HOUSE의 주요 타깃 시장. 불안을 자극하는 광고가 하루에도 수십 개 노출된다.", WorldCodexAccent.TextMuted),
                CardBlock("LUX-ZERO / 하층부", "LUX 10 미만 보유", "확률 부전증 환자들이 밀집한 빈민가. 이 구역에서는 비가 더 자주 내리는 것 같다고들 말한다. 기상 데이터를 분석하던 연구자가 있었다. 그는 발표 직전 교통사고로 사망했다.", WorldCodexAccent.Pink),
                CardBlock("LUX-BLACK / 지하", "공식 통계 없음", "LUX를 불법 재분배하는 지하 조직들. 정치적 동기의 급진파, 빈민에게 LUX를 무상 공급하는 의료 운동, 또 다른 카르텔 — 전부 섞여있다.", WorldCodexAccent.TextDim)
            ),
            Entry(
                "사회 구조",
                "국가 붕괴의 역사",
                "",
                "사회 구조 — 002",
                "국가 붕괴의 역사",
                "어떻게 기업이 국가를 대체했는가.",
                Paragraph("2209년 이전, 인류는 두 번의 대전쟁과 네 번의 경제 붕괴를 겪었다. 국가들은 여전히 지도 위에 존재하지만, 그것은 껍데기에 불과하다."),
                Data("실질 권력", "초거대 기업 / 군사 카르텔 / 기술 집단"),
                Data("국가의 역할", "형식적 행정 / THE HOUSE 앞에선 손님"),
                Data("법과 군대", "충분한 LUX를 가진 조직 앞에서 무력"),
                Paragraph("이 세계에서 가장 오래된 질문 — \"왜 어떤 사람은 잘 되고, 어떤 사람은 안 되는가\" — 은 더 이상 철학이 아니다. 그것은 과학이고, 경제학이고, 그리고 범죄의 영역이다."),
                Quote("\"정부가 무너진 게 아니에요. 그냥 더 이상 필요가 없어진 거죠. THE HOUSE가 더 효율적으로 질서를 유지하니까요.\"", "— 익명의 전직 외교관, 인터뷰 기록")
            ),
            Entry(
                "사회 구조",
                "행운 빈곤의 악순환",
                "",
                "사회 구조 — 003",
                "행운 빈곤의 악순환",
                "가난할수록 행운을 팔고, 팔수록 더 가난해진다.",
                Paragraph("THE HOUSE의 마케팅 부서가 가장 두려워하는 단어가 있다면, 그건 '악순환'이다. 물론 공식적으로는 부정한다."),
                Highlight("LUX 판매 → 불운 증가 → 사고·실직 → 돈 부족 → LUX 추가 판매 → 더 큰 불운 → ..."),
                Paragraph("이 사이클은 한 번 진입하면 빠져나오기가 거의 불가능하다. LUX를 팔지 않으면 당장 먹을 것이 없고, 팔면 나중에 더 큰 불운이 닥친다. 가난한 사람들은 미래의 행운을 담보로 현재를 산다."),
                Paragraph("THE HOUSE의 광고 슬로건은 이렇다. \"조금 더 운이 좋은 삶을 위하여.\" 이 문장이 2209년 가장 많이 노출된 광고 카피다. 누군가의 LUX를 빼앗아 다른 누군가에게 팔면서."),
                Quote("\"손녀 수술비가 없었어요. LUX를 팔면 된다고 했어요. 두 번이면 충분하다고. 두 번으로 모자랐어요. 그래서 세 번, 네 번.\"", "— 이다 펜 / 17구역 주민")
            ),
            Entry(
                "THE HOUSE",
                "조직 개요",
                "",
                "THE HOUSE — 001",
                "THE HOUSE 조직 개요",
                "표면: 글로벌 엔터테인먼트 복합기업. 실체: LUX 독점 카르텔.",
                Paragraph("THE HOUSE는 127개국에 카지노, 리조트, 확률 보험 서비스를 운영하는 합법적 기업이다. 주요 도시마다 'LUX 클리닉'이 있고, 웰니스 서비스라는 이름으로 LUX를 판다."),
                Data("운영 국가", "127개국"),
                Data("전 세계 LUX 시장 점유율", "78%"),
                Data("공식 슬로건", "\"조금 더 운이 좋은 삶을 위하여\""),
                Data("실질 활동", "강제 수거 / 경쟁 조직 와해 / 정보 독점"),
                Paragraph("THE HOUSE 앞에서 법도, 정부도, 군대도 손님이다. 아주 큰 카지노의 손님들."),
                Highlight("THE HOUSE의 조직 구조는 철저하게 카드 게임의 메타포로 설계되었다. 우연이 아니다.")
            ),
            Entry(
                "THE HOUSE",
                "조직도",
                "",
                "THE HOUSE — 002",
                "조직도",
                "카드 게임을 닮은 위계 구조.",
                OrgBlock("HM", "하우스마스터 (Housemaster)", "최상위 의사결정자. 딜러와 직접 소통하는 유일한 상위 존재. 정체는 기밀. 공개 석상에 절대 나타나지 않는다.", WorldCodexAccent.Pink),
                OrgBlock("D", "딜러 (Dealer)", "현장 집행관. 표적 제거, 강제 수거, 경쟁 조직 와해 담당. 전 세계 6명 뿐이다. 서로의 임무에 원칙적으로 간섭하지 않는다. 내부 실적 경쟁 치열. 사망 또는 배신 시 즉시 교체.", WorldCodexAccent.Pink),
                OrgBlock("HK", "하우스킵퍼 (Housekeeper)", "행정·정보 분석 담당. LUX 거래 기록, 표적 추적 데이터, 채무 현황 등 모든 내부 정보를 관리한다. THE HOUSE의 두뇌.", WorldCodexAccent.Pink),
                OrgBlock("CC", "카드 카운터 (Card Counter)", "시장 모니터링 및 가격 조작 전문가. LUX 시세를 인위적으로 조정해 빈민층의 판매를 유도한다.", WorldCodexAccent.Pink),
                OrgBlock("CR", "칩 러너 (Chip Runner)", "LUX 운반 및 세탁 담당 하위 조직. 조직도 최하위지만 가장 많은 인원이 배치된다. 대부분은 자신이 뭘 운반하는지도 모른다.", WorldCodexAccent.TextDim)
            ),
            Entry(
                "THE HOUSE",
                "딜러 제도",
                "",
                "THE HOUSE — 003",
                "딜러 제도",
                "6명. 그것이 전부다.",
                Paragraph("전 세계에 6명뿐인 THE HOUSE의 현장 집행관. 딜러는 하우스마스터의 직접 지시를 받으며, 어떠한 임무도 거절할 수 없다."),
                Data("총 인원", "6명 (전 세계)"),
                Data("보고 체계", "하우스마스터 직속"),
                Data("현장 재량권", "부여됨 (한도 있음)"),
                Data("교체 조건", "사망 또는 배신 시 즉시"),
                Paragraph("딜러 직위의 가장 큰 특징은 LUX 지급 방식이다. 임무 완료 시 LUX로 보상을 받는다. 이것이 함정이다 — LUX를 받을수록 잃을 것이 생기고, 잃을 것이 생길수록 THE HOUSE를 떠나기 어려워진다."),
                Highlight("THE HOUSE가 가장 두려워하는 딜러: 처음부터 LUX가 없어서 잃을 것이 없는 딜러. 즉, 제로.")
            ),
            Entry(
                "기밀 문서",
                "코어와 자아의 연결",
                "기밀 등급 A — 열람 제한",
                "기밀 문서 — 001",
                "코어와 자아의 연결",
                "행운이 없어지는 게 아니다. 자기 자신이 없어지는 것이다.",
                Paragraph("포르투나 코어는 단순한 확률 신호 발생기가 아니다. 신경공학계 최신 연구에 따르면 코어는 자아 인식 신경망과 직접 연동되어 있다."),
                Data("코어 활성 상태", "자기 확신 강함 / 결단력 있음 / 두려움 적음"),
                Data("코어 약화 상태", "자아 윤곽 흐릿 / 타인 결정 추종 / 미래 설계 불가"),
                Data("코어 완전 차단 시", "기록 없음 (THE HOUSE 독점)"),
                Paragraph("즉, LUX를 팔면 단순히 운이 나빠지는 게 아니다. 서서히 자기 자신을 잃는다. 주변의 결정을 따르게 되고, 위험 앞에서 멈추게 되고, 미래를 그리지 못하게 된다."),
                Paragraph("THE HOUSE는 이 사실을 알고 있다. 그리고 그것을 이용해 조직을 운영한다. LUX 없는 딜러는 순종적이다. LUX를 소모해 싸우는 딜러는 결국 의지마저 소진한다."),
                Quote("\"행운이 없어지는 것이 아니에요. 자기 자신이 없어지는 것이에요.\"", "— 닥터 올라 베이크 / 전 THE HOUSE 수석 신경공학자")
            ),
            Entry(
                "기밀 문서",
                "실험체 23번",
                "기밀 등급 S — 최고 제한",
                "기밀 문서 — 002",
                "실험체 23번",
                "THE HOUSE 초기 실험 기록. 이름 삭제. 번호만 남음.",
                Paragraph("THE HOUSE 내부 문서에서 '실험체 23번'은 단 한 줄로 기록되어 있다."),
                Highlight("\"23 — 최초 실험 대상. 강제 LUX 제거 성공. 포르투나 코어 완전 차단 확인.\""),
                Data("추정 나이 (수술 당시)", "6세"),
                Data("코어 상태", "파괴 아님 — 차단 (THE HOUSE 원격 제어)"),
                Data("수술 집도의", "닥터 올라 베이크"),
                Data("이후 처리", "훈련 프로그램 편입 (20년)"),
                Paragraph("연구 가설은 하나였다. LUX를 소모하지 않는 딜러를 만들 수 있는가? LUX가 없는 인간은 행운의 보호막이 없으니 무모하지 않고 계산적이다. 잃을 것이 없으니 충성이 가치 있다. 예측 가능하다. THE HOUSE에게 완벽한 도구다."),
                Paragraph("20년 후, 실험은 성공했다. THE HOUSE에서 가장 효율적인 딜러가 탄생했다."),
                Quote("\"기억도 없고, 과거도 없고, 되돌아갈 곳도 없고, 그러니 떠날 이유도 없는. 그런 존재를 만들고 싶었던 것 같아요. 이름조차 그래서 '제로'였겠죠.\"", "— 닥터 올라 베이크 / 수술 집도의")
            ),
            Entry(
                "기밀 문서",
                "하우스마스터의 정체",
                "기밀 등급 S — 최고 제한",
                "기밀 문서 — 003",
                "하우스마스터의 정체",
                "인간인가, 아닌가. 그 경계에 있는 존재.",
                Paragraph("하우스마스터에 대해 공식 확인된 정보는 없다. THE HOUSE 내부에서도 그 정체를 아는 딜러는 없다. 단, 하우스마스터 본인이 드물게 언급한 것들이 있다."),
                Data("신체 분류", "1세대 안드로이드 — 자칭"),
                Data("포르투나 코어", "없음 (처음부터)"),
                Data("LUX 보유량", "0 — 필요 없음"),
                Data("눈 색", "분홍. 숨이 없다. 너무 고요하다."),
                Paragraph("하우스마스터의 논리는 일관성이 있다. LUX가 없는 존재가 LUX의 가치를 가장 잘 안다. 없는 사람만이 가진 것의 값을 제대로 안다. 그래서 THE HOUSE를 만들었다."),
                Quote("\"우린 같은 거예요, 제로. LUX 없이 이 세계에서 살아남은 것들. 그러면 — 우리가 이기는 거예요.\"", "— 하우스마스터 / 제로에게"),
                Paragraph("그러나 하우스마스터가 말하지 않는 것이 있다. 자신은 처음부터 LUX가 없었지만, 제로는 — 빼앗겼다. 그 차이가 결국 두 존재를 가른다.")
            )
        };
    }
}
