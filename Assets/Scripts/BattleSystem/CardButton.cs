using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class CardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;
    public BattleManager battleManager;

    public TMP_Text cardNameText;

    [Header("Card Image")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Sprite dealImage;
    [SerializeField] private Sprite houseImage;
    [SerializeField] private Sprite gambleImage;
    [SerializeField] private Sprite povertyImage;
    [SerializeField] private Sprite negotiationImage;
    [SerializeField] private Sprite manipulationImage;
    [SerializeField] private Sprite fluxImage;
    [SerializeField] private Sprite barrierImage;
    [SerializeField] private Color nameOutlineColor = new Color(0.16f, 0.16f, 0.16f, 0.50f);
    [SerializeField] private float nameOutlineDistance = 1.7f;

    private static readonly Vector2[] NameOutlineOffsets =
    {
        new Vector2(-1f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, -1f),
        new Vector2(0f, 1f),
        new Vector2(-1f, -1f),
        new Vector2(-1f, 1f),
        new Vector2(1f, -1f),
        new Vector2(1f, 1f)
    };

    private readonly TextMeshProUGUI[] nameOutlineTexts = new TextMeshProUGUI[NameOutlineOffsets.Length];
    private Coroutine clickFeedbackRoutine;
    private Vector3 clickFeedbackBaseScale = Vector3.one;
    private Vector2 clickFeedbackBasePosition;
    private Color clickFeedbackBaseColor = Color.white;
#if UNITY_EDITOR
    private bool nameOutlineRefreshQueued;
#endif

    private void Awake()
    {
        EnsureImageReference();
        RefreshNameTextOutlineSafely();
    }

    private void OnValidate()
    {
        EnsureImageReference();
    }

    private void Start()
    {
        if (cardData != null)
        {
            RefreshCardUI();
        }
    }

    public void RefreshCardUI()
    {
        if (cardData == null)
        {
            Debug.LogWarning("CardData가 비어 있습니다.", gameObject);
            return;
        }

        if (cardNameText == null)
        {
            Debug.LogWarning("CardNameText가 연결되지 않았습니다.", gameObject);
            return;
        }

        cardNameText.richText = true;
        cardNameText.text = battleManager != null
            ? battleManager.GetCardButtonLabel(cardData)
            : $"{cardData.cardName}\n<LUX {cardData.luxCost}>";

        ApplyNameTextOutline();
        ApplyCardImage();
    }

    private void ApplyNameTextOutline()
    {
        if (cardNameText == null)
        {
            return;
        }

        TextMeshProUGUI source = cardNameText as TextMeshProUGUI;
        if (source == null)
        {
            return;
        }

        RemoveUnusedNameOutlineTexts(source.transform.parent);
        EnsureNameOutlineTexts(source);

        for (int i = 0; i < nameOutlineTexts.Length; i++)
        {
            TextMeshProUGUI outline = nameOutlineTexts[i];
            if (outline == null) continue;

            CopyNameTextStyle(source, outline);
            outline.text = source.text;
            outline.color = nameOutlineColor;
        }

        source.transform.SetAsLastSibling();
    }

    private void RefreshNameTextOutlineSafely()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueNameOutlineRefresh();
            return;
        }
#endif
        ApplyNameTextOutline();
    }

#if UNITY_EDITOR
    private void QueueNameOutlineRefresh()
    {
        if (nameOutlineRefreshQueued)
        {
            return;
        }

        nameOutlineRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            nameOutlineRefreshQueued = false;
            if (this == null)
            {
                return;
            }

            ApplyNameTextOutline();
        };
    }
