using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Battle State")]
    public int turn = 1;
    private bool battleEnded = false;

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
    public GameObject endPanel;

    private void Start()
    {
        if (endPanel != null)
        {
            endPanel.SetActive(false);
        }

        if (negotiationButton != null)
        {
            negotiationButton.gameObject.SetActive(false);
        }

        UpdateUI();
        UpdateEnemyDialogue();
        WriteLog("전투 시작. 카림 하산이 당신을 막아섭니다.");
    }

    public void UseCard(CardData card)
    {
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
            battleEnded = true;
            WriteLog("표적 제압 완료.");

            if (endPanel != null)
            {
                endPanel.SetActive(true);
            }
        }

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

    public void EndTurn()
    {
        if (battleEnded) return;

        playerHP -= enemyDamage;
        playerHP = Mathf.Clamp(playerHP, 0, playerMaxHP);

        turn++;

        WriteLog($"적의 반격. 제로가 {enemyDamage} 피해를 받았습니다.");

        if (playerHP <= 0)
        {
            battleEnded = true;
            WriteLog("제로가 쓰러졌습니다.");

            if (endPanel != null)
            {
                endPanel.SetActive(true);
            }
        }

        UpdateUI();
        UpdateEnemyDialogue();
    }

    public void NegotiateEndBattle()
    {
        if (battleEnded) return;

        if (enemyEmotion < negotiationNeedEmotion)
        {
            WriteLog("아직 협상할 수 없습니다.");
            return;
        }

        battleEnded = true;

        WriteLog("협상으로 전투를 종료했습니다. 보상은 감소하고, THE HOUSE의 신뢰도가 하락합니다.");

        if (endPanel != null)
        {
            endPanel.SetActive(true);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        playerHPText.text = $"HP {playerHP}/{playerMaxHP}";

        luxText.text = $"LUX {lux}/100";
        luxBar.value = lux / 100f;

        enemyNameText.text = enemyName;
        enemyHPText.text = $"HP {enemyHP}/{enemyMaxHP}";
        enemyHPBar.value = (float)enemyHP / enemyMaxHP;

        emotionText.text = $"감정 {enemyEmotion}/{maxEmotion}";
        emotionBar.value = (float)enemyEmotion / maxEmotion;

        turnText.text = $"턴 {turn}";

        UpdateLuxState();
        UpdateNegotiationButton();
    }

    private void UpdateLuxState()
    {
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
        battleLogText.text = message;
    }
}