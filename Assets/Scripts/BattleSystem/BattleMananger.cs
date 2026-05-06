using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    [Header("Player")]
    public int playerMaxHP = 100;
    public int playerHP = 80;
    public int lux = 60;
    public string playerName = "ZERO";

    [Header("Rage")]
    public int rageThreshold = 80;
    public bool enemyRaged = false;
    public int rageBonusDamage = 5;
    public int rageAttackReduction = 2;

    [Header("Enemy Image")]
    public Image enemyCharacterImage;
    public Sprite enemyNormalSprite;
    public Sprite enemyRageSprite;
    public Sprite enemySprite100;
    public Sprite enemySprite50;
    public Sprite enemySprite20;

    [Header("Healing")]
    public int healAmount;

    [Header("Player Extra Stats")]
    public int houseTrust = 0;
    public int shield = 0;
    public int damageReduction = 0;
    public bool ignoreNextDamage = false;

    [Header("Enemy")]
    public string enemyName = "카림 하산";
    public int enemyMaxHP = 100;
    public int enemyHP = 100;
    public int enemyDamage = 8;

    [Header("Emotion")]
    public int enemyEmotion = 0;
    public int maxEmotion = 100;
    public int negotiationNeedEmotion = 80;

    [Header("Betting")]
    public int selectedBet = 0;
    public int predictedWinRate = 58;
    public int rewardLux = 0;

    [Header("Battle State")]
    public int turn = 1;
    private bool battleEnded = false;
    private bool battleStarted = false;

    [Header("Panels")]
    public GameObject bettingPanel;
    public GameObject battlePanel;
    public GameObject endPanel;

    [Header("UI - Betting")]
    public TMP_Text winRateText;
    public TMP_Text currentLuxText;
    public TMP_Text selectedBetText;
    public TMP_Text bettingLogText;

    [Header("UI - Player")]
    public TMP_Text playerHPText;
    public TMP_Text luxText;
    public TMP_Text luxStateText;
    public Slider luxBar;
    public TMP_Text shieldText;
    public TMP_Text playerNameText;
    public Slider playerHPBar;

    [Header("UI - Enemy")]
    public TMP_Text enemyNameText;
    public TMP_Text enemyHPText;
    public Slider enemyHPBar;

    [Header("UI - Emotion")]
    public TMP_Text emotionText;
    public Slider emotionBar;
    public Button negotiationButton;

    [Header("UI - Story")]
    public TMP_Text enemyDialogueText;

    [Header("UI - Battle")]
    public TMP_Text turnText;
    public TMP_Text battleLogText;
    public TMP_Text stackTagText;

    [Header("Card Prefab")]
    public GameObject cardPrefab;
    public Transform handPanel;
    public CardData[] startingCards;

    [Header("Deck")]
    public int drawCount = 4;
    public List<CardData> drawPile = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("Card Extra")]
    public int selfDamage;
    public int gambleSuccessChance = 50;
    public bool canOnlyUseInPoverty;
    public bool isGambleCard;

    private Coroutine handRoutine;
    public int povertyStack = 0;

    [Header("Turn Control")]
    public bool enemyStunned = false;
    public bool playerStunned = false;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipDescriptionText;
    public TMP_Text tooltipNameText;

    public bool reflectNextDamage = false;
    private bool zeroDamageThisTurn = false;
    private bool grantDoubleDamageNextTurn = false;
    private float currentTurnDamageMultiplier = 1f;
    private int luxDrainTurnsRemaining = 0;
    private bool reduceEnemyDamageNextTurn = false;
    private readonly HashSet<CardData> forcedGambleCards = new HashSet<CardData>();
    private int bleedStacks = 0;

    private enum LuxState
    {
        Poverty,
        Normal,
        Lucky,
        Overflow
    }

    private LuxState GetLuxState()
    {
        if (lux <= 25) return LuxState.Poverty;
        if (lux <= 60) return LuxState.Normal;
        if (lux <= 85) return LuxState.Lucky;
        return LuxState.Overflow;
    }

    public void ShowCardTooltip(CardData card)
    {
        if (card == null) return;

        tooltipPanel.SetActive(true);

        if (tooltipNameText != null)
        {
            tooltipNameText.text = card.cardName;
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.text = card.description;
        }
    }

    public void HideCardTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void Start()
    {
        battleEnded = false;
        battleStarted = false;

        if (bettingPanel != null) bettingPanel.SetActive(true);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (negotiationButton != null) negotiationButton.gameObject.SetActive(false);

        UpdateBettingUI();
        UpdateUI();
        UpdateEnemyDialogue();
        UpdateEnemySprite();
        SetupDeck();
        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());
    }

    private void UpdateEnemySprite()
{
    if (enemyCharacterImage == null) return;

    float hpRate = (float)enemyHP / enemyMaxHP;

    if (hpRate <= 0.2f)
    {
        enemyCharacterImage.sprite = enemySprite20;
    }
    else if (hpRate <= 0.5f)
    {
        enemyCharacterImage.sprite = enemySprite50;
    }
    else
    {
        enemyCharacterImage.sprite = enemySprite100;
    }
}


    private void RefreshHandUI()
{
    if (handRoutine != null)
    {
        StopCoroutine(handRoutine);
    }

    handRoutine = StartCoroutine(RefreshHandUIRoutine());
}

    private IEnumerator CreateStartingHandRoutine()
{
    foreach (Transform child in handPanel)
    {
        Destroy(child.gameObject);
    }

    foreach (CardData card in startingCards)
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel);
        newCard.name = card.cardName;

        CardButton cardButton = newCard.GetComponent<CardButton>();
        cardButton.cardData = card;
        cardButton.battleManager = this;
        cardButton.RefreshCardUI();

        yield return new WaitForSeconds(0.15f);
    }
}

    public void SelectBet(int amount)
    {
        if (amount > lux)
        {
            bettingLogText.text = "보유 LUX보다 많이 베팅할 수 없습니다.";
            return;
        }

        selectedBet = amount;
        rewardLux = CalculateRewardLux(amount);

        bettingLogText.text = $"{amount} LUX를 베팅했습니다. 승리 시 보상 +{rewardLux} LUX";
        UpdateBettingUI();
    }

    public void SelectAllIn()
{
    if (lux <= 0)
    {
        bettingLogText.text = "베팅할 LUX가 없습니다.";
        return;
    }

    selectedBet = lux;
    rewardLux = CalculateRewardLux(selectedBet);

    bettingLogText.text = $"올인! {selectedBet} LUX를 전부 베팅했습니다. 승리 시 보상 +{rewardLux} LUX";
    UpdateBettingUI();
}

    public void StartBattleAfterBet()
    {
        if (selectedBet <= 0)
        {
            bettingLogText.text = "먼저 베팅할 LUX를 선택하세요.";
            return;
        }

        battleStarted = true;
        lux -= selectedBet;
        lux = Mathf.Clamp(lux, 0, 100);

        if (bettingPanel != null) bettingPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(true);

        hand.Clear();
        DrawCards(drawCount);
        RefreshHandUI();

        WriteLog($"전투 시작. {selectedBet} LUX가 베팅되었습니다.");
        UpdateUI();
    }

