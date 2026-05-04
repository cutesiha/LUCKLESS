// LuxForcedExtraction.cs
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LuxForcedExtraction : MonoBehaviour
{
    public PlayerTopDown player;
    public NPC_Dialogue idaPen;         // 이다 펜 NPC 연결
    public NPC_Dialogue gabriel;        // 가브리엘 연결
    public GameObject extractionVFX;    // 연출용 (없으면 null)

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;

        StartCoroutine(ExtractionSequence());
    }

    IEnumerator ExtractionSequence()
    {
        player.isLocked = true;

        // 이다 펜 최후 대사
        yield return StartCoroutine(DialogueManager.Instance.PlayDialogue(
            "이다 펜",
            new string[] {
                "...이제 어떻게 되는 거죠?",
                "미라는... 미라는 어떻게 해요.",
                "...",
                "알겠어요. 어차피 선택이 없었으니까."
            }
        ));

        // 수거 연출
        if (extractionVFX != null)
        {
            extractionVFX.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            extractionVFX.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 시스템 메시지
        yield return StartCoroutine(DialogueManager.Instance.PlayDialogue(
            "[ THE HOUSE 시스템 ]",
            new string[] {
                "포르투나 코어 추출 완료. 잔여 LUX: 0.0003%",
                "대상: 이다 펜 / 17구역 / 채무 청산 완료",
                "다음 임무 브리핑을 수신하세요."
            }
        ));

        // 가브리엘 반응
        yield return StartCoroutine(DialogueManager.Instance.PlayDialogue(
            "가브리엘",
            new string[] {
                "...지금 뭔 짓을 한 거야.",
                "할머니가 이제 계단도 못 올라가. 장도 못 봐. 다 알면서.",
                "비켜."
            }
        ));

        // 선택지 UI (사후 처리)
        SlumUIManager.Instance.ShowChoicePanel(
            "이다 펜을 어떻게 처리할까?",
            new string[] { "완전 제거 보고", "방치하고 이동", "도주 허용" },
            OnChoiceMade
        );
    }

    void OnChoiceMade(int choiceIndex)
    {
        // GameManager에 선택 저장 → 이후 챕터에 영향
        GameManager.Instance.slum17Choice = choiceIndex;

        // 가브리엘과 튜토리얼 전투 시작
        StartCoroutine(StartTutorialBattle());
    }

    IEnumerator StartTutorialBattle()
    {
        player.isLocked = true;

        yield return StartCoroutine(DialogueManager.Instance.PlayDialogue(
            "가브리엘",
            new string[] {
                "비키라고 했어.",
                "...",
                "이렇게라도 해야 내가 살 것 같으니까."
            }
        ));

        yield return new WaitForSeconds(0.5f);

        // 페이드 아웃 후 전투 씬
        SlumUIManager.Instance.FadeOut(() =>
            SceneManager.LoadScene("BattleScene")
        );
    }
}