#endif

    private void EnsureNameOutlineTexts(TextMeshProUGUI source)
    {
        Transform parent = source.transform.parent;
        for (int i = 0; i < nameOutlineTexts.Length; i++)
        {
            Vector2 offset = NameOutlineOffsets[i];

            if (nameOutlineTexts[i] == null)
            {
                Transform existing = parent.Find("CardNameTextOutline_" + i);
                if (existing != null)
                {
                    nameOutlineTexts[i] = existing.GetComponent<TextMeshProUGUI>();
                }
            }

            if (nameOutlineTexts[i] == null)
            {
                GameObject outlineObject = new GameObject("CardNameTextOutline_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                outlineObject.transform.SetParent(parent, false);
                outlineObject.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
                nameOutlineTexts[i] = outlineObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform sourceRect = source.rectTransform;
            RectTransform outlineRect = nameOutlineTexts[i].rectTransform;
            outlineRect.anchorMin = sourceRect.anchorMin;
            outlineRect.anchorMax = sourceRect.anchorMax;
            outlineRect.pivot = sourceRect.pivot;
            outlineRect.sizeDelta = sourceRect.sizeDelta;
            outlineRect.anchoredPosition = sourceRect.anchoredPosition + offset * nameOutlineDistance;
            outlineRect.localRotation = sourceRect.localRotation;
            outlineRect.localScale = sourceRect.localScale;
            nameOutlineTexts[i].raycastTarget = false;
            nameOutlineTexts[i].transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        }
    }

    private void RemoveUnusedNameOutlineTexts(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = nameOutlineTexts.Length; i < 24; i++)
        {
            Transform extra = parent.Find("CardNameTextOutline_" + i);
            if (extra == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(extra.gameObject);
            }
            else
#endif
            {
                Destroy(extra.gameObject);
            }
        }
    }

    private void CopyNameTextStyle(TextMeshProUGUI source, TextMeshProUGUI target)
    {
        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.alignment = source.alignment;
        target.textWrappingMode = source.textWrappingMode;
        target.overflowMode = source.overflowMode;
        target.richText = source.richText;
        target.characterSpacing = source.characterSpacing;
        target.wordSpacing = source.wordSpacing;
        target.lineSpacing = source.lineSpacing;
        target.enableAutoSizing = source.enableAutoSizing;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
    }

    private void ApplyCardImage()
    {
        EnsureImageReference();

        if (cardImage == null || cardData == null)
        {
            return;
        }

        Sprite sprite = cardData.cardImage != null ? cardData.cardImage : GetTypeImage(cardData.cardType);
        if (sprite != null)
        {
            cardImage.sprite = sprite;
        }
    }

    private void EnsureImageReference()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
    }

    private Sprite GetTypeImage(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Deal:
                return dealImage;
            case CardType.House:
                return houseImage;
            case CardType.Gamble:
                return gambleImage;
            case CardType.Poverty:
                return povertyImage;
            case CardType.Negotiation:
                return negotiationImage;
            case CardType.Manipulation:
                return manipulationImage;
            case CardType.Flux:
                return fluxImage;
            case CardType.Barrier:
                return barrierImage;
            default:
                return null;
        }
    }

    public void OnClickCard()
{
    if (battleManager == null)
    {
        Debug.LogWarning("BattleManager가 연결되지 않았습니다.", gameObject);
        return;
    }

    if (cardData == null)
    {
        Debug.LogWarning("CardData가 연결되지 않았습니다.", gameObject);
        return;
    }

    battleManager.PlayCardSelectSound();

    if (!battleManager.HasEnoughLuxForCard(cardData))
    {
        PlayInsufficientLuxFeedback();
        return;
    }

    PlayClickBoing();
    battleManager.UseCard(cardData);
}

    private void PlayClickBoing()
    {
        StartClickFeedback(ClickBoingRoutine());
    }

    private void PlayInsufficientLuxFeedback()
    {
        StartClickFeedback(InsufficientLuxFeedbackRoutine());
    }

    private void StartClickFeedback(IEnumerator routine)
    {
        if (clickFeedbackRoutine != null)
        {
            StopCoroutine(clickFeedbackRoutine);
            RestoreClickFeedbackState();
        }

        clickFeedbackRoutine = StartCoroutine(routine);
    }

    private IEnumerator ClickBoingRoutine()
    {
        Transform target = transform;
        clickFeedbackBaseScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;
        RectTransform rectTransform = transform as RectTransform;
        clickFeedbackBasePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        Image image = GetComponent<Image>();
        clickFeedbackBaseColor = image != null ? image.color : Color.white;

        yield return ScaleOver(clickFeedbackBaseScale, clickFeedbackBaseScale * 1.09f, 0.07f);
        yield return ScaleOver(clickFeedbackBaseScale * 1.09f, clickFeedbackBaseScale * 0.98f, 0.055f);
        yield return ScaleOver(clickFeedbackBaseScale * 0.98f, clickFeedbackBaseScale, 0.055f);

        target.localScale = clickFeedbackBaseScale;
        clickFeedbackRoutine = null;
    }

    private IEnumerator InsufficientLuxFeedbackRoutine()
    {
        RectTransform rectTransform = transform as RectTransform;
        Image image = GetComponent<Image>();

        clickFeedbackBaseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        clickFeedbackBasePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        clickFeedbackBaseColor = image != null ? image.color : Color.white;

        Color darkColor = Color.Lerp(clickFeedbackBaseColor, Color.black, 0.38f);
        if (image != null)
        {
            yield return LerpImageColor(image, clickFeedbackBaseColor, darkColor, 0.04f);
        }

        float startTime = Time.realtimeSinceStartup;
        const float duration = 0.24f;
        const float distance = 16f;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float offset = Mathf.Sin(t * Mathf.PI * 8f) * distance * (1f - t);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = clickFeedbackBasePosition + new Vector2(offset, 0f);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        RestoreClickFeedbackState();

        if (image != null)
        {
            yield return LerpImageColor(image, darkColor, clickFeedbackBaseColor, 0.08f);
        }

        RestoreClickFeedbackState();
        clickFeedbackRoutine = null;
    }

    private IEnumerator ScaleOver(Vector3 from, Vector3 to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private IEnumerator LerpImageColor(Image image, Color from, Color to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            if (image == null)
            {
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(from, to, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private void RestoreClickFeedbackState()
    {
        transform.localScale = clickFeedbackBaseScale;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = clickFeedbackBasePosition;
        }

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = clickFeedbackBaseColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (battleManager != null && cardData != null)
        {
            battleManager.ShowCardTooltip(cardData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (battleManager != null)
        {
            battleManager.HideCardTooltip();
        }
    }
}
