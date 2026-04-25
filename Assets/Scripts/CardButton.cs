using TMPro;
using UnityEngine;

public class CardButton : MonoBehaviour
{
    public CardData cardData;
    public BattleManager battleManager;

    public TMP_Text cardNameText;
    public TMP_Text descriptionText;

    private void Start()
    {
        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
    }

    public void OnClickCard()
    {
        battleManager.UseCard(cardData);
    }
}