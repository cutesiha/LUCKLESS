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
    public float playerHitEffectDuration = 0.22f;
    public float playerHitShakeStrength = 14f;
    public float playerHitHorizontalShakeFrequency = 85f;
    public Color playerHitFlashColor = new Color(1f, 0f, 0f, 0.45f);

    [Header("Hit Voice")]
    public AudioClip playerHitClip;
    public AudioClip enemyHitClip;
    [Range(0f, 3f)] public float hitSoundVolume = 1f;
    public AudioClip cardSelectClip;
    [Range(0f, 3f)] public float cardSelectSoundVolume = 1f;
    [Header("Card Drag Sound")]
    public AudioClip cardDragClip;
    [Range(0f, 3f)] public float cardDragSoundVolume = 1f;
    [Header("End Turn Sound")]
    public AudioClip endTurnClip;
    [Range(0f, 3f)] public float endTurnSoundVolume = 1f;

    [Header("No Damage Card Feedback")]
    public AudioClip noDamageCardClip;
    [Range(0f, 3f)] public float noDamageCardSoundVolume = 1f;
    public Camera noDamageCardShakeCamera;
    public RectTransform noDamageCardShakeTarget;
    public float noDamageCardShakeDuration = 0.22f;
    public float noDamageCardShakeDistance = 10f;

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

    [Header("Enemy Speech Lines")]
    [SerializeField] private string speechIdle = "";
    [SerializeField] private string speech80 = "";
    [SerializeField] private string speech60 = "";
    [SerializeField] private string speech45 = "";
    [SerializeField] private string speech30 = "";
    [SerializeField] private string speech15 = "";
    [SerializeField] private string speechDead = "";
    [SerializeField] private string speechDefeat = "";
    [SerializeField] private string speechVictory = "";

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
    [SerializeField] private string victorySceneName = "Victory2Scene";
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
    private TMP_Text tooltipCostText;
    private Image tooltipTailImage;
    private Coroutine tooltipAnimationRoutine;
    private CanvasGroup tooltipCanvasGroup;
    private Vector2 tooltipShownPosition;
    private Vector2 tooltipTailShownPosition;
    private const float TooltipSlideDistance = 28f;
    private const float TooltipOvershootDistance = 10f;
    private const float TooltipEnterDuration = 0.18f;
    private const float TooltipSettleDuration = 0.10f;
    private const float TooltipExitDuration = 0.18f;
    private Image playerHPPreviewOverlay;
    private Image luxPreviewOverlay;
    private Image enemyHPPreviewOverlay;
    private Image emotionPreviewOverlay;

    public bool reflectNextDamage = false;
    private bool zeroDamageThisTurn = false;
    private bool grantDoubleDamageNextTurn = false;
    private float currentTurnDamageMultiplier = 1f;
    private bool heartbeatAttackBonusActive = false;
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
    private Coroutine noDamageCardFeedbackRoutine;
    private Vector3 noDamageCameraOriginalPosition;
    private Vector2 noDamageTargetOriginalPosition;
    private Vector2 playerHitOriginalPosition;
    private bool isCardResolving = false;
    private int currentCardLuxCost = 0;
    private int currentCardLuxGain = 0;
    private int currentCardPlayerDamageTaken = 0;
    private bool suppressCardBattleLog = false;
    private bool resultTransitionStarted = false;
    private readonly List<FailedGambleRecord> failedGambleRecordsThisTurn = new List<FailedGambleRecord>();
    private int _prevPlayerHP;
    private int _prevLux;
    private int _prevEnemyHP;
    private int _prevEnemyEmotion;
    private int _prevShield;
    private Coroutine playerHPUiRoutine;
    private Coroutine luxUiRoutine;
    private Coroutine enemyHPUiRoutine;
    private Coroutine emotionUiRoutine;

    private static readonly Color CyberBg = new Color(0.051f, 0.051f, 0.051f, 1f);
    private static readonly Color CyberPanel = new Color(0.102f, 0.102f, 0.102f, 1f);
    private static readonly Color CyberPanelLight = new Color(0.165f, 0.165f, 0.165f, 1f);
    private static readonly Color CyberPink = new Color(1f, 0.431f, 0.78f, 1f);
    private static readonly Color CyberRed = new Color(1f, 0.267f, 0.267f, 1f);
    private static readonly Color CyberText = new Color(0.94f, 0.94f, 0.94f, 1f);
    private static readonly Color CyberMuted = new Color(0.62f, 0.62f, 0.62f, 1f);

    private struct FailedGambleRecord
    {
        public int chance;
        public int successDamage;
    }

    private struct CardUsePreview
    {
        public int enemyDamage;
        public int enemyEmotionDelta;
        public int playerHpDelta;
        public int luxDelta;
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
        ShowCardTooltip(card, null);
    }

    public void ShowCardTooltip(CardData card, RectTransform cardTransform)
    {
        if (card == null) return;

        EnsureBattleTooltip();
        if (tooltipPanel == null) return;

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling();
        if (tooltipTailImage != null)
        {
            tooltipTailImage.gameObject.SetActive(true);
        }

        if (tooltipNameText != null)
        {
            tooltipNameText.text = card.cardName;
        }

        if (tooltipCostText != null)
        {
            tooltipCostText.text = $"Lux Cost: {GetEffectiveLuxCost(card)}";
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.richText = true;
            tooltipDescriptionText.text = string.IsNullOrWhiteSpace(card.description) ? "No description." : FormatCardDescription(card.description);
        }

        ApplyTooltipTextSizing(card);
        PositionTooltipAboveCard(cardTransform);
        PlayTooltipAnimation(true);
    }


    private void ApplyTooltipTextSizing(CardData card)
    {
        int descriptionLength = 0;
        if (card != null && !string.IsNullOrWhiteSpace(card.description))
        {
            descriptionLength = Regex.Replace(card.description, "<.*?>", string.Empty).Trim().Length;
        }

        float descriptionSize = 29f;
        if (descriptionLength <= 45)
        {
            descriptionSize = 35f;
        }
        else if (descriptionLength <= 90)
        {
            descriptionSize = 32f;
        }

        if (tooltipNameText != null)
        {
            tooltipNameText.fontSize = descriptionLength <= 45 ? 33f : 32f;
        }

        if (tooltipCostText != null)
        {
            tooltipCostText.fontSize = descriptionLength <= 45 ? 27f : 26f;
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.fontSize = descriptionSize;
        }
    }


    public void HideCardTooltip()
    {
        if (tooltipPanel == null)
        {
            return;
        }

        PlayTooltipAnimation(false);
    }


    public void UpdateDraggedCardTargetPreview(CardData card, Vector2 screenPosition)
    {
        if (card == null || !IsScreenPointOverEnemy(screenPosition) || !CanUseCardSilently(card))
        {
            ClearDraggedCardPreview();
            return;
        }

        CardUsePreview preview = BuildCardUsePreview(card);
        int previewEnemyHP = Mathf.Clamp(enemyHP - preview.enemyDamage, 0, enemyMaxHP);
        int previewEmotion = Mathf.Clamp(enemyEmotion + preview.enemyEmotionDelta, 0, maxEmotion);
        int previewPlayerHP = Mathf.Clamp(playerHP + preview.playerHpDelta, 0, playerMaxHP);
        int previewLux = Mathf.Clamp(lux + preview.luxDelta, 0, 100);

        ShowSliderPreview(enemyHPBar, enemyMaxHP <= 0 ? 0f : (float)enemyHP / enemyMaxHP, enemyMaxHP <= 0 ? 0f : (float)previewEnemyHP / enemyMaxHP, CyberRed, ref enemyHPPreviewOverlay, "EnemyHPPreview");
        ShowSliderPreview(emotionBar, maxEmotion <= 0 ? 0f : (float)enemyEmotion / maxEmotion, maxEmotion <= 0 ? 0f : (float)previewEmotion / maxEmotion, CyberRed, ref emotionPreviewOverlay, "EnemyRagePreview");
        ShowSliderPreview(playerHPBar, playerMaxHP <= 0 ? 0f : (float)playerHP / playerMaxHP, playerMaxHP <= 0 ? 0f : (float)previewPlayerHP / playerMaxHP, preview.playerHpDelta < 0 ? CyberRed : CyberPink, ref playerHPPreviewOverlay, "PlayerHPPreview");
        ShowSliderPreview(luxBar, lux / 100f, previewLux / 100f, preview.luxDelta < 0 ? CyberRed : CyberPink, ref luxPreviewOverlay, "LuxPreview");
    }

    public void ClearDraggedCardPreview()
    {
        HideSliderPreview(playerHPPreviewOverlay);
        HideSliderPreview(luxPreviewOverlay);
        HideSliderPreview(enemyHPPreviewOverlay);
        HideSliderPreview(emotionPreviewOverlay);
    }

    public bool TryUseDraggedCardOnEnemy(CardData card, Vector2 screenPosition)
    {
        ClearDraggedCardPreview();

        if (card == null || IsScreenPointOverHand(screenPosition) || !IsScreenPointOverEnemy(screenPosition))
        {
            return false;
        }

        if (!CanUseCard(card))
        {
            return false;
        }

        PlayCardSelectSound();
        UseCard(card);
        return true;
    }

    private bool IsScreenPointOverEnemy(Vector2 screenPosition)
    {
        RectTransform target = null;
        if (enemyCharacterImage != null && enemyCharacterImage.gameObject.activeInHierarchy)
        {
            target = enemyCharacterImage.rectTransform;
        }
        else if (enemyHPBar != null)
        {
            target = enemyHPBar.transform as RectTransform;
        }

        if (target == null)
        {
            return false;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(target, screenPosition, camera);
    }

public bool IsScreenPointOverHand(Vector2 screenPosition)
    {
        if (handPanel == null)
        {
            return false;
        }

        RectTransform handRect = handPanel as RectTransform;
        if (handRect == null)
        {
            return false;
        }

        Canvas canvas = handRect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(handRect, screenPosition, camera);
    }


    private bool CanUseCardSilently(CardData card)
    {
        if (card == null || !battleStarted || battleEnded || isCardResolving || playerStunned)
        {
            return false;
        }

        if (lux < GetEffectiveLuxCost(card))
        {
            return false;
        }

        CardType effectiveType = GetEffectiveCardType(card);
        LuxState state = GetLuxState();
        if (effectiveType == CardType.House && state != LuxState.Lucky && state != LuxState.Overflow)
        {
            return false;
        }

        if (effectiveType == CardType.Poverty && state != LuxState.Poverty)
        {
            return false;
        }

        return true;
    }

    private CardUsePreview BuildCardUsePreview(CardData card)
    {
        CardUsePreview preview = new CardUsePreview();
        int effectiveCost = GetEffectiveLuxCost(card);
        preview.luxDelta = -effectiveCost + CalculateLuxGain(card);
        preview.enemyEmotionDelta = card.emotionGain;
        preview.enemyDamage = EstimateCardDamagePreview(card);

        if (!IsCardTreatedAsGamble(card) && card.selfDamage > 0)
        {
            preview.playerHpDelta -= ModifyIncomingDamage(card.selfDamage);
        }

        if (card.healAmount > 0)
        {
            preview.playerHpDelta += card.healAmount;
        }

        switch (card.specialEffect)
        {
            case SpecialCardEffect.BeastHeart:
                preview.playerHpDelta -= Mathf.Max(0, playerHP - 1);
                break;
            case SpecialCardEffect.ReverseOdds:
                preview.enemyDamage += failedGambleCountThisTurn * 10;
                break;
            case SpecialCardEffect.FakeLuck:
                preview.enemyEmotionDelta += 20;
                break;
            case SpecialCardEffect.BankruptcyDeclaration:
                preview.luxDelta = -lux;
                break;
        }

        return preview;
    }

    private int EstimateCardDamagePreview(CardData card)
    {
        if (card == null)
        {
            return 0;
        }

        int damage;
        switch (card.specialEffect)
        {
            case SpecialCardEffect.BeastHeart:
                return 0;
            case SpecialCardEffect.CoinTriple:
                damage = 40;
                break;
            case SpecialCardEffect.DoubleDice:
                damage = 25;
                break;
            case SpecialCardEffect.BankruptcyDeclaration:
                damage = Mathf.RoundToInt(Mathf.Clamp(lux, 0, 100) * 2f);
                break;
            case SpecialCardEffect.Lucky7777:
                damage = 14;
                break;
            default:
                damage = card.damage;
                break;
        }

        if (card.specialEffect == SpecialCardEffect.LightBilling)
        {
            damage = 0;
        }

        if (!card.canOnlyUseInPoverty)
        {
            LuxState state = GetLuxState();
            if (damage > 0 && state == LuxState.Lucky)
            {
                damage += 4;
            }
            else if (damage > 0 && state == LuxState.Overflow)
            {
                damage *= 2;
            }
        }

        if (heartbeatAttackBonusActive && playerHP <= Mathf.FloorToInt(playerMaxHP * 0.5f) && damage > 0)
        {
            damage += 5;
        }

        if (zeroDamageThisTurn)
        {
            damage = 0;
        }
        else if (currentTurnDamageMultiplier > 1f && damage > 0)
        {
            damage = Mathf.RoundToInt(damage * currentTurnDamageMultiplier);
        }

        if (enemyRaged && damage > 0 && GetEffectiveCardType(card) != CardType.Deal)
        {
            damage = Mathf.Max(0, damage - rageAttackReduction);
        }

        return Mathf.Max(0, damage);
    }

private void ShowSliderPreview(Slider slider, float currentValue, float previewValue, Color color, ref Image overlay, string overlayName)
    {
        bool increasing = previewValue > currentValue;
        Color overlayColor = increasing
            ? new Color(0.48f, 0.16f, 0.34f, 0.92f)
            : new Color(1f, 0.94f, 0.98f, 0.70f);
        ShowSliderPreviewSegment(slider, currentValue, previewValue, overlayColor, ref overlay, overlayName, !increasing);
    }

private void ShowSliderPreviewSegment(Slider slider, float startValue, float endValue, Color overlayColor, ref Image overlay, string overlayName, bool showOutline = false)
    {
        if (slider == null)
        {
            return;
        }

        startValue = Mathf.Clamp01(startValue);
        endValue = Mathf.Clamp01(endValue);
        if (Mathf.Approximately(startValue, endValue))
        {
            HideSliderPreview(overlay);
            return;
        }

        RectTransform previewParent = slider.fillRect != null && slider.fillRect.parent is RectTransform fillParent
            ? fillParent
            : slider.transform as RectTransform;
        if (previewParent == null)
        {
            return;
        }

        if (overlay == null)
        {
            GameObject overlayObject = new GameObject(overlayName, typeof(RectTransform), typeof(Image), typeof(Outline));
            overlay = overlayObject.GetComponent<Image>();
            overlay.raycastTarget = false;
        }

        if (overlay.transform.parent != previewParent)
        {
            overlay.transform.SetParent(previewParent, false);
        }

        overlay.gameObject.SetActive(true);
        overlay.transform.SetAsLastSibling();
        overlay.color = overlayColor;

        Outline outline = overlay.GetComponent<Outline>();
        if (outline == null)
        {
            outline = overlay.gameObject.AddComponent<Outline>();
        }
        outline.enabled = showOutline;
        outline.effectColor = new Color(0.46f, 0.05f, 0.09f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = false;

        RectTransform rectTransform = overlay.rectTransform;
        rectTransform.anchorMin = new Vector2(Mathf.Min(startValue, endValue), 0f);
        rectTransform.anchorMax = new Vector2(Mathf.Max(startValue, endValue), 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }


    private void HideSliderPreview(Image overlay)
    {
        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);
        }
    }

private void ShowAnimatedSliderPreview(Slider slider, int from, int to, int shown, int max, bool sliderUsesNormalizedValue)
    {
        if (slider == null || from == to)
        {
            return;
        }

        float fromValue = max <= 0 ? 0f : (float)from / max;
        float toValue = max <= 0 ? 0f : (float)to / max;
        float shownValue = max <= 0 ? 0f : (float)shown / max;

        bool increasing = to > from;
        float startValue = increasing ? shownValue : shownValue;
        float endValue = increasing ? toValue : fromValue;
        Color overlayColor = increasing
            ? new Color(0.48f, 0.16f, 0.34f, 0.92f)
            : new Color(0.92f, 0.25f, 0.30f, 0.64f);

        if (slider == playerHPBar)
        {
            ShowSliderPreviewSegment(slider, startValue, endValue, overlayColor, ref playerHPPreviewOverlay, "PlayerHPChangePreview", !increasing);
        }
        else if (slider == luxBar)
        {
            ShowSliderPreviewSegment(slider, startValue, endValue, overlayColor, ref luxPreviewOverlay, "LuxChangePreview", !increasing);
        }
        else if (slider == enemyHPBar)
        {
            ShowSliderPreviewSegment(slider, startValue, endValue, overlayColor, ref enemyHPPreviewOverlay, "EnemyHPChangePreview", !increasing);
        }
        else if (slider == emotionBar)
        {
            ShowSliderPreviewSegment(slider, startValue, endValue, overlayColor, ref emotionPreviewOverlay, "EmotionChangePreview", !increasing);
        }
    }

    private void HideAnimatedSliderPreview(Slider slider)
    {
        if (slider == playerHPBar)
        {
            HideSliderPreview(playerHPPreviewOverlay);
        }
        else if (slider == luxBar)
        {
            HideSliderPreview(luxPreviewOverlay);
        }
        else if (slider == enemyHPBar)
        {
            HideSliderPreview(enemyHPPreviewOverlay);
        }
        else if (slider == emotionBar)
        {
            HideSliderPreview(emotionPreviewOverlay);
        }
    }


    private void EnsureBattleTooltip()
    {
        Transform parent = battlePanel != null ? battlePanel.transform : transform;

        if (tooltipPanel == null)
        {
            tooltipPanel = new GameObject("CardTooltip", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
            tooltipPanel.transform.SetParent(parent, false);
        }

        tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipCanvasGroup.interactable = false;

        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.sizeDelta = new Vector2(430f, 250f);
        }

        Image bg = tooltipPanel.GetComponent<Image>();
        if (bg == null)
        {
            bg = tooltipPanel.AddComponent<Image>();
        }
        bg.color = new Color(0.067f, 0.067f, 0.067f, 0.96f);
        bg.raycastTarget = false;

        Outline outline = tooltipPanel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = tooltipPanel.AddComponent<Outline>();
        }
        outline.effectColor = CyberPink;
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = tooltipPanel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        }
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = tooltipPanel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        tooltipNameText = EnsureTooltipText("TooltipName", tooltipNameText, 31f, Color.white, FontStyles.Bold);
        tooltipCostText = EnsureTooltipText("TooltipCost", tooltipCostText, 25f, CyberPink, FontStyles.Normal);
        tooltipDescriptionText = EnsureTooltipText("TooltipDescription", tooltipDescriptionText, 29f, new Color(0.86f, 0.86f, 0.86f, 1f), FontStyles.Normal);

        if (tooltipTailImage == null)
        {
            Transform oldTail = tooltipPanel.transform.Find("TooltipTail");
            if (oldTail != null)
            {
                tooltipTailImage = oldTail.GetComponent<Image>();
            }
        }

        if (tooltipTailImage == null)
        {
            GameObject tail = new GameObject("TooltipTail", typeof(RectTransform), typeof(Image));
            tail.transform.SetParent(parent, false);
            tooltipTailImage = tail.GetComponent<Image>();
        }

        tooltipTailImage.color = CyberPink;
        tooltipTailImage.raycastTarget = false;
        RectTransform tailRect = tooltipTailImage.rectTransform;
        tailRect.sizeDelta = new Vector2(14f, 14f);
        tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }


    private TMP_Text EnsureTooltipText(string objectName, TMP_Text current, float fontSize, Color color, FontStyles style)
    {
        if (current == null && tooltipPanel != null)
        {
            Transform existing = tooltipPanel.transform.Find(objectName);
            if (existing != null)
            {
                current = existing.GetComponent<TMP_Text>();
            }
        }

        if (current == null && tooltipPanel != null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(tooltipPanel.transform, false);
            current = textObject.GetComponent<TMP_Text>();
        }

        if (current != null)
        {
            current.fontSize = fontSize;
            current.color = color;
            current.fontStyle = style;
            current.alignment = TextAlignmentOptions.Left;
            current.textWrappingMode = TextWrappingModes.Normal;
            current.overflowMode = TextOverflowModes.Overflow;
            current.raycastTarget = false;

            RectTransform rectTransform = current.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0f, 1f);

            LayoutElement layoutElement = current.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = objectName == "TooltipDescription" ? 132f : 38f;
                layoutElement.minHeight = objectName == "TooltipDescription" ? 108f : 34f;
                layoutElement.flexibleHeight = 0f;
            }
        }

        return current;
    }


    private void PositionTooltipAboveCard(RectTransform cardTransform)
    {
        if (tooltipPanel == null || cardTransform == null)
        {
            return;
        }

        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        RectTransform parentRect = tooltipPanel.transform.parent as RectTransform;
        if (tooltipRect == null || parentRect == null)
        {
            return;
        }

        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 worldPoint = cardTransform.TransformPoint(new Vector3(0f, cardTransform.rect.yMax + 18f, 0f));
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out Vector2 localPoint))
        {
            tooltipShownPosition = localPoint;
            tooltipRect.anchoredPosition = localPoint;
            if (tooltipTailImage != null)
            {
                tooltipTailImage.transform.SetParent(parentRect, false);
                tooltipTailImage.transform.SetAsLastSibling();
                RectTransform tailRect = tooltipTailImage.rectTransform;
                tailRect.anchorMin = new Vector2(0.5f, 0.5f);
                tailRect.anchorMax = new Vector2(0.5f, 0.5f);
                tailRect.pivot = new Vector2(0.5f, 0.5f);
                tooltipTailShownPosition = localPoint + new Vector2(0f, -2f);
                tailRect.anchoredPosition = tooltipTailShownPosition;
            }
        }
    }

    private void PlayTooltipAnimation(bool show)
    {
        EnsureBattleTooltip();
        if (tooltipPanel == null)
        {
            return;
        }

        if (tooltipAnimationRoutine != null)
        {
            StopCoroutine(tooltipAnimationRoutine);
        }

        tooltipAnimationRoutine = StartCoroutine(TooltipAnimationRoutine(show));
    }

    private IEnumerator TooltipAnimationRoutine(bool show)
    {
        RectTransform tooltipRect = tooltipPanel != null ? tooltipPanel.GetComponent<RectTransform>() : null;
        RectTransform tailRect = tooltipTailImage != null ? tooltipTailImage.rectTransform : null;
        if (tooltipRect == null)
        {
            yield break;
        }

        if (tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        }

        if (show)
        {
            tooltipPanel.SetActive(true);
            if (tooltipTailImage != null)
            {
                tooltipTailImage.gameObject.SetActive(true);
            }
        }

        Vector2 start = tooltipRect.anchoredPosition;
        Vector2 target = tooltipShownPosition;
        Vector2 hidden = target + new Vector2(0f, -TooltipSlideDistance);
        Vector2 overshoot = target + new Vector2(0f, TooltipOvershootDistance);
        Vector2 tailStart = tailRect != null ? tailRect.anchoredPosition : Vector2.zero;
        Vector2 tailTarget = tooltipTailShownPosition;
        Vector2 tailHidden = tailTarget + new Vector2(0f, -TooltipSlideDistance);
        Vector2 tailOvershoot = tailTarget + new Vector2(0f, TooltipOvershootDistance);

        if (show)
        {
            tooltipRect.anchoredPosition = hidden;
            if (tailRect != null) tailRect.anchoredPosition = tailHidden;
            if (tooltipCanvasGroup != null) tooltipCanvasGroup.alpha = 0f;
            SetTooltipTailAlpha(0f);

            yield return AnimateTooltipSegment(hidden, overshoot, tailHidden, tailOvershoot, 0f, 1f, TooltipEnterDuration);
            yield return AnimateTooltipSegment(overshoot, target, tailOvershoot, tailTarget, 1f, 1f, TooltipSettleDuration);
        }
        else
        {
            if (tooltipCanvasGroup != null) tooltipCanvasGroup.alpha = 1f;
            SetTooltipTailAlpha(1f);

            yield return AnimateTooltipSegment(start, overshoot, tailStart, tailOvershoot, 1f, 1f, TooltipSettleDuration);
            yield return AnimateTooltipSegment(overshoot, hidden, tailOvershoot, tailHidden, 1f, 0f, TooltipExitDuration);

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
            if (tooltipTailImage != null)
            {
                tooltipTailImage.gameObject.SetActive(false);
            }
        }

        tooltipAnimationRoutine = null;
    }

    private IEnumerator AnimateTooltipSegment(Vector2 from, Vector2 to, Vector2 tailFrom, Vector2 tailTo, float alphaFrom, float alphaTo, float duration)
    {
        RectTransform tooltipRect = tooltipPanel != null ? tooltipPanel.GetComponent<RectTransform>() : null;
        RectTransform tailRect = tooltipTailImage != null ? tooltipTailImage.rectTransform : null;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            if (tooltipRect == null)
            {
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            tooltipRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            if (tailRect != null)
            {
                tailRect.anchoredPosition = Vector2.LerpUnclamped(tailFrom, tailTo, eased);
            }

            float alpha = Mathf.Lerp(alphaFrom, alphaTo, eased);
            if (tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha = alpha;
            }
            SetTooltipTailAlpha(alpha);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private void SetTooltipTailAlpha(float alpha)
    {
        if (tooltipTailImage == null)
        {
            return;
        }

        Color color = tooltipTailImage.color;
        color.a = alpha;
        tooltipTailImage.color = color;
    }


    private void Start()
    {
        battleEnded = false;
        battleStarted = true;
        selectedBet = 0;
        rewardLux = 0;
        heartbeatAttackBonusActive = false;
        missionScoreId = PlayerPrefs.GetString(BattleScoreStore.ActiveBattleMissionKey, missionScoreId);
        PlayerPrefs.DeleteKey(BattleScoreStore.ActiveBattleMissionKey);
        if (string.IsNullOrWhiteSpace(missionScoreId))
        {
            missionScoreId = BattleScoreStore.DefaultMissionId;
        }

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
        // Preserve the scene-authored battle layout at runtime.
        EnsureEndTurnButtonHoverEffect();
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

    private void EnsureEndTurnButtonHoverEffect()
    {
        GameObject endTurnButtonObject = GameObject.Find("EndTurnButton");
        if (endTurnButtonObject == null)
        {
            return;
        }

        Button button = endTurnButtonObject.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
        }

        if (endTurnButtonObject.GetComponent<BattleHoverEffect>() == null)
        {
            endTurnButtonObject.AddComponent<BattleHoverEffect>();
        }
    }

    private void ApplyBattleCyberLayout()
    {
        if (battlePanel == null)
        {
            return;
        }

        Image rootImage = battlePanel.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = battlePanel.AddComponent<Image>();
        }
        rootImage.color = CyberBg;
        rootImage.raycastTarget = false;

        StyleBattleText(battleLogText, 15f, CyberText, TextAlignmentOptions.TopLeft);
        SetRect(battleLogText, new Vector2(0f, 1f), new Vector2(0.52f, 1f), new Vector2(0f, 1f), new Vector2(22f, -118f), new Vector2(-16f, -18f));
        StyleTextParentPanel(battleLogText, CyberPanel);

        StyleBattleText(enemyNameText, 22f, CyberText, TextAlignmentOptions.Right);
        SetRect(enemyNameText, new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-24f, -18f));

        StyleBattleText(turnText, 14f, CyberMuted, TextAlignmentOptions.Right);
        SetRect(turnText, new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), new Vector2(-24f, -50f));

        StyleBattleText(enemyHPText, 13f, CyberMuted, TextAlignmentOptions.Right);
        SetRect(enemyHPText, new Vector2(0.68f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -98f), new Vector2(-24f, -78f));
        StyleSlider(enemyHPBar, CyberPink);
        SetRect(enemyHPBar, new Vector2(0.74f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -112f), new Vector2(-24f, -104f));

        StyleBattleText(emotionText, 13f, CyberMuted, TextAlignmentOptions.Right);
        SetRect(emotionText, new Vector2(0.68f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -136f), new Vector2(-24f, -116f));
        StyleSlider(emotionBar, CyberRed);
        SetRect(emotionBar, new Vector2(0.74f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(-24f, -142f));

        if (enemyCharacterImage != null)
        {
            enemyCharacterImage.color = Color.white;
            enemyCharacterImage.preserveAspect = true;
            RectTransform enemyRt = enemyCharacterImage.rectTransform;
            enemyRt.anchorMin = new Vector2(0.5f, 0.5f);
            enemyRt.anchorMax = new Vector2(0.5f, 0.5f);
            enemyRt.pivot = new Vector2(0.5f, 0.5f);
            enemyRt.anchoredPosition = new Vector2(0f, 92f);
            enemyRt.sizeDelta = new Vector2(360f, 360f);
        }

        StyleBattleText(enemyDialogueText, 18f, Color.black, TextAlignmentOptions.Center);
        SetRect(enemyDialogueText, new Vector2(0.62f, 0.48f), new Vector2(0.88f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        StyleTextParentPanel(enemyDialogueText, Color.white);

        EnsureCyberPlayerPortrait();
        StyleBattleText(playerNameText, 15f, CyberMuted, TextAlignmentOptions.Left);
        SetRect(playerNameText, new Vector2(0f, 0f), new Vector2(0.28f, 0f), new Vector2(0f, 0f), new Vector2(120f, 244f), new Vector2(0f, 270f));

        StyleBattleText(playerHPText, 14f, CyberText, TextAlignmentOptions.Left);
        SetRect(playerHPText, new Vector2(0f, 0f), new Vector2(0.44f, 0f), new Vector2(0f, 0f), new Vector2(120f, 206f), new Vector2(0f, 228f));
        StyleSlider(playerHPBar, CyberPink);
        SetRect(playerHPBar, new Vector2(0f, 0f), new Vector2(0.44f, 0f), new Vector2(0f, 0f), new Vector2(120f, 188f), new Vector2(0f, 198f));

        StyleBattleText(luxText, 14f, CyberText, TextAlignmentOptions.Left);
        SetRect(luxText, new Vector2(0f, 0f), new Vector2(0.44f, 0f), new Vector2(0f, 0f), new Vector2(120f, 158f), new Vector2(0f, 180f));
        StyleSlider(luxBar, CyberPink);
        SetRect(luxBar, new Vector2(0f, 0f), new Vector2(0.44f, 0f), new Vector2(0f, 0f), new Vector2(120f, 140f), new Vector2(0f, 150f));

        StyleBattleText(luxStateText, 13f, CyberMuted, TextAlignmentOptions.Left);
        SetRect(luxStateText, new Vector2(0f, 0f), new Vector2(0.44f, 0f), new Vector2(0f, 0f), new Vector2(120f, 112f), new Vector2(0f, 134f));

        EnsureHandPanel();
        ConfigureHandPanelForCyberLayout();
        ConfigureEndTurnButtonForCyberLayout();
        EnsureBattleTooltip();
        HideCardTooltip();
    }

    private void EnsureCyberPlayerPortrait()
    {
        Transform existing = battlePanel.transform.Find("CyberPlayerPortrait");
        GameObject portrait = existing != null ? existing.gameObject : new GameObject("CyberPlayerPortrait", typeof(RectTransform), typeof(Image), typeof(Outline));
        portrait.transform.SetParent(battlePanel.transform, false);

        RectTransform rt = portrait.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(24f, 146f);
        rt.sizeDelta = new Vector2(82f, 96f);

        Image image = portrait.GetComponent<Image>();
        image.color = CyberPanel;
        image.raycastTarget = false;
        portrait.GetComponent<Outline>().effectColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        Transform labelTransform = portrait.transform.Find("Name");
        TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (label == null)
        {
            GameObject labelObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(portrait.transform, false);
            label = labelObject.GetComponent<TMP_Text>();
        }

        label.text = "ZERO";
        label.fontSize = 12f;
        label.color = CyberMuted;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        SetStretch(label.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -62f));
    }

    private void ConfigureHandPanelForCyberLayout()
    {
        if (handPanel == null)
        {
            return;
        }

        RectTransform rt = handPanel as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.78f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(120f, 22f);
            rt.sizeDelta = new Vector2(0f, 160f);
        }

        HorizontalLayoutGroup layout = handPanel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    private void ConfigureEndTurnButtonForCyberLayout()
    {
        GameObject endTurnButtonObject = GameObject.Find("EndTurnButton");
        if (endTurnButtonObject == null)
        {
            return;
        }

        RectTransform rt = endTurnButtonObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-28f, 28f);
            rt.sizeDelta = new Vector2(170f, 54f);
        }

        Image image = endTurnButtonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = CyberPanel;
        }

        TMP_Text text = endTurnButtonObject.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "턴 종료";
            text.fontSize = 20f;
            text.color = CyberText;
            text.alignment = TextAlignmentOptions.Center;
        }
    }

    private void StyleBattleText(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private void StyleTextParentPanel(TMP_Text text, Color color)
    {
        if (text == null || text.transform.parent == null)
        {
            return;
        }

        Image image = text.transform.parent.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void StyleSlider(Slider slider, Color fillColor)
    {
        if (slider == null)
        {
            return;
        }

        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = fillColor;
            }
        }

        Image[] images = slider.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (slider.fillRect != null && images[i].transform == slider.fillRect)
            {
                continue;
            }

            images[i].color = CyberPanelLight;
        }

        slider.transition = Selectable.Transition.None;
    }

    private void SetRect(Component component, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (component == null)
        {
            return;
        }

        RectTransform rt = component.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private void SetStretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
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
            CheckPlayerDefeat();
        }

        return actualDamage;
    }