private int CalculateRewardLux(int bet)
{
    float multiplier;

    if (predictedWinRate >= 70)
    {
        multiplier = 0.5f;
    }
    else if (predictedWinRate >= 50)
    {
        multiplier = 1f;
    }
    else
    {
        multiplier = 1.5f;
    }

    if (bet == lux)
    {
        multiplier += 0.5f;
    }

    return Mathf.RoundToInt(bet * multiplier);
}

    private CardType GetEffectiveCardType(CardData card)
    {
        return forcedGambleCards.Contains(card) ? CardType.Gamble : card.cardType;
    }

    private bool IsCardTreatedAsGamble(CardData card)
    {
        return card.isGambleCard || forcedGambleCards.Contains(card);
    }

    private void AddBleedStack(int amount, string reason)
    {
        bleedStacks += amount;
        bleedStacks = Mathf.Max(bleedStacks, 0);
        WriteLog($"{reason} <color=red>출혈 +{amount}</color> (현재 {bleedStacks}스택)");
    }

    private void ClearBleedStacks(string reason)
    {
        if (bleedStacks <= 0) return;
        bleedStacks = 0;
        WriteLog($"{reason} <color=#8fd3ff>출혈이 초기화</color>되었습니다.");
    }

    private bool CanUseCard(CardData card)
{
    if (card == null)
    {
        WriteLog("카드 데이터가 없습니다.");
        return false;
    }

    if (lux < card.luxCost)
    {
        WriteLog($"{card.cardName} 사용 불가. LUX가 부족합니다. 필요 LUX: {card.luxCost}, 현재 LUX: {lux}");
        return false;
    }

    CardType effectiveType = GetEffectiveCardType(card);

    if (effectiveType == CardType.House)
    {
        LuxState state = GetLuxState();

        if (state != LuxState.Lucky && state != LuxState.Overflow)
        {
            WriteLog($"{card.cardName} 사용 불가. 하우스 카드는 행운/폭주 구간에서만 사용할 수 있습니다.");
            return false;
        }
    }

    if (effectiveType == CardType.Poverty)
    {
        if (GetLuxState() != LuxState.Poverty)
        {
            WriteLog($"{card.cardName} 사용 불가. 빈곤 카드는 불운 구간에서만 사용할 수 있습니다.");
            return false;
        }
    }

    return true;
}

    public void UseCard(CardData card)
    {
        if (!battleStarted) return;
        if (battleEnded) return;
        if (playerStunned)
        {
            WriteLog("제로는 행동 불가 상태입니다. 이번 턴 카드를 사용할 수 없습니다.");
            return;
        }

        if (!CanUseCard(card))
        {
            return;
        }

        lux -= card.luxCost;
        lux += card.luxGain;
        lux = Mathf.Clamp(lux, 0, 100);

        if (card.reflectNextEnemyDamage)
        {
            reflectNextDamage = true;
        }

        ApplySpecialCardEffect(card);

        // 체력 회복
        if (card.healAmount > 0)
        {
            playerHP += card.healAmount;
            playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

            WriteLog($"{card.cardName} 사용 → HP {card.healAmount} 회복");
            ClearBleedStacks($"{card.cardName} 효과로");
        }

        int finalDamage = CalculateDamage(card);
        CardType effectiveType = GetEffectiveCardType(card);
        if (bleedStacks > 0 && (effectiveType == CardType.Deal || effectiveType == CardType.Gamble))
        {
            finalDamage += bleedStacks * 2;
        }
        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        // 분노 상태일 때 플레이어의 공격력 감소
        if (enemyRaged && finalDamage > 0)
        {
            finalDamage -= rageAttackReduction;
            finalDamage = Mathf.Max(finalDamage, 0);
        }

        enemyHP -= finalDamage;
        enemyHP = Mathf.Clamp(enemyHP, 0, enemyMaxHP);
        CheckEnemyRage();

        enemyEmotion += card.emotionGain;
        if (!IsCardTreatedAsGamble(card) && card.selfDamage > 0)
        {
            int beforeHp = playerHP;
            playerHP -= card.selfDamage;
            playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);
            if (playerHP < beforeHp) AddBleedStack(1, $"{card.cardName} 자해로");
        }
        enemyEmotion = Mathf.Clamp(enemyEmotion, 0, maxEmotion);

        if (card.rerollOtherCards)
        {
            RerollOtherCards(card);
            return;
        }

        // 하우스 신뢰도
        houseTrust += card.houseTrustChange;

        // 쉴드
        if (card.shieldAmount > 0)
        {
            shield += card.shieldAmount;
        }

        // 데미지 경감
        if (card.damageReduction > 0)
        {
            damageReduction += card.damageReduction;
        }

        // 다음 공격 무시
        if (card.ignoreEnemyDamage)
        {
            ignoreNextDamage = true;
        }

        if (card.stunEnemyNextTurn)
        {
            enemyStunned = true;
        }

        if (card.stunPlayerNextTurn)
        {
            playerStunned = true;
        }

        string logMessage = $"<color=yellow>{card.cardName}</color> 사용!";

        if (finalDamage > 0)
        {
            logMessage += $" 적에게 {finalDamage} 피해.";
        }

        if (card.emotionGain > 0)
        {
            logMessage += $" 감정 게이지 +{card.emotionGain}.";
        }

        logMessage += $" 현재 LUX: {lux}";

        WriteLog(logMessage);


        if (enemyHP <= 0)
        {
            WinBattle();
            return;
        }

        hand.Remove(card);
        forcedGambleCards.Remove(card);
        bool isOneTimeCard = card.specialEffect == SpecialCardEffect.No23;
        if (!isOneTimeCard)
        {
            discardPile.Add(card);
        }
        StartCoroutine(RefreshHandUIRoutine());

        UpdateUI();
        UpdateEnemyDialogue();
    }

    private int CalculateDamage(CardData card)
    {
        int specialDamage = ResolveSpecialCardDamage(card);
        if (specialDamage >= 0)
        {
            return specialDamage;
        }

        int damage = card.damage;

        LuxState state = GetLuxState();

        if (card.canOnlyUseInPoverty)
        {
            damage = povertyStack * card.povertyStackMultiplier;

            povertyStack = 0;

            return damage;
        }

        if (IsCardTreatedAsGamble(card))
        {
            int chance = card.gambleSuccessChance;

            if (state == LuxState.Lucky)
            {
                chance += 10;
            }
            else if (state == LuxState.Poverty)
            {
                chance -= 10;
            }

            chance = Mathf.Clamp(chance, 5, 95);

            int roll = Random.Range(1, 101);

            if (roll > chance)
            {
                int beforeHp = playerHP;
                playerHP -= card.selfDamage;
                playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);
                if (playerHP < beforeHp) AddBleedStack(1, $"{card.cardName} 도박 실패로");

                WriteLog($"<color=red>{card.cardName} 실패!</color> ({roll}/{chance}) 제로가 {card.selfDamage} 피해를 받았습니다.");
                return 0;
            }

            WriteLog($"<color=yellow>{card.cardName} 성공!</color> ({roll}/{chance})");
        }

        if (damage <= 0)
        {
            return 0;
        }

        if (state == LuxState.Poverty && GetEffectiveCardType(card) == CardType.Deal)
        {
            if (Random.value < 0.2f)
            {
                int beforeHp = playerHP;
                playerHP -= damage;
                playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);
                if (playerHP < beforeHp) AddBleedStack(1, "불운 역효과로");

                WriteLog("<color=red>불운 발동!</color> 공격이 역효과로 돌아왔습니다.");
                return 0;
            }
        }

        if (state == LuxState.Lucky)
        {
            damage += 4;
        }

        if (state == LuxState.Overflow)
        {
            damage *= 2;
            WriteLog("<color=cyan>폭주 상태!</color> 피해가 2배가 됩니다.");
        }

        return damage;
    }

    private void ApplySpecialCardEffect(CardData card)
    {
        switch (card.specialEffect)
        {
            case SpecialCardEffect.LightBilling:
                zeroDamageThisTurn = true;
                grantDoubleDamageNextTurn = true;
                WriteLog("<color=#8fd3ff>빛 청구:</color> 이번 턴 피해 0, 다음 턴 피해 +80%.");
                break;
            case SpecialCardEffect.WeaponDiscard:
                DrawCards(hand.Count + 1);
                WriteLog("<color=#8fd3ff>무기 버리기:</color> 카드 1장을 추가로 뽑았습니다.");
                break;
            case SpecialCardEffect.BeastHeart:
                int beforeHp = playerHP;
                playerHP = 1;
                if (playerHP < beforeHp) AddBleedStack(1, "야수의 심장 대가로");
                forcedGambleCards.Clear();
                foreach (CardData handCard in hand)
                {
                    if (handCard != card)
                    {
                        forcedGambleCards.Add(handCard);
                    }
                }
                WriteLog("<color=#8fd3ff>야수의 심장:</color> HP를 1로 만들고 손패를 전부 도박 카드로 전환.");
                break;
            case SpecialCardEffect.LuxDrain:
                luxDrainTurnsRemaining = 3;
                WriteLog("<color=#8fd3ff>럭스 드레인:</color> 3턴 동안 매턴 LUX +2.");
                break;
            case SpecialCardEffect.MissTrigger:
                reduceEnemyDamageNextTurn = true;
                WriteLog("<color=#8fd3ff>미스 트리거:</color> 다음 턴 적 피해 70% 감소.");
                break;
            case SpecialCardEffect.No23:
                WriteLog("<color=#8fd3ff>No.23:</color> 사용 후 소멸합니다.");
                break;
        }
    }

    private int ResolveSpecialCardDamage(CardData card)
    {
        switch (card.specialEffect)
        {
            case SpecialCardEffect.CoinTriple:
            {
                int heads = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (Random.value < 0.5f) heads++;
                }

                bool success = heads >= 2;
                if (success)
                {
                    WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 성공");
                    return 40;
                }

                playerHP -= 20;
                playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);
                AddBleedStack(1, "코인 실패 반동으로");
                WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 실패, 제로가 20 피해를 받았습니다.");
                return 0;
            }
            case SpecialCardEffect.DoubleDice:
            {
                int d1 = Random.Range(1, 7);
                int d2 = Random.Range(1, 7);
                int total = d1 + d2;
                bool success = total >= 8;
                WriteLog($"<color=#ffd166>주사위:</color> {d1} + {d2} = {total} {(success ? "성공" : "실패")}");
                return success ? 25 : 0;
            }
            default:
                return -1;
        }
    }

    private int ApplyTurnDamageModifiers(int damage)
    {
        if (damage <= 0) return 0;

        if (zeroDamageThisTurn)
        {
            return 0;
        }

        if (currentTurnDamageMultiplier > 1f)
        {
            damage = Mathf.RoundToInt(damage * currentTurnDamageMultiplier);
        }

        return damage;
    }

    public void EndTurn()
    {
        if (!battleStarted) return;
        if (battleEnded) return;

        string resultLog;

        if (enemyStunned)
        {
            resultLog = "적은 행동 불가 상태입니다. 이번 턴 아무 행동도 하지 못했습니다.";
            enemyStunned = false;
        }
        else
        {
            int finalDamage = enemyDamage;
            
            // 분노 상태일 때 추가 피해
            if (enemyRaged)
            {
                finalDamage += rageBonusDamage;
            }

            if (reduceEnemyDamageNextTurn)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * 0.3f);
                reduceEnemyDamageNextTurn = false;
            }

            // 공격 무시
            if (ignoreNextDamage)
            {
                resultLog = "공격을 완전히 무시했습니다.";
                ignoreNextDamage = false;
            }
            else
            {
                // 데미지 감소
                finalDamage -= damageReduction;
                finalDamage = Mathf.Max(finalDamage, 0);

                // 쉴드 먼저 적용
                if (shield > 0)
                {
                    int absorbed = Mathf.Min(shield, finalDamage);
                    shield -= absorbed;
                    finalDamage -= absorbed;
                }

                playerHP -= finalDamage;
                playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

                resultLog = $"적의 공격 발동. 제로가 {finalDamage} 피해를 받았습니다.";

                if (reflectNextDamage && finalDamage > 0)
                {
                    enemyHP -= finalDamage;
                    enemyHP = Mathf.Clamp(enemyHP, 0, enemyMaxHP);

                    resultLog += $"\n반사 발동! 적에게 {finalDamage} 피해를 되돌렸습니다.";

                    reflectNextDamage = false;
                }
            }
        }

        WriteLog(resultLog);

        if (playerStunned)
        {
            WriteLog("제로는 행동 불가 상태입니다. 이번 턴 아무 행동도 할 수 없습니다.");
            playerStunned = false;
        }

        turn++;

        if (bleedStacks > 0)
        {
            playerHP -= bleedStacks;
            playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);
            WriteLog($"<color=red>출혈</color>로 턴 종료 시 {bleedStacks} 피해를 받았습니다.");
        }

        if (GetLuxState() == LuxState.Poverty)
        {
            povertyStack++;
        }
        else
        {
            povertyStack = 0;
        }

        if (GetLuxState() == LuxState.Overflow)
        {
            lux -= 30;
            lux = Mathf.Clamp(lux, 0, 100);

            WriteLog("<color=cyan>폭주 반동:</color> 턴 종료 시 LUX -30.");
        }

        if (luxDrainTurnsRemaining > 0)
        {
            lux += 2;
            lux = Mathf.Clamp(lux, 0, 100);
            luxDrainTurnsRemaining--;
            WriteLog("<color=#8fd3ff>럭스 드레인:</color> LUX +2");
        }

        discardPile.AddRange(hand);
        forcedGambleCards.Clear();
        hand.Clear();

        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());

        if (playerHP <= 0)
        {
            LoseBattle();
            return;
        }

        if (playerStunned)
        {
            playerStunned = false;
        }

        // 턴이 넘어가며 카드 기반 공격 버프/디버프를 정리
        zeroDamageThisTurn = false;
        if (grantDoubleDamageNextTurn)
        {
            currentTurnDamageMultiplier = 1.8f;
            grantDoubleDamageNextTurn = false;
        }
        else
        {
            currentTurnDamageMultiplier = 1f;
        }

        UpdateUI();
        UpdateEnemyDialogue();

        damageReduction = 0;
    }

    public void NegotiateEndBattle()
    {
        if (!battleStarted) return;
        if (battleEnded) return;

        if (enemyEmotion < negotiationNeedEmotion)
        {
            WriteLog("아직 협상할 수 없습니다.");
            return;
        }

        battleEnded = true;

        int negotiationReward = Mathf.RoundToInt(rewardLux * 0.5f);
        lux += Mathf.RoundToInt(selectedBet * 0.5f) + negotiationReward;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"협상으로 전투를 종료했습니다. 보상 +{negotiationReward} LUX. THE HOUSE 신뢰도 하락.");

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }

        UpdateUI();
    }

    private void WinBattle()
    {
        battleEnded = true;

        lux += selectedBet + rewardLux;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"표적 제압 완료. 베팅 성공! 보상 +{rewardLux} LUX. 현재 LUX: {lux}");

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }

        UpdateUI();
        UpdateEnemyDialogue();
    }

    private void LoseBattle()
    {
        battleEnded = true;

        int extraLoss = selectedBet;
        lux -= extraLoss;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"제로가 쓰러졌습니다. 베팅 실패. 추가 손실 -{extraLoss} LUX. 현재 LUX: {lux}");

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }

        UpdateUI();
    }

    private void UpdateBettingUI()
    {
        if (winRateText != null)
        {
            winRateText.text = $"예측 승률 {predictedWinRate}%";
        }

        if (currentLuxText != null)
        {
            currentLuxText.text = $"보유 LUX {lux}";
        }

        if (selectedBetText != null)
        {
            if (selectedBet <= 0)
            {
                selectedBetText.text = "선택한 베팅 없음";
            }
            else
            {
                selectedBetText.text = $"베팅 {selectedBet} LUX / 승리 보상 +{rewardLux}";
            }
        }
    }

    private void SetupDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        forcedGambleCards.Clear();

        drawPile = startingCards.ToList();

        Shuffle(drawPile);
    }

