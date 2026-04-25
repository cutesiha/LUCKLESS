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

    [Header("Reverse Betting")]
    public int enemyActionChance = 70;
    public int reverseBetCost = 10;
    public int reverseBetReduceAmount = 25;
    public int minEnemyActionChance = 10;
    public bool usedReverseBetThisTurn = false;

    [Header("UI - Player")]
    public TMP_Text playerHPText;
    public TMP_Text luxText;
    public TMP_Text luxStateText;
    public Slider luxBar;

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

    public TMP_Text enemyActionChanceText;
    public Button reverseBetButton;

    [Header("Card Prefab")]
    public GameObject cardPrefab;
    public Transform handPanel;
    public CardData[] startingCards;

    [Header("Deck")]
    public int drawCount = 3;
    public List<CardData> drawPile = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();


    private void Start()
    {
        battleEnded = false;
        battleStarted = false;

        if (bettingPanel != null) bettingPanel.SetActive(true);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (negotiationButton != null) negotiationButton.gameObject.SetActive(false);
        if (reverseBetButton != null) reverseBetButton.interactable = false;

        UpdateBettingUI();
        UpdateUI();
        UpdateEnemyDialogue();
        SetupDeck();
        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());
    }

    private void RefreshHandUI()
{
    StopAllCoroutines();
    StartCoroutine(RefreshHandUIRoutine());
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

        if (bettingPanel != null) bettingPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(true);

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

    public void UseCard(CardData card)
    {
        if (!battleStarted) return;
        if (battleEnded) return;

        if (lux < card.luxCost)
        {
            WriteLog("LUX가 부족합니다.");
            return;
        }

        if (card.cardType == CardType.Poverty && lux > 25)
        {
            WriteLog("빈곤 카드는 LUX가 25 이하일 때만 사용할 수 있습니다.");
            return;
        }

        lux -= card.luxCost;
        lux += card.luxGain;
        lux = Mathf.Clamp(lux, 0, 100);

        int finalDamage = CalculateDamage(card);

        enemyHP -= finalDamage;
        enemyHP = Mathf.Clamp(enemyHP, 0, enemyMaxHP);

        enemyEmotion += card.emotionGain;
        enemyEmotion = Mathf.Clamp(enemyEmotion, 0, maxEmotion);

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
        discardPile.Add(card);
        StartCoroutine(RefreshHandUIRoutine());

        UpdateUI();
        UpdateEnemyDialogue();
    }

    private int CalculateDamage(CardData card)
    {
        int damage = card.damage;

        if (damage <= 0)
        {
            return 0;
        }

        if (lux <= 25 && card.cardType == CardType.Deal)
        {
            if (Random.value < 0.2f)
            {
                playerHP -= damage;
                playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

                WriteLog("<color=red>불운 발동!</color> 공격이 역효과로 돌아왔습니다.");
                return 0;
            }
        }

        if (lux >= 61 && lux <= 85)
        {
            damage += 4;
        }

        if (lux >= 86)
        {
            damage *= 2;
            lux -= 30;
            lux = Mathf.Clamp(lux, 0, 100);

            WriteLog("<color=cyan>폭주 상태 발동!</color> 카드 효과가 강화되었습니다.");
        }

        return damage;
    }

    public void ReverseBet()
{
    if (!battleStarted) return;
    if (battleEnded) return;

    if (usedReverseBetThisTurn)
    {
        WriteLog("역베팅은 한 턴에 한 번만 사용할 수 있습니다.");
        return;
    }

    if (lux < reverseBetCost)
    {
        WriteLog("역베팅할 LUX가 부족합니다.");
        return;
    }

    lux -= reverseBetCost;
    lux = Mathf.Clamp(lux, 0, 100);

    enemyActionChance -= reverseBetReduceAmount;
    enemyActionChance = Mathf.Clamp(enemyActionChance, minEnemyActionChance, 100);

    usedReverseBetThisTurn = true;

    WriteLog($"역베팅 실행. LUX -{reverseBetCost}. 적 공격 확률이 {enemyActionChance}%로 감소했습니다.");

    UpdateUI();
}

    public void EndTurn()
    {
        if (!battleStarted) return;
        if (battleEnded) return;

        int roll = Random.Range(1, 101);

        if (roll <= enemyActionChance)
        {
            playerHP -= enemyDamage;
            playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

            WriteLog($"적의 공격 발동. 제로가 {enemyDamage} 피해를 받았습니다. ({roll}/{enemyActionChance})");
        }
        else
        {
            WriteLog($"역베팅 성공. 적의 공격이 빗나갔습니다. ({roll}/{enemyActionChance})");
        }

        turn++;

        enemyActionChance = 70;
        usedReverseBetThisTurn = false;

        discardPile.AddRange(hand);
        hand.Clear();

        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());

        if (playerHP <= 0)
        {
            LoseBattle();
            return;
        }

        UpdateUI();
        UpdateEnemyDialogue();
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
        lux += negotiationReward;
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

        lux += rewardLux;
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

        int loss = selectedBet * 2;
        lux -= loss;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"제로가 쓰러졌습니다. 베팅 실패. LUX -{loss}. 현재 LUX: {lux}");

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

    drawPile = startingCards.ToList();

    Shuffle(drawPile);
}

private void DrawCards(int count)
{
    for (int i = 0; i < count; i++)
    {
        if (drawPile.Count <= 0)
        {
            ReshuffleDiscardIntoDeck();
        }

        if (drawPile.Count <= 0)
        {
            return;
        }

        CardData drawnCard = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(drawnCard);
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

    WriteLog("버림더미를 섞어 새 덱을 만들었습니다.");
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

    foreach (CardData card in hand)
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

    private void UpdateUI()
    {
        if (playerHPText != null)
        {
            playerHPText.text = $"HP {playerHP}/{playerMaxHP}";
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
            emotionText.text = $"감정 {enemyEmotion}/{maxEmotion}";
        }

        if (emotionBar != null)
        {
            emotionBar.value = (float)enemyEmotion / maxEmotion;
        }

        if (turnText != null)
        {
            turnText.text = $"턴 {turn}";
        }

        if (enemyActionChanceText != null)
        {
            enemyActionChanceText.text = $"적 공격 확률: {enemyActionChance}%";
        }

        if (reverseBetButton != null)
        {
            reverseBetButton.interactable = battleStarted && !battleEnded && !usedReverseBetThisTurn && lux >= reverseBetCost;
        }

        UpdateLuxState();
        UpdateNegotiationButton();
        UpdateBettingUI();
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

    private void UpdateNegotiationButton()
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
    }

    private void UpdateEnemyDialogue()
    {
        if (enemyDialogueText == null) return;

        float hpRate = (float)enemyHP / enemyMaxHP;

        if (enemyHP <= 0)
        {
            enemyDialogueText.text = "카림은 쓰러졌지만, 그의 기록 장치는 아직 깜빡이고 있다.";
        }
        else if (hpRate <= 0.25f)
        {
            enemyDialogueText.text = "“부탁입니다. 영상만 올리게 해주세요. 그게 전부입니다.”";
        }
        else if (hpRate <= 0.5f)
        {
            enemyDialogueText.text = "“당신도 23번이잖아요. 당신도 피해자잖아요.”";
        }
        else if (hpRate <= 0.75f)
        {
            enemyDialogueText.text = "“내 아내는 LUX를 팔고 죽었습니다. 당신들 때문에.”";
        }
        else
        {
            enemyDialogueText.text = "“당신 같은 사람이 올 줄 알았습니다. 이미 각오했습니다.”";
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