private void CheckPlayerDefeat()
    {
        if (!battleEnded && playerHP <= 0)
        {
            LoseBattle();
        }
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

public void PlayCardDragSound()
    {
        AudioClip clip = cardDragClip != null ? cardDragClip : cardSelectClip;
        float volume = cardDragClip != null ? cardDragSoundVolume : cardSelectSoundVolume;
        PlaySfxClip(clip, volume);
    }

public void PlayEndTurnSound()
    {
        AudioClip clip = endTurnClip != null ? endTurnClip : cardSelectClip;
        float volume = endTurnClip != null ? endTurnSoundVolume : cardSelectSoundVolume;
        PlaySfxClip(clip, volume);
    }



    private void PlaySfxClip(AudioClip clip, float volumeScale)
    {
        if (clip == null || _sfxAudioSource == null) return;

        _sfxAudioSource.volume = GameAudioSettings.SfxVolume;
        _sfxAudioSource.PlayOneShot(clip, Mathf.Clamp(volumeScale, 0f, 3f));
    }

    private void PlayNoDamageCardFeedbackIfNeeded(int enemyDamageDealt)
    {
        if (battleEnded || enemyDamageDealt > 0 || currentCardPlayerDamageTaken > 0)
        {
            return;
        }

        if (noDamageCardFeedbackRoutine != null)
        {
            StopCoroutine(noDamageCardFeedbackRoutine);
            RestoreNoDamageCardFeedback();
        }

        noDamageCardFeedbackRoutine = StartCoroutine(NoDamageCardFeedbackRoutine());
    }

    private IEnumerator NoDamageCardFeedbackRoutine()
    {
        Transform cameraTransform = GetNoDamageShakeCameraTransform();
        RectTransform targetRect = GetNoDamageShakeTarget();
        Vector3 cameraStart = cameraTransform != null ? cameraTransform.localPosition : Vector3.zero;
        Vector2 targetStart = targetRect != null ? targetRect.anchoredPosition : Vector2.zero;
        noDamageCameraOriginalPosition = cameraStart;
        noDamageTargetOriginalPosition = targetStart;

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = noDamageCardShakeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / noDamageCardShakeDuration);
            float shake = Mathf.Sin(t * Mathf.PI * 8f) * noDamageCardShakeDistance * (1f - t);

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = cameraStart + new Vector3(shake, 0f, 0f);
            }

            if (targetRect != null)
            {
                targetRect.anchoredPosition = targetStart + new Vector2(shake, 0f);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.localPosition = cameraStart;
        }

        if (targetRect != null)
        {
            targetRect.anchoredPosition = targetStart;
        }

        PlaySfxClip(noDamageCardClip != null ? noDamageCardClip : cardSelectClip, noDamageCardSoundVolume);
        noDamageCardFeedbackRoutine = null;
    }

    private Transform GetNoDamageShakeCameraTransform()
    {
        if (noDamageCardShakeCamera == null)
        {
            noDamageCardShakeCamera = Camera.main;
        }

        return noDamageCardShakeCamera != null ? noDamageCardShakeCamera.transform : null;
    }

    private RectTransform GetNoDamageShakeTarget()
    {
        if (noDamageCardShakeTarget != null)
        {
            return noDamageCardShakeTarget;
        }

        return battlePanel != null ? battlePanel.GetComponent<RectTransform>() : null;
    }

    private void RestoreNoDamageCardFeedback()
    {
        Transform cameraTransform = GetNoDamageShakeCameraTransform();
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = noDamageCameraOriginalPosition;
        }

        RectTransform targetRect = GetNoDamageShakeTarget();
        if (targetRect != null)
        {
            targetRect.anchoredPosition = noDamageTargetOriginalPosition;
        }
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
                float xShake = Mathf.Sin(elapsed * playerHitHorizontalShakeFrequency) * shake;
                playerHitShakeTarget.anchoredPosition = playerHitOriginalPosition + new Vector2(xShake, 0f);
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
            EnsureSmoothHandLayoutAnimator();
            return;
        }

        GameObject existingHandPanel = GameObject.Find("HandPanel");
        if (existingHandPanel != null)
        {
            handPanel = existingHandPanel.transform;
            EnsureSmoothHandLayoutAnimator();
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

        GameObject handPanelObject = new GameObject("HandPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(SmoothHandLayoutAnimator));
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

private void EnsureSmoothHandLayoutAnimator()
    {
        if (handPanel == null)
        {
            return;
        }

        if (handPanel.GetComponent<SmoothHandLayoutAnimator>() == null)
        {
            handPanel.gameObject.AddComponent<SmoothHandLayoutAnimator>();
        }
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

    public bool HasEnoughLuxForCard(CardData card)
    {
        return card != null && lux >= GetEffectiveLuxCost(card);
    }

    public bool CanAcceptCardClick(CardData card, out bool insufficientLux)
    {
        insufficientLux = false;

        if (card == null || !battleStarted || battleEnded || isCardResolving || playerStunned)
        {
            return false;
        }

        int effectiveCost = GetEffectiveLuxCost(card);
        if (lux < effectiveCost)
        {
            insufficientLux = true;
            return false;
        }

        return true;
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
        suppressCardBattleLog = true;

        CardType effectiveType = GetEffectiveCardType(card);
        gambleResolvedThisUse = false;
        gambleSucceededThisUse = false;

        if (card.reflectNextEnemyDamage)
        {
            reflectNextDamage = true;
        }

        int enemyHPBeforeCard = enemyHP;
        ApplySpecialCardEffect(card);
        if (battleEnded)
        {
            suppressCardBattleLog = false;
            return;
        }

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

        int actualEnemyDamage = enemyHPBeforeCard - enemyHP;
        WriteCardUseSummary(card, actualEnemyDamage);
        PlayNoDamageCardFeedbackIfNeeded(actualEnemyDamage);


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
        suppressCardBattleLog = false;
        if (card == null)
        {
            return;
        }

        string resultText = "사용!";
        if (IsCardTreatedAsGamble(card) && gambleResolvedThisUse)
        {
            resultText = gambleSucceededThisUse ? "성공!" : "실패!";
        }

        WriteLog($"<color=yellow>{card.cardName}</color> {resultText}");
    }

    private string FormatCardDescription(string description)
    {
        if (string.IsNullOrEmpty(description)) return description;

        string formatted = description;
        formatted = Regex.Replace(
            formatted,
            @"(Lux\s*cost\s*:\s*\d+)",
            "<color=#ff6ec7>$1</color>",
            RegexOptions.IgnoreCase);
        formatted = Regex.Replace(
            formatted,
            @"(Lux\s*[+-]\s*\d+)",
            "<color=#ff6ec7>$1</color>",
            RegexOptions.IgnoreCase);
        formatted = Regex.Replace(
            formatted,
            @"(피해\s*\d+)",
            "<color=#ff4444>$1</color>");
        formatted = Regex.Replace(
            formatted,
            @"(\d+\s*피해)",
            "<color=#ff4444>$1</color>");

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
        PlayNoDamageCardFeedbackIfNeeded(finalDamage);

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
                ReplaceHandWithGambleCards(card, 4);
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
                heartbeatAttackBonusActive = true;
                WriteLog("<color=#8fd3ff>심장 박동:</color> HP가 50% 이하일 때 공격력 +5.");
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
            case SpecialCardEffect.BeastHeart:
                return 0;
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

        if (heartbeatAttackBonusActive && playerHP <= Mathf.FloorToInt(playerMaxHP * 0.5f))
        {
            damage += 5;
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

        PlayEndTurnSound();

        List<string> turnLogs = new List<string>();

        if (enemyStunned)
        {
            turnLogs.Add("적 행동 불가!");
            enemyStunned = false;
        }
        else
        {
            int finalDamage = enemyDamage;

            if (enemyRaged)
            {
                finalDamage += rageBonusDamage;
            }

            if (reduceEnemyDamageNextTurn)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * 0.3f);
                reduceEnemyDamageNextTurn = false;
            }

            if (ignoreNextDamage)
            {
                turnLogs.Add("적의 공격 무효!");
                ignoreNextDamage = false;
            }
            else
            {
                finalDamage -= damageReduction;
                finalDamage = Mathf.Max(finalDamage, 0);

                if (shield > 0)
                {
                    int absorbed = Mathf.Min(shield, finalDamage);
                    shield -= absorbed;
                    finalDamage -= absorbed;
                }

                finalDamage = ModifyIncomingDamage(finalDamage);
                ApplyPlayerDamage(finalDamage);
                turnLogs.Add("적의 공격 발동!");

                if (reflectNextDamage && finalDamage > 0)
                {
                    ApplyEnemyDamage(finalDamage);
                    turnLogs.Add("반사 발동!");
                    reflectNextDamage = false;
                }
            }
        }

        if (playerStunned)
        {
            turnLogs.Add("제로 행동 불가!");
            playerStunned = false;
        }

        turn++;

        if (GetLuxState() == LuxState.Overflow)
        {
            lux -= 30;
            lux = Mathf.Clamp(lux, 0, 100);
            turnLogs.Add("폭주 반동 발동!");
        }

        if (luxDrainTurnsRemaining > 0)
        {
            lux += 3;
            lux = Mathf.Clamp(lux, 0, 100);
            luxDrainTurnsRemaining--;
            turnLogs.Add("럭스 드레인 발동!");
        }

        if (illegalLoanTurnsRemaining > 0)
        {
            lux += 5;
            lux = Mathf.Clamp(lux, 0, 100);
            illegalLoanTurnsRemaining--;
            turnLogs.Add("불법 대출 발동!");
            if (illegalLoanTurnsRemaining == 0 && illegalLoanPenaltyPending)
            {
                int incoming = ModifyIncomingDamage(10);
                ApplyPlayerDamage(incoming);
                illegalLoanPenaltyPending = false;
                turnLogs.Add("불법 대출 만기!");
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

        bool idapenFirstVictory = missionScoreId == BattleScoreStore.DefaultMissionId
            && BattleScoreStore.GetBestScore(BattleScoreStore.DefaultMissionId) <= 0
            && BattleScoreStore.GetCurrentScore(BattleScoreStore.DefaultMissionId) <= 0;
        bool karimFirstVictory = missionScoreId == BattleScoreStore.KarimHasanMissionId
            && BattleScoreStore.GetBestScore(BattleScoreStore.KarimHasanMissionId) <= 0
            && BattleScoreStore.GetCurrentScore(BattleScoreStore.KarimHasanMissionId) <= 0;
        int score = BattleScoreStore.CalculateScore(turn);
        BattleScoreStore.SaveScore(missionScoreId, score);
        PlayerPrefs.SetInt("BattleVictory", 1);
        PlayerPrefs.DeleteKey("BattleDefeat");
        PlayerPrefs.SetString("LastVictoryMission", missionScoreId);
        if (idapenFirstVictory)
        {
            PlayerPrefs.SetInt(BattleScoreStore.IdapenFirstVictoryJustWonKey, 1);
        }
        if (karimFirstVictory)
        {
            PlayerPrefs.SetInt(BattleScoreStore.KarimFirstVictoryJustWonKey, 1);
        }
        PlayerPrefs.Save();
        StartCoroutine(WinBattleSequence(score));
    }

private void LoseBattle()
    {
        if (battleEnded)
        {
            return;
        }

        battleEnded = true;

        int extraLoss = selectedBet;
        lux -= extraLoss;
        lux = Mathf.Clamp(lux, 0, 100);

        WriteLog($"Battle defeat. Loss -{extraLoss} LUX. Current LUX: {lux}");

        if (enemyDialogueText != null)
        {
            enemyDialogueText.text = GetSpeech(speechDefeat, "다신 이곳에 발도 들이지 마세요.");
        }

        PlayerPrefs.SetInt("BattleDefeat", 1);
        PlayerPrefs.DeleteKey("BattleVictory");
        PlayerPrefs.Save();
        UpdateUI();
        StartResultTransition("\uB2F9\uC2E0\uC740 \uD328\uBC30\uD588\uC5B4\uC694... \uC548\uD0C0\uAE5D\uB124\uC694.", gameOverSceneName, -1, true, 1.35f);
    }

    private IEnumerator WinBattleSequence(int score)
    {
        if (enemyDialogueText != null)
        {
            enemyDialogueText.text = GetSpeech(speechVictory, "크윽... 지켜주지 못해 미안해요..");
        }

        yield return new WaitForSecondsRealtime(0.55f);
        yield return FadeEnemyAndDialogueAway();
        StartResultTransition("\uB2F9\uC2E0\uC740 \uC2B9\uB9AC\uD588\uC5B4\uC694.", victorySceneName, score);
    }

    private IEnumerator FadeEnemyAndDialogueAway()
    {
        float duration = 1.15f;

        if (enemyHitEffectRoutine != null)
        {
            StopCoroutine(enemyHitEffectRoutine);
            enemyHitEffectRoutine = null;
            enemyHitEffectActive = false;
        }

        Coroutine particleRoutine = StartCoroutine(PlayEnemyDissolveParticles(duration));

        CanvasGroup dialogueGroup = GetEnemyDialogueCanvasGroup();
        Color enemyStartColor = enemyCharacterImage != null ? enemyCharacterImage.color : Color.white;
        float dialogueStartAlpha = dialogueGroup != null ? dialogueGroup.alpha : 1f;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (enemyCharacterImage != null)
            {
                Color color = enemyStartColor;
                color.a = Mathf.Lerp(enemyStartColor.a, 0f, t);
                enemyCharacterImage.color = color;
            }

            if (dialogueGroup != null)
            {
                dialogueGroup.alpha = Mathf.Lerp(dialogueStartAlpha, 0f, t);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        if (enemyCharacterImage != null)
        {
            enemyCharacterImage.gameObject.SetActive(false);
        }

        if (dialogueGroup != null)
        {
            dialogueGroup.alpha = 0f;
        }

        if (particleRoutine != null)
        {
            yield return particleRoutine;
        }
    }

    private CanvasGroup GetEnemyDialogueCanvasGroup()
    {
        if (enemyDialogueText == null)
        {
            return null;
        }

        Transform panel = enemyDialogueText.transform.parent;
        if (panel == null)
        {
            return null;
        }

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.gameObject.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private IEnumerator PlayEnemyDissolveParticles(float duration)
    {
        if (enemyCharacterImage == null)
        {
            yield break;
        }

        Canvas canvas = enemyCharacterImage.canvas;
        RectTransform canvasTransform = canvas != null ? canvas.transform as RectTransform : null;
        RectTransform enemyTransform = enemyCharacterImage.rectTransform;

        if (canvasTransform == null || enemyTransform == null)
        {
            yield break;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        enemyTransform.GetWorldCorners(corners);
        Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;
        Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(eventCamera, centerWorld);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, centerScreen, eventCamera, out Vector2 centerLocal))
        {
            yield break;
        }

        const int particleCount = 20;
        Image[] particles = new Image[particleCount];
        Vector2[] startPositions = new Vector2[particleCount];
        Vector2[] endPositions = new Vector2[particleCount];
        Color[] startColors = new Color[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particleObject = new GameObject("EnemyDissolveParticle", typeof(RectTransform), typeof(Image));
            particleObject.transform.SetParent(canvasTransform, false);
            particleObject.transform.SetAsLastSibling();

            RectTransform rectTransform = particleObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.one * Random.Range(5f, 13f);

            startPositions[i] = centerLocal + new Vector2(Random.Range(-150f, 150f), Random.Range(-180f, 180f));
            endPositions[i] = startPositions[i] + new Vector2(Random.Range(-95f, 95f), Random.Range(90f, 230f));
            rectTransform.anchoredPosition = startPositions[i];

            Image particleImage = particleObject.GetComponent<Image>();
            particleImage.color = Random.value > 0.45f
                ? new Color(1f, 0.82f, 0.92f, 0.78f)
                : new Color(0.65f, 0.78f, 1f, 0.7f);
            particleImage.raycastTarget = false;
            particles[i] = particleImage;
            startColors[i] = particleImage.color;
        }

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null)
                {
                    continue;
                }

                RectTransform rectTransform = particles[i].rectTransform;
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], endPositions[i], 1f - Mathf.Pow(1f - t, 2f));

                Color color = startColors[i];
                color.a = Mathf.Lerp(startColors[i].a, 0f, t);
                particles[i].color = color;
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
            {
                Destroy(particles[i].gameObject);
            }
        }
    }

    private void StartResultTransition(string message, string sceneName, int score = -1, bool fadeIn = false, float fadeDuration = -1f)
    {
        if (resultTransitionStarted)
        {
            return;
        }

        resultTransitionStarted = true;
        float duration = fadeDuration >= 0f ? fadeDuration : resultFadeDuration;
        StartCoroutine(ResultTransitionRoutine(message, sceneName, score, fadeIn, duration));
    }

private IEnumerator ResultTransitionRoutine(string message, string sceneName, int score, bool fadeIn, float fadeDuration)
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
        overlayImage.color = fadeIn ? Color.clear : Color.black;
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
        resultText.color = fadeIn ? Color.clear : Color.white;
        resultText.raycastTarget = false;
        resultText.textWrappingMode = TextWrappingModes.NoWrap;

        if (fadeIn)
        {
            yield return FadeResultOverlay(overlayImage, 0f, 1f, fadeDuration);
            resultText.color = Color.white;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, resultHoldDuration));

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        Destroy(overlayCanvasObject);
    }

    private IEnumerator FadeResultOverlay(Image image, float from, float to, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            image.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
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
        hintText.text = "카드를 드래그하여 공격하세요!";
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
        text.fontSize = Mathf.Max(anchor.fontSize * 1.55f, 42f);
        Color baseColor = delta > 0 ? new Color(1f, 0.78f, 0.94f, 1f) : CyberRed;
        text.color = baseColor;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180f, 76f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, anchor.rectTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, cam, out Vector2 localPos);
        localPos.x += anchor.rectTransform.rect.width * 0.5f + 8f;
        localPos += extraOffset;
        rt.anchoredPosition = localPos;
        rt.localScale = Vector3.one;

        const float fadeDuration = 3.2f;
        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            if (obj == null) yield break;
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / fadeDuration);
            float alpha = t < 0.42f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.42f) / 0.58f);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
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

