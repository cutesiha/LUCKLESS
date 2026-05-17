using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
    public Sprite enemyHitSprite;
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

    [Header("Hit Voice")]
    public AudioClip playerHitClip;
    public AudioClip enemyHitClip;
    [Range(0f, 3f)] public float hitSoundVolume = 1f;
    public AudioClip cardSelectClip;
    [Range(0f, 3f)] public float cardSelectSoundVolume = 1f;
    private AudioSource _hitAudioSource;
    private AudioSource _sfxAudioSource;

    [Header("Dice Roll Effect")]
    public GameObject diceRollPanel;
    public Image diceLeftImage;
    public Image diceRightImage;
    public TMP_Text diceTotalText;
    public Sprite[] diceFaceSprites = new Sprite[6];
    public float diceRollDuration = 1.25f;
    public float diceResultHoldDuration = 1.3f;
    public float diceFaceChangeInterval = 0.05f;
    public Color diceHoverColor = new Color(0.85f, 0.35f, 0.65f, 1f);

    [Header("Coin Toss Effect")]
    public GameObject coinTossPanel;
    public Image coinFirstImage;
    public Image coinSecondImage;
    public Image coinThirdImage;
    public TMP_Text coinTotalText;
    public Sprite coinIdleSprite;
    public Sprite[] coinFaceSprites = new Sprite[2];
    public float coinRollDuration = 0.9f;
    public float coinResultHoldDuration = 1.2f;
    public float coinFaceChangeInterval = 0.02f;
    public Color coinHoverColor = new Color(1f, 0.55f, 0.9f, 1f);

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
    [SerializeField] private string gameOverSceneName = "MainScene";
    [SerializeField] private string victorySceneName = "MainScene";
    [SerializeField] private float resultFadeDuration = 0.8f;
    [SerializeField] private float resultHoldDuration = 1.1f;
    [SerializeField] private string missionScoreId = BattleScoreStore.DefaultMissionId;


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
    public Vector2 luxFloatingDeltaOffset = new Vector2(-96f, 0f);
    public Color povertyLuxBarColor = new Color(0.78f, 0.05f, 0.38f, 1f);
    public Color normalLuxBarColor = new Color(1f, 0.25f, 0.62f, 1f);
    public Color luckyLuxBarColor = new Color(1f, 0.48f, 0.48f, 1f);
    public Color overflowLuxBarColor = new Color(1f, 0.78f, 0.18f, 1f);
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
    private bool gambleResolvedThisUse = false;
    private bool gambleSucceededThisUse = false;
    private bool usedCardThisTurn = false;
    private int illegalLoanTurnsRemaining = 0;
    private bool illegalLoanPenaltyPending = false;
    private int probabilityManipulationTurnsRemaining = 0;
    private bool probabilityManipulationActivatedThisTurn = false;
    private int overloadTurnsRemaining = 0;
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
    private int currentCardLuxCost = 0;
    private int currentCardLuxGain = 0;
    private int currentCardPlayerDamageTaken = 0;
    private bool resultTransitionStarted = false;
    private readonly List<FailedGambleRecord> failedGambleRecordsThisTurn = new List<FailedGambleRecord>();
    private int _prevPlayerHP;
    private int _prevLux;
    private int _prevEnemyHP;
    private int _prevEnemyEmotion;
    private int _prevShield;

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
            tooltipDescriptionText.richText = true;
            tooltipDescriptionText.text = FormatCardDescription(card.description);
        }
    }

    public void HideCardTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void Start()
    {
        battleEnded = false;
        battleStarted = true;
        selectedBet = 0;
        rewardLux = 0;

        if (bettingPanel != null) bettingPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(true);
        if (endPanel != null) endPanel.SetActive(false);
        if (negotiationButton != null) negotiationButton.gameObject.SetActive(false);

        _hitAudioSource = gameObject.AddComponent<AudioSource>();
        _hitAudioSource.playOnAwake = false;
        _hitAudioSource.spatialBlend = 0f;
        _sfxAudioSource = gameObject.AddComponent<AudioSource>();
        _sfxAudioSource.playOnAwake = false;
        _sfxAudioSource.spatialBlend = 0f;
        InitializePlayerHitEffect();
        InitializeDiceRollEffect();
        InitializeCoinTossEffect();
        EnsureHandPanel();
        _prevPlayerHP = playerHP;
        _prevLux = lux;
        _prevEnemyHP = enemyHP;
        _prevEnemyEmotion = enemyEmotion;
        _prevShield = shield;
        UpdateUI();
        UpdateEnemyDialogue();
        UpdateEnemySprite();
        SetupDeck();
        DrawCards(drawCount);
        StartCoroutine(RefreshHandUIRoutine());
        StartCoroutine(ShowBattleIntroHint());
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
        PlayHitClip(enemyHitClip);
        if (enemyCharacterImage == null) return;

        if (enemyHitEffectRoutine != null)
        {
            StopCoroutine(enemyHitEffectRoutine);
            enemyCharacterImage.rectTransform.anchoredPosition = enemyHitOriginalPosition;
            enemyCharacterImage.color = enemyHitOriginalColor;
            enemyHitEffectActive = false;
        }

        enemyHitOriginalColor = enemyCharacterImage.color;
        enemyHitEffectActive = true;
        enemyCharacterImage.color = enemyHitFlashColor;
        enemyHitEffectRoutine = StartCoroutine(EnemyHitEffectRoutine());
    }

    private IEnumerator EnemyHitEffectRoutine()
    {
        RectTransform rectTransform = enemyCharacterImage.rectTransform;
        enemyHitOriginalPosition = rectTransform.anchoredPosition;

        Sprite hitSprite = enemyHitSprite != null ? enemyHitSprite : (enemyRageSprite != null ? enemyRageSprite : GetEnemyCurrentSprite());
        enemyCharacterImage.sprite = hitSprite;

        float elapsed = 0f;
        while (elapsed < enemyHitEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enemyHitEffectDuration);
            float shake = enemyHitShakeStrength * (1f - t);

            float xShake = Mathf.Sin(elapsed * 90f) * shake;
            float yShake = Mathf.Cos(elapsed * 65f) * shake * 0.45f;
            rectTransform.anchoredPosition = enemyHitOriginalPosition + new Vector2(xShake, yShake);
            enemyCharacterImage.color = Color.Lerp(enemyHitFlashColor, enemyHitOriginalColor, t);

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
        currentCardPlayerDamageTaken += actualDamage;
        if (actualDamage > 0)
        {
            PlayPlayerHitEffect();
        }

        return actualDamage;
    }

    private void PlayHitClip(AudioClip clip)
    {
        if (clip == null || _hitAudioSource == null) return;
        _hitAudioSource.volume = hitSoundVolume;
        _hitAudioSource.PlayOneShot(clip);
    }

    public void PlayCardSelectSound()
    {
        PlaySfxClip(cardSelectClip, cardSelectSoundVolume);
    }

    private void PlaySfxClip(AudioClip clip, float volumeScale)
    {
        if (clip == null || _sfxAudioSource == null) return;

        _sfxAudioSource.volume = GameAudioSettings.SfxVolume;
        _sfxAudioSource.PlayOneShot(clip, Mathf.Clamp(volumeScale, 0f, 3f));
    }

    private void PlayPlayerHitEffect()
    {
        PlayHitClip(playerHitClip);
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

        SetDiceImage(diceLeftImage, 1);
        SetDiceImage(diceRightImage, 1);

        bool leftResolved = false;
        bool rightResolved = false;
        StartClickableDice(diceLeftImage, leftResult, () => leftResolved = true);
        StartClickableDice(diceRightImage, rightResult, () => rightResolved = true);

        yield return new WaitUntil(() => leftResolved && rightResolved);

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

    private void StartClickableDice(Image diceImage, int result, System.Action onResolved)
    {
        Button button = GetOrCreateDiceClickButton(diceImage);
        if (button == null)
        {
            onResolved?.Invoke();
            return;
        }

        button.onClick.RemoveAllListeners();
        button.interactable = true;
        diceImage.color = Color.white;
        UnityEngine.Events.UnityAction clickAction = null;
        clickAction = () =>
        {
            button.onClick.RemoveListener(clickAction);
            button.interactable = false;
            diceImage.color = Color.white;
            StartCoroutine(RollDiceImageRoutine(diceImage, result, onResolved));
        };
        button.onClick.AddListener(clickAction);
    }

    private IEnumerator RollDiceImageRoutine(Image diceImage, int result, System.Action onResolved)
    {
        float elapsed = 0f;
        float nextFaceChange = 0f;
        float rollDuration = Mathf.Max(0.01f, diceRollDuration);

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            nextFaceChange -= Time.deltaTime;
            if (nextFaceChange <= 0f)
            {
                SetDiceImage(diceImage, Random.Range(1, 7));
                nextFaceChange = Mathf.Max(0.01f, diceFaceChangeInterval);
            }

            yield return null;
        }

        SetDiceImage(diceImage, result);
        onResolved?.Invoke();
    }

    private void InitializeCoinTossEffect()
    {
        if (coinTossPanel == null)
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
                coinTossPanel = new GameObject("CoinTossPanel", typeof(RectTransform));
                coinTossPanel.transform.SetParent(canvas.transform, false);

                RectTransform panelTransform = coinTossPanel.GetComponent<RectTransform>();
                panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
                panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
                panelTransform.pivot = new Vector2(0.5f, 0.5f);
                panelTransform.sizeDelta = new Vector2(500f, 220f);
                panelTransform.anchoredPosition = Vector2.zero;

                coinFirstImage = CreateCoinImage("FirstCoin", coinTossPanel.transform, new Vector2(-130f, 30f));
                coinSecondImage = CreateCoinImage("SecondCoin", coinTossPanel.transform, new Vector2(0f, 30f));
                coinThirdImage = CreateCoinImage("ThirdCoin", coinTossPanel.transform, new Vector2(130f, 30f));
                coinTotalText = CreateCoinTotalText(coinTossPanel.transform);
            }
        }

        if (coinTossPanel != null)
        {
            if (coinFirstImage == null)
            {
                coinFirstImage = CreateCoinImage("FirstCoin", coinTossPanel.transform, new Vector2(-130f, 30f));
            }

            if (coinSecondImage == null)
            {
                coinSecondImage = CreateCoinImage("SecondCoin", coinTossPanel.transform, new Vector2(0f, 30f));
            }

            if (coinThirdImage == null)
            {
                coinThirdImage = CreateCoinImage("ThirdCoin", coinTossPanel.transform, new Vector2(130f, 30f));
            }

            if (coinTotalText == null)
            {
                coinTotalText = CreateCoinTotalText(coinTossPanel.transform);
            }

            coinTossPanel.SetActive(false);
        }

        if (coinTotalText != null)
        {
            coinTotalText.text = string.Empty;
        }
    }

    private Image CreateCoinImage(string objectName, Transform parent, Vector2 anchoredPosition)
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

    private TMP_Text CreateCoinTotalText(Transform parent)
    {
        GameObject textObject = new GameObject("CoinTotalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(420f, 60f);
        rectTransform.anchoredPosition = new Vector2(0f, -70f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private IEnumerator PlayCoinTossRoutine(bool[] results)
    {
        if (coinTossPanel == null || coinFirstImage == null || coinSecondImage == null || coinThirdImage == null)
        {
            yield break;
        }

        coinTossPanel.SetActive(true);
        coinTossPanel.transform.SetAsLastSibling();

        if (coinTotalText != null)
        {
            coinTotalText.text = string.Empty;
        }

        SetCoinIdleImage(coinFirstImage);
        SetCoinIdleImage(coinSecondImage);
        SetCoinIdleImage(coinThirdImage);

        bool firstResolved = false;
        bool secondResolved = false;
        bool thirdResolved = false;
        StartClickableCoin(coinFirstImage, results[0], () => firstResolved = true);
        StartClickableCoin(coinSecondImage, results[1], () => secondResolved = true);
        StartClickableCoin(coinThirdImage, results[2], () => thirdResolved = true);

        yield return new WaitUntil(() => firstResolved && secondResolved && thirdResolved);

        int heads = 0;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i]) heads++;
        }

        if (coinTotalText != null)
        {
            coinTotalText.text = $"총합: 앞면 {heads}/3";
        }

        yield return new WaitForSeconds(coinResultHoldDuration);

        if (coinTossPanel != null)
        {
            coinTossPanel.SetActive(false);
        }
    }

    private IEnumerator RollSingleCoinRoutine(Image coinImage, bool resultIsHeads)
    {
        yield return RollCoinImageRoutine(coinImage, resultIsHeads);
    }

    private void StartClickableCoin(Image coinImage, bool resultIsHeads, System.Action onResolved)
    {
        Button button = GetOrCreateCoinClickButton(coinImage);
        if (button == null)
        {
            onResolved?.Invoke();
            return;
        }

        button.onClick.RemoveAllListeners();
        button.interactable = true;
        coinImage.color = Color.white;
        UnityEngine.Events.UnityAction clickAction = null;
        clickAction = () =>
        {
            button.onClick.RemoveListener(clickAction);
            button.interactable = false;
            coinImage.color = Color.white;
            StartCoroutine(RollCoinImageRoutine(coinImage, resultIsHeads, onResolved));
        };
        button.onClick.AddListener(clickAction);
    }

    private IEnumerator RollCoinImageRoutine(Image coinImage, bool resultIsHeads, System.Action onResolved = null)
    {
        float elapsed = 0f;
        float nextFaceChange = 0f;
        float rollDuration = Mathf.Max(0.01f, coinRollDuration);

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            nextFaceChange -= Time.deltaTime;
            if (nextFaceChange <= 0f)
            {
                SetCoinImage(coinImage, Random.value < 0.5f);
                nextFaceChange = Mathf.Max(0.01f, coinFaceChangeInterval);
            }

            yield return null;
        }

        SetCoinImage(coinImage, resultIsHeads);
        coinImage.color = Color.white;
        onResolved?.Invoke();
    }

    private void SetCoinImage(Image targetImage, bool isHeads)
    {
        if (targetImage == null) return;
        if (coinFaceSprites == null || coinFaceSprites.Length < 2) return;

        int index = isHeads ? 0 : 1;
        if (coinFaceSprites[index] != null)
        {
            targetImage.sprite = coinFaceSprites[index];
        }
    }

    private void SetCoinIdleImage(Image targetImage)
    {
        if (targetImage == null) return;

        if (coinIdleSprite != null)
        {
            targetImage.sprite = coinIdleSprite;
            return;
        }

        SetCoinImage(targetImage, true);
    }

    private Button GetOrCreateClickButton(Image image, Color hoverColor)
    {
        if (image == null) return null;

        image.raycastTarget = true;
        Button button = image.GetComponent<Button>();
        if (button == null)
        {
            button = image.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = Color.Lerp(Color.white, hoverColor, 0.75f);
        colors.selectedColor = hoverColor;
        colors.disabledColor = Color.white;
        button.colors = colors;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        return button;
    }

    private Button GetOrCreateCoinClickButton(Image coinImage)
    {
        if (coinImage == null) return null;

        coinImage.raycastTarget = false;
        RectTransform coinTransform = coinImage.rectTransform;
        Transform parent = coinTransform.parent;
        if (parent == null) return null;

        string hitAreaName = coinImage.gameObject.name + "HitArea";
        Transform existing = parent.Find(hitAreaName);
        GameObject hitObject;
        if (existing == null)
        {
            hitObject = new GameObject(hitAreaName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
            hitObject.transform.SetParent(parent, false);
        }
        else
        {
            hitObject = existing.gameObject;
        }

        RectTransform hitTransform = hitObject.GetComponent<RectTransform>();
        hitTransform.anchorMin = coinTransform.anchorMin;
        hitTransform.anchorMax = coinTransform.anchorMax;
        hitTransform.pivot = coinTransform.pivot;
        hitTransform.sizeDelta = coinTransform.sizeDelta;
        hitTransform.anchoredPosition = coinTransform.anchoredPosition;
        hitTransform.localScale = coinTransform.localScale;
        hitObject.transform.SetAsLastSibling();

        Image hitImage = hitObject.GetComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0.01f);
        hitImage.raycastTarget = true;

        Button button = hitObject.GetComponent<Button>();
        button.targetGraphic = hitImage;
        button.transition = Selectable.Transition.None;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        EventTrigger trigger = hitObject.GetComponent<EventTrigger>();
        trigger.triggers.Clear();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            if (button.interactable)
            {
                coinImage.color = coinHoverColor;
            }
        });
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => coinImage.color = Color.white);

        return button;
    }

    private Button GetOrCreateDiceClickButton(Image diceImage)
    {
        if (diceImage == null) return null;

        diceImage.raycastTarget = false;
        RectTransform diceTransform = diceImage.rectTransform;
        Transform parent = diceTransform.parent;
        if (parent == null) return null;

        string hitAreaName = diceImage.gameObject.name + "HitArea";
        Transform existing = parent.Find(hitAreaName);
        GameObject hitObject;
        if (existing == null)
        {
            hitObject = new GameObject(hitAreaName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
            hitObject.transform.SetParent(parent, false);
        }
        else
        {
            hitObject = existing.gameObject;
        }

        RectTransform hitTransform = hitObject.GetComponent<RectTransform>();
        hitTransform.anchorMin = diceTransform.anchorMin;
        hitTransform.anchorMax = diceTransform.anchorMax;
        hitTransform.pivot = diceTransform.pivot;
        hitTransform.sizeDelta = diceTransform.sizeDelta;
        hitTransform.anchoredPosition = diceTransform.anchoredPosition;
        hitTransform.localScale = diceTransform.localScale;
        hitObject.transform.SetAsLastSibling();

        Image hitImage = hitObject.GetComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0.01f);
        hitImage.raycastTarget = true;

        Button button = hitObject.GetComponent<Button>();
        button.targetGraphic = hitImage;
        button.transition = Selectable.Transition.None;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        EventTrigger trigger = hitObject.GetComponent<EventTrigger>();
        trigger.triggers.Clear();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            if (button.interactable)
            {
                diceImage.color = diceHoverColor;
            }
        });
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => diceImage.color = Color.white);

        return button;
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(_ => action?.Invoke());
        trigger.triggers.Add(entry);
    }


    private void RefreshHandUI()
{
    EnsureHandPanel();
    if (handPanel == null)
    {
        Debug.LogError("[BattleManager] HandPanel을 찾을 수 없습니다.", this);
        return;
    }

    if (handRoutine != null)
    {
        StopCoroutine(handRoutine);
    }

    handRoutine = StartCoroutine(RefreshHandUIRoutine());
}

    private void EnsureHandPanel()
    {
        if (handPanel != null)
        {
            return;
        }

        GameObject existingHandPanel = GameObject.Find("HandPanel");
        if (existingHandPanel != null)
        {
            handPanel = existingHandPanel.transform;
            return;
        }

        Transform parent = battlePanel != null ? battlePanel.transform : null;
        if (parent == null)
        {
#if UNITY_2023_1_OR_NEWER
            Canvas canvas = FindFirstObjectByType<Canvas>();
#else
            Canvas canvas = FindObjectOfType<Canvas>();
#endif
            parent = canvas != null ? canvas.transform : transform;
        }

        GameObject handPanelObject = new GameObject("HandPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        handPanelObject.transform.SetParent(parent, false);
        handPanel = handPanelObject.transform;

        RectTransform rectTransform = handPanelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 40f);
        rectTransform.sizeDelta = new Vector2(980f, 260f);

        HorizontalLayoutGroup layout = handPanelObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = handPanelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private IEnumerator CreateStartingHandRoutine()
{
    EnsureHandPanel();
    if (handPanel == null)
    {
        yield break;
    }

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

    private int ModifyIncomingDamage(int damage)
    {
        return Mathf.Max(damage, 0);
    }

    private int CalculateLuxGain(CardData card)
    {
        return card.luxGain;
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

    public string GetCardButtonLabel(CardData card)
    {
        if (card == null)
        {
            return "";
        }

        int effectiveCost = GetEffectiveLuxCost(card);
        string costText = effectiveCost == card.luxCost
            ? $"LUX {effectiveCost}"
            : $"LUX <s>{card.luxCost}</s> <color=#12346f>{effectiveCost}</color>";

        return $"{card.cardName}\n<{costText}>";
    }

    private int GetGambleChanceBonus()
    {
        int chanceBonus = 0;
        chanceBonus += probabilityManipulationTurnsRemaining > 0 ? 20 : 0;

        return chanceBonus;
    }

    private int GetAdjustedGambleSuccessChance(int baseChance, bool includeLuxState)
    {
        int chance = baseChance + GetGambleChanceBonus();

        if (includeLuxState)
        {
            LuxState state = GetLuxState();
            if (state == LuxState.Lucky)
            {
                chance += 10;
            }
            else if (state == LuxState.Poverty)
            {
                chance -= 10;
            }
        }

        return Mathf.Clamp(chance, 5, 95);
    }

    private bool TryUseFirstGambleGuarantee()
    {
        if (!firstGambleGuaranteedThisTurn || firstGambleUsedThisTurn) return false;

        firstGambleUsedThisTurn = true;
        return true;
    }

    private void RollDoubleDiceForResult(bool shouldSucceed, out int d1, out int d2)
    {
        int safety = 0;
        do
        {
            d1 = Random.Range(1, 7);
            d2 = Random.Range(1, 7);
            safety++;
        }
        while (((d1 + d2 >= 8) != shouldSucceed) && safety < 50);
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
        currentCardLuxCost = effectiveCost;
        currentCardLuxGain = gainedLux;
        currentCardPlayerDamageTaken = 0;

        CardType effectiveType = GetEffectiveCardType(card);
        gambleResolvedThisUse = false;
        gambleSucceededThisUse = false;

        if (card.reflectNextEnemyDamage)
        {
            reflectNextDamage = true;
        }

        int enemyHPBeforeCard = enemyHP;
        ApplySpecialCardEffect(card);

        // 체력 회복
        if (card.healAmount > 0)
        {
            playerHP += card.healAmount;
            playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

            WriteLog($"{card.cardName} 사용 → HP {card.healAmount} 회복");
        }

        if (card.specialEffect == SpecialCardEffect.CoinTriple)
        {
            StartCoroutine(ResolveCoinTripleCardRoutine(card, effectiveType));
            return;
        }

        if (card.specialEffect == SpecialCardEffect.DoubleDice)
        {
            StartCoroutine(ResolveDoubleDiceCardRoutine(card, effectiveType));
            return;
        }

        int finalDamage = CalculateDamage(card);
        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        // 분노 상태일 때 플레이어의 공격력 감소
        if (enemyRaged && finalDamage > 0 && effectiveType != CardType.Deal)
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
            ApplyPlayerDamage(incoming);
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

        WriteCardUseSummary(card, enemyHPBeforeCard - enemyHP);


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

        int chance = GetAdjustedGambleSuccessChance(42, false);
        bool success = TryUseFirstGambleGuarantee() || Random.Range(1, 101) <= chance;
        RollDoubleDiceForResult(success, out int d1, out int d2);

        yield return PlayDoubleDiceRollRoutine(d1, d2);

        int total = d1 + d2;
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
            ApplyPlayerDamage(incoming);
            failedGambleCountThisTurn += 1;
            failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = chance, successDamage = 25 });
        }

        WriteLog($"<color=#ffd166>주사위:</color> {d1} + {d2} = {total} {(success ? "성공" : "실패")}");

        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        if (enemyRaged && finalDamage > 0 && effectiveType != CardType.Deal)
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

    private void WriteCardUseSummary(CardData card, int finalDamage)
    {
        List<string> parts = new List<string>();

        if (IsCardTreatedAsGamble(card) && gambleResolvedThisUse)
        {
            parts.Add(gambleSucceededThisUse ? "도박에 성공했습니다!" : "도박에 실패했습니다!");
        }

        parts.Add($"<color=yellow>{card.cardName}</color> 사용!");

        if (currentCardLuxCost > 0)
        {
            parts.Add($"<color=#66ccff>LUX -{currentCardLuxCost}</color>");
        }

        if (currentCardLuxGain > 0)
        {
            parts.Add($"<color=#66ccff>LUX +{currentCardLuxGain}</color>");
        }
        else if (currentCardLuxGain < 0)
        {
            parts.Add($"<color=#66ccff>LUX {currentCardLuxGain}</color>");
        }

        if (finalDamage > 0)
        {
            parts.Add($"<color=red>적에게 {finalDamage} 피해</color>");
        }

        if (currentCardPlayerDamageTaken > 0)
        {
            parts.Add($"<color=red>제로가 {currentCardPlayerDamageTaken} 피해를 입었습니다</color>");
        }

        if (card.healAmount > 0)
        {
            parts.Add($"HP {card.healAmount} 회복");
        }

        if (card.emotionGain > 0)
        {
            parts.Add($"분노 +{card.emotionGain}");
        }

        parts.Add($"현재 LUX: {lux}");
        WriteLog(string.Join(". ", parts));
    }

    private string FormatCardDescription(string description)
    {
        if (string.IsNullOrEmpty(description)) return description;

        string formatted = description;
        formatted = Regex.Replace(
            formatted,
            @"(Lux\s*cost\s*:\s*\d+)",
            "<color=#ffd63d>$1</color>",
            RegexOptions.IgnoreCase);
        formatted = Regex.Replace(
            formatted,
            @"(Lux\s*[+-]\s*\d+)",
            "<color=#ffd63d>$1</color>",
            RegexOptions.IgnoreCase);
        formatted = Regex.Replace(
            formatted,
            @"(피해\s*\d+)",
            "<color=red>$1</color>");
        formatted = Regex.Replace(
            formatted,
            @"(\d+\s*피해)",
            "<color=red>$1</color>");

        return formatted;
    }

    private IEnumerator ResolveCoinTripleCardRoutine(CardData card, CardType effectiveType)
    {
        isCardResolving = true;
        InitializeCoinTossEffect();

        int chanceBonus = GetGambleChanceBonus();
        float headChance = Mathf.Clamp01(0.5f + chanceBonus / 100f);

        bool guaranteed = TryUseFirstGambleGuarantee();

        bool[] results = new bool[3];
        int heads = 0;
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = Random.value < headChance;
            if (results[i]) heads++;
        }

        if (guaranteed && heads < 2)
        {
            for (int i = 0; i < results.Length && heads < 2; i++)
            {
                if (!results[i])
                {
                    results[i] = true;
                    heads++;
                }
            }
        }

        yield return PlayCoinTossRoutine(results);

        bool success = heads >= 2;
        gambleResolvedThisUse = true;
        gambleSucceededThisUse = success;

        int finalDamage = 0;
        if (success)
        {
            finalDamage = 40;
            WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 성공");
        }
        else
        {
            int incoming = ModifyIncomingDamage(20);
            ApplyPlayerDamage(incoming);
            failedGambleCountThisTurn += 1;
            failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = GetAdjustedGambleSuccessChance(50, false), successDamage = 40 });
            WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 실패, 제로가 20 피해를 받습니다.");
        }

        finalDamage = ApplyTurnDamageModifiers(finalDamage);

        if (enemyRaged && finalDamage > 0 && effectiveType != CardType.Deal)
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
        WriteCardUseSummary(card, finalDamage);

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
            return damage;
        }

        if (IsCardTreatedAsGamble(card))
        {
            int chance = GetAdjustedGambleSuccessChance(card.gambleSuccessChance, true);
            int roll = Random.Range(1, 101);
            bool guaranteed = TryUseFirstGambleGuarantee();
            if (guaranteed)
            {
                roll = 1;
            }

            if (roll > chance)
            {
                int incoming = ModifyIncomingDamage(card.selfDamage);
                ApplyPlayerDamage(incoming);
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
                ApplyPlayerDamage(beastHeartDamage);
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
                WriteLog("<color=#8fd3ff>럭스 드레인:</color> 3턴 동안 매턴 LUX +3.");
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
                probabilityManipulationActivatedThisTurn = true;
                WriteLog("<color=#8fd3ff>확률 조작:</color> 2턴 동안 도박 성공률 +20%.");
                break;
            case SpecialCardEffect.Overload:
                overloadTurnsRemaining = 2;
                WriteLog("<color=#8fd3ff>과부하:</color> 2턴 동안 모든 카드 코스트 -3, 분노 +20.");
                break;
            case SpecialCardEffect.Heartbeat:
                WriteLog("<color=#8fd3ff>심장 박동:</color> 현재 효과가 없습니다.");
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
                    int rerollDamage = record.successDamage;
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
                break;
            case SpecialCardEffect.Lucky7777:
                break;
        }
    }

    private int ResolveSpecialCardDamage(CardData card)
    {
        switch (card.specialEffect)
        {
            case SpecialCardEffect.CoinTriple:
            {
                int chanceBonus = GetGambleChanceBonus();
                float headChance = Mathf.Clamp01(0.5f + chanceBonus / 100f);

                bool guaranteed = TryUseFirstGambleGuarantee();

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
                ApplyPlayerDamage(incoming);
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = false;
                failedGambleCountThisTurn += 1;
                failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = GetAdjustedGambleSuccessChance(50, false), successDamage = 40 });
                WriteLog($"<color=#ffd166>코인 3연속:</color> 앞면 {heads}/3 실패, 제로가 20 피해를 받았습니다.");
                return 0;
            }
            case SpecialCardEffect.DoubleDice:
            {
                int chance = GetAdjustedGambleSuccessChance(42, false);
                bool success = TryUseFirstGambleGuarantee() || Random.Range(1, 101) <= chance;
                RollDoubleDiceForResult(success, out int d1, out int d2);
                int total = d1 + d2;
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = success;
                if (!success)
                {
                    failedGambleCountThisTurn += 1;
                    failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = chance, successDamage = 25 });
                }
                WriteLog($"<color=#ffd166>주사위:</color> {d1} + {d2} = {total} {(success ? "성공" : "실패")}");
                return success ? 25 : 0;
            }
            case SpecialCardEffect.BankruptcyDeclaration:
            {
                int consumedLux = Mathf.Clamp(lux + currentCardLuxCost - currentCardLuxGain, 0, 100);
                int chance = GetAdjustedGambleSuccessChance(card.gambleSuccessChance, true);
                int roll = Random.Range(1, 101);
                bool guaranteed = TryUseFirstGambleGuarantee();
                if (guaranteed)
                {
                    roll = 1;
                }

                lux = 0;
                gambleResolvedThisUse = true;
                bool success = roll <= chance;
                gambleSucceededThisUse = success;

                if (success)
                {
                    int damage = Mathf.RoundToInt(consumedLux * 2f);
                    WriteLog($"<color=#8fd3ff>파산 선언 성공:</color> LUX {consumedLux} 소모, {damage} 피해. ({roll}/{chance})");
                    return damage;
                }

                int incoming = ModifyIncomingDamage(15);
                ApplyPlayerDamage(incoming);
                failedGambleCountThisTurn += 1;
                failedGambleRecordsThisTurn.Add(new FailedGambleRecord
                {
                    chance = chance,
                    successDamage = Mathf.RoundToInt(consumedLux * 2f)
                });

                WriteLog($"<color=#8fd3ff>파산 선언 실패:</color> LUX 전부 소실, HP -15. ({roll}/{chance})");
                return 0;
            }
            case SpecialCardEffect.Lucky7777:
            {
                int roll = Random.Range(1, 101);
                gambleResolvedThisUse = true;
                gambleSucceededThisUse = roll > 25;

                if (roll <= 25)
                {
                    ApplyPlayerDamage(7);
                    failedGambleCountThisTurn += 1;
                    failedGambleRecordsThisTurn.Add(new FailedGambleRecord { chance = 75, successDamage = 14 });
                    WriteLog($"<color=#8fd3ff>7777:</color> 실패! 제로가 7 피해. ({roll}/100)");
                    return 0;
                }

                if (roll <= 50)
                {
                    WriteLog($"<color=#8fd3ff>7777:</color> 7 피해. ({roll}/100)");
                    return 7;
                }

                if (roll <= 75)
                {
                    WriteLog($"<color=#8fd3ff>7777:</color> 14 피해. ({roll}/100)");
                    return 14;
                }

                WriteLog($"<color=#8fd3ff>7777:</color> 21 피해. ({roll}/100)");
                return 21;
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

        if (GetLuxState() == LuxState.Overflow)
        {
            lux -= 30;
            lux = Mathf.Clamp(lux, 0, 100);

            turnLogs.Add("<color=cyan>폭주 반동:</color> 턴 종료 시 LUX -30.");
        }

        if (luxDrainTurnsRemaining > 0)
        {
            lux += 3;
            lux = Mathf.Clamp(lux, 0, 100);
            luxDrainTurnsRemaining--;
            turnLogs.Add("<color=#8fd3ff>럭스 드레인</color> LUX +3");
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
                ApplyPlayerDamage(incoming);
                illegalLoanPenaltyPending = false;
                turnLogs.Add("<color=#8fd3ff>불법 대출:</color> 만기 도달, HP -10");
            }
        }

        if (probabilityManipulationActivatedThisTurn)
        {
            probabilityManipulationActivatedThisTurn = false;
        }
        else if (probabilityManipulationTurnsRemaining > 0)
        {
            probabilityManipulationTurnsRemaining--;
        }

        if (overloadTurnsRemaining > 0)
        {
            overloadTurnsRemaining--;
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

        WriteLog($"Battle victory. Reward +{rewardLux} LUX. Current LUX: {lux}");

        UpdateUI();
        UpdateEnemyDialogue();

        int score = BattleScoreStore.CalculateScore(turn);
        BattleScoreStore.SaveScore(missionScoreId, score);
        PlayerPrefs.SetInt("BattleVictory", 1);
        PlayerPrefs.Save();
        StartResultTransition("\uB2F9\uC2E0\uC740 \uC2B9\uB9AC\uD588\uC5B4\uC694.", "MainScene", score);
    }

