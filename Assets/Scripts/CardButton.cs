using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;
    public BattleManager battleManager;

    public TMP_Text cardNameText;
    public TMP_Text descriptionText;

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

        if (descriptionText == null)
        {
            Debug.LogWarning("DescriptionText가 연결되지 않았습니다.", gameObject);
            return;
        }

        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
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