private void ReplaceHandWithGambleCards(CardData usedCard, int targetCount)
{
    forcedGambleCards.Clear();

    foreach (CardData handCard in hand)
    {
        if (handCard != null && handCard != usedCard)
        {
            discardPile.Add(handCard);
        }
    }

    hand.Clear();

    for (int i = 0; i < targetCount; i++)
    {
        CardData gambleCard = TakeGambleCard(drawPile, usedCard);
        if (gambleCard == null)
        {
            ReshuffleDiscardIntoDeck();
            gambleCard = TakeGambleCard(drawPile, usedCard);
        }

        if (gambleCard == null)
        {
            gambleCard = GetFallbackGambleCard(usedCard);
        }

        if (gambleCard != null)
        {
            hand.Add(gambleCard);
        }
    }
}

private CardData TakeGambleCard(List<CardData> source, CardData usedCard)
{
    int index = source.FindIndex(card => IsBeastHeartGambleCard(card, usedCard));
    if (index < 0)
    {
        return null;
    }

    CardData card = source[index];
    source.RemoveAt(index);
    return card;
}

private CardData GetFallbackGambleCard(CardData usedCard)
{
    if (startingCards == null)
    {
        return null;
    }

    List<CardData> candidates = startingCards
        .Where(card => IsBeastHeartGambleCard(card, usedCard))
        .ToList();

    if (candidates.Count <= 0)
    {
        return null;
    }

    return candidates[Random.Range(0, candidates.Count)];
}

