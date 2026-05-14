using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        ApplyCardImage();
    }

    private void ApplyCardImage()
    {
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
