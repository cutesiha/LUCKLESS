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
    [SerializeField] private Color nameOutlineColor = new Color(0.16f, 0.16f, 0.16f, 0.62f);
    [SerializeField] private float nameOutlineDistance = 1.8f;

    private readonly TextMeshProUGUI[] nameOutlineTexts = new TextMeshProUGUI[24];
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
            int x = (i % 5) - 2;
            int y = (i / 5) - 2;
            if (x == 0 && y == 0)
            {
                x = 2;
                y = 2;
            }

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
            outlineRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(x * nameOutlineDistance, y * nameOutlineDistance);
            outlineRect.localRotation = sourceRect.localRotation;
            outlineRect.localScale = sourceRect.localScale;
            nameOutlineTexts[i].raycastTarget = false;
            nameOutlineTexts[i].transform.SetSiblingIndex(source.transform.GetSiblingIndex());
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

    battleManager.UseCard(cardData);
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