private bool CanDrawCardInCurrentLuxState(CardData card)
{
    LuxState state = GetLuxState();

    if (card.cardType == CardType.House)
    {
        return state == LuxState.Lucky || state == LuxState.Overflow;
    }

    if (card.cardType == CardType.Poverty)
    {
        return state == LuxState.Poverty;
    }

    return true;
}

private void DrawCards(int count)
{
    int safety = 0;

    while (hand.Count < count && safety < 100)
    {
        safety++;

        if (drawPile.Count <= 0)
        {
            ReshuffleDiscardIntoDeck();
        }

        if (drawPile.Count <= 0)
        {
            return;
        }

        CardData card = drawPile[0];
        drawPile.RemoveAt(0);

        // 잭팟 카드 확률 제한 
        
        if (card.isJackpot)
        {
            if (Random.value > 0.3f)
            {
                discardPile.Add(card);
                continue;
            }
        }

        if (CanDrawCardInCurrentLuxState(card))
        {
            hand.Add(card);
        }
        else
        {
            discardPile.Add(card);
        }

    }
}

private void ReshuffleDiscardIntoDeck()
{
    if (discardPile.Count <= 0)
    {
        return;
    }

    drawPile.AddRange(discardPile);
    discardPile.Clear();
    Shuffle(drawPile);

}