private void LoseBattle()
    {
        battleEnded = true;

        int extraLoss = selectedBet;
        lux -= extraLoss;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"Battle defeat. Loss -{extraLoss} LUX. Current LUX: {lux}");

        UpdateUI();
        StartResultTransition("\uB2F9\uC2E0\uC740 \uD328\uBC30\uD588\uC5B4\uC694... \uC548\uD0C0\uAE5D\uB124\uC694.", "MainScene");
    }

    private void StartResultTransition(string message, string sceneName, int score = -1)
    {
        if (resultTransitionStarted)
        {
            return;
        }

        resultTransitionStarted = true;
        StartCoroutine(ResultTransitionRoutine(message, sceneName, score));
    }

private IEnumerator ResultTransitionRoutine(string message, string sceneName, int score)
    {
        GameObject overlayCanvasObject = new GameObject("BattleResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas overlayCanvas = overlayCanvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 10000;

        CanvasScaler scaler = overlayCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(overlayCanvasObject);

        GameObject overlayObject = new GameObject("BattleResultOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(overlayCanvasObject.transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;

        GameObject textObject = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(overlayObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1800f, 240f);

        TextMeshProUGUI resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.text = score >= 0 ? $"{message}\n\uC810\uC218: {score}" : message;
        if (TMP_Settings.defaultFontAsset != null)
        {
            resultText.font = TMP_Settings.defaultFontAsset;
        }

        resultText.fontSize = 74f;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = Color.white;
        resultText.raycastTarget = false;
        resultText.enableWordWrapping = false;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, resultHoldDuration));

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        Destroy(overlayCanvasObject);
    }

    private IEnumerator ShowBattleIntroHint()
    {
        GameObject canvasObject = new GameObject("BattleIntroHintCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bgObject = new GameObject("HintBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObject.transform.SetParent(canvasObject.transform, false);
        RectTransform bgRect = bgObject.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.18f, 0.42f);
        bgRect.anchorMax = new Vector2(0.82f, 0.58f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObject.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.55f);
        bgImage.raycastTarget = false;

        GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bgObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI hintText = textObject.GetComponent<TextMeshProUGUI>();
        hintText.text = "인벤토리에서 카드를 살펴보세요!";
        if (TMP_Settings.defaultFontAsset != null)
        {
            hintText.font = TMP_Settings.defaultFontAsset;
        }

        hintText.fontSize = 52f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = Color.white;
        hintText.raycastTarget = false;

        CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        float blinkOnDuration = 0.12f;
        float blinkOffDuration = 0.10f;
        float holdDuration = 0.22f;
        int blinkCount = 4;

        for (int i = 0; i < blinkCount; i++)
        {
            yield return FadeCanvasGroup(group, 0f, 1f, blinkOnDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return FadeCanvasGroup(group, 1f, 0f, blinkOffDuration);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        Destroy(canvasObject);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            if (group == null) yield break;
            float t = duration <= 0f ? 1f : Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            if (t >= 1f) break;
            yield return null;
        }
    }

    private void SpawnFloatingDelta(TMP_Text anchor, int delta)
    {
        SpawnFloatingDelta(anchor, delta, Vector2.zero);
    }

    private void SpawnFloatingDelta(TMP_Text anchor, int delta, Vector2 extraOffset)
    {
        if (anchor == null || delta == 0) return;
        StartCoroutine(FloatingDeltaRoutine(anchor, delta, extraOffset));
    }

    private IEnumerator FloatingDeltaRoutine(TMP_Text anchor, int delta, Vector2 extraOffset)
    {
        if (anchor == null) yield break;
        Canvas canvas = anchor.canvas;
        if (canvas == null) yield break;

        GameObject obj = new GameObject("FloatingDelta", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        obj.transform.SetParent(canvasRt, false);
        obj.transform.SetAsLastSibling();

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = delta > 0 ? $"+{delta}" : $"{delta}";
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = Mathf.Max(anchor.fontSize * 0.9f, 26f);
        Color baseColor = delta > 0 ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.3f, 0.3f);
        text.color = baseColor;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 60f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, anchor.rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, cam, out Vector2 localPos);
        localPos.x += anchor.rectTransform.rect.width * 0.5f + 8f;
        localPos += extraOffset;
        rt.anchoredPosition = localPos;

        float holdDelay = 1.0f;
        float fadeDuration = 0.9f;
        Vector2 startPos = rt.anchoredPosition;

        yield return new WaitForSecondsRealtime(holdDelay);

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            if (obj == null) yield break;
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / fadeDuration);
            rt.anchoredPosition = startPos;
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(1f, 0f, t));
            if (t >= 1f) break;
            yield return null;
        }

        Destroy(obj);
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
            EnsureLowLuxHandHasLuxCard(count);
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
    EnsureLowLuxHandHasLuxCard(count);
}

