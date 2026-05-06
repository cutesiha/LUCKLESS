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

    [Header("Enemy Hit Effect")]
    public float enemyHitEffectDuration = 0.35f;
    public float enemyHitShakeStrength = 12f;
    public Color enemyHitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Player Hit Effect")]
    public Image playerHitFlashImage;
    public RectTransform playerHitShakeTarget;
    public float playerHitEffectDuration = 0.35f;
    public float playerHitShakeStrength = 10f;
    public Color playerHitFlashColor = new Color(1f, 0f, 0f, 0.45f);

    [Header("Dice Roll Effect")]
    public GameObject diceRollPanel;
    public Image diceLeftImage;
    public Image diceRightImage;
    public TMP_Text diceTotalText;
    public Sprite[] diceFaceSprites = new Sprite[6];
    public float diceRollDuration = 1.25f;
    public float diceSecondDelay = 0.45f;
    public float diceResultHoldDuration = 1.3f;
    public float diceFaceChangeInterval = 0.05f;

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
    private int addictionStacks = 0;   // 중독
    private int debtStacks = 0;        // 채무
    private int excitementStacks = 0;  // 흥분
    private int greedStacks = 0;       // 탐욕
    private int resignationStacks = 0; // 체념
    private bool gambleResolvedThisUse = false;
    private bool gambleSucceededThisUse = false;
    private bool usedCardThisTurn = false;
    private int illegalLoanTurnsRemaining = 0;
    private bool illegalLoanPenaltyPending = false;
    private int probabilityManipulationTurnsRemaining = 0;
    private int overloadTurnsRemaining = 0;
    private int heartbeatStacks = 0;
    private bool heartbeatEnabled = false;
    private int failedGambleCountThisTurn = 0;
    private bool firstGambleGuaranteedThisTurn = false;
    private bool firstGambleUsedThisTurn = false;
    private Coroutine enemyHitEffectRoutine;
    private bool enemyHitEffectActive = false;
    private Vector2 enemyHitOriginalPosition;
    private Color enemyHitOriginalColor = Color.white;
    private Coroutine playerHitEffectRoutine;
    private Vector2 playerHitOriginalPosition;
    private bool isCardResolving = false;
    private readonly List<FailedGambleRecord> failedGambleRecordsThisTurn = new List<FailedGambleRecord>();

    private struct FailedGambleRecord
    {
        public int chance;
        public int successDamage;
    }

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

        InitializePlayerHitEffect();
        InitializeDiceRollEffect();
        UpdateBettingUI();
        UpdateUI();
        UpdateEnemyDialogue();
        UpdateEnemySprite();
        SetupDeck();
        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());
    }

    private Sprite GetEnemyCurrentSprite()
    {
        if (enemyRaged && enemyRageSprite != null)
        {
            return enemyRageSprite;
        }

        float hpRate = (float)enemyHP / enemyMaxHP;

        if (hpRate <= 0.4f && enemySprite20 != null)
        {
            return enemySprite20;
        }

        if (hpRate <= 0.7f && enemySprite50 != null)
        {
            return enemySprite50;
        }

        if (enemySprite100 != null)
        {
            return enemySprite100;
        }

        return enemyRaged ? enemyRageSprite : enemyNormalSprite;
    }

    private void UpdateEnemySprite()
    {
        if (enemyCharacterImage == null || enemyHitEffectActive) return;

        enemyCharacterImage.sprite = GetEnemyCurrentSprite();
    }

    private int ApplyEnemyDamage(int damage)
    {
        if (damage <= 0) return 0;

        int beforeHp = enemyHP;
        enemyHP -= damage;
        enemyHP = Mathf.Clamp(enemyHP, 0, enemyMaxHP);

        int actualDamage = beforeHp - enemyHP;
        if (actualDamage > 0)
        {
            PlayEnemyHitEffect();
        }

        return actualDamage;
    }

    private void PlayEnemyHitEffect()
    {
        if (enemyCharacterImage == null) return;

        if (enemyHitEffectRoutine != null)
        {
            StopCoroutine(enemyHitEffectRoutine);
            enemyCharacterImage.rectTransform.anchoredPosition = enemyHitOriginalPosition;
            enemyCharacterImage.color = enemyHitOriginalColor;
            enemyHitEffectActive = false;
        }

        enemyHitEffectRoutine = StartCoroutine(EnemyHitEffectRoutine());
    }

    private IEnumerator EnemyHitEffectRoutine()
    {
        enemyHitEffectActive = true;

        RectTransform rectTransform = enemyCharacterImage.rectTransform;
        enemyHitOriginalPosition = rectTransform.anchoredPosition;
        enemyHitOriginalColor = enemyCharacterImage.color;

        Sprite angrySprite = enemyRageSprite != null ? enemyRageSprite : GetEnemyCurrentSprite();
        enemyCharacterImage.sprite = angrySprite;

        float elapsed = 0f;
        while (elapsed < enemyHitEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enemyHitEffectDuration);
            float shake = enemyHitShakeStrength * (1f - t);

            rectTransform.anchoredPosition = enemyHitOriginalPosition + Random.insideUnitCircle * shake;
            enemyCharacterImage.color = Color.Lerp(enemyHitOriginalColor, enemyHitFlashColor, 1f - t);

            yield return null;
        }

        rectTransform.anchoredPosition = enemyHitOriginalPosition;
        enemyCharacterImage.color = enemyHitOriginalColor;
        enemyHitEffectActive = false;
        enemyHitEffectRoutine = null;
        UpdateEnemySprite();
    }

    private void InitializePlayerHitEffect()
    {
        if (playerHitShakeTarget == null && battlePanel != null)
        {
            playerHitShakeTarget = battlePanel.GetComponent<RectTransform>();
        }

        if (playerHitFlashImage == null)
        {
            Canvas canvas = null;
            if (battlePanel != null)
            {
                canvas = battlePanel.GetComponentInParent<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>(true);
            }

            if (canvas != null)
            {
                GameObject flashObject = new GameObject("PlayerHitFlash", typeof(RectTransform), typeof(Image));
                flashObject.transform.SetParent(canvas.transform, false);

                RectTransform flashTransform = flashObject.GetComponent<RectTransform>();
                flashTransform.anchorMin = Vector2.zero;
                flashTransform.anchorMax = Vector2.one;
                flashTransform.offsetMin = Vector2.zero;
                flashTransform.offsetMax = Vector2.zero;

                playerHitFlashImage = flashObject.GetComponent<Image>();
                playerHitFlashImage.raycastTarget = false;
            }
        }

        if (playerHitFlashImage != null)
        {
            playerHitFlashImage.color = Color.clear;
            playerHitFlashImage.transform.SetAsLastSibling();
        }
    }

    private int ApplyPlayerDamage(int damage)
    {
        if (damage <= 0) return 0;

        int beforeHp = playerHP;
        playerHP -= damage;
        playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

        int actualDamage = beforeHp - playerHP;
        if (actualDamage > 0)
        {
            PlayPlayerHitEffect();
        }

        return actualDamage;
    }

    private void PlayPlayerHitEffect()
    {
        if (playerHitFlashImage == null && playerHitShakeTarget == null) return;

        if (playerHitEffectRoutine != null)
        {
            StopCoroutine(playerHitEffectRoutine);
            RestorePlayerHitEffect();
        }

        playerHitEffectRoutine = StartCoroutine(PlayerHitEffectRoutine());
    }

    private IEnumerator PlayerHitEffectRoutine()
    {
        if (playerHitFlashImage != null)
        {
            playerHitFlashImage.transform.SetAsLastSibling();
        }

        playerHitOriginalPosition = playerHitShakeTarget != null ? playerHitShakeTarget.anchoredPosition : Vector2.zero;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, playerHitEffectDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float shake = playerHitShakeStrength * (1f - t);

            if (playerHitShakeTarget != null)
            {
                playerHitShakeTarget.anchoredPosition = playerHitOriginalPosition + Random.insideUnitCircle * shake;
            }

            if (playerHitFlashImage != null)
            {
                playerHitFlashImage.color = Color.Lerp(Color.clear, playerHitFlashColor, 1f - t);
            }

            yield return null;
        }

        RestorePlayerHitEffect();
        playerHitEffectRoutine = null;
    }

    private void RestorePlayerHitEffect()
    {
        if (playerHitShakeTarget != null)
        {
            playerHitShakeTarget.anchoredPosition = playerHitOriginalPosition;
        }

        if (playerHitFlashImage != null)
        {
            playerHitFlashImage.color = Color.clear;
        }
    }

    private void InitializeDiceRollEffect()
    {
        if (diceRollPanel == null)
        {
            Canvas canvas = null;
            if (battlePanel != null)
            {
                canvas = battlePanel.GetComponentInParent<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>(true);
            }

            if (canvas != null)
            {
                diceRollPanel = new GameObject("DiceRollPanel", typeof(RectTransform));
                diceRollPanel.transform.SetParent(canvas.transform, false);

                RectTransform panelTransform = diceRollPanel.GetComponent<RectTransform>();
                panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
                panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
                panelTransform.pivot = new Vector2(0.5f, 0.5f);
                panelTransform.sizeDelta = new Vector2(420f, 220f);
                panelTransform.anchoredPosition = Vector2.zero;

                diceLeftImage = CreateDiceImage("LeftDice", diceRollPanel.transform, new Vector2(-90f, 30f));
                diceRightImage = CreateDiceImage("RightDice", diceRollPanel.transform, new Vector2(90f, 30f));
                diceTotalText = CreateDiceTotalText(diceRollPanel.transform);
            }
        }

        if (diceRollPanel != null)
        {
            if (diceLeftImage == null)
            {
                diceLeftImage = CreateDiceImage("LeftDice", diceRollPanel.transform, new Vector2(-90f, 30f));
            }

            if (diceRightImage == null)
            {
                diceRightImage = CreateDiceImage("RightDice", diceRollPanel.transform, new Vector2(90f, 30f));
            }

            if (diceTotalText == null)
            {
                diceTotalText = CreateDiceTotalText(diceRollPanel.transform);
            }

            diceRollPanel.SetActive(false);
        }

        if (diceTotalText != null)
        {
            diceTotalText.text = string.Empty;
        }
    }

    private Image CreateDiceImage(string objectName, Transform parent, Vector2 anchoredPosition)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(96f, 96f);
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private TMP_Text CreateDiceTotalText(Transform parent)
    {
        GameObject textObject = new GameObject("DiceTotalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(360f, 60f);
        rectTransform.anchoredPosition = new Vector2(0f, -70f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private IEnumerator PlayDoubleDiceRollRoutine(int leftResult, int rightResult)
    {
        if (diceRollPanel == null || diceLeftImage == null || diceRightImage == null)
        {
            yield break;
        }

        diceRollPanel.SetActive(true);
        diceRollPanel.transform.SetAsLastSibling();

        if (diceTotalText != null)
        {
            diceTotalText.text = string.Empty;
        }

        float elapsed = 0f;
        float nextFaceChange = 0f;
        float rollDuration = Mathf.Max(0.01f, diceRollDuration);

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            nextFaceChange -= Time.deltaTime;
            if (nextFaceChange <= 0f)
            {
                SetDiceImage(diceLeftImage, Random.Range(1, 7));
                SetDiceImage(diceRightImage, Random.Range(1, 7));
                nextFaceChange = Mathf.Max(0.01f, diceFaceChangeInterval);
            }

            yield return null;
        }

        SetDiceImage(diceLeftImage, leftResult);

        elapsed = 0f;
        nextFaceChange = 0f;
        float secondDelay = Mathf.Max(0f, diceSecondDelay);
        while (elapsed < secondDelay)
        {
            elapsed += Time.deltaTime;
            nextFaceChange -= Time.deltaTime;
            if (nextFaceChange <= 0f)
            {
                SetDiceImage(diceRightImage, Random.Range(1, 7));
                nextFaceChange = Mathf.Max(0.01f, diceFaceChangeInterval);
            }

            yield return null;
        }

        SetDiceImage(diceRightImage, rightResult);

        if (diceTotalText != null)
        {
            diceTotalText.text = $"총합: {leftResult + rightResult}";
        }

        yield return new WaitForSeconds(diceResultHoldDuration);

        if (diceRollPanel != null)
        {
            diceRollPanel.SetActive(false);
        }
    }

    private void SetDiceImage(Image targetImage, int diceValue)
    {
        if (targetImage == null) return;
        if (diceFaceSprites == null || diceFaceSprites.Length < 6) return;

        int index = Mathf.Clamp(diceValue, 1, 6) - 1;
        if (diceFaceSprites[index] != null)
        {
            targetImage.sprite = diceFaceSprites[index];
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
        usedCardThisTurn = false;

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

    private int ModifyIncomingDamage(int damage)
    {
        int modified = damage + debtStacks - resignationStacks;
        return Mathf.Max(modified, 0);
    }

    private bool IsLuxTypeCard(CardData card)
    {
        return card.luxGain > 0 || card.specialEffect == SpecialCardEffect.LuxDrain;
    }

    private int CalculateLuxGain(CardData card)
    {
        if (card.luxGain <= 0) return card.luxGain;

        float percentBonus = 1f + (excitementStacks * 0.05f);
        int scaled = Mathf.RoundToInt(card.luxGain * percentBonus);
        return scaled + (greedStacks * 3);
    }

    private int GetEffectiveLuxCost(CardData card)
    {
        int cost = card.luxCost;
        if (overloadTurnsRemaining > 0)
        {
            cost -= 3;
        }
        return Mathf.Max(cost, 0);
    }

    private int ApplyOutgoingCardBonuses(int damage, CardType effectiveType)
    {
        if (damage <= 0) return 0;

        if (bleedStacks > 0 && (effectiveType == CardType.Deal || effectiveType == CardType.Gamble))
        {
            damage += bleedStacks * 2;
        }

        if (effectiveType == CardType.Gamble)
        {
            damage += addictionStacks * 2;
            damage += excitementStacks * 3;
            if (debtStacks >= 10)
            {
                damage = Mathf.RoundToInt(damage * 1.5f);
            }
        }

        if (effectiveType == CardType.Deal && greedStacks > 0)
        {
            damage = Mathf.Max(0, damage - greedStacks);
        }

        if (heartbeatEnabled && heartbeatStacks > 0 && playerHP <= Mathf.RoundToInt(playerMaxHP * 0.5f))
        {
            damage = Mathf.RoundToInt(damage * (1f + heartbeatStacks * 0.05f));
        }

        return damage;
    }

    private void ApplyGambleResultStacks(CardData card)
    {
        if (!IsCardTreatedAsGamble(card) || !gambleResolvedThisUse) return;

        if (gambleSucceededThisUse)
        {
            addictionStacks += 1;
            excitementStacks += 1;
            resignationStacks = Mathf.Max(0, resignationStacks - 2);
        }
        else
        {
            excitementStacks = 0;
            resignationStacks += 1;
        }
    }

    private bool CanUseCard(CardData card)
{
    if (card == null)
    {
        WriteLog("카드 데이터가 없습니다.");
        return false;
    }

    int effectiveCost = GetEffectiveLuxCost(card);
    if (lux < effectiveCost)
    {
        WriteLog($"{card.cardName} 사용 불가. LUX가 부족합니다. 필요 LUX: {effectiveCost}, 현재 LUX: {lux}");
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
        if (isCardResolving) return;
        if (playerStunned)
        {
            WriteLog("제로는 행동 불가 상태입니다. 이번 턴 카드를 사용할 수 없습니다.");
            return;
        }

        if (!CanUseCard(card))
        {
            return;
        }

        usedCardThisTurn = true;

        int effectiveCost = GetEffectiveLuxCost(card);
        lux -= effectiveCost;
        int gainedLux = CalculateLuxGain(card);
        lux += gainedLux;
        lux = Mathf.Clamp(lux, 0, 100);

        CardType effectiveType = GetEffectiveCardType(card);
        bool isGambleCard = IsCardTreatedAsGamble(card);
        bool isLuxTypeCard = IsLuxTypeCard(card);

        if (isGambleCard)
        {
            addictionStacks += 1;
        }

        if (effectiveType == CardType.Poverty || isLuxTypeCard)
        {
            debtStacks += 1;
        }

        if (isLuxTypeCard)
        {
            greedStacks += 1;
        }

        gambleResolvedThisUse = false;
        gambleSucceededThisUse = false;

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

        if (card.specialEffect == SpecialCardEffect.DoubleDice)
        {
            StartCoroutine(ResolveDoubleDiceCardRoutine(card, effectiveType));
            return;
        }

        int finalDamage = CalculateDamage(card);
        ApplyGambleResultStacks(card);
        finalDamage = ApplyOutgoingCardBonuses(finalDamage, effectiveType);
        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        // 분노 상태일 때 플레이어의 공격력 감소
        if (enemyRaged && finalDamage > 0)
        {
            finalDamage -= rageAttackReduction;
            finalDamage = Mathf.Max(finalDamage, 0);
        }

        ApplyEnemyDamage(finalDamage);
        CheckEnemyRage();

        enemyEmotion += card.emotionGain;
        if (!IsCardTreatedAsGamble(card) && card.selfDamage > 0)
        {
            int incoming = ModifyIncomingDamage(card.selfDamage);
            if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, $"{card.cardName} 자해로");
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

    private IEnumerator ResolveDoubleDiceCardRoutine(CardData card, CardType effectiveType)
    {
        isCardResolving = true;
        InitializeDiceRollEffect();

        int d1 = Random.Range(1, 7);
        int d2 = Random.Range(1, 7);

        bool guaranteed = firstGambleGuaranteedThisTurn && !firstGambleUsedThisTurn;
        if (guaranteed)
        {
            firstGambleUsedThisTurn = true;
            int safety = 0;
            while (d1 + d2 < 8 && safety < 50)
            {
                d1 = Random.Range(1, 7);
                d2 = Random.Range(1, 7);
                safety++;
            }
        }

        yield return PlayDoubleDiceRollRoutine(d1, d2);

        int total = d1 + d2;
        bool success = total >= 8;
        gambleResolvedThisUse = true;
        gambleSucceededThisUse = success;

        int finalDamage = 0;
        if (success)
        {
            finalDamage = 25;
        }
        else
        {
            int incoming = ModifyIncomingDamage(20);
            if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "주사위 실패 반동으로");
            failedGambleCountThisTurn += 1;
            failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = 42, successDamage = 25 });
        }

        WriteLog($"<color=#ffd166>주사위:</color> {d1} + {d2} = {total} {(success ? "성공" : "실패")}");

        ApplyGambleResultStacks(card);
        finalDamage = ApplyOutgoingCardBonuses(finalDamage, effectiveType);
        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        if (enemyRaged && finalDamage > 0)
        {
            finalDamage -= rageAttackReduction;
            finalDamage = Mathf.Max(finalDamage, 0);
        }

        ApplyEnemyDamage(finalDamage);
        CheckEnemyRage();

        enemyEmotion += card.emotionGain;
        enemyEmotion = Mathf.Clamp(enemyEmotion, 0, maxEmotion);

        houseTrust += card.houseTrustChange;

        if (card.shieldAmount > 0)
        {
            shield += card.shieldAmount;
        }

        if (card.damageReduction > 0)
        {
            damageReduction += card.damageReduction;
        }

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

        FinishCardUseAfterSpecialRoutine(card, finalDamage);
        isCardResolving = false;
    }

    private void FinishCardUseAfterSpecialRoutine(CardData card, int finalDamage)
    {
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
            chance += addictionStacks * 2;
            chance -= resignationStacks * 3;
            chance += probabilityManipulationTurnsRemaining > 0 ? 20 : 0;
            if (excitementStacks >= 3)
            {
                chance += 10;
            }

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
            bool guaranteed = firstGambleGuaranteedThisTurn && !firstGambleUsedThisTurn;
            if (guaranteed)
            {
                firstGambleUsedThisTurn = true;
                roll = 1;
            }

            if (roll > chance)
            {
                int incoming = ModifyIncomingDamage(card.selfDamage);
                if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, $"{card.cardName} 도박 실패로");
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = false;
                failedGambleCountThisTurn += 1;
                failedGambleRecordsThisTurn.Add(new FailedGambleRecord
                {
                    chance = chance,
                    successDamage = Mathf.Max(card.damage, 0)
                });

                WriteLog($"<color=red>{card.cardName} 실패!</color> ({roll}/{chance}) 제로가 {card.selfDamage} 피해를 받았습니다.");
                return 0;
            }

            gambleResolvedThisUse = true;
            gambleSucceededThisUse = true;
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
                int incoming = ModifyIncomingDamage(damage);
                if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "불운 역효과로");

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
                int beastHeartDamage = Mathf.Max(0, playerHP - 1);
                if (ApplyPlayerDamage(beastHeartDamage) > 0) AddBleedStack(1, "야수의 심장 대가로");
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
            case SpecialCardEffect.IllegalLoan:
                illegalLoanTurnsRemaining = 3;
                illegalLoanPenaltyPending = true;
                WriteLog("<color=#8fd3ff>불법 대출:</color> 3턴 동안 매턴 LUX +5, 이후 HP -10.");
                break;
            case SpecialCardEffect.ProbabilityManipulation:
                probabilityManipulationTurnsRemaining = 2;
                WriteLog("<color=#8fd3ff>확률 조작:</color> 2턴 동안 도박 성공률 +20%.");
                break;
            case SpecialCardEffect.Overload:
                overloadTurnsRemaining = 2;
                enemyEmotion = Mathf.Clamp(enemyEmotion + 20, 0, maxEmotion);
                WriteLog("<color=#8fd3ff>과부하:</color> 2턴 동안 모든 카드 코스트 -3, 분노 +20.");
                break;
            case SpecialCardEffect.Heartbeat:
                heartbeatEnabled = true;
                WriteLog("<color=#8fd3ff>심장 박동:</color> HP 50% 이하일 때 매턴 공격력 보정이 누적됩니다.");
                break;
            case SpecialCardEffect.ReverseOdds:
            {
                int bonusDamage = failedGambleCountThisTurn * 10;
                ApplyEnemyDamage(bonusDamage);
                WriteLog($"<color=#8fd3ff>역배당:</color> 이번 턴 실패 도박 {failedGambleCountThisTurn}회로 {bonusDamage} 피해.");
                break;
            }
            case SpecialCardEffect.ProbabilityLaundering:
            {
                if (failedGambleRecordsThisTurn.Count <= 0)
                {
                    WriteLog("<color=#8fd3ff>확률 세탁:</color> 재판정할 실패 도박이 없습니다.");
                    break;
                }

                FailedGambleRecord record = failedGambleRecordsThisTurn[failedGambleRecordsThisTurn.Count - 1];
                failedGambleRecordsThisTurn.RemoveAt(failedGambleRecordsThisTurn.Count - 1);
                failedGambleCountThisTurn = Mathf.Max(0, failedGambleCountThisTurn - 1);

                int reroll = Random.Range(1, 101);
                if (reroll <= record.chance)
                {
                    int rerollDamage = ApplyOutgoingCardBonuses(record.successDamage, CardType.Gamble);
                    rerollDamage = ApplyTurnDamageModifiers(rerollDamage);
                    if (enemyRaged && rerollDamage > 0)
                    {
                        rerollDamage = Mathf.Max(0, rerollDamage - rageAttackReduction);
                    }
                    ApplyEnemyDamage(rerollDamage);
                    WriteLog($"<color=#8fd3ff>확률 세탁:</color> 재판정 성공! 추가 피해 {rerollDamage}");
                }
                else
                {
                    failedGambleCountThisTurn += 1;
                    failedGambleRecordsThisTurn.Add(record);
                    WriteLog("<color=#8fd3ff>확률 세탁:</color> 재판정도 실패했습니다.");
                }
                break;
            }
            case SpecialCardEffect.FakeLuck:
                firstGambleGuaranteedThisTurn = true;
                firstGambleUsedThisTurn = false;
                enemyEmotion = Mathf.Clamp(enemyEmotion + 20, 0, maxEmotion);
                WriteLog("<color=#8fd3ff>위조 행운:</color> 이번 턴 첫 도박은 무조건 성공, 분노 +20.");
                break;
            case SpecialCardEffect.BankruptcyDeclaration:
            {
                greedStacks = 0;
                int currentLux = lux;
                if (Random.value < 0.5f)
                {
                    lux = 0;
                    int damage = Mathf.RoundToInt(currentLux * 2f);
                    ApplyEnemyDamage(damage);
                    WriteLog($"<color=#8fd3ff>파산 선언 성공:</color> LUX {currentLux} 소모, {damage} 피해.");
                }
                else
                {
                    lux = 0;
                    int incoming = ModifyIncomingDamage(15);
                    if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "파산 선언 실패 반동으로");
                    WriteLog("<color=#8fd3ff>파산 선언 실패:</color> LUX 전부 소실, HP -15.");
                }
                break;
            }
            case SpecialCardEffect.Lucky7777:
            {
                int roll = Random.Range(0, 4);
                if (roll == 0)
                {
                    ApplyEnemyDamage(7);
                    WriteLog("<color=#8fd3ff>7777:</color> 7 피해");
                }
                else if (roll == 1)
                {
                    ApplyEnemyDamage(14);
                    WriteLog("<color=#8fd3ff>7777:</color> 14 피해");
                }
                else if (roll == 2)
                {
                    ApplyEnemyDamage(21);
                    WriteLog("<color=#8fd3ff>7777:</color> 21 피해");
                }
                else
                {
                    int incoming = ModifyIncomingDamage(10);
                    if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "7777 역반동으로");
                    WriteLog("<color=#8fd3ff>7777:</color> 역효과! 제로가 10 피해.");
                }
                enemyHP = Mathf.Clamp(enemyHP, 0, enemyMaxHP);
                break;
            }
        }
    }

    private int ResolveSpecialCardDamage(CardData card)
    {
        switch (card.specialEffect)
        {
            case SpecialCardEffect.CoinTriple:
            {
                int chanceBonus = 0;
                chanceBonus += addictionStacks * 2;
                chanceBonus += probabilityManipulationTurnsRemaining > 0 ? 20 : 0;
                chanceBonus -= resignationStacks * 3;
                if (excitementStacks >= 3) chanceBonus += 10;
                float headChance = Mathf.Clamp01(0.5f + chanceBonus / 100f);

                bool guaranteed = firstGambleGuaranteedThisTurn && !firstGambleUsedThisTurn;
                if (guaranteed)
                {
                    firstGambleUsedThisTurn = true;
                }

                int heads = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (Random.value < headChance) heads++;
                }

                bool success = guaranteed || heads >= 2;
                if (success)
                {
                    gambleResolvedThisUse = true;
                    gambleSucceededThisUse = true;
                    WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 성공");
                    return 40;
                }

                int incoming = ModifyIncomingDamage(20);
                if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "코인 실패 반동으로");
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = false;
                failedGambleCountThisTurn += 1;
                failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = 50, successDamage = 40 });
                WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 실패, 제로가 20 피해를 받았습니다.");
                return 0;
            }
            case SpecialCardEffect.DoubleDice:
            {
                bool guaranteed = firstGambleGuaranteedThisTurn && !firstGambleUsedThisTurn;
                if (guaranteed)
                {
                    firstGambleUsedThisTurn = true;
                }
                int d1 = Random.Range(1, 7);
                int d2 = Random.Range(1, 7);
                int total = d1 + d2;
                bool success = guaranteed || total >= 8;
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = success;
                if (!success)
                {
                    failedGambleCountThisTurn += 1;
                    failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = 42, successDamage = 25 });
                }
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
        List<string> turnLogs = new List<string>();

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

                finalDamage = ModifyIncomingDamage(finalDamage);
                ApplyPlayerDamage(finalDamage);

                resultLog = $"적의 공격 발동. 제로가 {finalDamage} 피해를 받았습니다.";

                if (reflectNextDamage && finalDamage > 0)
                {
                    ApplyEnemyDamage(finalDamage);

                    resultLog += $"\n반사 발동! 적에게 {finalDamage} 피해를 되돌렸습니다.";

                    reflectNextDamage = false;
                }
            }
        }

        turnLogs.Add(resultLog);

        if (playerStunned)
        {
            turnLogs.Add("제로는 행동 불가 상태입니다. 이번 턴 아무 행동도 할 수 없습니다.");
            playerStunned = false;
        }

        turn++;

        if (bleedStacks > 0)
        {
            int bleedDamage = ModifyIncomingDamage(bleedStacks);
            ApplyPlayerDamage(bleedDamage);
            turnLogs.Add($"<color=red>출혈</color>로 턴 종료 시 {bleedDamage} 피해를 받았습니다.");
        }

        if (addictionStacks >= 5)
        {
            int addictionPenalty = ModifyIncomingDamage(2);
            ApplyPlayerDamage(addictionPenalty);
            turnLogs.Add($"<color=#a35dff>중독 부작용:</color> 턴 종료 시 {addictionPenalty} 피해를 받았습니다.");
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

            turnLogs.Add("<color=cyan>폭주 반동:</color> 턴 종료 시 LUX -30.");
        }

        if (luxDrainTurnsRemaining > 0)
        {
            lux += 2;
            lux = Mathf.Clamp(lux, 0, 100);
            luxDrainTurnsRemaining--;
            turnLogs.Add("<color=#8fd3ff>럭스 드레인:</color> LUX +2");
        }

        if (illegalLoanTurnsRemaining > 0)
        {
            lux += 5;
            lux = Mathf.Clamp(lux, 0, 100);
            illegalLoanTurnsRemaining--;
            turnLogs.Add("<color=#8fd3ff>불법 대출:</color> LUX +5");
            if (illegalLoanTurnsRemaining == 0 && illegalLoanPenaltyPending)
            {
                int incoming = ModifyIncomingDamage(10);
                if (ApplyPlayerDamage(incoming) > 0) AddBleedStack(1, "불법 대출 상환으로");
                illegalLoanPenaltyPending = false;
                turnLogs.Add("<color=#8fd3ff>불법 대출:</color> 만기 도달, HP -10");
            }
        }

        if (probabilityManipulationTurnsRemaining > 0)
        {
            probabilityManipulationTurnsRemaining--;
        }

        if (overloadTurnsRemaining > 0)
        {
            overloadTurnsRemaining--;
        }

        if (heartbeatEnabled && playerHP <= Mathf.RoundToInt(playerMaxHP * 0.5f))
        {
            heartbeatStacks += 1;
            turnLogs.Add($"<color=#8fd3ff>심장 박동:</color> 저체력 보정 {heartbeatStacks * 5}% 활성");
        }

        if (!usedCardThisTurn)
        {
            // 아무 카드도 안 쓴 턴은 손패를 덱으로 되돌려 다시 셔플
            drawPile.AddRange(hand);
            Shuffle(drawPile);
        }
        else
        {
            discardPile.AddRange(hand);
        }
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

        usedCardThisTurn = false;
        failedGambleCountThisTurn = 0;
        failedGambleRecordsThisTurn.Clear();
        firstGambleGuaranteedThisTurn = false;
        firstGambleUsedThisTurn = false;

        UpdateUI();
        UpdateEnemyDialogue();
        WriteLog(string.Join("\n", turnLogs));

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

        if (startingCards == null)
        {
            Debug.LogError("[BattleManager] startingCards가 비어 있습니다.", this);
            return;
        }

        drawPile = startingCards.Where(c => c != null).ToList();
        if (drawPile.Count != startingCards.Length)
        {
            Debug.LogWarning("[BattleManager] startingCards에 null 카드가 있어 제외했습니다.", this);
        }

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
        if (card == null)
        {
            Debug.LogWarning("[BattleManager] drawPile의 null 카드를 스킵했습니다.", this);
            continue;
        }

        // 잭팟 카드 확률 제한 
        
        if (card.isJackpot)
        {
            if (Random.value > 0.005f)
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

    drawPile.AddRange(discardPile.Where(c => c != null));
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

    UpdateEnemySprite();
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
            stackTagText.text =
                $"출혈 x{bleedStacks} | 중독 x{addictionStacks} | 채무 x{debtStacks}\n" +
                $"흥분 x{excitementStacks} | 탐욕 x{greedStacks} | 체념 x{resignationStacks}";
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