private void Shuffle(List<CardData> list)
{
    for (int i = 0; i < list.Count; i++)
    {
        CardData temp = list[i];
        int randomIndex = Random.Range(i, list.Count);
        list[i] = list[randomIndex];
        list[randomIndex] = temp;
    }
}

private IEnumerator RefreshHandUIRoutine()
{
    foreach (Transform child in handPanel)
    {
        Destroy(child.gameObject);
    }
    List<CardData> handSnapshot = new List<CardData>(hand);

    foreach (CardData card in handSnapshot)
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel);
        newCard.name = card.cardName;

        CardButton cardButton = newCard.GetComponent<CardButton>();
        cardButton.cardData = card;
        cardButton.battleManager = this;
        cardButton.RefreshCardUI();

        yield return new WaitForSeconds(0.15f);
    }

}

private void RerollOtherCards(CardData usedCard)
{
    hand.Remove(usedCard);
    forcedGambleCards.Remove(usedCard);
    discardPile.Add(usedCard);

    foreach (CardData card in hand)
    {
        forcedGambleCards.Remove(card);
        discardPile.Add(card);
    }

    hand.Clear();

    DrawCards(drawCount);
    RefreshHandUI();

    WriteLog($"{usedCard.cardName} 사용. 나머지 카드들을 전부 리롤했습니다.");
}