private void EnsureLowLuxHandHasLuxCard(int targetHandCount)
{
    if (lux > 10) return;
    if (hand.Any(IsLowLuxGuaranteedLuxCard)) return;

    CardData luxCard = TakeLowLuxGuaranteedLuxCard(drawPile);
    if (luxCard == null)
    {
        luxCard = TakeLowLuxGuaranteedLuxCard(discardPile);
    }

    if (luxCard == null) return;

    if (hand.Count >= targetHandCount)
    {
        int replaceIndex = hand.FindLastIndex(card => card != null && !IsLowLuxGuaranteedLuxCard(card));
        if (replaceIndex >= 0)
        {
            discardPile.Add(hand[replaceIndex]);
            hand.RemoveAt(replaceIndex);
        }
    }

    hand.Add(luxCard);
}

private CardData TakeLowLuxGuaranteedLuxCard(List<CardData> source)
{
    int index = source.FindIndex(card => card != null && IsLowLuxGuaranteedLuxCard(card) && CanDrawCardInCurrentLuxState(card));
    if (index < 0) return null;

    CardData card = source[index];
    source.RemoveAt(index);
    return card;
}

private bool IsLowLuxGuaranteedLuxCard(CardData card)
{
    if (card == null) return false;
    if (card.isJackpot) return false;

    return card.luxGain > 0 || card.specialEffect == SpecialCardEffect.LuxDrain;
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
    EnsureHandPanel();
    if (handPanel == null)
    {
        yield break;
    }

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
        int dPlayerHP = playerHP - _prevPlayerHP;
        int dLux = lux - _prevLux;
        int dEnemyHP = enemyHP - _prevEnemyHP;
        int dEmotion = enemyEmotion - _prevEnemyEmotion;
        int dShield = shield - _prevShield;
        _prevPlayerHP = playerHP;
        _prevLux = lux;
        _prevEnemyHP = enemyHP;
        _prevEnemyEmotion = enemyEmotion;
        _prevShield = shield;

        if (playerHPText != null)
        {
            playerHPText.text = $"HP {playerHP}/{playerMaxHP}";
            if (dPlayerHP != 0) SpawnFloatingDelta(playerHPText, dPlayerHP);
        }

        if (shieldText != null)
        {
            shieldText.gameObject.SetActive(shield > 0);
            shieldText.text = $"SHIELD {shield}";
            if (dShield != 0) SpawnFloatingDelta(shieldText, dShield);
        }

        // 이름
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        // HP Bar
        if (playerHPBar != null)
        {
            EnsureSliderFillVisible(playerHPBar);
            playerHPBar.maxValue = playerMaxHP;
            playerHPBar.value = playerHP;
        }


        if (luxText != null)
        {
            luxText.text = $"LUX {lux}/100";
            if (dLux != 0) SpawnFloatingDelta(luxText, dLux, luxFloatingDeltaOffset);
        }

        if (luxBar != null)
        {
            EnsureSliderFillVisible(luxBar);
            luxBar.minValue = 0f;
            luxBar.maxValue = 100f;
            luxBar.SetValueWithoutNotify(lux);
            UpdateLuxBarColor();
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
        }

        if (enemyHPText != null)
        {
            enemyHPText.text = $"HP {enemyHP}/{enemyMaxHP}";
            if (dEnemyHP != 0) SpawnFloatingDelta(enemyHPText, dEnemyHP);
        }

        if (enemyHPBar != null)
        {
            EnsureSliderFillVisible(enemyHPBar);
            enemyHPBar.value = (float)enemyHP / enemyMaxHP;
        }

        if (emotionText != null)
        {
            emotionText.text = enemyRaged ? $"분노 {enemyEmotion}/{maxEmotion} - 분노 상태" : $"분노 {enemyEmotion}/{maxEmotion}";
            if (dEmotion != 0) SpawnFloatingDelta(emotionText, dEmotion);
        }

        if (emotionBar != null)
        {
            EnsureSliderFillVisible(emotionBar);
            emotionBar.value = (float)enemyEmotion / maxEmotion;
        }

        if (turnText != null)
        {
            turnText.text = $"턴 {turn}";
        }

        UpdateLuxState();
        //UpdateNegotiationButton();
        UpdateBettingUI();
        CheckEnemyRage();
    }

    private void UpdateLuxState()
    {
        if (luxStateText == null) return;

        LuxState state = GetLuxState();

        if (state == LuxState.Poverty)
        {
            luxStateText.text = "상태: 불운";
        }
        else if (state == LuxState.Normal)
        {
            luxStateText.text = "상태: 보통";
        }
        else if (state == LuxState.Lucky)
        {
            luxStateText.text = "상태: 행운";
        }
        else
        {
            luxStateText.text = "상태: 폭주";
        }
    }

    private void UpdateLuxBarColor()
    {
        if (luxBar == null || luxBar.fillRect == null) return;

        Image fillImage = luxBar.fillRect.GetComponent<Image>();
        if (fillImage == null) return;

        switch (GetLuxState())
        {
            case LuxState.Poverty:
                fillImage.color = povertyLuxBarColor;
                break;
            case LuxState.Normal:
                fillImage.color = normalLuxBarColor;
                break;
            case LuxState.Lucky:
                fillImage.color = luckyLuxBarColor;
                break;
            case LuxState.Overflow:
                fillImage.color = overflowLuxBarColor;
                break;
        }
    }

    private void EnsureSliderFillVisible(Slider slider)
    {
        if (slider == null || slider.fillRect == null)
        {
            return;
        }

        Transform current = slider.fillRect;
        while (current != null && current != slider.transform)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
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
            enemyDialogueText.text = "...";
        }
        else if (hpRate <= 0.15f)
        {
            enemyDialogueText.text = "하하.. 하하하... 악마같은 사람.";
        }
        else if (hpRate <= 0.30f)
        {
            enemyDialogueText.text = "“...절 죽일 셈이군요.”";
        }
        else if (hpRate <= 0.45f)
        {
            enemyDialogueText.text = "“안타깝게도 전 물러서지 않을 겁니다.”";
        }
        else if (hpRate <= 0.60f)
        {
            enemyDialogueText.text = "“할머니가 무슨 죄를 지었다고 이러는 겁니까.”";
        }
        else if (hpRate <= 0.80f)
        {
            enemyDialogueText.text = "“당신들 때문에 지하 구역 시민들은 죽어나가고 있습니다.”";
        }
        else
        {
            enemyDialogueText.text = "“전 이미 각오했습니다. 덤벼요.”";
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