private bool IsBeastHeartGambleCard(CardData card, CardData usedCard)
{
    if (card == null || card == usedCard || card.isJackpot)
    {
        return false;
    }

    return card.isGambleCard || card.cardType == CardType.Gamble;
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
        WriteCardUseSummary(usedCard, 0);
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

    private void StartStatAnimation(
        ref Coroutine routine,
        TMP_Text text,
        Slider slider,
        int from,
        int to,
        int max,
        string label,
        bool sliderUsesNormalizedValue,
        Color flashColor,
        Vector2 floatingOffset)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        if (to != from)
        {
            SpawnFloatingDelta(text, to - from, floatingOffset);
        }

        routine = StartCoroutine(AnimateStatChangeRoutine(text, slider, from, to, max, label, sliderUsesNormalizedValue, flashColor));
    }

private IEnumerator AnimateStatChangeRoutine(
        TMP_Text text,
        Slider slider,
        int from,
        int to,
        int max,
        string label,
        bool sliderUsesNormalizedValue,
        Color flashColor)
    {
        const float duration = 2.4f;
        Color normalColor = text != null ? text.color : Color.white;
        if (slider != null)
        {
            EnsureSliderFillVisible(slider);
            if (sliderUsesNormalizedValue)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
            }
            else
            {
                slider.minValue = 0f;
                slider.maxValue = Mathf.Max(1, max);
            }

            ShowAnimatedSliderPreview(slider, from, to, from, max, sliderUsesNormalizedValue);
        }

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            int shown = Mathf.RoundToInt(Mathf.Lerp(from, to, eased));
            ApplyDisplayedStat(text, slider, shown, max, label, sliderUsesNormalizedValue);
            ShowAnimatedSliderPreview(slider, from, to, shown, max, sliderUsesNormalizedValue);

            if (text != null)
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                text.color = Color.Lerp(normalColor, flashColor, pulse);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        ApplyDisplayedStat(text, slider, to, max, label, sliderUsesNormalizedValue);
        HideAnimatedSliderPreview(slider);
        if (text != null)
        {
            text.color = normalColor;
        }
    }

    private void ApplyDisplayedStat(TMP_Text text, Slider slider, int value, int max, string label, bool sliderUsesNormalizedValue)
    {
        value = Mathf.Clamp(value, 0, Mathf.Max(1, max));

        if (text != null)
        {
            text.text = FormatStatText(label, value, max);
        }

        if (slider != null)
        {
            if (sliderUsesNormalizedValue)
            {
                slider.SetValueWithoutNotify(max <= 0 ? 0f : (float)value / max);
            }
            else
            {
                slider.SetValueWithoutNotify(value);
            }
        }
    }

    private string FormatStatText(string label, int value, int max)
    {
        if (label == "분노")
        {
            return value >= rageThreshold ? $"분노 {value}/{max} - 분노 상태" : $"분노 {value}/{max}";
        }

        return $"{label} {value}/{max}";
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
            if (dPlayerHP != 0)
            {
                StartStatAnimation(ref playerHPUiRoutine, playerHPText, playerHPBar, playerHP - dPlayerHP, playerHP, playerMaxHP, "HP", false, dPlayerHP < 0 ? CyberRed : CyberPink, Vector2.zero);
            }
            else
            {
                ApplyDisplayedStat(playerHPText, playerHPBar, playerHP, playerMaxHP, "HP", false);
            }
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
            if (dPlayerHP == 0 && playerHPUiRoutine == null)
            {
                playerHPBar.value = playerHP;
            }
        }


        if (luxText != null)
        {
            if (dLux != 0)
            {
                StartStatAnimation(ref luxUiRoutine, luxText, luxBar, lux - dLux, lux, 100, "LUX", false, dLux < 0 ? CyberRed : CyberPink, luxFloatingDeltaOffset);
            }
            else
            {
                ApplyDisplayedStat(luxText, luxBar, lux, 100, "LUX", false);
            }
        }

        if (luxBar != null)
        {
            EnsureSliderFillVisible(luxBar);
            luxBar.minValue = 0f;
            luxBar.maxValue = 100f;
            if (dLux == 0 && luxUiRoutine == null)
            {
                luxBar.SetValueWithoutNotify(lux);
            }
            UpdateLuxBarColor();
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
        }

        if (enemyHPText != null)
        {
            if (dEnemyHP != 0)
            {
                StartStatAnimation(ref enemyHPUiRoutine, enemyHPText, enemyHPBar, enemyHP - dEnemyHP, enemyHP, enemyMaxHP, "HP", true, dEnemyHP < 0 ? CyberRed : CyberPink, Vector2.zero);
            }
            else
            {
                ApplyDisplayedStat(enemyHPText, enemyHPBar, enemyHP, enemyMaxHP, "HP", true);
            }
        }

        if (enemyHPBar != null)
        {
            EnsureSliderFillVisible(enemyHPBar);
            if (dEnemyHP == 0 && enemyHPUiRoutine == null)
            {
                enemyHPBar.value = (float)enemyHP / enemyMaxHP;
            }
        }

        if (emotionText != null)
        {
            if (dEmotion != 0)
            {
                StartStatAnimation(ref emotionUiRoutine, emotionText, emotionBar, enemyEmotion - dEmotion, enemyEmotion, maxEmotion, "분노", true, dEmotion < 0 ? CyberRed : CyberPink, Vector2.zero);
            }
            else
            {
                ApplyDisplayedStat(emotionText, emotionBar, enemyEmotion, maxEmotion, "분노", true);
            }
        }

        if (emotionBar != null)
        {
            EnsureSliderFillVisible(emotionBar);
            if (dEmotion == 0 && emotionUiRoutine == null)
            {
                emotionBar.value = (float)enemyEmotion / maxEmotion;
            }
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

        fillImage.color = CyberPink;
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

    private string GetSpeech(string field, string fallback)
    {
        return !string.IsNullOrWhiteSpace(field) ? field : fallback;
    }

    private void UpdateEnemyDialogue()
    {
        if (enemyDialogueText == null) return;

        float hpRate = (float)enemyHP / enemyMaxHP;

        if (enemyHP <= 0)
        {
            enemyDialogueText.text = GetSpeech(speechDead, "...");
        }
        else if (hpRate <= 0.15f)
        {
            enemyDialogueText.text = GetSpeech(speech15, "하하.. 하하하... 악마같은 사람.");
        }
        else if (hpRate <= 0.30f)
        {
            enemyDialogueText.text = GetSpeech(speech30, "“...절 죽일 셈이군요.”");
        }
        else if (hpRate <= 0.45f)
        {
            enemyDialogueText.text = GetSpeech(speech45, "“안타깝게도 전 물러서지 않을 겁니다.”");
        }
        else if (hpRate <= 0.60f)
        {
            enemyDialogueText.text = GetSpeech(speech60, "“할머니가 무슨 죄를 지었다고 이러는 겁니까.”");
        }
        else if (hpRate <= 0.80f)
        {
            enemyDialogueText.text = GetSpeech(speech80, "“당신들 때문에 지하 구역 시민들은 죽어나가고 있습니다.”");
        }
        else
        {
            enemyDialogueText.text = GetSpeech(speechIdle, "“전 이미 각오했습니다.\n덤벼요.”");
        }
    }

private void WriteLog(string message)
    {
        if (suppressCardBattleLog)
        {
            return;
        }

        if (battleLogText != null)
        {
            battleLogText.text = message;
        }
    }
}