private void CheckEnemyRage()
{
    if (enemyEmotion >= rageThreshold)
    {
        enemyRaged = true;
    }
    else
    {
        enemyRaged = false;
    }

    if (enemyCharacterImage != null)
    {
        enemyCharacterImage.sprite = enemyRaged ? enemyRageSprite : enemyNormalSprite;
    }
}

    private void UpdateUI()
    {
        if (playerHPText != null)
        {
            playerHPText.text = $"HP {playerHP}/{playerMaxHP}";
        }

        if (shieldText != null)
        {
            shieldText.gameObject.SetActive(shield > 0);
            shieldText.text = $"SHIELD {shield}";
        }

        // 이름
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // HP Bar
        if (playerHPBar != null)
        {
            playerHPBar.maxValue = playerMaxHP;
            playerHPBar.value = playerHP;
        }


        if (luxText != null)
        {
            luxText.text = $"LUX {lux}/100";
        }

        if (luxBar != null)
        {
            luxBar.value = lux / 100f;
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
        }

        if (enemyHPText != null)
        {
            enemyHPText.text = $"HP {enemyHP}/{enemyMaxHP}";
        }

        if (enemyHPBar != null)
        {
            enemyHPBar.value = (float)enemyHP / enemyMaxHP;
        }

        if (emotionText != null)
        {
            emotionText.text = enemyRaged ? $"분노 {enemyEmotion}/{maxEmotion} - 분노 상태" : $"분노 {enemyEmotion}/{maxEmotion}";
        }

        if (emotionBar != null)
        {
            emotionBar.value = (float)enemyEmotion / maxEmotion;
        }

        if (turnText != null)
        {
            turnText.text = $"턴 {turn}";
        }

        if (stackTagText != null)
        {
            stackTagText.text = bleedStacks > 0 ? $"태그: 출혈 x{bleedStacks}" : "태그: 없음";
        }

        UpdateLuxState();
        //UpdateNegotiationButton();
        UpdateBettingUI();
        CheckEnemyRage();
    }

    private void UpdateLuxState()
    {
        if (luxStateText == null) return;

        if (lux <= 25)
        {
            luxStateText.text = "상태: 불운";
        }
        else if (lux <= 60)
        {
            luxStateText.text = "상태: 보통";
        }
        else if (lux <= 85)
        {
            luxStateText.text = "상태: 행운";
        }
        else
        {
            luxStateText.text = "상태: 폭주";
        }
    }

    /* private void UpdateNegotiationButton()
    {
        if (negotiationButton == null) return;

        if (enemyEmotion >= negotiationNeedEmotion && !battleEnded)
        {
            negotiationButton.gameObject.SetActive(true);
        }
        else
        {
            negotiationButton.gameObject.SetActive(false);
        }
    }*/

    private void UpdateEnemyDialogue()
    {
        if (enemyDialogueText == null) return;

        float hpRate = (float)enemyHP / enemyMaxHP;

        if (enemyHP <= 0)
        {
            enemyDialogueText.text = "카림은 쓰러졌지만, 그녀의 기록 장치는 아직 깜빡이고 있다.";
        }
        else if (hpRate <= 0.15f)
        {
            enemyDialogueText.text = "하하… 하하하… 재미있네요. 같은 피해자끼리 비극을 만들다니.";
        }
        else if (hpRate <= 0.30f)
        {
            enemyDialogueText.text = "“당신이 날 죽여도 영상은 업로드될 겁니다. 무얼 위해서 날 처리하려는 거죠?”";
        }
        else if (hpRate <= 0.45f)
        {
            enemyDialogueText.text = "“당신도 같은 피해자입니다. 안타깝게도.”";
        }
        else if (hpRate <= 0.60f)
        {
            enemyDialogueText.text = "“당신도 저 벽에 있어요. 23번.”";
        }
        else if (hpRate <= 0.80f)
        {
            enemyDialogueText.text = "“내 아내는 LUX를 팔고 죽었습니다. 당신들 때문에.”";
        }
        else
        {
            enemyDialogueText.text = "“당신 같은 사람이 올 줄 알았습니다. 난 이미 각오했습니다.”";
        }
    }

    private void WriteLog(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text = message;
        }
    }
}