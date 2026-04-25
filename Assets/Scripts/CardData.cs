using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "LUCKLESS/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea]
    public string description;

    public int damage;
    public int luxCost;
    public int luxGain;

    public int emotionGain;
    
    public CardType cardType;
}

public enum CardType
{
    Deal,
    House,
    Gamble,
    Poverty,
    Negotiation
}
