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

    [Header("Card Extra")]
    public int selfDamage;
    public int gambleSuccessChance = 50;
    public bool canOnlyUseInPoverty;
    public bool isGambleCard;

    [Header("Turn Control")]
    public bool stunEnemyNextTurn;
    public bool stunPlayerNextTurn;

    public CardType cardType;
    public int povertyStackMultiplier;
}

public enum CardType
{
    Deal,
    House,
    Gamble,
    Poverty,
    Negotiation
}
