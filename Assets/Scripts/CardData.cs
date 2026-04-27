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

    [Header("Advanced Stats")]
    public int houseTrustChange;
    public bool ignoreEnemyDamage;
    public int shieldAmount;
    public int damageReduction;
    public int healAmount;

    [Header("Reflect")]
    public bool reflectNextEnemyDamage;

    [Header("Reroll")]
    public bool rerollOtherCards;
}

public enum CardType
{
    Deal,
    House,
    Gamble,
    Poverty,
    Negotiation,
    Manipulation,
    Flux,
    Barrier
}
